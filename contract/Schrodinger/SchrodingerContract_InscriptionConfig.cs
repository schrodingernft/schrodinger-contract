using System.Linq;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace Schrodinger;

public partial class SchrodingerContract
{
    public override Empty SetAttributes(SetAttributesInput input)
    {
        Assert(!string.IsNullOrWhiteSpace(input.Tick), "Invalid input.");
        var inscription = CheckInscriptionExistAndPermission(input.Tick);
        var attributeList = SetAttributeList(input.Tick, inscription.MaxGen, input.Attributes,
            inscription.AttributesPerGen, out var toRemoveFixed, out var toRemoveRandom);
        Context.Fire(new AttributesSet
        {
            Tick = input.Tick,
            RemovedFixedAttributes = new AttributeSets
            {
                Data = { toRemoveFixed }
            },
            RemovedRandomAttributes = new AttributeSets
            {
                Data = { toRemoveRandom }
            },
            AddedFixedAttributes = new AttributeSets
            {
                Data = { attributeList.FixedAttributes }
            },
            AddedRandomAttributes = new AttributeSets
            {
                Data = { attributeList.RandomAttributes }
            }
        });
        return new Empty();
    }

    public override Empty SetImageCount(SetImageCountInput input)
    {
        Assert(!string.IsNullOrWhiteSpace(input.Tick), "Invalid input.");
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
        Assert(!string.IsNullOrWhiteSpace(input.Tick), "Invalid input.");
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
        Assert(!string.IsNullOrWhiteSpace(input.Tick), "Invalid input tick.");
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
        Assert(!string.IsNullOrWhiteSpace(input.Tick), "Invalid input tick.");
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
        Assert(!string.IsNullOrWhiteSpace(input.Tick), "Invalid input tick.");
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
        Assert(!string.IsNullOrEmpty(input.Tick), "Invalid input.");
        var inscription = CheckInscriptionExistAndPermission(input.Tick);
        var crossGenerationConfig = input.Config;
        CheckCrossGenerationConfig(crossGenerationConfig, inscription.MaxGen);
        inscription.CrossGenerationConfig = crossGenerationConfig;
        Context.Fire(new CrossGenerationConfigSet
        {
            Tick = input.Tick,
            CrossGenerationConfig = crossGenerationConfig
        });
        return new Empty();
    }

    public override Empty SetAttributesPerGen(SetAttributesPerGenInput input)
    {
        Assert(!string.IsNullOrEmpty(input.Tick), "Invalid input.");
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