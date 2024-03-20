using AElf;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace BatchTransfer;

public partial class BatchTransferContract : BatchTransferContractContainer.BatchTransferContractBase
{
    public override Empty Initialize(Address input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        State.TokenContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        var author = State.GenesisContract.GetContractAuthor.Call(Context.Self);
        Assert(Context.Sender == author, "No permission.");
        State.Manager.Value = input;
        State.Initialized.Value = true;
        return new Empty();
    }

    public override Empty SetManager(Address input)
    {
        Assert(State.Initialized.Value, "Not initialized.");
        Assert(input != null && !input.Value.IsNullOrEmpty(),"Invalid input.");
        var author = State.GenesisContract.GetContractAuthor.Call(Context.Self);
        Assert(Context.Sender == author, "No permission.");
        State.Manager.Value = input;
        return new Empty();
    }

    public override Empty BatchTransfer(BatchTransferInput input)
    {
        Assert(State.Initialized.Value, "Not initialized.");
        Assert(input.Value.Count > 0,"Invalid input.");
        Assert(Context.Sender == State.Manager.Value, "No permission.");
        foreach (var transferInfo in input.Value)
        {
            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = transferInfo.To,
                Symbol = transferInfo.Symbol,
                Amount = transferInfo.Amount
            });
        }
        return new Empty();
    }
}