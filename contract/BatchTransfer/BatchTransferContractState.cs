using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace BatchTransfer;

public partial class BatchTransferContractState : ContractState
{
    public SingletonState<bool> Initialized { get; set; }
    public SingletonState<Address> Manager { get; set; }
}