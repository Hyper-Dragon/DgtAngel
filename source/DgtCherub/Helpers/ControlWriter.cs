using System.IO;
using System.Text;
using System.Windows.Forms;

namespace DgtCherub.Helpers
{
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

        public override Encoding Encoding => Encoding.ASCII;
    }
}
