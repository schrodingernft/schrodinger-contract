using System.IO;
using AElf;
using AElf.Boilerplate.TestBase;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace Schrodinger;

public class SchrodingerContractTestBase : DAppContractTestBase<SchrodingerContractTestModule>
{
    internal ACS0Container.ACS0Stub ZeroContractStub { get; set; }
    internal Address SchrodingerContractAddress { get; set; }

    internal SchrodingerContractContainer.SchrodingerContractStub SchrodingerContractStub { get; set; }
    internal SchrodingerContractContainer.SchrodingerContractStub UserSchrodingerContractStub { get; set; }
    internal SchrodingerContractContainer.SchrodingerContractStub User2SchrodingerContractStub { get; set; }

    protected ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
    protected Address DefaultAddress => Accounts[0].Address;
    protected ECKeyPair UserKeyPair => Accounts[1].KeyPair;
    protected Address UserAddress => Accounts[1].Address;
    protected ECKeyPair User2KeyPair => Accounts[2].KeyPair;
    protected Address User2Address => Accounts[2].Address;
    protected readonly IBlockTimeProvider BlockTimeProvider;

    protected SchrodingerContractTestBase()
    {
        BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();

        ZeroContractStub = GetContractStub<ACS0Container.ACS0Stub>(BasicContractZeroAddress, DefaultKeyPair);

        var result = AsyncHelper.RunSync(async () => await ZeroContractStub.DeploySmartContract.SendAsync(
            new ContractDeploymentInput
            {
                Category = KernelConstants.CodeCoverageRunnerCategory,
                Code = ByteString.CopyFrom(
                    File.ReadAllBytes(typeof(SchrodingerContract).Assembly.Location))
            }));

        SchrodingerContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);

        SchrodingerContractStub =
            GetContractStub<SchrodingerContractContainer.SchrodingerContractStub>(SchrodingerContractAddress,
                DefaultKeyPair);
        UserSchrodingerContractStub =
            GetContractStub<SchrodingerContractContainer.SchrodingerContractStub>(SchrodingerContractAddress,
                UserKeyPair);
        User2SchrodingerContractStub =
            GetContractStub<SchrodingerContractContainer.SchrodingerContractStub>(SchrodingerContractAddress,
                User2KeyPair);
    }

    private T GetContractStub<T>(Address contractAddress, ECKeyPair senderKeyPair)
        where T : ContractStubBase, new()
    {
        return GetTester<T>(contractAddress, senderKeyPair);
    }

    private ByteString GenerateContractSignature(byte[] privateKey, ContractOperation contractOperation)
    {
        var dataHash = HashHelper.ComputeFrom(contractOperation);
        var signature = CryptoHelper.SignWithPrivateKey(privateKey, dataHash.ToByteArray());
        return ByteStringHelper.FromHexString(signature.ToHex());
    }
}