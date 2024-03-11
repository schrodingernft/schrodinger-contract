using System.Collections.Generic;
using System.Text;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using SchrodingerMain;

namespace Schrodinger.Main;

public partial class SchrodingerMainContract : SchrodingerMainContractContainer.SchrodingerMainContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        Assert(input != null, "Invalid input.");

        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        Assert(State.GenesisContract.GetContractInfo.Call(Context.Self).Deployer == Context.Sender, "No permission.");

        Assert(input.Admin == null || !input.Admin.Value.IsNullOrEmpty(), "Invalid input admin.");
        State.Admin.Value = input.Admin ?? Context.Sender;

        Assert(input.ImageMaxSize > 0, "Invalid input image max size.");
        State.ImageMaxSize.Value = input.ImageMaxSize;

        State.TokenContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

        State.Initialized.Value = true;
        return new Empty();
    }

    public override Empty Deploy(DeployInput input)
    {
        CheckInitialized();
        Assert(!string.IsNullOrEmpty(input.Tick) && !string.IsNullOrEmpty(input.TokenName), "Invalid input.");
        Assert(input.Decimals >= 0, "Invalid input decimals.");

        CheckImageSize(input.Image);

        State.TokenContract.TransferFrom.Send(new TransferFromInput
        {
            Symbol = input.SeedSymbol,
            From = Context.Sender,
            To = Context.Self,
            Amount = 1,
        });
        var externalInfo = GenerateExternalInfo(input.Tick, input.Image);
        CreateInscriptionCollection(input.Tick, externalInfo, input.TokenName, input.Decimals);
        Context.Fire(new Deployed
        {
            Symbol = GetInscriptionCollectionSymbol(input.Tick),
            TotalSupply = SchrodingerMainContractConstants.DefaultCollectionTotalSupply,
            CollectionExternalInfos = new ExternalInfos
            {
                Value = { externalInfo.Value }
            },
            Deployer = Context.Sender,
            IssueChainId = Context.ChainId,
            Issuer = Context.Sender,
            Owner = Context.Self,
            TokenName = input.TokenName,
            Decimals = input.Decimals
        });
        return new Empty();
    }

    private void CreateInscriptionCollection(string tick, ExternalInfo externalInfo, string tokenName, int decimals)
    {
        var createTokenInput = new CreateInput
        {
            Symbol = GetInscriptionCollectionSymbol(tick),
            TokenName = tokenName,
            TotalSupply = SchrodingerMainContractConstants.DefaultCollectionTotalSupply,
            Decimals = decimals,
            Issuer = Context.Sender,
            IsBurnable = true,
            IssueChainId = Context.ChainId,
            ExternalInfo = externalInfo,
            Owner = Context.Self
        };
        State.TokenContract.Create.Send(createTokenInput);
    }

    private void CheckImageSize(string image)
    {
        var config = State.ImageMaxSize;
        var maxImageSize = config?.Value ?? SchrodingerMainContractConstants.DefaultImageMaxSize;
        Assert(IsStringValid(image) && Encoding.UTF8.GetByteCount(image) <= maxImageSize,
            "Invalid image data.");
    }

    private void CheckInitialized()
    {
        Assert(State.Initialized.Value, "Not initialized.");
    }

    private string GetInscriptionCollectionSymbol(string tick)
    {
        return
            $"{tick}{SchrodingerMainContractConstants.Separator}{SchrodingerMainContractConstants.CollectionSymbolSuffix}";
    }

    private ExternalInfo GenerateExternalInfo(string tick, string image)
    {
        var externalInfo = new ExternalInfo();
        var dic = new Dictionary<string, string>
        {
            [SchrodingerMainContractConstants.InscriptionImageKey] = image
        };

        var info = new DeployInscriptionInfo
        {
            P = SchrodingerMainContractConstants.InscriptionType,
            Op = SchrodingerMainContractConstants.DeployOp,
            Tick = tick,
            Max = SchrodingerMainContractConstants.DefaultCollectionTotalSupply.ToString(),
            Lim = SchrodingerMainContractConstants.Lim
        };
        dic[SchrodingerMainContractConstants.InscriptionDeployKey] = info.ToString();

        externalInfo.Value.Add(dic);
        return externalInfo;
    }

    public override Empty SetAdmin(Address input)
    {
        Assert(Context.Sender == State.Admin.Value, "No permission.");

        Assert(IsAddressValid(input), "Invalid input.");

        if (State.Admin.Value == input) return new Empty();

        State.Admin.Value = input;

        return new Empty();
    }

    public override Address GetAdmin(Empty input)
    {
        return State.Admin.Value;
    }

    public override Empty SetImageMaxSize(Int64Value input)
    {
        Assert(Context.Sender == State.Admin.Value, "No permission.");
        Assert(input != null && input.Value > 0, "Invalid input.");

        State.ImageMaxSize.Value = input.Value;

        return new Empty();
    }

    public override Int64Value GetImageMaxSize(Empty input)
    {
        return new Int64Value { Value = State.ImageMaxSize.Value };
    }

    private bool IsAddressValid(Address input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }

    private bool IsStringValid(string input)
    {
        return !string.IsNullOrWhiteSpace(input);
    }
}