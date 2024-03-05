using AElf;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Points.Contracts.Point;

namespace Schrodinger;

public partial class SchrodingerContract : SchrodingerContractContainer.SchrodingerContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        Assert(State.GenesisContract.GetContractAuthor.Call(Context.Self) == Context.Sender, "No permission.");
        Assert(input.Admin == null || !input.Admin.Value.IsNullOrEmpty(), "Invalid input admin.");
        if (input != null && input.PointsContract.Value != null)
        {
            State.PointsContract.Value = input.PointsContract;
        }
        
        if (input != null && input.PointsContractDappId != null)
        {
            State.PointsContractDAppId.Value = input.PointsContractDappId;
        }
        State.Admin.Value = input.Admin ?? Context.Sender;
        State.Initialized.Value = true;
        return new Empty();
    }

    public override Empty SetAdmin(Address input)
    {
        AssertInitialized();
        AssertAdmin();
        Assert(input != null && !input.Value.IsNullOrEmpty(), "Invalid input.");
        State.Admin.Value = input;
        
        return new Empty();
    }
    
    public override Empty SetPointsContractDAppId(Hash input)
    {
        AssertInitialized();
        AssertAdmin();
        Assert(input != null && !input.Value.IsNullOrEmpty(), "Invalid input.");
        Assert(State.PointsContractDAppId == null, "has Configured");
        State.PointsContractDAppId.Value = input;
        return new Empty();
    }
    
    public override Empty SetPointsContract(Address address)
    {
        AssertInitialized();
        AssertAdmin();
        Assert(address != null && !address.Value.IsNullOrEmpty(), "Invalid input.");
        Assert(State.PointsContract == null, "has Configured");
        State.PointsContract.Value = address;
        return new Empty();
    }

    public override Empty Join(JoinInput input)
    {
        var joinRecord = State.JoinRecord[Context.Sender];
        Assert(!joinRecord, "you have joined");
        State.JoinRecord[Context.Sender] = true;

        Context.Fire(new Joined()
        {
            Domain = input.Domain,
            Registrant = Context.Sender
        });
        
        var pointsContractDAppId = State.PointsContractDAppId;
        var pointsContractAddress = State.PointsContract;
        if(pointsContractDAppId == null || pointsContractAddress == null) return new Empty();
        
        State.PointsContract.Join.Send(new Points.Contracts.Point.JoinInput()
        {
            DappId = State.PointsContractDAppId.Value,
            Domain = input.Domain,
            Registrant = Context.Sender
        });
        
        return new Empty();
    }
}