using AElf.Standards.ACS0;

namespace Schrodinger;

public partial class SchrodingerContractState
{
    internal ACS0Container.ACS0ReferenceState GenesisContract { get; set; }
    internal Points.Contracts.Point.PointsContractContainer.PointsContractReferenceState PointsContract { get; set; }
}