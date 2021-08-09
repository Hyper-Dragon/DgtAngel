using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace DgtCherub
{
    public static class ControlHelper
    {
        public static void AddLine(this TextBox box, string text, int? maxLine = null)
        {
            Action updateAction = new(() =>
            {
                box.AppendText($"{ ((box.Lines.Length == 0) ? "" : $"{Environment.NewLine}")}{text}");
                box.Lines = (box.Lines.TakeLast(((maxLine != null && maxLine > 1) ? maxLine.Value - 1 : box.Lines.Length))).ToArray();
                box.SelectionStart = box.TextLength;
                box.ScrollToCaret();
            });

            if (box.InvokeRequired)
            {
                box.BeginInvoke(updateAction);
            }
            else
            {
                updateAction.Invoke();
            }
        }
    }
}
