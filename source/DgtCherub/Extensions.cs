using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace DgtCherub
{
    public static class ControlHelper
    {
        public static void AddLines(this TextBox box, string[] text, int? maxLine = null, bool timeStamp = true)
        {
            foreach(var line in text)
            {
                box.AddLine($">> {line}", maxLine, timeStamp);
            }
        }

        public static void AddLine(this TextBox box, string text, int? maxLine = null, bool timeStamp = true)
        {
            Action updateAction = new(() =>
            {
                box.SuspendLayout();
                box.AppendText($"{((box.Lines.Length == 0) ? "" : $"{Environment.NewLine}")}{((timeStamp) ? $"[{System.DateTime.Now.ToLongTimeString()}] " : "")}{text}");
                box.Lines = (box.Lines.TakeLast(((maxLine != null && maxLine > 1) ? maxLine.Value - 1 : box.Lines.Length))).ToArray();
                box.SelectionStart = box.TextLength-box.Lines[^1].Length;
                box.ScrollToCaret();
                box.ResumeLayout();
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
