using System.Threading.Tasks;
using AElf.Types;
using Shouldly;
using Xunit;

namespace Schrodinger;

public partial class SchrodingerContractTests
{
    [Fact]
    public async Task RerollTests()
    {
        const string ancestor = Tick + "-1";
        const string symbol = Tick + "-2";

        await ConfirmTests();
        await Approve(5000, SchrodingerContractAddress, symbol);

        GetBalance(DefaultAddress, ancestor).Result.ShouldBe(0);
        GetBalance(DefaultAddress, symbol).Result.ShouldBe(5000);

        var result = await SchrodingerContractStub.Reroll.SendAsync(new RerollInput
        {
            Symbol = symbol,
            Amount = 5000,
            Domain = Domain
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var log = GetLogEvent<Rerolled>(result.TransactionResult);
        log.Symbol.ShouldBe(symbol);
        log.Amount.ShouldBe(5000);
        log.Ancestor.ShouldBe(ancestor);
        log.Recipient.ShouldBe(DefaultAddress);

        GetBalance(DefaultAddress, ancestor).Result.ShouldBe(5000);
        GetBalance(DefaultAddress, symbol).Result.ShouldBe(0);
    }

    [Fact]
    public async Task RerollTests_Fail()
    {
        const string ancestor = Tick + "-1";
        const string symbol = Tick + "-2";

        await ConfirmTests();
        await Approve(5000, SchrodingerContractAddress, symbol);

        {
            var result = await SchrodingerContractStub.Reroll.SendWithExceptionAsync(new RerollInput());
            result.TransactionResult.Error.ShouldContain("Invalid input symbol.");
        }
        {
            var result = await SchrodingerContractStub.Reroll.SendWithExceptionAsync(new RerollInput
            {
                Symbol = "test-1"
            });
            result.TransactionResult.Error.ShouldContain("Invalid input amount.");
        }
        {
            var result = await SchrodingerContractStub.Reroll.SendWithExceptionAsync(new RerollInput
            {
                Symbol = "test-1",
                Amount = 1
            });
            result.TransactionResult.Error.ShouldContain("Invalid input domain.");
        }
        {
            var result = await SchrodingerContractStub.Reroll.SendWithExceptionAsync(new RerollInput
            {
                Symbol = "test-1",
                Amount = 1,
                Domain = Domain
            });
            result.TransactionResult.Error.ShouldContain("Tick not deployed.");
        }
        {
            var result = await SchrodingerContractStub.Reroll.SendWithExceptionAsync(new RerollInput
            {
                Symbol = ancestor,
                Amount = 1,
                Domain = Domain
            });
            result.TransactionResult.Error.ShouldContain("Can not reroll gen0.");
        }
    }
}