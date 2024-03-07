using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace Schrodinger;

public partial class SchrodingerContractState : ContractState
{
    // contract
    public SingletonState<bool> Initialized { get; set; }
    public SingletonState<Address> Admin { get; set; }
    
    // inscription
    // tick -> attribute infos
    public MappedState<string, AttributeInfos> TraitTypeMap { get; set; }
    // tick -> attribute type -> attribute infos
    public MappedState<string, string, AttributeInfos> TraitValueMap { get; set; }
    // tick -> recommend rule
    public MappedState<string, RecommendRule> RecommendRuleMap { get; set; }
    // tick -> inscription info
    public MappedState<string, InscriptionInfo> InscriptionInfoMap { get; set; }
    // adopt id -> adopt info
    public MappedState<Hash, AdoptInfo> AdoptInfoMap { get; set; }
    // symbol -> adopt id
    public MappedState<string, Hash> SymbolAdoptIdMap { get; set; }
    public SingletonState<long> SymbolCount { get; set; }  // start from 2
    
    // config
    public SingletonState<Config> Config { get; set; }
    
    // point contract
    public MappedState<Address, bool> JoinRecord { get; set; }
    public SingletonState<Hash> PointsContractDAppId { get; set; }

}