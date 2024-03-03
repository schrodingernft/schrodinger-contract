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

    public override Empty Join(JoinInput input)
    {
        var joinRecord = State.JoinRecord[Context.Sender];
        Assert(!joinRecord, "you have joined");
        State.JoinRecord[Context.Sender] = true;
        
        //check domain
        var domainOperatorRelationship = State.PointsContract.GetDomainApplyInfo.Call(new StringValue()
        {
            Value = input.Domain
        });
        Assert(domainOperatorRelationship != null, "invalid domain");
        
        State.PointsContract.Join.Send(new Points.Contracts.Point.JoinInput()
        {
            DappId = new Hash(),
            Domain = "",
            Registrant = Context.Sender
        });
        Context.Fire(new Joined()
        {
            Domain = input.Domain,
            Registrant = Context.Sender
        });
        return new Empty();
    }
}