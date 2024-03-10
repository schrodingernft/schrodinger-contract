using System.Linq;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Schrodinger;

public partial class SchrodingerContract
{
    public override Address GetAdmin(Empty input)
    {
        return State.Admin.Value;
    }

    // public override BoolValue GetJoinRecord(Address address)
    // {
    //     return new BoolValue{ Value = State.JoinRecord[address] };
    // }

    public override Hash GetPointsContractDAppId(Empty input)
    {
        return State.PointsContractDAppId.Value;
    }

    public override Address GetPointsContract(Empty input)
    {
        return State.PointsContract.Value;
    }

    #region inscription

    public override InscriptionInfo GetInscriptionInfo(StringValue input)
    {
        var result = new InscriptionInfo();
        if (input != null && !string.IsNullOrEmpty(input.Value))
        {
            result = State.InscriptionInfoMap[input.Value] ?? new InscriptionInfo();
        }

        return result;
    }

    public override StringValue GetTick(StringValue input)
    {
        return new StringValue
        {
            Value = input.Value.Split(SchrodingerContractConstants.Separator).First()
        };
    }

    public override StringValue GetParent(StringValue input)
    {
        return new StringValue();
    }

    public override AttributeLists GetAttributes(StringValue input)
    {
        var result = new AttributeLists();
        var tick = input.Value;
        if (string.IsNullOrEmpty(tick))
        {
            return result;
        }

        var fixedTraitTypeMap = State.FixedTraitTypeMap[tick] ?? new AttributeInfos();
        result.FixedAttributes.Add(GetAttributes(tick, fixedTraitTypeMap));
        var randomTraitTypeMap = State.RandomTraitTypeMap[tick] ?? new AttributeInfos();
        result.RandomAttributes.Add(GetAttributes(tick, randomTraitTypeMap));
        return result;
    }

    public override AttributeInfos GetAttributeTypes(StringValue input)
    {
        var result = new AttributeInfos();
        var tick = input.Value;
        if (string.IsNullOrEmpty(tick))
        {
            return result;
        }

        var fixedTraitTypeMap = State.FixedTraitTypeMap[tick] ?? new AttributeInfos();
        var randomTraitTypeMap = State.RandomTraitTypeMap[tick] ?? new AttributeInfos();
        result.Data.AddRange(fixedTraitTypeMap.Data);
        result.Data.AddRange(randomTraitTypeMap.Data);
        return result;
    }

    public override AttributeInfos GetAttributeValues(GetAttributeValuesInput input)
    {
        var result = new AttributeInfos();
        var tick = input.Tick;
        var traitType = input.TraitType;
        if (string.IsNullOrEmpty(tick) && string.IsNullOrEmpty(traitType))
        {
            return result;
        }

        result = State.TraitValueMap[tick][traitType] ?? new AttributeInfos();
        return result;
    }

    public override AdoptInfo GetAdoptInfo(Hash input)
    {
        return State.AdoptInfoMap[input];
    }

    public override GetTokenInfoOutput GetTokenInfo(StringValue input)
    {
        return new GetTokenInfoOutput();
    }

    #endregion
}