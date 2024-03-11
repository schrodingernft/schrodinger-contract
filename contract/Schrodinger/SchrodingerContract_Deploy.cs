using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace Schrodinger;

public partial class SchrodingerContract
{
    public override Empty Deploy(DeployInput input)
    {
        CheckDeployParams(input);
        var tick = input.Tick;
        Assert(State.InscriptionInfoMap[tick] == null, "Already exist.");
        var ancestor = GetInscriptionSymbol(tick);
        var inscription = new InscriptionInfo
        {
            Ancestor = ancestor,
            Decimals = input.Decimals,
            MaxGen = input.MaxGeneration,
            LossRate = input.LossRate,
            CommissionRate = input.CommissionRate,
            Recipient = input.Recipient ?? Context.Sender,
            Admin = Context.Sender,
            CrossGenerationConfig = input.CrossGenerationConfig,
            IsWeightEnabled = input.IsWeightEnabled,
            ImageCount = input.ImageCount,
            AttributesPerGen = input.AttributesPerGen
        };
        State.InscriptionInfoMap[tick] = inscription;
        var attributeList = SetAttributeList(tick, inscription.MaxGen, input.AttributeLists,
            inscription.AttributesPerGen, out _, out _);
        // Generate external info
        var externalInfo = GenerateExternalInfo(tick, input.Image, input.TotalSupply);
        CreateInscription(tick, inscription.Decimals, input.TotalSupply, externalInfo, input.Issuer);
        Assert(int.TryParse(SchrodingerContractConstants.AncestorSymbolSuffix, out var symbolCount),
            "Invalid symbol count.");
        State.SymbolCountMap[tick] = ++symbolCount;

        JoinPointsContract(input.Domain);

        Context.Fire(new Deployed
        {
            Tick = tick,
            Ancestor = ancestor,
            MaxGeneration = inscription.MaxGen,
            TotalSupply = input.TotalSupply,
            Decimals = inscription.Decimals,
            AttributeLists = attributeList,
            ImageCount = input.ImageCount,
            Issuer = input.Issuer ?? Context.Sender,
            Owner = Context.Self,
            IssueChainId = Context.ChainId,
            Deployer = Context.Sender,
            TokenName = GetInscriptionName(tick),
            ExternalInfos = new ExternalInfos
            {
                Value = { externalInfo.Value }
            },
            CrossGenerationConfig = input.CrossGenerationConfig,
            IsWeightEnabled = input.IsWeightEnabled,
            Admin = inscription.Admin,
            LossRate = inscription.LossRate,
            CommissionRate = inscription.CommissionRate,
            AttributesPerGen = input.AttributesPerGen
        });
        return new Empty();
    }
}