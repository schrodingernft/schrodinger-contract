using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Schrodinger;

public partial class SchrodingerContract
{
    public override Empty SetConfig(Config input)
    {
        CheckAdminPermission();

        ValidateConfigInput(input);
        if (State.Config.Value.Equals(input)) return new Empty();

        State.Config.Value = input;

        Context.Fire(new ConfigSet
        {
            Config = input
        });

        return new Empty();
    }

    private void ValidateConfigInput(Config input)
    {
        Assert(input != null, "Invalid input.");
        Assert(input.MaxGen > 0, "Invalid max generation.");
        Assert(input.ImageMaxSize > 0, "Invalid image max size.");
        Assert(input.ImageMaxCount > 0, "Invalid image max count.");
        Assert(input.TraitTypeMaxCount > 0, "Invalid trait type max count.");
        Assert(input.TraitValueMaxCount > 0, "Invalid trait value max count.");
        Assert(input.AttributeMaxLength > 0, "Invalid attribute max length.");
        Assert(IsAddressValid(input.Signatory), "Invalid signatory.");
        Assert(input.MaxAttributesPerGen > 0, "Invalid max attributes per generation.");
    }

    public override Empty SetMaxGenerationConfig(Int32Value input)
    {
        CheckAdminPermission();

        Assert(input != null && input.Value > 0, "Invalid input.");

        if (State.Config.Value.MaxGen == input.Value) return new Empty();

        State.Config.Value.MaxGen = input.Value;

        Context.Fire(new MaxGenerationConfigSet
        {
            MaxGen = input.Value
        });

        return new Empty();
    }

    public override Empty SetImageMaxSize(Int64Value input)
    {
        CheckAdminPermission();

        Assert(input != null && input.Value > 0, "Invalid input.");

        if (State.Config.Value.ImageMaxSize == input.Value) return new Empty();

        State.Config.Value.ImageMaxSize = input.Value;

        Context.Fire(new ImageMaxSizeSet
        {
            ImageMaxSize = input.Value
        });

        return new Empty();
    }

    public override Empty SetImageMaxCount(Int64Value input)
    {
        CheckAdminPermission();

        Assert(input != null && input.Value > 0, "Invalid input.");

        if (State.Config.Value.ImageMaxCount == input.Value) return new Empty();

        State.Config.Value.ImageMaxCount = input.Value;

        Context.Fire(new ImageMaxCountSet
        {
            ImageMaxCount = input.Value
        });

        return new Empty();
    }

    public override Empty SetAttributeConfig(SetAttributeConfigInput input)
    {
        CheckAdminPermission();

        Assert(input != null, "Invalid input.");
        Assert(input.TraitTypeMaxCount > 0, "Invalid trait type max count.");
        Assert(input.TraitValueMaxCount > 0, "Invalid trait value max count.");
        Assert(input.AttributeMaxLength > 0, "Invalid attribute max length.");
        Assert(input.MaxAttributesPerGen > 0, "Invalid max attributes per generation.");

        if (State.Config.Value.TraitTypeMaxCount == input.TraitTypeMaxCount &&
            State.Config.Value.TraitValueMaxCount == input.TraitValueMaxCount &&
            State.Config.Value.AttributeMaxLength == input.AttributeMaxLength &&
            State.Config.Value.MaxAttributesPerGen == input.MaxAttributesPerGen) return new Empty();

        State.Config.Value.TraitTypeMaxCount = input.TraitTypeMaxCount;
        State.Config.Value.TraitValueMaxCount = input.TraitValueMaxCount;
        State.Config.Value.AttributeMaxLength = input.AttributeMaxLength;
        State.Config.Value.MaxAttributesPerGen = input.MaxAttributesPerGen;

        Context.Fire(new AttributeConfigSet
        {
            TraitTypeMaxCount = input.TraitTypeMaxCount,
            TraitValueMaxCount = input.TraitValueMaxCount,
            AttributeMaxLength = input.AttributeMaxLength,
            MaxAttributesPerGen = input.MaxAttributesPerGen
        });

        return new Empty();
    }

    public override Empty SetSignatoryConfig(Address input)
    {
        CheckAdminPermission();

        Assert(IsAddressValid(input), "Invalid input.");

        if (State.Config.Value.Signatory == input) return new Empty();

        State.Config.Value.Signatory = input;

        Context.Fire(new SignatoryConfigSet
        {
            Signatory = input
        });

        return new Empty();
    }
    
    public override Empty SetAdmin(Address input)
    {
        CheckAdminPermission();
        
        Assert(IsAddressValid(input), "Invalid input.");

        if (State.Admin.Value == input) return new Empty();
        
        State.Admin.Value = input;
        
        Context.Fire(new AdminSet
        {
            OldAdmin = Context.Sender,
            NewAdmin = input
        });

        return new Empty();
    }
}