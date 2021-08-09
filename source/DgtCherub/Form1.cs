using DgtEbDllWrapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DgtCherub
{
    public partial class Form1 : Form
    {
        const int TEXTBOX_MAX_LINES = 100;
        const string VERSION_NUMBER = "0.0.1";

        private readonly ILogger _logger;
        private readonly IAppDataService _appDataService;
        private readonly IDgtEbDllFacade _dgtEbDllFacade;

        public Form1(ILogger<Form1> logger, IAppDataService appData, IDgtEbDllFacade dgtEbDllFacade)
        {
            _logger = logger;
            _appDataService = appData;
            _dgtEbDllFacade = dgtEbDllFacade;
            InitializeComponent();
        }

        private void ButtonSendTestMsg1_Click(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($">> Sending a test message to the clock.", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"   You should see '{"  *DGT*  "}' and '{" *ANGEL*"}'.  If not then check your settings.", TEXTBOX_MAX_LINES);

            _dgtEbDllFacade.DisplayMessageSeries("  *DGT*  "," *ANGEL*");
        }

        private void ButtonSendTestMsg2_Click(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($">> Sending a test message to the clock.", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"   You should see '{"ABCDEFGH"}'and '{"12345678"}'.  If not then check your settings.", TEXTBOX_MAX_LINES);

            _dgtEbDllFacade.DisplayMessageSeries("ABCDEFGH", "12345678");
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            _dgtEbDllFacade.Init();

            ToolStripStatusLabelVersion.Text = $"Ver. {VERSION_NUMBER}";

            TextBoxConsole.AddLine($"Welcome to...                                                                  ", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"██████╗  ██████╗ ████████╗     ██████╗██╗  ██╗███████╗██████╗ ██╗   ██╗██████╗ ", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"██╔══██╗██╔════╝ ╚══██╔══╝    ██╔════╝██║  ██║██╔════╝██╔══██╗██║   ██║██╔══██╗", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"██║  ██║██║  ███╗   ██║       ██║     ███████║█████╗  ██████╔╝██║   ██║██████╔╝", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"██║  ██║██║   ██║   ██║       ██║     ██╔══██║██╔══╝  ██╔══██╗██║   ██║██╔══██╗", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"██████╔╝╚██████╔╝   ██║       ╚██████╗██║  ██║███████╗██║  ██║╚██████╔╝██████╔╝", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"╚═════╝  ╚═════╝    ╚═╝        ╚═════╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝ ╚═════╝ ╚═════╝ ", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"   Hyper-Dragon :: Version {VERSION_NUMBER} :: https://github.com/Hyper-Dragon/DgtAngel   ", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"-------------------------------------------------------------------------------", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"WARNING: This is an Alpha version.  The best I can say is that it works on my", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"         machine.  Your mileage may vary.", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"Requirements: You will need A DGT Board (Bluetooth version), a DGT 3000 Clock,", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"              the Live Chess Software, DGT Drivers and DGT Angel (see link)", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"              installed on this machine.", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($">> Using Rabbit Version {_dgtEbDllFacade.GetRabbitVersionString()}", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES);

            _appDataService.OnClockChange += () =>
            {
                TextBoxConsole.AddLine($"[{System.DateTime.Now.ToLongTimeString()}] Recieved Clock Update ({_appDataService.WhiteClock}) ({_appDataService.BlackClock})", TEXTBOX_MAX_LINES);
            };

            
            _appDataService.OnUserMessageArrived += (source,message) =>
            {
                TextBoxConsole.AddLine($"[{System.DateTime.Now.ToLongTimeString()}] From {source}::{message}", TEXTBOX_MAX_LINES);
            };
  
        }

        private void CheckBoxOnTop_CheckedChanged(object sender, EventArgs e)
        {
            ((Form)TopLevelControl).TopMost = CheckBoxOnTop.Checked;
        }

        private void CheckBoxShowRabbit_CheckedChanged(object sender, EventArgs e)
        {
            if (CheckBoxShowRabbit.Checked)
            {
                _dgtEbDllFacade.ShowCongigDialog();
            }
            else
            {
                _dgtEbDllFacade.HideCongigDialog();
            }
        }
    }
}
