using AElf;
using AElf.Types;

namespace Schrodinger;

public partial class SchrodingerContract
{
    private void AssertAdmin()
    {
        Assert(Context.Sender == State.Admin.Value, "No permission.");
    }

    private void AssertInitialized()
    {
        Assert(State.Initialized.Value, "Not initialized.");
    }

    private bool IsAddressValid(Address input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }

    private bool IsHashValid(Hash input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }
}