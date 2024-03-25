using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace Schrodinger;

public partial class SchrodingerContractTests : SchrodingerContractTestBase
{
    [Fact]
    public async Task InitializeSchrodingerMainTests()
    {
        var result = await SchrodingerMainContractStub.Initialize.SendAsync(new SchrodingerMain.InitializeInput
        {
            Admin = DefaultAddress,
            ImageMaxSize = 100,
            SchrodingerContractAddress = SchrodingerContractAddress
        });

        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        {
            var output = await SchrodingerMainContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(DefaultAddress);
        }
        {
            var output = await SchrodingerMainContractStub.GetImageMaxSize.CallAsync(new Empty());
            output.Value.ShouldBe(100);
        }
        {
            var output = await SchrodingerMainContractStub.GetSchrodingerContractAddress.CallAsync(new Empty());
            output.ShouldBe(SchrodingerContractAddress);
        }
    }

    [Fact]
    public async Task InitializeSchrodingerMainTests_Fail()
    {
        {
            var result = await UserSchrodingerMainContractStub.Initialize.SendWithExceptionAsync(
                new SchrodingerMain.InitializeInput
                {
                    Admin = DefaultAddress,
                    ImageMaxSize = 100,
                    SchrodingerContractAddress = SchrodingerContractAddress
                });
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await SchrodingerMainContractStub.Initialize.SendWithExceptionAsync(
                new SchrodingerMain.InitializeInput
                {
                    Admin = new Address()
                });
            result.TransactionResult.Error.ShouldContain("Invalid input admin.");
        }
        {
            var result =
                await SchrodingerMainContractStub.Initialize.SendWithExceptionAsync(
                    new SchrodingerMain.InitializeInput());
            result.TransactionResult.Error.ShouldContain("Invalid input image max size.");
        }
        {
            var result = await SchrodingerMainContractStub.Initialize.SendWithExceptionAsync(
                new SchrodingerMain.InitializeInput
                {
                    ImageMaxSize = 100
                });
            result.TransactionResult.Error.ShouldContain("Invalid input schrodinger contract address.");
        }
        {
            var result = await SchrodingerMainContractStub.Initialize.SendWithExceptionAsync(
                new SchrodingerMain.InitializeInput
                {
                    ImageMaxSize = 100,
                    SchrodingerContractAddress = new Address()
                });
            result.TransactionResult.Error.ShouldContain("Invalid input schrodinger contract address.");
        }

        await InitializeSchrodingerMainTests();

        {
            var result = await SchrodingerMainContractStub.Initialize.SendWithExceptionAsync(
                new SchrodingerMain.InitializeInput
                {
                    ImageMaxSize = 100,
                    SchrodingerContractAddress = SchrodingerContractAddress
                });
            result.TransactionResult.Error.ShouldContain("Already initialized.");
        }
    }

