using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace Schrodinger;

public partial class SchrodingerContractTests
{
    private readonly string _image =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACgAAAAoCAYAAACM/rhtAAADfklEQVR4nO2Yy2sTQRzHv7t5lTa2HtRjaGhJpfRSkFAPtiJBL3rz0iBWkC6ilLTE+ijYg0p9hlaLoCko9pDc/Av2UFvBEq+12lgIBk9WkEJqk5rselh3O9nsYybbood+IWTnN7uzn/nOY3+7XCopyfiPxf9rADvtATrVHqBTuZ1cLAx5tePk9JZjGCPVDSgMedEvFgAA6Yif6nxVLJ2pC5CEm569hTQFWM/EMoLhAFVnHAEKQ170TCxrcLRg9coNAJGpkBYQh7PaMRkn64xuSA6hEZjaGSu3DQH1EPqyHsLMPTWuanGsE4sA+sWCpdO2gKwXGLn34/wIDox1avMSAIJiAblMHumIH2n1nNlJ5tXOfe9slw9eX9cCaw9atGMy7rqwhovzRabGSb051YRShWMGdJMQeihV+4SfOPE4WxNnUfedT3g/1sV8HdWTpFAyHloWBcMBHJ1YQlRgm1XK2b2cUprXpYa9HHxta4gkVhzBqaqnk5ycO2SZsPra1jDwdpOp0VwmXwVElnOZPMT4YaSSZaq2diVZGAicRv75yaqyGA9pkE1ejrotHgDGW4sYb61doUYxGp05m7Usb2zRv2W4SYh6gXZTOzLEuUxem2cAMPquiL6XnwEAsW4fvm3IWCpsu9bc6KFu2w0AT6aUvS823FJVqcTt+2C0OmPdPgBA+8i17djkQwDAK5YhVuFIUFIuSXHIbIvQr1gSzkzlMt0KBijs8UxKmB/tMK0PhgOWcKt/XVP/WcUDwNWnyk8vMk7OMb30c7DL76qqb5hJaMdHml3UeyAAcLeDMtWEuPuVR++jFdungeogCblUqAAAhl98YdqkmQDrgdSry8/mHgBw6reZ1Xu1u3v7Tbnq4d7i9WCzrKRLdqAkZD3O1QCaSZ99qDe5HHPhV8kYlJyPH26EsP4bSCUlZjgqQAA4d8kNiWifdELtgJeXDZMKJTkwf42wE1VyJll0vgr2OI/BuerHZTAcwOBC0XC/pBGVg4D5UJMauOJD3/2Ptps6i5jSWy/nwpZcMa1//ayEqMBjcME46QiGA0hF9mOjRJ+UUDsI0LkYFXhEElnHrwiqHH08MlIqKVm6yDrMTA4CO+PizLEG6vvtuIPAtotmkOqqFuMh2/2ROWHVO2b2GplKSpiLd5gOqbr9RAVrBMcZdaPHHHI2WYEYD1nOu0giawn5B8F9gRyqFJDiAAAAAElFTkSuQmCC";

    private readonly string _tick = "SGR";

    [Fact]
    public async Task Initialize()
    {
        await SchrodingerContractStub.Initialize.SendAsync(new InitializeInput
        {
            Admin = DefaultAddress,
            PointsContract = DefaultAddress,
            PointsContractDappId = HashHelper.ComputeFrom("PointsContractDappId")
        });
    }

