using System.Linq;
using AElf.Types;
namespace Schrodinger;

public partial class SchrodingerContract
{
    private void AssertAdmin() => Assert(Context.Sender == State.Admin.Value, "No permission.");
    private void AssertInitialized() => Assert(State.Initialized.Value, "Not initialized.");
}