    [Fact]
    public async Task InitializeTests()
    {
        var dAppId = Hash.Empty;
        const int maxGen = 10;
        const long imageMaxSize = 51200;
        const long imageMaxCount = 2;
        const long traitTypeMaxCount = 50;
        const long traitValueMaxCount = 200;
        const long attributeMaxLength = 80;
        const int maxAttributesPerGen = 5;
        const long fixedTraitTypeMaxCount = 5;
        const long imageUriMaxSize = 64;


        var result = await SchrodingerContractStub.Initialize.SendAsync(new InitializeInput
        {
            Admin = DefaultAddress,
            PointsContract = TestPointsContractAddress,
            PointsContractDappId = dAppId,
            MaxGen = maxGen,
            ImageMaxSize = imageMaxSize,
            ImageMaxCount = imageMaxCount,
            TraitTypeMaxCount = traitTypeMaxCount,
            TraitValueMaxCount = traitValueMaxCount,
            AttributeMaxLength = attributeMaxLength,
            MaxAttributesPerGen = maxAttributesPerGen,
            FixedTraitTypeMaxCount = fixedTraitTypeMaxCount,
            ImageUriMaxSize = imageUriMaxSize
        });

        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        {
            var output = await SchrodingerContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(DefaultAddress);
        }
        {
            var output = await SchrodingerContractStub.GetConfig.CallAsync(new Empty());
            output.MaxGen.ShouldBe(maxGen);
            output.ImageMaxSize.ShouldBe(imageMaxSize);
            output.ImageMaxCount.ShouldBe(imageMaxCount);
            output.TraitTypeMaxCount.ShouldBe(traitTypeMaxCount);
            output.TraitValueMaxCount.ShouldBe(traitValueMaxCount);
            output.AttributeMaxLength.ShouldBe(attributeMaxLength);
            output.MaxAttributesPerGen.ShouldBe(maxAttributesPerGen);
            output.FixedTraitTypeMaxCount.ShouldBe(fixedTraitTypeMaxCount);
            output.ImageUriMaxSize.ShouldBe(imageUriMaxSize);
        }
        {
            var output = await SchrodingerContractStub.GetPointsContract.CallAsync(new Empty());
            output.ShouldBe(TestPointsContractAddress);
        }
        {
            var output = await SchrodingerContractStub.GetPointsContractDAppId.CallAsync(new Empty());
            output.ShouldBe(dAppId);
        }
    }

    [Fact]
    public async Task InitializeTests_Fail()
    {
        {
            var result = await UserSchrodingerContractStub.Initialize.SendWithExceptionAsync(new InitializeInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await SchrodingerContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                Admin = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input admin.");
        }
        {
            var result = await SchrodingerContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                PointsContract = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input points contract.");
        }
        {
            var result = await SchrodingerContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                PointsContractDappId = new Hash()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input points contract dapp id");
        }

        await InitializeTests();

        {
            var result = await SchrodingerContractStub.Initialize.SendWithExceptionAsync(new InitializeInput());
            result.TransactionResult.Error.ShouldContain("Already initialized.");
        }
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, "Invalid input max gen.")]
    [InlineData(10, 0, 0, 0, 0, 0, 0, 0, 0, "Invalid input image max size.")]
    [InlineData(10, 10, 0, 0, 0, 0, 0, 0, 0, "Invalid input image max count.")]
    [InlineData(10, 10, 10, 0, 0, 0, 0, 0, 0, "Invalid input trait type max count.")]
    [InlineData(10, 10, 10, 10, 0, 0, 0, 0, 0, "Invalid input trait value max count.")]
    [InlineData(10, 10, 10, 10, 10, 0, 0, 0, 0, "Invalid input attribute max length.")]
    [InlineData(10, 10, 10, 10, 10, 10, 0, 0, 0, "Invalid input max attributes per gen.")]
    [InlineData(10, 10, 10, 10, 10, 10, 10, 0, 0, "Invalid input fixed trait type max count.")]
    [InlineData(10, 10, 10, 10, 10, 10, 10, 10, 0, "Invalid input image uri max size.")]
    public async Task InitializeTests_Numerical_Fail(int maxGen, long imageMaxSize, long imageMaxCount,
        long traitTypeMaxCount, long traitValueMaxCount, long attributesMaxLength, int maxAttributesPerGen,
        long fixedTraitTypeMaxCount, long imageUriMaxSize, string error)
    {
        var result = await SchrodingerContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
        {
            MaxGen = maxGen,
            ImageMaxSize = imageMaxSize,
            ImageMaxCount = imageMaxCount,
            TraitTypeMaxCount = traitTypeMaxCount,
            TraitValueMaxCount = traitValueMaxCount,
            AttributeMaxLength = attributesMaxLength,
            MaxAttributesPerGen = maxAttributesPerGen,
            FixedTraitTypeMaxCount = fixedTraitTypeMaxCount,
            ImageUriMaxSize = imageUriMaxSize
        });
        result.TransactionResult.Error.ShouldContain(error);
    }
}