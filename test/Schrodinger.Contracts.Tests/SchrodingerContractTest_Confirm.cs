using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace Schrodinger;

public partial class SchrodingerContractTests
{
    [Fact]
    public async Task ConfirmTests()
    {
        const string parent = Tick + "-1";
        const string symbol = Tick + "-2";

        var adoptId = await AdoptTests();

        GetBalance(DefaultAddress, symbol).Result.ShouldBe(0);

        var input = new ConfirmInput
        {
            AdoptId = adoptId,
            Image = "image",
            ImageUri = "imageUri",
            Signature = GenerateSignature(DefaultKeyPair.PrivateKey, adoptId, "image", "imageUri")
        };

        var result = await SchrodingerContractStub.Confirm.SendAsync(input);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var log = GetLogEvent<Confirmed>(result.TransactionResult);
        log.AdoptId.ShouldBe(adoptId);
        log.Parent.ShouldBe(parent);
        log.Symbol.ShouldBe(symbol);
        log.TotalSupply.ShouldBe(5000);
        log.Decimals.ShouldBe(0);
        log.Gen.ShouldBe(1);
        log.Attributes.Data.Count.ShouldBe(1);
        log.Issuer.ShouldBe(SchrodingerContractAddress);
        log.Owner.ShouldBe(SchrodingerContractAddress);
        log.IssueChainId.ShouldBe(ChainHelper.ConvertBase58ToChainId("AELF"));
        log.TokenName.ShouldBe(symbol + "GEN1");
        log.Deployer.ShouldBe(DefaultAddress);
        log.ImageUri.ShouldBe("imageUri");

        GetBalance(DefaultAddress, symbol).Result.ShouldBe(5000);
        GetTokenInfo(symbol).Result.ExternalInfo.ShouldBe(new ExternalInfo
        {
            Value = { log.ExternalInfos.Value }
        });

        {
            var output = await SchrodingerContractStub.GetAdoptInfo.CallAsync(adoptId);
            output.IsConfirmed.ShouldBeTrue();
        }
        {
            var output = await SchrodingerContractStub.GetTick.CallAsync(new StringValue
            {
                Value = symbol
            });
            output.Value.ShouldBe(Tick);
        }
        {
            var output = await SchrodingerContractStub.GetParent.CallAsync(new StringValue
            {
                Value = symbol
            });
            output.Value.ShouldBe(parent);
        }
        {
            var output = await SchrodingerContractStub.GetTokenInfo.CallAsync(new StringValue
            {
                Value = symbol
            });

            output.AdoptId.ShouldBe(adoptId);
            output.Parent.ShouldBe(parent);
            output.ParentGen.ShouldBe(0);
            output.ParentAttributes.Data.Count.ShouldBe(0);
            output.Attributes.Data.Count.ShouldBe(1);
            output.Gen.ShouldBe(1);
        }
    }

    [Fact]
    public async Task ConfirmTests_Fail()
    {
        var adoptId = await AdoptTests();

        {
            var result = await SchrodingerContractStub.Confirm.SendWithExceptionAsync(new ConfirmInput());
            result.TransactionResult.Error.ShouldContain("Invalid input adopt id.");
        }
        {
            var result = await SchrodingerContractStub.Confirm.SendWithExceptionAsync(new ConfirmInput
            {
                AdoptId = Hash.Empty
            });
            result.TransactionResult.Error.ShouldContain("Invalid input image uri.");
        }
        {
            var result = await SchrodingerContractStub.Confirm.SendWithExceptionAsync(new ConfirmInput
            {
                AdoptId = Hash.Empty,
                ImageUri = "imageUri"
            });
            result.TransactionResult.Error.ShouldContain("Invalid input signature.");
        }
        {
            var result = await SchrodingerContractStub.Confirm.SendWithExceptionAsync(new ConfirmInput
            {
                AdoptId = Hash.Empty,
                ImageUri = "imageUri",
                Signature = HashHelper.ComputeFrom(1).ToByteString()
            });
            result.TransactionResult.Error.ShouldContain("Invalid image data.");
        }
        {
            await SchrodingerContractStub.SetImageMaxSize.SendAsync(new Int64Value
            {
                Value = 1
            });
            await SchrodingerContractStub.SetImageUriMaxSize.SendAsync(new Int64Value
            {
                Value = 1
            });

            var result = await SchrodingerContractStub.Confirm.SendWithExceptionAsync(new ConfirmInput
            {
                AdoptId = Hash.Empty,
                ImageUri = "imageUri",
                Signature = HashHelper.ComputeFrom(1).ToByteString()
            });
            result.TransactionResult.Error.ShouldContain("Invalid image data.");
        }
        {
            var result = await SchrodingerContractStub.Confirm.SendWithExceptionAsync(new ConfirmInput
            {
                AdoptId = Hash.Empty,
                Image = "image",
                ImageUri = "imageUri",
                Signature = HashHelper.ComputeFrom(1).ToByteString()
            });
            result.TransactionResult.Error.ShouldContain("Invalid image data.");
        }
        {
            var result = await SchrodingerContractStub.Confirm.SendWithExceptionAsync(new ConfirmInput
            {
                AdoptId = Hash.Empty,
                Image = "i",
                ImageUri = "imageUri",
                Signature = HashHelper.ComputeFrom(1).ToByteString()
            });
            result.TransactionResult.Error.ShouldContain("Invalid image uri.");
        }
        {
            var result = await SchrodingerContractStub.Confirm.SendWithExceptionAsync(new ConfirmInput
            {
                AdoptId = Hash.Empty,
                Image = "i",
                ImageUri = "i",
                Signature = HashHelper.ComputeFrom(1).ToByteString()
            });
            result.TransactionResult.Error.ShouldContain("Adopt id not exists.");
        }
        {
            var result = await UserSchrodingerContractStub.Confirm.SendWithExceptionAsync(new ConfirmInput
            {
                AdoptId = adoptId,
                Image = "i",
                ImageUri = "i",
                Signature = HashHelper.ComputeFrom(1).ToByteString()
            });
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await SchrodingerContractStub.Confirm.SendWithExceptionAsync(new ConfirmInput
            {
                AdoptId = adoptId,
                Image = "i",
                ImageUri = "i",
                Signature = HashHelper.ComputeFrom(1).ToByteString()
            });
            result.TransactionResult.Error.ShouldContain("Invalid signature.");
        }
        {
            var result = await SchrodingerContractStub.Confirm.SendWithExceptionAsync(new ConfirmInput
            {
                AdoptId = adoptId,
                Image = "i",
                ImageUri = "i",
                Signature = GenerateSignature(UserKeyPair.PrivateKey, adoptId, "i", "i")
            });
            result.TransactionResult.Error.ShouldContain("Not authorized.");
        }
        {
            await SchrodingerContractStub.Confirm.SendAsync(new ConfirmInput
            {
                AdoptId = adoptId,
                Image = "i",
                ImageUri = "i",
                Signature = GenerateSignature(DefaultKeyPair.PrivateKey, adoptId, "i", "i")
            });

            var result = await SchrodingerContractStub.Confirm.SendWithExceptionAsync(new ConfirmInput
            {
                AdoptId = adoptId,
                Image = "i",
                ImageUri = "i",
                Signature = HashHelper.ComputeFrom(1).ToByteString()
            });
            result.TransactionResult.Error.ShouldContain("Adopt id already confirmed.");
        }
    }
}