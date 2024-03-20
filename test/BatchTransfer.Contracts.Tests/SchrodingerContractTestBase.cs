using System.IO;
using System.Threading.Tasks;
using AElf;
using AElf.Boilerplate.TestBase;
using AElf.Contracts.MultiToken;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace BatchTransfer;

public class BatchTransferContractTestBase : DAppContractTestBase<BatchTransferContractTestModule>
{
    internal ACS0Container.ACS0Stub ZeroContractStub { get; set; }
    internal Address BatchTransferContractAddress { get; set; }

    internal BatchTransferContractContainer.BatchTransferContractStub BatchTransferContractStub { get; set; }
    
    internal BatchTransferContractContainer.BatchTransferContractStub BatchTransferContract1Stub { get; set; }


    internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
    
    protected ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
    protected Address DefaultAddress => Accounts[0].Address;
    protected ECKeyPair User1KeyPair => Accounts[1].KeyPair;
    protected Address User1Address => Accounts[1].Address;
    protected ECKeyPair User2KeyPair => Accounts[2].KeyPair;
    protected Address User2Address => Accounts[2].Address;
    protected ECKeyPair User3KeyPair => Accounts[3].KeyPair;
    protected Address User3Address => Accounts[3].Address;
    protected ECKeyPair User4KeyPair => Accounts[4].KeyPair;
    protected Address User4Address => Accounts[4].Address;
    protected ECKeyPair User5KeyPair => Accounts[5].KeyPair;
    protected Address User5Address => Accounts[5].Address;
    protected ECKeyPair User6KeyPair => Accounts[6].KeyPair;
    protected Address User6Address => Accounts[6].Address;
    protected ECKeyPair User7KeyPair => Accounts[7].KeyPair;
    protected Address User7Address => Accounts[7].Address;

    protected BatchTransferContractTestBase()
    {
      
        ZeroContractStub = GetContractStub<ACS0Container.ACS0Stub>(BasicContractZeroAddress, DefaultKeyPair);

        var result = AsyncHelper.RunSync(async () => await ZeroContractStub.DeploySmartContract.SendAsync(
            new ContractDeploymentInput
            {
                Category = KernelConstants.CodeCoverageRunnerCategory,
                Code = ByteString.CopyFrom(
                    File.ReadAllBytes(typeof(BatchTransferContract).Assembly.Location))
            }));

        
        BatchTransferContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);

        BatchTransferContractStub = GetContractStub<BatchTransferContractContainer.BatchTransferContractStub>(BatchTransferContractAddress,
            DefaultKeyPair);
        BatchTransferContract1Stub = GetContractStub<BatchTransferContractContainer.BatchTransferContractStub>(BatchTransferContractAddress,
            User2KeyPair);
        
        TokenContractStub =
            GetContractStub<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultKeyPair);
        
    }

    internal T GetContractStub<T>(Address contractAddress, ECKeyPair senderKeyPair)
        where T : ContractStubBase, new()
    {
        return GetTester<T>(contractAddress, senderKeyPair);
    }
    
}