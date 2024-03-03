using System;
using System.Linq;
using System.Threading.Tasks;
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
}