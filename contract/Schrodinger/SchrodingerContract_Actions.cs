using AElf;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

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

    public override Empty JoinRecord(Empty input)
    {
        var joinRecord = State.JoinRecord[Context.Sender];
        Assert(!joinRecord, "you have joined");
        State.JoinRecord[Context.Sender] = true;
        return new Empty();
    }
}