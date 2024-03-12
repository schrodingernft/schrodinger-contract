using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Schrodinger.Main;
using SchrodingerMain;
using Shouldly;
using Xunit;

namespace Schrodinger;

public partial class SchrodingerContractTests
{
    private readonly string _image =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACgAAAAoCAYAAACM/rhtAAADfklEQVR4nO2Yy2sTQRzHv7t5lTa2HtRjaGhJpfRSkFAPtiJBL3rz0iBWkC6ilLTE+ijYg0p9hlaLoCko9pDc/Av2UFvBEq+12lgIBk9WkEJqk5rselh3O9nsYybbood+IWTnN7uzn/nOY3+7XCopyfiPxf9rADvtATrVHqBTuZ1cLAx5tePk9JZjGCPVDSgMedEvFgAA6Yif6nxVLJ2pC5CEm569hTQFWM/EMoLhAFVnHAEKQ170TCxrcLRg9coNAJGpkBYQh7PaMRkn64xuSA6hEZjaGSu3DQH1EPqyHsLMPTWuanGsE4sA+sWCpdO2gKwXGLn34/wIDox1avMSAIJiAblMHumIH2n1nNlJ5tXOfe9slw9eX9cCaw9atGMy7rqwhovzRabGSb051YRShWMGdJMQeihV+4SfOPE4WxNnUfedT3g/1sV8HdWTpFAyHloWBcMBHJ1YQlRgm1XK2b2cUprXpYa9HHxta4gkVhzBqaqnk5ycO2SZsPra1jDwdpOp0VwmXwVElnOZPMT4YaSSZaq2diVZGAicRv75yaqyGA9pkE1ejrotHgDGW4sYb61doUYxGp05m7Usb2zRv2W4SYh6gXZTOzLEuUxem2cAMPquiL6XnwEAsW4fvm3IWCpsu9bc6KFu2w0AT6aUvS823FJVqcTt+2C0OmPdPgBA+8i17djkQwDAK5YhVuFIUFIuSXHIbIvQr1gSzkzlMt0KBijs8UxKmB/tMK0PhgOWcKt/XVP/WcUDwNWnyk8vMk7OMb30c7DL76qqb5hJaMdHml3UeyAAcLeDMtWEuPuVR++jFdungeogCblUqAAAhl98YdqkmQDrgdSry8/mHgBw6reZ1Xu1u3v7Tbnq4d7i9WCzrKRLdqAkZD3O1QCaSZ99qDe5HHPhV8kYlJyPH26EsP4bSCUlZjgqQAA4d8kNiWifdELtgJeXDZMKJTkwf42wE1VyJll0vgr2OI/BuerHZTAcwOBC0XC/pBGVg4D5UJMauOJD3/2Ptps6i5jSWy/nwpZcMa1//ayEqMBjcME46QiGA0hF9mOjRJ+UUDsI0LkYFXhEElnHrwiqHH08MlIqKVm6yDrMTA4CO+PizLEG6vvtuIPAtotmkOqqFuMh2/2ROWHVO2b2GplKSpiLd5gOqbr9RAVrBMcZdaPHHHI2WYEYD1nOu0giawn5B8F9gRyqFJDiAAAAAElFTkSuQmCC";

    private readonly string _tick = "SGR";
    private readonly string _tokenName = "SCHRODINGER";
    private readonly int _mainChainId = 9992731;


    private async Task InitializeSchrodingerMain()
    {
        await SchrodingerMainContractStub.Initialize.SendAsync(new SchrodingerMain.InitializeInput
        {
            Admin = DefaultAddress,
            ImageMaxSize = 10240
        });
    }
    
    private async Task Initialize()
    {
        await UpdateContract();

        // await SchrodingerContractStub.Initialize.SendAsync(new InitializeInput
        // {
        //     Admin = DefaultAddress,
        //     PointsContract = TestPointsContractAddress,
        //     PointsContractDappId = HashHelper.ComputeFrom("PointsContractDappId"),
        //     MaxGen = 10,
        //     ImageMaxSize = 10240,
        //     ImageMaxCount = 2,
        //     TraitTypeMaxCount = 50,
        //     TraitValueMaxCount = 100,
        //     AttributeMaxLength = 80,
        //     MaxAttributesPerGen = 5,
        //     Signatory = DefaultAddress
        // });

        await SchrodingerContractStub.SetConfig.SendAsync(new Config
        {
            MaxGen = 10,
            ImageMaxSize = 10240,
            ImageMaxCount = 2,
            TraitTypeMaxCount = 50,
            TraitValueMaxCount = 100,
            AttributeMaxLength = 80,
            MaxAttributesPerGen = 5,
            Signatory = DefaultAddress,
            FixedTraitTypeMaxCount = 5
        });
        
        await SchrodingerContractStub.SetPointsContract.SendAsync(TestPointsContractAddress);
        await SchrodingerContractStub.SetPointsContractDAppId.SendAsync(HashHelper.ComputeFrom("PointsContractDappId"));
    }

