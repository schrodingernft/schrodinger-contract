using AElf;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Schrodinger;

public partial class SchrodingerContract : SchrodingerContractContainer.SchrodingerContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        Assert(input != null, "Invalid input.");

        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        Assert(State.GenesisContract.GetContractAuthor.Call(Context.Self) == Context.Sender, "No permission.");

        Assert(input.Admin == null || !input.Admin.Value.IsNullOrEmpty(), "Invalid input admin.");

        Assert(input.PointsContract == null || IsAddressValid(input.PointsContract), "Invalid input points contract.");
        State.PointsContract.Value = input.PointsContract;

        Assert(input.PointsContractDappId == null || IsHashValid(input.PointsContractDappId),
            "Invalid input points contract dapp id");
        State.PointsContractDAppId.Value = input.PointsContractDappId;

        State.Admin.Value = input.Admin ?? Context.Sender;
        State.Initialized.Value = true;
        return new Empty();
    }

    public override Empty SetAdmin(Address input)
    {
        CheckAdminPermission();
        Assert(IsAddressValid(input), "Invalid input.");

        State.Admin.Value = input;

        return new Empty();
    }

    public override Empty SetPointsContractDAppId(Hash input)
    {
        CheckAdminPermission();
        Assert(!IsHashValid(State.PointsContractDAppId.Value), "Already set.");

        Assert(IsHashValid(input), "Invalid input.");

        State.PointsContractDAppId.Value = input;

        return new Empty();
    }

    public override Empty SetPointsContract(Address input)
    {
        CheckAdminPermission();
        Assert(!IsAddressValid(State.PointsContract.Value), "Already set.");

        Assert(IsAddressValid(input), "Invalid input.");

        State.PointsContract.Value = input;

        return new Empty();
    }

    // public override Empty Join(JoinInput input)
    // {
    //     var joinRecord = State.JoinRecord[Context.Sender];
    //     Assert(!joinRecord, "you have joined");
    //     State.JoinRecord[Context.Sender] = true;
    //
    //     Context.Fire(new Joined()
    //     {
    //         Domain = input.Domain,
    //         Registrant = Context.Sender
    //     });
    //     
    //     var pointsContractDAppId = State.PointsContractDAppId;
    //     var pointsContractAddress = State.PointsContract;
    //     if(pointsContractDAppId == null || pointsContractAddress == null) return new Empty();
    //     
    //     State.PointsContract.Join.Send(new Points.Contracts.Point.JoinInput()
    //     {
    //         DappId = State.PointsContractDAppId.Value,
    //         Domain = input.Domain,
    //         Registrant = Context.Sender
    //     });
    //     
    //     return new Empty();
    // }
}