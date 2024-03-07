using System;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace Schrodinger;

public partial class SchrodingerContractTests : SchrodingerContractTestBase
{
    [Fact]
    public async Task InitializeTests()
    {
        //initialize not deployer
        var result =  await SchrodingerContractUser2Stub.Initialize.SendWithExceptionAsync(new InitializeInput
        {
            Admin = DefaultAddress
        });
        result.TransactionResult.Error.ShouldContain("No permission");
        
        // initialize is deployer
        await Initialize();
        var admin = await SchrodingerContractStub.GetAdmin.CallAsync(new Empty());
        admin.ShouldBe(DefaultAddress);
        
        // initialize twice
        result = await SchrodingerContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
        {
        });
        result.TransactionResult.Error.ShouldContain("Already initialized.");
    }
    
    private async Task Initialize()
    {
        var init = new InitializeInput
        {
            Admin = DefaultAddress
        };
        await SchrodingerContractStub.Initialize.SendAsync(init);
    }
    
    [Fact]
    public async Task SetAdmin()
    {
        await Initialize();
        var result = await SchrodingerContractUserStub.SetAdmin.SendWithExceptionAsync(DefaultAddress);
        result.TransactionResult.Error.ShouldContain("No permission.");
            
        try
        {
            result = await SchrodingerContractStub.SetAdmin.SendAsync(null);
        }catch (Exception e)
        {
            Assert.Contains("Value cannot be null", e.Message);
        }
            
        await SchrodingerContractStub.SetAdmin.SendAsync(UserAddress);
        var admin = await SchrodingerContractStub.GetAdmin.CallAsync(new Empty());
        admin.ShouldBe(UserAddress);
            
        await SchrodingerContractUserStub.SetAdmin.SendAsync(DefaultAddress);
        admin = await SchrodingerContractStub.GetAdmin.CallAsync(new Empty());
        admin.ShouldBe(DefaultAddress);
    }
    
    [Fact]
    public async Task InitializeTests_All()
    {
        //initialize not deployer
        var result =  await SchrodingerContractUser2Stub.Initialize.SendWithExceptionAsync(new InitializeInput
        {
            Admin = DefaultAddress,
            PointsContractDappId = new Hash(),
            PointsContract = DefaultAddress
        });
        result.TransactionResult.Error.ShouldContain("No permission");
        
        // initialize is deployer
        var init = new InitializeInput
        {
            Admin = DefaultAddress,
            PointsContractDappId = new Hash(),
            PointsContract = DefaultAddress
        };
        await SchrodingerContractStub.Initialize.SendAsync(init);
        var admin = await SchrodingerContractStub.GetAdmin.CallAsync(new Empty());
        admin.ShouldBe(DefaultAddress);
        var PointsContractDappId = await SchrodingerContractStub.GetPointsContractDAppId.CallAsync(new Empty());
        PointsContractDappId.ShouldNotBe(null);
        var PointsContract = await SchrodingerContractStub.GetPointsContract.CallAsync(new Empty());
        PointsContract.Value.ShouldBe(DefaultAddress.Value);
        
        // initialize twice
        result = await SchrodingerContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
        {
        });
        result.TransactionResult.Error.ShouldContain("Already initialized.");
        
        //set PointsContract
        var resultPointsContract =  await SchrodingerContractUser2Stub.SetPointsContract.SendWithExceptionAsync(DefaultAddress);
        resultPointsContract.TransactionResult.Error.ShouldContain("No permission");
        
        resultPointsContract =  await SchrodingerContractStub.SetPointsContract.SendWithExceptionAsync(DefaultAddress);
        resultPointsContract.TransactionResult.Error.ShouldContain("has Configured");
        
        //set PointsContractDappId
        var resultPointsContractDappId =  await SchrodingerContractUser2Stub.SetPointsContractDAppId.SendWithExceptionAsync(new Hash());
        resultPointsContractDappId.TransactionResult.Error.ShouldContain("No permission");
        
        resultPointsContractDappId =  await SchrodingerContractStub.SetPointsContractDAppId.SendWithExceptionAsync(HashHelper.ComputeFrom("points"));
        resultPointsContractDappId.TransactionResult.Error.ShouldContain("has Configured");
    }
}