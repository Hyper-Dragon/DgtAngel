using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DgtCherub
{
    public static class ControlHelper
    {
        public static bool RunProcessWithComments(this TextBox box, string filename, string arguments, string preStartText, string successText, int? maxLine = null)
        {
            try
            {
                box.AddLine($"...{preStartText}", maxLine, true);

                _ = (new Process()
                {
                    StartInfo = new()
                    {
                        UseShellExecute = true,
                        FileName = filename,
                        Arguments = arguments
                    }
                }).Start();

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

        public static void AddChar(this TextBox box, char character, bool timeStamp = true)
        {
            box.Text += character;
        }

        public static void AddLines(this TextBox box, string[] text, int? maxLine = null, bool timeStamp = true)
        {
            foreach (string line in text)
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
                box.SelectionStart = box.TextLength - box.Lines[^1].Length;
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


    public class ControlWriter : TextWriter
    {
        private readonly TextBox textbox;
        public ControlWriter(TextBox textbox)
        {
            this.textbox = textbox;
        }

        public override void Write(char value)
        {
            textbox.AddChar(value);
        }

        public override void Write(string value)
        {
            textbox.AddLine(value);
        }

        public override Encoding Encoding
        {
            get { return Encoding.ASCII; }
        }
    }


}
