using AElf.Contracts.MultiToken;
using AElf.Standards.ACS0;
using AetherLink.Contracts.Oracle;
using Points.Contracts.Point;

namespace Schrodinger;

public partial class SchrodingerContractState
{
    internal ACS0Container.ACS0ReferenceState GenesisContract { get; set; }
    internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    internal PointsContractContainer.PointsContractReferenceState PointsContract { get; set; }
    internal OracleContractContainer.OracleContractReferenceState OracleContract { get; set; }
}