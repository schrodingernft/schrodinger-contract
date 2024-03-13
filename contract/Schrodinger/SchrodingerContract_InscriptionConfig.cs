using System.Collections.Generic;
using System.Linq;
using AElf.Sdk.CSharp;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace Schrodinger;

public partial class SchrodingerContract
{
    public override Empty SetFixedAttribute(SetAttributeInput input)
    {
        CheckParamsAndGetInscription(input);
        var inputTraitType = input.AttributeSet.TraitType;
        var traitTypeName = inputTraitType.Name;
        var inputTraitValues = input.AttributeSet.Values;

        var traitTypes = State.FixedTraitTypeMap[input.Tick] ?? new AttributeInfos();
        var traitValues = State.TraitValueMap[input.Tick][traitTypeName];
        traitTypes = UpdateAttributeSet(input.Tick, traitTypes, traitValues, inputTraitType, inputTraitValues,
            out var toRemove);
        var fixCount = CheckAndGetFixedAttributesCount<AttributeInfo>(traitTypes.Data.ToList());
        CheckTraitTypeCount(fixCount, State.RandomTraitTypeMap[input.Tick]?.Data.Count ?? 0);
        FireFixedAttributeSetLogEvent(toRemove,input.AttributeSet);
        return new Empty();
    }

    public override Empty SetRandomAttribute(SetAttributeInput input)
    {
        var inscription = CheckParamsAndGetInscription(input);
        var inputTraitType = input.AttributeSet.TraitType;
        var traitTypeName = inputTraitType.Name;
        var inputTraitValues = input.AttributeSet.Values;

        var traitTypes = State.RandomTraitTypeMap[input.Tick] ?? new AttributeInfos();
        var traitValues = State.TraitValueMap[input.Tick][traitTypeName];
        traitTypes = UpdateAttributeSet(input.Tick, traitTypes, traitValues, inputTraitType,
            inputTraitValues, out var toRemove, true);
        var list = traitTypes.Data.ToList();
        var randomCount = CheckAndGetRandomAttributesCount<AttributeInfo>(list);
        CheckRandomAttributeList(list, inscription.MaxGen, inscription.AttributesPerGen);
        CheckTraitTypeCount(State.FixedTraitTypeMap[input.Tick]?.Data.Count ?? 0, randomCount);
        FireRandomAttributeSetLogEvent(toRemove,input.AttributeSet);
        return new Empty();
    }

    private void FireRandomAttributeSetLogEvent(AttributeInfo toRemove,AttributeSet attributeSet)
    {
        var logEvent = new RandomAttributeSet();
        if (toRemove != null) 
        {
            logEvent.RemovedAttribute = new AttributeSet
            {
                TraitType = toRemove
            };
        }
        else
        {
            logEvent.AddedAttribute = attributeSet;
        }
        Context.Fire(logEvent);
    }
    
    private void FireFixedAttributeSetLogEvent(AttributeInfo toRemove,AttributeSet attributeSet)
    {
        var logEvent = new FixedAttributeSet();
        if (toRemove != null)
        {
            logEvent.RemovedAttribute = new AttributeSet
            {
                TraitType = toRemove
            };
        }
        else
        {
            logEvent.AddedAttribute = attributeSet;
        }
        Context.Fire(logEvent);
    }

    public override Empty SetImageCount(SetImageCountInput input)
    {
        Assert(IsStringValid(input.Tick), "Invalid input.");
        var inscription = CheckInscriptionExistAndPermission(input.Tick);
        CheckImageCount(input.ImageCount);
        inscription.ImageCount = input.ImageCount;
        State.InscriptionInfoMap[input.Tick] = inscription;
        Context.Fire(new ImageCountSet
        {
            Tick = input.Tick,
            ImageCount = inscription.ImageCount
        });
        return new Empty();
    }

