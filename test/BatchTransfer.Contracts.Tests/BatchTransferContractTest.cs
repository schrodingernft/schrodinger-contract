using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace BatchTransfer;

public class BatchTransferContractTests : BatchTransferContractTestBase
{
    [Fact]
    public async Task Initialize()
    {
        await BatchTransferContractStub.Initialize.SendAsync(DefaultAddress);
        var manager = await BatchTransferContractStub.GetManager.CallAsync(new Empty());
        manager.ShouldBe(DefaultAddress);
    }
    
    [Fact]
    public async Task Initialize_Failed()
    {
        var executionResult = await BatchTransferContract1Stub.Initialize.SendWithExceptionAsync(DefaultAddress);
        executionResult.TransactionResult.Error.ShouldContain("No permission.");
    }

    [Fact]
    public async Task SetManager()
    {
        await Initialize();
        await BatchTransferContractStub.SetManager.SendAsync(User1Address);
        var manager = await BatchTransferContractStub.GetManager.CallAsync(new Empty());
        manager.ShouldBe(User1Address);
        var exception = await BatchTransferContract1Stub.SetManager.SendWithExceptionAsync(User1Address);
        exception.TransactionResult.Error.ShouldContain("No permission.");
    }

    [Fact]
    public async Task BatchTransfer()
    {
        await Initialize();
        await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            To = BatchTransferContractAddress,
            Amount = 100000_00000000,
            Symbol = "ELF"
        });
        var balanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = BatchTransferContractAddress,
            Symbol = "ELF"
        });
        balanceOutput.Balance.ShouldBe(100000_00000000);
        var user1BalanceBefore = await GetBalance(User1Address);
        user1BalanceBefore.ShouldBe(0);
        var executionResult = await BatchTransferContract1Stub.BatchTransfer.SendWithExceptionAsync(new BatchTransferInput
        {
            Value =
            {
                new TransferInfo
                {
                    To = User1Address,
                    Symbol = "ELF",
                    Amount = 100_00000000
                }
            }
        });
        executionResult.TransactionResult.Error.ShouldContain("No permission.");
        await BatchTransferContractStub.BatchTransfer.SendAsync(new BatchTransferInput
        {
            Value =
            {
                new TransferInfo
                {
                    To = User1Address,
                    Symbol = "ELF",
                    Amount = 100_00000000
                },
                new TransferInfo
                {
                    To = User2Address,
                    Symbol = "ELF",
                    Amount = 200_00000000
                },
                new TransferInfo
                {
                    To = User3Address,
                    Symbol = "ELF",
                    Amount = 300_00000000
                },
                new TransferInfo
                {
                    To = User4Address,
                    Symbol = "ELF",
                    Amount = 400_00000000
                },
                new TransferInfo
                {
                    To = User5Address,
                    Symbol = "ELF",
                    Amount = 500_00000000
                },
                new TransferInfo
                {
                    To = User6Address,
                    Symbol = "ELF",
                    Amount = 600_00000000
                },
                new TransferInfo
                {
                    To = User7Address,
                    Symbol = "ELF",
                    Amount = 700_00000000
                }
            }
        });
        var user1Balance = await GetBalance(User1Address);
        user1Balance.ShouldBe(100_00000000);
        var user2Balance = await GetBalance(User2Address);
        user2Balance.ShouldBe(200_00000000);
        var user3Balance = await GetBalance(User3Address);
        user3Balance.ShouldBe(300_00000000);

        var user4Balance = await GetBalance(User4Address);
        user4Balance.ShouldBe(400_00000000);

        var user5Balance = await GetBalance(User5Address);
        user5Balance.ShouldBe(500_00000000);

        var user6Balance = await GetBalance(User6Address);
        user6Balance.ShouldBe(600_00000000);

        var user7Balance = await GetBalance(User7Address);
        user7Balance.ShouldBe(700_00000000);

        var contractBalance = await GetBalance(BatchTransferContractAddress);
        contractBalance.ShouldBe(100000_00000000 - 2800_00000000);

    }

    private async Task<long> GetBalance(Address user)
    {
        var balanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = user,
            Symbol = "ELF"
        });
        return balanceOutput.Balance;
    }

}