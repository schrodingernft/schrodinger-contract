using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Cryptography;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using Shouldly;

namespace Schrodinger;

public partial class SchrodingerContractTests
{
    private T GetLogEvent<T>(TransactionResult transactionResult) where T : IEvent<T>, new()
    {
        var log = transactionResult.Logs.FirstOrDefault(l => l.Name == typeof(T).Name);
        log.ShouldNotBeNull();

        var logEvent = new T();
        logEvent.MergeFrom(log.NonIndexed);

        return logEvent;
    }

    private string GenerateSignature(byte[] privateKey, Hash adoptId, string image)
    {
        var data = new ConfirmInput
        {
            AdoptId = adoptId,
            Image = image
        };
        var dataHash = HashHelper.ComputeFrom(data);
        var signature = CryptoHelper.SignWithPrivateKey(privateKey, dataHash.ToByteArray());
        return signature.ToHex();
    }

    private async Task<long> GetTokenBalance(string symbol, Address sender)
    {
        var output = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = sender,
            Symbol = symbol
        });

        return output.Balance;
    }
}