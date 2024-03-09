using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace Schrodinger;

public partial class SchrodingerContract
{
    public override Empty DeployCollection(DeployCollectionInput input)
    {
        CheckInitialized();
        Assert(!string.IsNullOrEmpty(input.Tick),"Invalid input.");
        CheckImageSize(input.Image);
        var externalInfo = GenerateExternalInfo(input.Tick, input.Image, 0);
        CreateInscription(input.Tick, 0, 0, externalInfo, Context.Self, Context.Self);
        Context.Fire(new CollectionDeployed
        {
            Symbol = GetInscriptionCollectionSymbol(input.Tick),
            TotalSupply = 0,
            CollectionExternalInfos = new ExternalInfos
            {
                Value = { externalInfo.Value }
            },
            Deployer = Context.Sender,
            IssueChainId = Context.ChainId,
            Issuer = Context.Self,
            Owner = Context.Self
        });
        return new Empty();
    }
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
        CreateInscription(tick, inscription.Decimals, input.TotalSupply, externalInfo, input.Issuer, input.Owner);
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
            Owner = input.Owner ?? Context.Sender,
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