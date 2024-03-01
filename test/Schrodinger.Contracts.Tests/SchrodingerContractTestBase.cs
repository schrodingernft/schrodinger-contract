using AElf.Boilerplate.TestBase;

namespace Schrodinger;

public class SchrodingerContractTestBase : DAppContractTestBase<SchrodingerContractTestModule>
{
    /*internal ACS0Container.ACS0Stub ZeroContractStub { get; set; }
    internal Address PointsContractAddress { get; set; }
    internal PointsContractContainer.PointsContractStub PointsContractStub { get; set; }
    internal PointsContractContainer.PointsContractStub PointsContractUserStub { get; set; }
    internal PointsContractContainer.PointsContractStub PointsContractUser2Stub { get; set; }

    protected ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
    protected Address DefaultAddress => Accounts[0].Address;
    protected ECKeyPair UserKeyPair => Accounts[1].KeyPair;
    protected Address UserAddress => Accounts[1].Address;
    protected ECKeyPair User2KeyPair => Accounts[2].KeyPair;
    protected Address User2Address => Accounts[2].Address;
    protected ECKeyPair User3KeyPair => Accounts[3].KeyPair;
    protected Address User3Address => Accounts[3].Address;

    protected readonly IBlockTimeProvider BlockTimeProvider;

    protected PointsContractTestBase()
    {
        BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();

        ZeroContractStub = GetContractZeroTester(DefaultKeyPair);

        var result = AsyncHelper.RunSync(async () => await ZeroContractStub.DeploySmartContract.SendAsync(
            new ContractDeploymentInput
            {
                Category = KernelConstants.CodeCoverageRunnerCategory,
                Code = ByteString.CopyFrom(
                    File.ReadAllBytes(typeof(PointsContract).Assembly.Location))
            }));

        PointsContractAddress = Address.Parser.ParseFrom(result.TransactionResult.ReturnValue);

        PointsContractStub = GetPointsContractContainerStub(DefaultKeyPair);
        PointsContractUserStub = GetPointsContractContainerStub(UserKeyPair);
        PointsContractUser2Stub = GetPointsContractContainerStub(User2KeyPair);
    }

    internal PointsContractContainer.PointsContractStub GetPointsContractContainerStub(ECKeyPair senderKeyPair)
        => GetTester<PointsContractContainer.PointsContractStub>(PointsContractAddress, senderKeyPair);

    private ACS0Container.ACS0Stub GetContractZeroTester(ECKeyPair senderKeyPair)
        => GetTester<ACS0Container.ACS0Stub>(BasicContractZeroAddress, senderKeyPair);*/
}