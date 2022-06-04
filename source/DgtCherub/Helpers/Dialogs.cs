using System.Windows.Forms;

namespace DgtCherub.Helpers
{
    internal static class Dialogs
    {
        internal static void ShowCantStartDialog(in string message)
        {
            _ = MessageBox.Show($"{message}",
                             "Statup Failed",
                             MessageBoxButtons.OK,
                             MessageBoxIcon.Warning,
                             MessageBoxDefaultButton.Button1);
        }

        internal static void ShowErrorDialog(in string message)
        {
            _ = MessageBox.Show($"{message}",
                             "Fatal Error",
                             MessageBoxButtons.OK,
                             MessageBoxIcon.Error,
                             MessageBoxDefaultButton.Button1);
        }
    }
}