    [Fact]
    public async Task DeployCollectionTest()
    {
        await InitializeSchrodingerMain();
        await BuySeed();
        
        await SchrodingerMainContractStub.Deploy.SendAsync(new SchrodingerMain.DeployInput
        {
            Tick = _tick,
            Image = _image,
            SeedSymbol = "SEED-1",
            TokenName = _tokenName,
            Decimals = 0,
            IssueChainId = _mainChainId
        });
        var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
        {
            Symbol = $"{_tick}-0"
        });
        tokenInfo.Symbol.ShouldBe($"{_tick}-0");
        tokenInfo.Owner.ShouldBe(SchrodingerContractAddress);
    }
    
    [Fact]
    public async Task DeployTest()
    {
        await DeployCollectionTest();
        await Initialize();
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
                CrossGenerationProbability = 10000,
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

    // [Fact]
    // public async Task SetAttributeListTest()
    // {
    //     await DeployTest();
    //     var attribute = GetAttributeLists_other();
    //     await SchrodingerContractStub.SetAttributes.SendAsync(new SetAttributesInput
    //     {
    //         Tick = _tick,
    //         Attributes = attribute
    //     });
    //     var attributeList = await SchrodingerContractStub.GetAttributes.CallAsync(new StringValue
    //     {
    //         Value = _tick
    //     });
    //     attributeList.FixedAttributes.Count.ShouldBe(4);
    //     attributeList.RandomAttributes.Count.ShouldBe(5);
    //     attributeList.FixedAttributes[3].Values.Data.Count.ShouldBe(3);
    //     attributeList.FixedAttributes[3].Values.Data[0].Name.ShouldBe("Alien");
    //     attributeList.FixedAttributes[3].Values.Data[0].Weight.ShouldBe(760);
    //     attributeList.FixedAttributes[3].Values.Data[1].Name.ShouldBe("Ape");
    //     attributeList.FixedAttributes[3].Values.Data[1].Weight.ShouldBe(95);
    //     attributeList.FixedAttributes[3].Values.Data[2].Name.ShouldBe("Zombie");
    //     attributeList.FixedAttributes[3].Values.Data[2].Weight.ShouldBe(95);
    //     attributeList.RandomAttributes[4].Values.Data.Count.ShouldBe(3);
    //     attributeList.RandomAttributes[4].Values.Data[0].Name.ShouldBe("Boots");
    //     attributeList.RandomAttributes[4].Values.Data[0].Weight.ShouldBe(5);
    //     attributeList.RandomAttributes[4].Values.Data[1].Name.ShouldBe("Clogs");
    //     attributeList.RandomAttributes[4].Values.Data[1].Weight.ShouldBe(10);
    //     attributeList.RandomAttributes[4].Values.Data[2].Name.ShouldBe("Brogues");
    //     attributeList.RandomAttributes[4].Values.Data[2].Weight.ShouldBe(9);
    // }
    //
    // [Fact]
    // public async Task SetAttributeList_Remove_Test()
    // {
    //     await SetAttributeListTest();
    //     var attribute = GetAttributeLists_remove_duplicated_values();
    //     await SchrodingerContractStub.SetAttributes.SendAsync(new SetAttributesInput
    //     {
    //         Tick = _tick,
    //         Attributes = attribute
    //     });
    //     var attributeList = await SchrodingerContractStub.GetAttributes.CallAsync(new StringValue
    //     {
    //         Value = _tick
    //     });
    //     attributeList.FixedAttributes.Count.ShouldBe(3);
    //     attributeList.RandomAttributes.Count.ShouldBe(4);
    //     attributeList.FixedAttributes[0].TraitType.Name.ShouldBe("Background");
    //     attributeList.FixedAttributes[1].TraitType.Name.ShouldBe("Eyes");
    //     attributeList.FixedAttributes[2].TraitType.Name.ShouldBe("Breed");
    //     attributeList.FixedAttributes[2].Values.Data.Count.ShouldBe(3);
    //     attributeList.FixedAttributes[2].Values.Data[0].Name.ShouldBe("Alien");
    //     attributeList.FixedAttributes[2].Values.Data[0].Weight.ShouldBe(760);
    //     attributeList.FixedAttributes[2].Values.Data[1].Name.ShouldBe("Ape");
    //     attributeList.FixedAttributes[2].Values.Data[1].Weight.ShouldBe(95);
    //     attributeList.FixedAttributes[2].Values.Data[2].Name.ShouldBe("Zombie");
    //     attributeList.FixedAttributes[2].Values.Data[2].Weight.ShouldBe(95);
    //     attributeList.RandomAttributes[0].TraitType.Name.ShouldBe("Hat");
    //     attributeList.RandomAttributes[1].TraitType.Name.ShouldBe("Pet");
    //     attributeList.RandomAttributes[2].TraitType.Name.ShouldBe("Face");
    //     attributeList.RandomAttributes[3].TraitType.Name.ShouldBe("Shoes");
    //     attributeList.RandomAttributes[1].Values.Data.Count.ShouldBe(3);
    //     attributeList.RandomAttributes[1].Values.Data[0].Name.ShouldBe("Alien");
    //     attributeList.RandomAttributes[1].Values.Data[0].Weight.ShouldBe(300);
    //     attributeList.RandomAttributes[1].Values.Data[1].Name.ShouldBe("Ape");
    //     attributeList.RandomAttributes[1].Values.Data[1].Weight.ShouldBe(20);
    //     attributeList.RandomAttributes[1].Values.Data[2].Name.ShouldBe("Zombie");
    //     attributeList.RandomAttributes[1].Values.Data[2].Weight.ShouldBe(95);
    //     attributeList.RandomAttributes[2].Values.Data[0].Name.ShouldBe("Boots");
    //     attributeList.RandomAttributes[2].Values.Data[0].Weight.ShouldBe(720);
    //     attributeList.RandomAttributes[2].Values.Data[1].Name.ShouldBe("Clogs");
    //     attributeList.RandomAttributes[2].Values.Data[1].Weight.ShouldBe(10);
    //     attributeList.RandomAttributes[2].Values.Data[2].Name.ShouldBe("Brogues");
    //     attributeList.RandomAttributes[2].Values.Data[2].Weight.ShouldBe(60);
    // }
    
}