    public override Empty SetMaxGeneration(SetMaxGenerationInput input)
    {
        Assert(IsStringValid(input.Tick), "Invalid input.");
        var inscription = CheckInscriptionExistAndPermission(input.Tick);
        CheckGeneration(input.Gen);
        var oldGen = inscription.MaxGen;
        inscription.MaxGen = input.Gen;
        State.InscriptionInfoMap[input.Tick] = inscription;
        Context.Fire(new MaxGenerationSet
        {
            Tick = input.Tick,
            OldGen = oldGen,
            NewGen = inscription.MaxGen
        });
        return new Empty();
    }

    public override Empty SetRates(SetRatesInput input)
    {
        Assert(IsStringValid(input.Tick), "Invalid input tick.");
        CheckRate(input.LossRate, input.CommissionRate);
        var inscription = CheckInscriptionExistAndPermission(input.Tick);
        var oldCommissionRate = inscription.CommissionRate;
        var oldLossRate = inscription.LossRate;
        inscription.CommissionRate = input.CommissionRate;
        inscription.LossRate = input.LossRate;
        State.InscriptionInfoMap[input.Tick] = inscription;
        Context.Fire(new RatesSet
        {
            Tick = input.Tick,
            OldCommissionRate = oldCommissionRate,
            OldLossRate = oldLossRate,
            NewCommissionRate = inscription.CommissionRate,
            NewLossRate = inscription.LossRate
        });
        return new Empty();
    }

    public override Empty SetRecipient(SetRecipientInput input)
    {
        Assert(IsStringValid(input.Tick), "Invalid input tick.");
        Assert(IsAddressValid(input.Recipient), "Invalid recipient address.");
        var inscription = CheckInscriptionExistAndPermission(input.Tick);
        var oldRecipient = inscription.Recipient;
        inscription.Recipient = input.Recipient;
        State.InscriptionInfoMap[input.Tick] = inscription;
        Context.Fire(new RecipientSet
        {
            Tick = input.Tick,
            OldRecipient = oldRecipient,
            NewRecipient = inscription.Recipient
        });
        return new Empty();
    }

    public override Empty SetInscriptionAdmin(SetInscriptionAdminInput input)
    {
        Assert(IsStringValid(input.Tick), "Invalid input tick.");
        Assert(IsAddressValid(input.Admin), "Invalid admin address.");
        var inscription = CheckInscriptionExistAndPermission(input.Tick);
        var oldAdmin = inscription.Admin;
        inscription.Admin = input.Admin;
        State.InscriptionInfoMap[input.Tick] = inscription;
        Context.Fire(new InscriptionAdminSet
        {
            Tick = input.Tick,
            OldAdmin = oldAdmin,
            NewAdmin = inscription.Admin
        });
        return new Empty();
    }

    public override Empty SetCrossGenerationConfig(SetCrossGenerationConfigInput input)
    {
        Assert(IsStringValid(input.Tick), "Invalid input.");
        var inscription = CheckInscriptionExistAndPermission(input.Tick);
        var crossGenerationConfig = input.Config;
        CheckAndSetCrossGenerationConfig(input.Tick, crossGenerationConfig, inscription.MaxGen);
        inscription.CrossGenerationConfig = crossGenerationConfig;
        State.InscriptionInfoMap[input.Tick] = inscription;
        Context.Fire(new CrossGenerationConfigSet
        {
            Tick = input.Tick,
            CrossGenerationConfig = crossGenerationConfig
        });
        return new Empty();
    }

    public override Empty SetAttributesPerGen(SetAttributesPerGenInput input)
    {
        Assert(IsStringValid(input.Tick), "Invalid input.");
        var inscription = CheckInscriptionExistAndPermission(input.Tick);
        CheckAttributePerGen(input.AttributesPerGen, inscription.MaxGen);
        var oldAttributesPerGen = inscription.AttributesPerGen;
        inscription.AttributesPerGen = input.AttributesPerGen;
        State.InscriptionInfoMap[input.Tick] = inscription;
        Context.Fire(new AttributesPerGenerationSet
        {
            Tick = input.Tick,
            OldAttributesPerGen = oldAttributesPerGen,
            NewAttributesPerGen = inscription.AttributesPerGen
        });
        return new Empty();
    }
}