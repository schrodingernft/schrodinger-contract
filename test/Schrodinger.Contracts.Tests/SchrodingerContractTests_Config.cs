using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace Schrodinger;

public partial class SchrodingerContractTests
{
    [Fact]
    public async Task SetAdminSchrodingerMainTests()
    {
        await InitializeSchrodingerMainTests();

        {
            var output = await SchrodingerMainContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(DefaultAddress);
        }

        var result = await SchrodingerMainContractStub.SetAdmin.SendAsync(UserAddress);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        {
            var output = await SchrodingerMainContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(UserAddress);
        }
    }

    [Fact]
    public async Task SetAdminSchrodingerMainTests_Fail()
    {
        await InitializeSchrodingerMainTests();

        {
            var result = await UserSchrodingerMainContractStub.SetAdmin.SendWithExceptionAsync(UserAddress);
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await SchrodingerMainContractStub.SetAdmin.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
    }

    [Fact]
    public async Task SetImageMaxSizeSchrodingerMainTests()
    {
        const long imageMaxSize = 100;
        const long updatedImageMaxSize = 200;

        await InitializeSchrodingerMainTests();

        {
            var output = await SchrodingerMainContractStub.GetImageMaxSize.CallAsync(new Empty());
            output.Value.ShouldBe(imageMaxSize);
        }

        var result =
            await SchrodingerMainContractStub.SetImageMaxSize.SendAsync(new Int64Value { Value = updatedImageMaxSize });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        {
            var output = await SchrodingerMainContractStub.GetImageMaxSize.CallAsync(new Empty());
            output.Value.ShouldBe(updatedImageMaxSize);
        }
    }

    [Fact]
    public async Task SetImageMaxSizeSchrodingerMainTests_Fail()
    {
        await InitializeSchrodingerMainTests();

        {
            var result = await UserSchrodingerMainContractStub.SetImageMaxSize.SendWithExceptionAsync(new Int64Value());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await SchrodingerMainContractStub.SetImageMaxSize.SendWithExceptionAsync(new Int64Value());
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
    }

    [Fact]
    public async Task SetSchrodingerContractAddressSchrodingerMainTests()
    {
        await InitializeSchrodingerMainTests();

        {
            var output = await SchrodingerMainContractStub.GetSchrodingerContractAddress.CallAsync(new Empty());
            output.ShouldBe(SchrodingerContractAddress);
        }

        var result = await SchrodingerMainContractStub.SetSchrodingerContractAddress.SendAsync(DefaultAddress);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        {
            var output = await SchrodingerMainContractStub.GetSchrodingerContractAddress.CallAsync(new Empty());
            output.ShouldBe(DefaultAddress);
        }
    }

    [Fact]
    public async Task SetSchrodingerContractAddressSchrodingerMainTests_Fail()
    {
        await InitializeSchrodingerMainTests();

        {
            var result =
                await UserSchrodingerMainContractStub.SetSchrodingerContractAddress.SendWithExceptionAsync(
                    new Address());
            result.TransactionResult.Error.ShouldContain("Not permission.");
        }
        {
            var result =
                await SchrodingerMainContractStub.SetSchrodingerContractAddress.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
    }

    [Fact]
    public async Task SetConfigTests()
    {
        var config = new Config
        {
            MaxGen = 10,
            ImageMaxSize = 51200,
            ImageMaxCount = 2,
            TraitTypeMaxCount = 50,
            TraitValueMaxCount = 200,
            AttributeMaxLength = 80,
            MaxAttributesPerGen = 5,
            FixedTraitTypeMaxCount = 5,
            ImageUriMaxSize = 64
        };

        var updatedConfig = new Config
        {
            MaxGen = 1,
            ImageMaxSize = 1,
            ImageMaxCount = 1,
            TraitTypeMaxCount = 1,
            TraitValueMaxCount = 1,
            AttributeMaxLength = 1,
            MaxAttributesPerGen = 1,
            FixedTraitTypeMaxCount = 1,
            ImageUriMaxSize = 1
        };

        await InitializeTests();

        {
            var output = await SchrodingerContractStub.GetConfig.CallAsync(new Empty());
            output.ShouldBe(config);
        }

        {
            var result = await SchrodingerContractStub.SetConfig.SendAsync(updatedConfig);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<ConfigSet>(result.TransactionResult);
            log.Config.ShouldBe(updatedConfig);
        }

        {
            var output = await SchrodingerContractStub.GetConfig.CallAsync(new Empty());
            output.ShouldBe(updatedConfig);
        }

        {
            var result = await SchrodingerContractStub.SetConfig.SendAsync(updatedConfig);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<ConfigSet>(result.TransactionResult);
            log.ShouldBe(new ConfigSet());
        }
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, "Invalid max generation.")]
    [InlineData(10, 0, 0, 0, 0, 0, 0, 0, 0, "Invalid image max size.")]
    [InlineData(10, 10, 0, 0, 0, 0, 0, 0, 0, "Invalid image max count.")]
    [InlineData(10, 10, 10, 0, 0, 0, 0, 0, 0, "Invalid trait type max count.")]
    [InlineData(10, 10, 10, 10, 0, 0, 0, 0, 0, "Invalid trait value max count.")]
    [InlineData(10, 10, 10, 10, 10, 0, 0, 0, 0, "Invalid attribute max length.")]
    [InlineData(10, 10, 10, 10, 10, 10, 0, 0, 0, "Invalid max attributes per generation.")]
    [InlineData(10, 10, 10, 10, 10, 10, 10, 0, 0, "Invalid fixed trait type max count.")]
    [InlineData(10, 10, 10, 10, 10, 10, 10, 10, 0, "Invalid image uri max size.")]
    public async Task SetConfigTests_Fail(int maxGen, long imageMaxSize, long imageMaxCount, long traitTypeMaxCount,
        long traitValueMaxCount, long attributeMaxLength, int maxAttributesPerGen, long fixedTraitTypeMaxCount,
        long imageUriMaxSize, string error)
    {
        await InitializeTests();

        {
            var result = await UserSchrodingerContractStub.SetConfig.SendWithExceptionAsync(new Config());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await SchrodingerContractStub.SetConfig.SendWithExceptionAsync(new Config
            {
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
            result.TransactionResult.Error.ShouldContain(error);
        }
    }

    [Fact]
    public async Task SetMaxGenerationConfigTests()
    {
        const int maxGen = 10;
        const int updatedMaxGen = 1;

        await InitializeTests();

        {
            var output = await SchrodingerContractStub.GetConfig.CallAsync(new Empty());
            output.MaxGen.ShouldBe(maxGen);
        }

        {
            var result = await SchrodingerContractStub.SetMaxGenerationConfig.SendAsync(new Int32Value
            {
                Value = updatedMaxGen
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<MaxGenerationConfigSet>(result.TransactionResult);
            log.MaxGen.ShouldBe(updatedMaxGen);
        }

        {
            var output = await SchrodingerContractStub.GetConfig.CallAsync(new Empty());
            output.MaxGen.ShouldBe(updatedMaxGen);
        }

        {
            var result = await SchrodingerContractStub.SetMaxGenerationConfig.SendAsync(new Int32Value
            {
                Value = updatedMaxGen
            });

            var log = GetLogEvent<MaxGenerationConfigSet>(result.TransactionResult);
            log.ShouldBe(new MaxGenerationConfigSet());
        }
    }

    [Fact]
    public async Task SetMaxGenerationConfigTests_Fail()
    {
        await InitializeTests();

        {
            var result =
                await UserSchrodingerContractStub.SetMaxGenerationConfig.SendWithExceptionAsync(new Int32Value());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await SchrodingerContractStub.SetMaxGenerationConfig.SendWithExceptionAsync(new Int32Value());
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
    }

    [Fact]
    public async Task SetImageMaxSizeTests()
    {
        const int imageMaxSize = 51200;
        const int updatedImageMaxSize = 1;

        await InitializeTests();

        {
            var output = await SchrodingerContractStub.GetConfig.CallAsync(new Empty());
            output.ImageMaxSize.ShouldBe(imageMaxSize);
        }

        {
            var result = await SchrodingerContractStub.SetImageMaxSize.SendAsync(new Int64Value
            {
                Value = updatedImageMaxSize
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<ImageMaxSizeSet>(result.TransactionResult);
            log.ImageMaxSize.ShouldBe(updatedImageMaxSize);
        }

        {
            var output = await SchrodingerContractStub.GetConfig.CallAsync(new Empty());
            output.ImageMaxSize.ShouldBe(updatedImageMaxSize);
        }

        {
            var result = await SchrodingerContractStub.SetImageMaxSize.SendAsync(new Int64Value
            {
                Value = updatedImageMaxSize
            });

            var log = GetLogEvent<ImageMaxSizeSet>(result.TransactionResult);
            log.ShouldBe(new ImageMaxSizeSet());
        }
    }

    [Fact]
    public async Task SetImageMaxSizeTests_Fail()
    {
        await InitializeTests();

        {
            var result = await UserSchrodingerContractStub.SetImageMaxSize.SendWithExceptionAsync(new Int64Value());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await SchrodingerContractStub.SetImageMaxSize.SendWithExceptionAsync(new Int64Value());
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
    }

    [Fact]
    public async Task SetImageMaxCountTests()
    {
        const int imageMaxCount = 2;
        const int updatedImageMaxCount = 1;

        await InitializeTests();

        {
            var output = await SchrodingerContractStub.GetConfig.CallAsync(new Empty());
            output.ImageMaxCount.ShouldBe(imageMaxCount);
        }

        {
            var result = await SchrodingerContractStub.SetImageMaxCount.SendAsync(new Int64Value
            {
                Value = updatedImageMaxCount
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<ImageMaxCountSet>(result.TransactionResult);
            log.ImageMaxCount.ShouldBe(updatedImageMaxCount);
        }

        {
            var output = await SchrodingerContractStub.GetConfig.CallAsync(new Empty());
            output.ImageMaxCount.ShouldBe(updatedImageMaxCount);
        }

        {
            var result = await SchrodingerContractStub.SetImageMaxCount.SendAsync(new Int64Value
            {
                Value = updatedImageMaxCount
            });

            var log = GetLogEvent<ImageMaxCountSet>(result.TransactionResult);
            log.ShouldBe(new ImageMaxCountSet());
        }
    }

    [Fact]
    public async Task SetImageMaxCountTests_Fail()
    {
        await InitializeTests();

        {
            var result = await UserSchrodingerContractStub.SetImageMaxCount.SendWithExceptionAsync(new Int64Value());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await SchrodingerContractStub.SetImageMaxCount.SendWithExceptionAsync(new Int64Value());
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
    }

    [Fact]
    public async Task SetImageUriMaxSizeTests()
    {
        const int imageUriMaxSize = 64;
        const int updatedImageUriMaxSize = 1;

        await InitializeTests();

        {
            var output = await SchrodingerContractStub.GetConfig.CallAsync(new Empty());
            output.ImageUriMaxSize.ShouldBe(imageUriMaxSize);
        }

        {
            var result = await SchrodingerContractStub.SetImageUriMaxSize.SendAsync(new Int64Value
            {
                Value = updatedImageUriMaxSize
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<ImageUriMaxSizeSet>(result.TransactionResult);
            log.ImageUriMaxSize.ShouldBe(updatedImageUriMaxSize);
        }

        {
            var output = await SchrodingerContractStub.GetConfig.CallAsync(new Empty());
            output.ImageUriMaxSize.ShouldBe(updatedImageUriMaxSize);
        }

        {
            var result = await SchrodingerContractStub.SetImageUriMaxSize.SendAsync(new Int64Value
            {
                Value = updatedImageUriMaxSize
            });

            var log = GetLogEvent<ImageUriMaxSizeSet>(result.TransactionResult);
            log.ShouldBe(new ImageUriMaxSizeSet());
        }
    }

    [Fact]
    public async Task SetImageUriMaxSizeTests_Fail()
    {
        await InitializeTests();

        {
            var result = await UserSchrodingerContractStub.SetImageUriMaxSize.SendWithExceptionAsync(new Int64Value());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await SchrodingerContractStub.SetImageUriMaxSize.SendWithExceptionAsync(new Int64Value());
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
    }

    [Fact]
    public async Task SetAdminTests()
    {
        await InitializeTests();

        {
            var output = await SchrodingerContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(DefaultAddress);
        }

        {
            var result = await SchrodingerContractStub.SetAdmin.SendAsync(UserAddress);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<AdminSet>(result.TransactionResult);
            log.Admin.ShouldBe(UserAddress);
        }

        {
            var output = await SchrodingerContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(UserAddress);
        }

        {
            var result = await UserSchrodingerContractStub.SetAdmin.SendAsync(UserAddress);

            var log = GetLogEvent<AdminSet>(result.TransactionResult);
            log.ShouldBe(new AdminSet());
        }
    }

    [Fact]
    public async Task SetAdminTests_Fail()
    {
        await InitializeTests();

        {
            var result = await UserSchrodingerContractStub.SetAdmin.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await SchrodingerContractStub.SetAdmin.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
    }

    [Fact]
    public async Task SetAttributeConfigTests()
    {
        const long traitTypeMaxCount = 50;
        const long traitValueMaxCount = 200;
        const long attributeMaxLength = 80;
        const int maxAttributesPerGen = 5;
        const long fixedTraitTypeMaxCount = 5;

        const long updatedTraitTypeMaxCount = 1;
        const long updatedTraitValueMaxCount = 1;
        const long updatedAttributeMaxLength = 1;
        const int updatedMaxAttributesPerGen = 1;
        const long updatedFixedTraitTypeMaxCount = 1;

        var input = new SetAttributeConfigInput
        {
            TraitTypeMaxCount = updatedTraitTypeMaxCount,
            TraitValueMaxCount = updatedTraitValueMaxCount,
            AttributeMaxLength = updatedAttributeMaxLength,
            MaxAttributesPerGen = updatedMaxAttributesPerGen,
            FixedTraitTypeMaxCount = updatedFixedTraitTypeMaxCount
        };

        await InitializeTests();

        {
            var output = await SchrodingerContractStub.GetConfig.CallAsync(new Empty());
            output.TraitTypeMaxCount.ShouldBe(traitTypeMaxCount);
            output.TraitValueMaxCount.ShouldBe(traitValueMaxCount);
            output.AttributeMaxLength.ShouldBe(attributeMaxLength);
            output.MaxAttributesPerGen.ShouldBe(maxAttributesPerGen);
            output.FixedTraitTypeMaxCount.ShouldBe(fixedTraitTypeMaxCount);
        }

        {
            var result = await SchrodingerContractStub.SetAttributeConfig.SendAsync(input);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<AttributeConfigSet>(result.TransactionResult);
            log.TraitTypeMaxCount.ShouldBe(updatedTraitTypeMaxCount);
            log.TraitValueMaxCount.ShouldBe(updatedTraitValueMaxCount);
            log.AttributeMaxLength.ShouldBe(updatedAttributeMaxLength);
            log.MaxAttributesPerGen.ShouldBe(updatedMaxAttributesPerGen);
            log.FixedTraitTypeMaxCount.ShouldBe(updatedFixedTraitTypeMaxCount);
        }

        {
            var output = await SchrodingerContractStub.GetConfig.CallAsync(new Empty());
            output.TraitTypeMaxCount.ShouldBe(updatedTraitTypeMaxCount);
            output.TraitValueMaxCount.ShouldBe(updatedTraitValueMaxCount);
            output.AttributeMaxLength.ShouldBe(updatedAttributeMaxLength);
            output.MaxAttributesPerGen.ShouldBe(updatedMaxAttributesPerGen);
            output.FixedTraitTypeMaxCount.ShouldBe(updatedFixedTraitTypeMaxCount);
        }

        {
            var result = await SchrodingerContractStub.SetAttributeConfig.SendAsync(input);

            var log = GetLogEvent<AttributeConfigSet>(result.TransactionResult);
            log.ShouldBe(new AttributeConfigSet());
        }
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0, "Invalid trait type max count.")]
    [InlineData(10, 0, 0, 0, 0, "Invalid trait value max count.")]
    [InlineData(10, 10, 0, 0, 0, "Invalid attribute max length.")]
    [InlineData(10, 10, 10, 0, 0, "Invalid max attributes per generation.")]
    [InlineData(10, 10, 10, 10, 0, "Invalid fixed trait type max count.")]
    public async Task SetAttributeConfigTests_Fail(long traitTypeMaxCount, long traitValueMaxCount,
        long attributeMaxLength,
        int maxAttributesPerGen, long fixedTraitTypeMaxCount, string error)
    {
        await InitializeTests();

        {
            var result =
                await UserSchrodingerContractStub.SetAttributeConfig.SendWithExceptionAsync(
                    new SetAttributeConfigInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await SchrodingerContractStub.SetAttributeConfig.SendWithExceptionAsync(
                new SetAttributeConfigInput
                {
                    TraitTypeMaxCount = traitTypeMaxCount,
                    TraitValueMaxCount = traitValueMaxCount,
                    AttributeMaxLength = attributeMaxLength,
                    MaxAttributesPerGen = maxAttributesPerGen,
                    FixedTraitTypeMaxCount = fixedTraitTypeMaxCount
                });
            result.TransactionResult.Error.ShouldContain(error);
        }
    }
}