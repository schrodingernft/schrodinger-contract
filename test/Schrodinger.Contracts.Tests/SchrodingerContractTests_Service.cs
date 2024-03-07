using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Points.Contracts.Point;
using Shouldly;
using Xunit;

namespace Schrodinger;

public partial class SchrodingerContractTests :SchrodingerContractTestBase
{
    private const string DAPP_ID = "Schrodinger";
    private const string DOMAIN = "www.schrodinger.123.com";

    private async Task ApplyDomain()
    {
        await PointsContractStub.ApplyToOperator.SendAsync(new ApplyToOperatorInput()
        {
            Domain = DOMAIN,
            DappId = HashHelper.ComputeFrom(DAPP_ID),
            Invitee = DefaultAddress
        });

        var result = await PointsContractStub.GetDomainApplyInfo.CallAsync(new StringValue()
        {
            Value = DOMAIN
        });
        result.ShouldNotBe(null);
        result.Domain.ShouldBeUnique(DOMAIN);
        
    }
}