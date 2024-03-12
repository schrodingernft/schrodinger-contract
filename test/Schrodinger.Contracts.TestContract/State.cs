using AElf.Sdk.CSharp.State;
using Schrodinger.Contracts.Contract;

namespace Schrodinger.Contracts.TestContract;

public class TestContractState : ContractState
{
    // tick -> fix attribute infos
    public MappedState<string, AttributeInfos> FixedTraitTypeMap { get; set; }

    // tick -> random attribute infos(greater than 1)
    public MappedState<string, AttributeInfos> RandomTraitTypeMap { get; set; }

    // tick -> attribute type -> attribute infos
    public MappedState<string, string, AttributeInfos> TraitValueMap { get; set; }
}