using System;
using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Points.Contracts.Point;

namespace Schrodinger;

public partial class SchrodingerContract
{
    public override Empty Join(JoinInput input)
    {
        Assert(input != null && IsStringValid(input.Domain), "Invalid input.");
        Assert(!State.JoinRecord[Context.Sender], "Already joined.");

        JoinPointsContract(input.Domain);

        return new Empty();
    }

    public override BoolValue GetJoinRecord(Address address)
    {
        return new BoolValue { Value = State.JoinRecord[address] };
    }

    private void JoinPointsContract(string domain)
    {
        if (!IsHashValid(State.PointsContractDAppId.Value) || State.PointsContract.Value == null)
        {
            return;
        }

        if (State.JoinRecord[Context.Sender]) return;

        State.JoinRecord[Context.Sender] = true;

        State.PointsContract.Join.Send(new Points.Contracts.Point.JoinInput
        {
            DappId = State.PointsContractDAppId.Value,
            Domain = domain,
            Registrant = Context.Sender
        });

        Context.Fire(new Joined
        {
            Domain = domain,
            Registrant = Context.Sender
        });
    }

    private void SettlePoints(string actionName, long amount, int inscriptionDecimal)
    {
        var proportion = State.PointsProportion[actionName];
        Assert(proportion > 0, "Invalid action name.");
        var points = new BigIntValue(amount).Mul(new BigIntValue(proportion));
        var userPointsValue = new BigIntValue(points).Div(new BigIntValue(10).Pow(inscriptionDecimal));

        Assert(long.TryParse(userPointsValue.Value,out var userPoints),"Invalid points.");
        State.PointsContract.Settle.Send(new SettleInput
        {
            DappId = State.PointsContractDAppId.Value,
            ActionName = actionName,
            UserAddress = Context.Sender,
            UserPoints = userPoints
        });
    }

    public override Empty BatchSettle(BatchSettleInput input)
    {
        var userPointsList = input.UserPointsList.Select(userPoint => new Points.Contracts.Point.UserPoints
        {
            UserAddress = userPoint.UserAddress, UserPoints_ = userPoint.UserPoints_,
        }).ToList();
        State.PointsContract.BatchSettle.Send(new Points.Contracts.Point.BatchSettleInput
        {
            ActionName = input.ActionName,
            DappId = State.PointsContractDAppId.Value,
            UserPointsList = { userPointsList }
        });
        return new Empty();
    }

    public override Empty SetPointsProportionList(SetPointsProportionListInput input)
    {
        CheckAdminPermission();
        Assert(input.Data.Count > 0 && input.Data.Count <= SchrodingerContractConstants.DefaultMaxProportionListCount,
            "Invalid input list count.");
        foreach (var pointsProportion in input.Data)
        {
            Assert(pointsProportion != null, "Invalid input.");
            var actionName = pointsProportion.ActionName;
            var proportion = pointsProportion.Proportion;
            Assert(IsStringValid(actionName) && proportion > 0, "Invalid action name and proportion.");
            State.PointsProportion[actionName] = proportion;
        }

        return new Empty();
    }

    public override Empty SetPointsSettleAdmin(Address input)
    {
        Assert(IsAddressValid(input), "Invalid input points settle admin.");
        CheckAdminPermission();
        State.PointsSettleAdmin.Value = input;
        return new Empty();
    }

    public override Int64Value GetPointsProportion(StringValue input)
    {
        return new Int64Value
        {
            Value = State.PointsProportion[input.Value]
        };
    }

    public override Address GetPointsSettleAdmin(Empty input)
    {
        return State.PointsSettleAdmin.Value;
    }
}