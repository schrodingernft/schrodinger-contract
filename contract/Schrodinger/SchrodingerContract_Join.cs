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

    private void SettlePointsContract(string actionName)
    {
        State.PointsContract.Settle.Send(new SettleInput
        {
            DappId = State.PointsContractDAppId.Value,
            ActionName = actionName,
            UserAddress = Context.Sender
        });
    }
}