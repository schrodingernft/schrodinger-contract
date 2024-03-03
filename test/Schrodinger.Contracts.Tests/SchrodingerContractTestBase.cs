using System.IO;
using AElf.Boilerplate.TestBase;
using AElf.Boilerplate.TestBase.SmartContractNameProviders;
using AElf.Contracts.MultiToken;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Points.Contracts.Point;
using Volo.Abp.Threading;

namespace Schrodinger;

public class SchrodingerContractTestBase : DAppContractTestBase<SchrodingerContractTestModule>
{
    internal ACS0Container.ACS0Stub ZeroContractStub { get; set; }
    internal Address SchrodingerContractAddress { get; set; }
    
    internal Address PointsContractAddress => GetAddress(PointsSmartContractAddressNameProvider.StringName);

    internal SchrodingerContractContainer.SchrodingerContractStub SchrodingerContractStub { get; set; }
    internal SchrodingerContractContainer.SchrodingerContractStub SchrodingerContractUserStub { get; set; }
    internal SchrodingerContractContainer.SchrodingerContractStub SchrodingerContractUser2Stub { get; set; }
    
    internal PointsContractContainer.PointsContractStub PointsContractStub { get; set; }

    protected ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
    protected Address DefaultAddress => Accounts[0].Address;
    protected ECKeyPair UserKeyPair => Accounts[1].KeyPair;
    protected Address UserAddress => Accounts[1].Address;
    protected ECKeyPair User2KeyPair => Accounts[2].KeyPair;
    protected Address User2Address => Accounts[2].Address;
    protected ECKeyPair User3KeyPair => Accounts[3].KeyPair;
    protected Address User3Address => Accounts[3].Address;

    protected readonly IBlockTimeProvider BlockTimeProvider;

    protected SchrodingerContractTestBase()
    {
        BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();

        ZeroContractStub = GetContractZeroTester(DefaultKeyPair);

        var result = AsyncHelper.RunSync(async () => await ZeroContractStub.DeploySmartContract.SendAsync(
            new ContractDeploymentInput
            {
                Category = KernelConstants.CodeCoverageRunnerCategory,
                Code = ByteString.CopyFrom(
                    File.ReadAllBytes(typeof(SchrodingerContract).Assembly.Location))
            }));

        SchrodingerContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);

        SchrodingerContractStub = GetSchrodingerContractContainerStub(DefaultKeyPair);
        SchrodingerContractUserStub = GetSchrodingerContractContainerStub(UserKeyPair);
        SchrodingerContractUser2Stub = GetSchrodingerContractContainerStub(User2KeyPair);

        //PointsContractStub = GetPointsContractStub(DefaultKeyPair);
    }

    internal SchrodingerContractContainer.SchrodingerContractStub GetSchrodingerContractContainerStub(ECKeyPair senderKeyPair)
        => GetTester<SchrodingerContractContainer.SchrodingerContractStub>(SchrodingerContractAddress, senderKeyPair);
    
    internal PointsContractContainer.PointsContractStub GetPointsContractStub(ECKeyPair senderKeyPair)
    {
        return GetTester<PointsContractContainer.PointsContractStub>(PointsContractAddress, senderKeyPair);
    }

    private ACS0Container.ACS0Stub GetContractZeroTester(ECKeyPair senderKeyPair)
        => GetTester<ACS0Container.ACS0Stub>(BasicContractZeroAddress, senderKeyPair);
}