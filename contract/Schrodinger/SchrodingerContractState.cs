using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace Schrodinger;

public partial class SchrodingerContractState : ContractState
{
    public SingletonState<bool> Initialized { get; set; }
    public SingletonState<Address> Admin { get; set; }
    public MappedState<Address, bool> JoinRecord { get; set; }
    
    public SingletonState<Hash> PointsContractDAppId { get; set; }

}