using AElf.Contracts.MultiToken;
using AElf.Standards.ACS0;

namespace BatchTransfer;

public partial class BatchTransferContractState
{
    internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    internal ACS0Container.ACS0ReferenceState GenesisContract { get; set; }


}