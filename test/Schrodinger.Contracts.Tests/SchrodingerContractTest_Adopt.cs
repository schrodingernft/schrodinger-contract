using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using Shouldly;
using Xunit;

namespace Schrodinger;

public partial class SchrodingerContractTests
{
    private const string Tick = "SGR";
    private const string Domain = "domain";

    [Fact]
    public async Task<Hash> AdoptTests()
    {
        const string parent = Tick + "-1";
        const string symbol = Tick + "-2";

        const long amount = 10000;
        const long outputAmount = 5000;
        const long lossAmount = 2500;
        const long commissionAmount = 2500;

        await Initialize();

        await Approve(amount, SchrodingerContractAddress, parent);

        GetBalance(DefaultAddress, parent).Result.ShouldBe(amount);
        GetBalance(DefaultAddress, symbol).Result.ShouldBe(0);

        var result = await SchrodingerContractStub.Adopt.SendAsync(new AdoptInput
        {
            Parent = parent,
            Amount = amount,
            Domain = Domain
        });

        var log = GetLogEvent<Adopted>(result.TransactionResult);
        log.AdoptId.ShouldBe(HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(Tick), HashHelper.ComputeFrom(2L)));
        log.Parent.ShouldBe(parent);
        log.ParentGen.ShouldBe(0);
        log.InputAmount.ShouldBe(amount);
        log.LossAmount.ShouldBe(lossAmount);
        log.CommissionAmount.ShouldBe(commissionAmount);
        log.OutputAmount.ShouldBe(outputAmount);
        log.ImageCount.ShouldBe(2);
        log.Adopter.ShouldBe(DefaultAddress);
        log.BlockHeight.ShouldBe(result.TransactionResult.BlockNumber);
        log.Gen.ShouldBe(1);
        log.Ancestor.ShouldBe(parent);
        log.Symbol.ShouldBe(symbol);
        log.TokenName.ShouldBe(symbol + "GEN1");
        log.Attributes.Data.Count.ShouldBe(1);

        GetBalance(DefaultAddress, parent).Result.ShouldBe(0);
        GetBalance(RecipientAddress, parent).Result.ShouldBe(commissionAmount);

        {
            var output = await SchrodingerContractStub.GetAdoptInfo.CallAsync(log.AdoptId);
            output.AdoptId.ShouldBe(log.AdoptId);
            output.Parent.ShouldBe(parent);
            output.ParentGen.ShouldBe(0);
            output.InputAmount.ShouldBe(amount);
            output.OutputAmount.ShouldBe(outputAmount);
            output.ParentAttributes.Data.Count.ShouldBe(0);
            output.ImageCount.ShouldBe(2);
            output.BlockHeight.ShouldBe(result.TransactionResult.BlockNumber);
            output.Symbol.ShouldBe(symbol);
            output.TokenName.ShouldBe(symbol + "GEN1");
            output.Gen.ShouldBe(1);
            output.Adopter.ShouldBe(DefaultAddress);
            output.Attributes.Data.Count.ShouldBe(1);
            output.IsConfirmed.ShouldBeFalse();
        }

