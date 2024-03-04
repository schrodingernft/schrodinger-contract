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
    
    [Fact]
    public async Task Join_Test()
    {
        /*
         await ApplyDomain();
        //join
        await SchrodingerContractStub.Join.SendAsync(new JoinInput()
        {
            Domain = DOMAIN
        });
        
        //get join
        var joinRecord = await SchrodingerContractStub.GetJoinRecord.CallAsync(DefaultAddress);
        joinRecord.Value.ShouldBe(true);
        
        //join twice
        var result = await SchrodingerContractStub.Join.SendWithExceptionAsync(new JoinInput()
        {
            Domain = DOMAIN
        });
        result.TransactionResult.Error.ShouldContain("you have joined");
        */
    }

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