    [Fact]
    public async Task DeployCollectionTest()
    {
        await Initialize();
        await BuySeed();
        await SchrodingerContractStub.DeployCollection.SendAsync(new DeployCollectionInput
        {
            Tick = _tick,
            Image = _image,
            SeedSymbol = "SEED-1"
        });
        var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
        {
            Symbol = $"{_tick}-0"
        });
        tokenInfo.Symbol.ShouldBe($"{_tick}-0");
        tokenInfo.Owner.ShouldBe(SchrodingerContractAddress);
    }

    private AttributeLists GetAttributeLists()
    {
         var traitValues1 = new List<AttributeInfo>
        {
            new AttributeInfo { Name = "Black", Weight = 8 },
            new AttributeInfo { Name = "white", Weight = 2 },
            new AttributeInfo { Name = "Red", Weight = 14 }
        };
        var traitValues2 = new List<AttributeInfo>
        {
            new AttributeInfo { Name = "Big", Weight = 5 },
            new AttributeInfo { Name = "Small", Weight = 10 },
            new AttributeInfo { Name = "Medium", Weight = 9 }
        };

        var traitValues3 = new List<AttributeInfo>
        {
            new AttributeInfo { Name = "Halo", Weight = 170 },
            new AttributeInfo { Name = "Tiara", Weight = 38 },
            new AttributeInfo { Name = "Crown", Weight = 100 }
        };
        var traitValues4 = new List<AttributeInfo>
        {
            new AttributeInfo { Name = "Pizza", Weight = 310 },
            new AttributeInfo { Name = "Rose", Weight = 210 },
            new AttributeInfo { Name = "Roar", Weight = 160 }
        };
        var traitValues5 = new List<AttributeInfo>
        {
            new AttributeInfo { Name = "Alien", Weight = 400 },
            new AttributeInfo { Name = "Elf", Weight = 10 },
            new AttributeInfo { Name = "Star", Weight = 199 }
        };
        var traitValues6 = new List<AttributeInfo>
        {
            new AttributeInfo { Name = "Sad", Weight = 600 },
            new AttributeInfo { Name = "Happy", Weight = 120 },
            new AttributeInfo { Name = "Angry", Weight = 66 }
        };
        var traitValues7 = new List<AttributeInfo>
        {
            new AttributeInfo { Name = "Hoddie", Weight = 127 },
            new AttributeInfo { Name = "Kimono", Weight = 127 },
            new AttributeInfo { Name = "Student", Weight = 127 }
        };
        var fixedAttributes = new List<AttributeSet>()
        {
            new AttributeSet
            {
                TraitType = new AttributeInfo
                {
                    Name = "Background",
                    Weight = 170
                },
                Values = new AttributeInfos
                {
                    Data = { traitValues1 }
                }
            },
            new AttributeSet
            {
                TraitType = new AttributeInfo
                {
                    Name = "Eyes",
                    Weight = 100
                },
                Values = new AttributeInfos
                {
                    Data = { traitValues2 }
                }
            },
            new AttributeSet
            {
                TraitType = new AttributeInfo
                {
                    Name = "Clothes",
                    Weight = 200
                },
                Values = new AttributeInfos
                {
                    Data = { traitValues7 }
                }
            }
        };
        var randomAttributes = new List<AttributeSet>()
        {
            new AttributeSet
            {
                TraitType = new AttributeInfo
                {
                    Name = "Hat",
                    Weight = 170
                },
                Values = new AttributeInfos
                {
                    Data = { traitValues3 }
                }
            },
            new AttributeSet
            {
                TraitType = new AttributeInfo
                {
                    Name = "Mouth",
                    Weight = 200
                },
                Values = new AttributeInfos
                {
                    Data = { traitValues4 }
                }
            },
            new AttributeSet
            {
                TraitType = new AttributeInfo
                {
                    Name = "Pet",
                    Weight = 300
                },
                Values = new AttributeInfos
                {
                    Data = { traitValues5 }
                }
            },
            new AttributeSet
            {
                TraitType = new AttributeInfo
                {
                    Name = "Face",
                    Weight = 450
                },
                Values = new AttributeInfos
                {
                    Data = { traitValues6 }
                }
            }
        };
        return new AttributeLists
        {
            FixedAttributes = { fixedAttributes },
            RandomAttributes = { randomAttributes }
        };
    }

    [Fact]
    public async Task DeployTest()
    {
        await DeployCollectionTest();
        await SchrodingerContractStub.Deploy.SendAsync(new DeployInput()
        {
            Tick = _tick,
            AttributesPerGen = 1,
            MaxGeneration = 4,
            ImageCount = 2,
            Decimals = 0,
            CommissionRate = 1000,
            LossRate = 500,
            AttributeLists = GetAttributeLists(),
            Image = _image,
            IsWeightEnabled = true,
            TotalSupply = 21000000,
            CrossGenerationConfig = new CrossGenerationConfig
            {
                Gen = 0,
                CrossGenerationProbability = 0,
                IsWeightEnabled = false
            }
        });
        var inscription = await SchrodingerContractStub.GetInscriptionInfo.CallAsync(new StringValue
        {
            Value = _tick
        });
        inscription.ImageCount.ShouldBe(2);
        inscription.MaxGen.ShouldBe(4);
        inscription.CommissionRate.ShouldBe(1000);
        inscription.LossRate.ShouldBe(500);
        inscription.IsWeightEnabled.ShouldBe(true);
        var attributeList = await SchrodingerContractStub.GetAttributes.CallAsync(new StringValue
        {
            Value = _tick
        });
        attributeList.FixedAttributes.Count.ShouldBe(3);
        attributeList.RandomAttributes.Count.ShouldBe(4);
        attributeList.FixedAttributes[0].TraitType.Name.ShouldBe("Background");
        attributeList.FixedAttributes[1].TraitType.Name.ShouldBe("Eyes");
        attributeList.FixedAttributes[2].TraitType.Name.ShouldBe("Clothes");
        attributeList.FixedAttributes[0].Values.Data.Count.ShouldBe(3);
        attributeList.FixedAttributes[0].Values.Data[0].Name.ShouldBe("Black");
        attributeList.FixedAttributes[0].Values.Data[0].Weight.ShouldBe(8);
        attributeList.FixedAttributes[0].Values.Data[1].Name.ShouldBe("white");
        attributeList.FixedAttributes[0].Values.Data[1].Weight.ShouldBe(2);
        attributeList.FixedAttributes[0].Values.Data[2].Name.ShouldBe("Red");
        attributeList.FixedAttributes[0].Values.Data[2].Weight.ShouldBe(14);
        attributeList.FixedAttributes[1].Values.Data.Count.ShouldBe(3);
        attributeList.FixedAttributes[1].Values.Data[0].Name.ShouldBe("Big");
        attributeList.FixedAttributes[1].Values.Data[0].Weight.ShouldBe(5);
        attributeList.FixedAttributes[1].Values.Data[1].Name.ShouldBe("Small");
        attributeList.FixedAttributes[1].Values.Data[1].Weight.ShouldBe(10);
        attributeList.FixedAttributes[1].Values.Data[2].Name.ShouldBe("Medium");
        attributeList.FixedAttributes[1].Values.Data[2].Weight.ShouldBe(9);
        attributeList.FixedAttributes[2].Values.Data.Count.ShouldBe(3);
        attributeList.FixedAttributes[2].Values.Data[0].Name.ShouldBe("Hoddie");
        attributeList.FixedAttributes[2].Values.Data[0].Weight.ShouldBe(127);
        attributeList.FixedAttributes[2].Values.Data[1].Name.ShouldBe("Kimono");
        attributeList.FixedAttributes[2].Values.Data[1].Weight.ShouldBe(127);
        attributeList.FixedAttributes[2].Values.Data[2].Name.ShouldBe("Student");
        attributeList.FixedAttributes[2].Values.Data[2].Weight.ShouldBe(127);
        attributeList.RandomAttributes[0].TraitType.Name.ShouldBe("Hat");
        attributeList.RandomAttributes[0].Values.Data.Count.ShouldBe(3);
        attributeList.RandomAttributes[0].Values.Data[0].Name.ShouldBe("Halo");
        attributeList.RandomAttributes[0].Values.Data[0].Weight.ShouldBe(170);
        attributeList.RandomAttributes[0].Values.Data[1].Name.ShouldBe("Tiara");
        attributeList.RandomAttributes[0].Values.Data[1].Weight.ShouldBe(38);
        attributeList.RandomAttributes[0].Values.Data[2].Name.ShouldBe("Crown");
        attributeList.RandomAttributes[0].Values.Data[2].Weight.ShouldBe(100);
        attributeList.RandomAttributes[1].Values.Data.Count.ShouldBe(3);
        attributeList.RandomAttributes[1].Values.Data[0].Name.ShouldBe("Pizza");
        attributeList.RandomAttributes[1].Values.Data[0].Weight.ShouldBe(310);
        attributeList.RandomAttributes[1].Values.Data[1].Name.ShouldBe("Rose");
        attributeList.RandomAttributes[1].Values.Data[1].Weight.ShouldBe(210);
        attributeList.RandomAttributes[1].Values.Data[2].Name.ShouldBe("Roar");
        attributeList.RandomAttributes[1].Values.Data[2].Weight.ShouldBe(160);
        attributeList.RandomAttributes[2].Values.Data.Count.ShouldBe(3);
        attributeList.RandomAttributes[2].Values.Data[0].Name.ShouldBe("Alien");
        attributeList.RandomAttributes[2].Values.Data[0].Weight.ShouldBe(400);
        attributeList.RandomAttributes[2].Values.Data[1].Name.ShouldBe("Elf");
        attributeList.RandomAttributes[2].Values.Data[1].Weight.ShouldBe(10);
        attributeList.RandomAttributes[2].Values.Data[2].Name.ShouldBe("Star");
        attributeList.RandomAttributes[2].Values.Data[2].Weight.ShouldBe(199);
    }

    [Fact]
    public async Task SetAttributeList()
    {
        await DeployTest();
        var attribute = GetAttributeLists_other();
        await SchrodingerContractStub.SetAttributes.SendAsync(new SetAttributesInput
        {
            Tick = _tick,
            Attributes = attribute
        });
    }
    
    private AttributeLists GetAttributeLists_other()
    {
         var traitValues1 = new List<AttributeInfo>
        {
            new AttributeInfo { Name = "Alien", Weight = 760 },
            new AttributeInfo { Name = "Ape", Weight = 95 },
            new AttributeInfo { Name = "Zombie", Weight = 95 }
        };
        var traitValues2 = new List<AttributeInfo>
        {
            new AttributeInfo { Name = "Boots", Weight = 5 },
            new AttributeInfo { Name = "Clogs", Weight = 10 },
            new AttributeInfo { Name = "Brogues", Weight = 9 }
        };
        var fixedAttributes = new List<AttributeSet>()
        {
            new AttributeSet
            {
                TraitType = new AttributeInfo
                {
                    Name = "Breed",
                    Weight = 170
                },
                Values = new AttributeInfos
                {
                    Data = { traitValues1 }
                }
            }
        };
        var randomAttributes = new List<AttributeSet>()
        {
            new AttributeSet
            {
                TraitType = new AttributeInfo
                {
                    Name = "Shoes",
                    Weight = 170
                },
                Values = new AttributeInfos
                {
                    Data = { traitValues2 }
                }
            }
        };
        return new AttributeLists
        {
            FixedAttributes = { fixedAttributes },
            RandomAttributes = { randomAttributes }
        };
    }
    private async Task BuySeed()
    {
        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = "SEED-0",
            TokenName = "SEED-0 token",
            TotalSupply = 1,
            Decimals = 0,
            Issuer = DefaultAddress,
            IsBurnable = true,
            IssueChainId = 0,
        });

        var seedOwnedSymbol = "SGR" + "-0";
        var seedExpTime = "1720590467";
        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = "SEED-1",
            TokenName = "SEED-1 token",
            TotalSupply = 1,
            Decimals = 0,
            Issuer = DefaultAddress,
            IsBurnable = true,
            IssueChainId = 0,
            LockWhiteList = { TokenContractAddress },
            ExternalInfo = new ExternalInfo()
            {
                Value =
                {
                    {
                        "__seed_owned_symbol",
                        seedOwnedSymbol
                    },
                    {
                        "__seed_exp_time",
                        seedExpTime
                    }
                }
            }
        });

        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = "SEED-1",
            Amount = 1,
            To = DefaultAddress,
            Memo = ""
        });

        var balance = await TokenContractStub.GetBalance.SendAsync(new GetBalanceInput()
        {
            Owner = DefaultAddress,
            Symbol = "SEED-1"
        });
        balance.Output.Balance.ShouldBe(1);
        await TokenContractStub.Approve.SendAsync(new ApproveInput()
        {
            Symbol = "SEED-1",
            Amount = 1,
            Spender = SchrodingerContractAddress
        });
    }
}