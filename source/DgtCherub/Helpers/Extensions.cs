using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace DgtCherub.Helpers
{
    internal static class TextBoxExtensions
    {
        internal static bool RunProcessWithComments(this TextBox box, string filename, string arguments, string preStartText, string successText, int? maxLine = null, bool useShellExecute = true)
        {
            try
            {
                box.AddLine($"{preStartText}", maxLine, true);

                _ = new Process()
                {
                    StartInfo = new()
                    {
                        UseShellExecute = useShellExecute,
                        FileName = filename,
                        Arguments = arguments,
                        CreateNoWindow = true,
                        RedirectStandardError = !useShellExecute,
                        RedirectStandardOutput = !useShellExecute,
                    }
                }.Start();

                box.AddLine($"...{successText}", maxLine, true);
                return true;
            }
            catch (Win32Exception ex)
            {
                box.AddLine($"...but an error occured. [{ex.Message}]", maxLine, true);
            }
            catch (Exception ex)
            {
                box.AddLine($"...but we have an unexpected error: {ex.Message} {ex.GetType()}", maxLine, true);
                box.AddLines(ex.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None), maxLine, true);
                box.AddLine($">>If the problem persists then try the Chess.com forums or report it as an issue on GitHub.", maxLine, true);
            }

            return false;
        }

        internal static void AddChar(this TextBox box, char character)
        {
            if (box.IsDisposed)
            {
                return;
            }

            if (!box.IsHandleCreated)
            {
                return;
            }

            if (box.TopLevelControl.IsDisposed)
            {
                return;
            }

            box.Text += character;
        }

        //Extension method to sanitise text input for log files
        internal static string SanitiseText(this string text)
        {
            return text.Replace("\"", ".").Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace("\0", "");
        }

        internal static void AddLines(this TextBox box, string[] text, int? maxLine = null, bool timeStamp = true)
        {
            foreach (string line in text)
            {
                box.AddLine($">> {line}", maxLine, timeStamp);
            }
        }

        internal static void AddLine(this TextBox box, string text, int? maxLine = null, bool timeStamp = true)
        {
            Action updateAction = new(() =>
            {
                if (box.IsDisposed || !box.IsHandleCreated || box.TopLevelControl.IsDisposed)
                {
                    return;
                }

                box.SuspendLayout();
                box.AppendText($"{((box.Lines.Length == 0) ? "" : $"{Environment.NewLine}")}{(timeStamp ? $"[{System.DateTime.Now.ToLongTimeString()}] " : "")}{text}");
                box.Lines = box.Lines.TakeLast((maxLine is not null and > 1) ? maxLine.Value - 1 : box.Lines.Length).ToArray();
                box.SelectionStart = box.TextLength - box.Lines[^1].Length;
                box.ScrollToCaret();
                box.ResumeLayout();
            });

            if (box.InvokeRequired)
            {
                _ = box.BeginInvoke(updateAction);
            }
            else
            {
                updateAction.Invoke();
            }
        }
    }
}
