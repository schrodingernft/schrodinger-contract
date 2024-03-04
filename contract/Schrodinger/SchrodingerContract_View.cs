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
    
    public override Hash GetDAppId(Empty input)
    {
        return State.DAppId.Value;
    }
    
    public override Address GetPointContract(Empty input)
    {
        return  State.PointsContract.Value;
    }
    
    
}