        return log.AdoptId;
    }

    [Theory]
    [InlineData("", 0, "", "Invalid input parent.")]
    [InlineData("test-1", 0, "", "Invalid input amount.")]
    [InlineData("test-1", 10000, "", "Invalid input domain.")]
    [InlineData("test-1", 10000, Domain, "Tick not deployed.")]
    [InlineData(Tick + "-2", 10000, Domain, $"{Tick + "-2"} not exists.")]
    [InlineData(Tick + "-1", 1, Domain, "Input amount not enough.")]
    public async Task AdoptTests_Fail(string parent, long amount, string domain, string error)
    {
        await Initialize();
        await Approve(10000, SchrodingerContractAddress, Tick + "-1");

        var result = await SchrodingerContractStub.Adopt.SendWithExceptionAsync(new AdoptInput
        {
            Parent = parent,
            Amount = amount,
            Domain = domain
        });
        result.TransactionResult.Error.ShouldContain(error);
    }

    [Fact]
    public async Task AdoptTests_MaxGen_Fail()
    {
        await ConfirmTests();

        var result = await SchrodingerContractStub.Adopt.SendWithExceptionAsync(new AdoptInput
        {
            Parent = Tick + "-2",
            Amount = 5000,
            Domain = Domain
        });
        result.TransactionResult.Error.ShouldContain("Exceeds max gen.");
    }

    private async Task Initialize()
    {
        await InitializeSchrodingerMain();
        await InitSeed();
        await Approve(1, SchrodingerMainContractAddress, "SEED-1");
        await DeployCollection();
        await InitializeSchrodinger();
        await DeployGen0();
        await Issue(10000, DefaultAddress, "SGR-1");
    }

    private async Task InitSeed()
    {
        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Owner = DefaultAddress,
            Issuer = DefaultAddress,
            Symbol = "SEED-0",
            TokenName = "TOKEN SEED-0",
            TotalSupply = 1,
            Decimals = 0,
            IsBurnable = false,
            LockWhiteList = { TokenContractAddress }
        });

        var externalInfo = new ExternalInfo();
        externalInfo.Value.Add("__seed_exp_time", BlockTimeProvider.GetBlockTime().AddSeconds(1000).Seconds.ToString());
        externalInfo.Value.Add("__seed_owned_symbol", Tick + "-0");

        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Owner = DefaultAddress,
            Issuer = DefaultAddress,
            Symbol = "SEED-1",
            TokenName = "SEED-" + Tick,
            TotalSupply = 1,
            Decimals = 0,
            IsBurnable = true,
            LockWhiteList = { TokenContractAddress },
            ExternalInfo = externalInfo
        });

        await Issue(1, DefaultAddress, "SEED-1");
    }

    private async Task Approve(long amount, Address spender, string symbol)
    {
        await TokenContractStub.Approve.SendAsync(new ApproveInput
        {
            Amount = amount,
            Spender = spender,
            Symbol = symbol
        });
    }

    private async Task Issue(long amount, Address to, string symbol)
    {
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Amount = amount,
            Symbol = symbol,
            To = to
        });
    }

    private async Task<long> GetBalance(Address owner, string symbol)
    {
        var output = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = owner,
            Symbol = symbol
        });

        return output.Balance;
    }

    private async Task<TokenInfo> GetTokenInfo(string symbol)
    {
        var output = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
        {
            Symbol = symbol
        });

        return output;
    }

    private async Task DeployCollection()
    {
        await SchrodingerMainContractStub.Deploy.SendAsync(new SchrodingerMain.DeployInput
        {
            Tick = Tick,
            TokenName = "Schrodinger",
            Decimals = 0,
            Image = "image",
            ImageUri = "imageUri",
            IssueChainId = ChainHelper.ConvertBase58ToChainId("AELF"),
            SeedSymbol = "SEED-1"
        });
    }

    private async Task DeployGen0()
    {
        await SchrodingerContractStub.Deploy.SendAsync(new DeployInput
        {
            Tick = Tick,
            MaxGeneration = 1,
            TotalSupply = 21000000_00000000,
            Decimals = 0,
            LossRate = 5000,
            CommissionRate = 5000,
            ImageCount = 2,
            Recipient = RecipientAddress,
            Issuer = DefaultAddress,
            Image = "image",
            ImageUri = "imageUri",
            CrossGenerationConfig = new CrossGenerationConfig
            {
                Gen = 0
            },
            IsWeightEnabled = true,
            Admin = DefaultAddress,
            AttributesPerGen = 1,
            Domain = "domain",
            Signatory = DefaultAddress,
            AttributeLists = new AttributeLists
            {
                FixedAttributes =
                {
                    new AttributeSet
                    {
                        TraitType = new AttributeInfo
                        {
                            Name = "fix",
                            Weight = 1
                        },
                        Values = new AttributeInfos
                        {
                            Data =
                            {
                                new AttributeInfo
                                {
                                    Name = "fix-1",
                                    Weight = 1
                                }
                            }
                        }
                    }
                },
                RandomAttributes =
                {
                    new AttributeSet
                    {
                        TraitType = new AttributeInfo
                        {
                            Name = "random",
                            Weight = 1
                        },
                        Values = new AttributeInfos
                        {
                            Data =
                            {
                                new AttributeInfo
                                {
                                    Name = "random-1",
                                    Weight = 1
                                }
                            }
                        }
                    }
                }
            }
        });
    }
}