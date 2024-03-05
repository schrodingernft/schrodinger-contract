using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Schrodinger;

public partial class SchrodingerContract
{
    public override Address GetAdmin(Empty input) => State.Admin.Value;

    public override BoolValue GetJoinRecord(Address address)
    {
        return new BoolValue{ Value = State.JoinRecord[address] };
    }
    
    public override Hash GetPointsContractDAppId(Empty input)
    {
        return State.PointsContractDAppId.Value;
    }
    
    public override Address GetPointsContract(Empty input)
    {
        return  State.PointsContract.Value;
    }
    
    
}