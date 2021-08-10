using DgtEbDllWrapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace DgtCherub
{
    public partial class Form1 : Form
    {
        const int TEXTBOX_MAX_LINES = 250;
        const string VERSION_NUMBER = "0.0.1";
        const string CHESS_DOT_COM_DYN_BOARD_URL = "https://www.chess.com/dynboard?board=green&fen=";
        const string EMPTY_BOARD_FEN = "8/8/8/8/8/8/8/8";
        const string PROJECT_URL = "https://github.com/Hyper-Dragon/DgtAngel";

        private readonly ILogger _logger;
        private readonly IAppDataService _appDataService;
        private readonly IDgtEbDllFacade _dgtEbDllFacade;

        public Form1(ILogger<Form1> logger, IAppDataService appData, IDgtEbDllFacade dgtEbDllFacade)
        {
            _logger = logger;
            _appDataService = appData;
            _dgtEbDllFacade = dgtEbDllFacade;
            InitializeComponent();

            // Start the Rabbit Plugin
            _dgtEbDllFacade.Init();
        }

        private void ButtonSendTestMsg1_Click(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"Sending a test message to the clock. You should see '{"  *DGT*  "}' ", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"{" ",11}and '{" *ANGEL*"}'.  If not then check your settings.", TEXTBOX_MAX_LINES,false);

            _dgtEbDllFacade.DisplayMessageSeries("  *DGT*  "," *ANGEL*");
        }

        private void ButtonSendTestMsg2_Click(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"Sending a test message to the clock. You should see '{"ABCDEFGH"}'", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"{" ",11}and '{"12345678"}'.  If not then check your settings.", TEXTBOX_MAX_LINES,false);

            _dgtEbDllFacade.DisplayMessageSeries("ABCDEFGH", "12345678");
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"Welcome to...                                                                  ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"██████╗  ██████╗ ████████╗     ██████╗██╗  ██╗███████╗██████╗ ██╗   ██╗██████╗ ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"██╔══██╗██╔════╝ ╚══██╔══╝    ██╔════╝██║  ██║██╔════╝██╔══██╗██║   ██║██╔══██╗", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"██║  ██║██║  ███╗   ██║       ██║     ███████║█████╗  ██████╔╝██║   ██║██████╔╝", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"██║  ██║██║   ██║   ██║       ██║     ██╔══██║██╔══╝  ██╔══██╗██║   ██║██╔══██╗", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"██████╔╝╚██████╔╝   ██║       ╚██████╗██║  ██║███████╗██║  ██║╚██████╔╝██████╔╝", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"╚═════╝  ╚═════╝    ╚═╝        ╚═════╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝ ╚═════╝ ╚═════╝ ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"   Hyper-Dragon :: Version {VERSION_NUMBER} :: {PROJECT_URL}", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"-------------------------------------------------------------------------------", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"WARNING: This is an Alpha version.  The best I can say is that it works on my", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         machine.  Your mileage may vary.", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"Requirements: You will need A DGT Board (Bluetooth version), a DGT 3000 Clock,", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"              the Live Chess Software, DGT Drivers and DGT Angel (see link)", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"              installed on this machine.", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"Using {_dgtEbDllFacade.GetRabbitVersionString()}", TEXTBOX_MAX_LINES, true);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES, false);


            _appDataService.OnLocalFenChange += () =>
            {
                Action updateAction = new(() =>
                {
                    PictureBoxLocal.ImageLocation = $"{CHESS_DOT_COM_DYN_BOARD_URL}{HttpUtility.UrlEncode(_appDataService.LocalBoardFEN)}";
                    PictureBoxLocal.Load();
                });

                PictureBoxRemote.BeginInvoke(updateAction);
            };

            _appDataService.OnChessDotComFenChange += () =>
            {
                Action updateAction = new(() =>
                {
                    PictureBoxRemote.ImageLocation = $"{CHESS_DOT_COM_DYN_BOARD_URL}{HttpUtility.UrlEncode(_appDataService.ChessDotComBoardFEN)}";
                    PictureBoxRemote.Load();
                });

                PictureBoxRemote.BeginInvoke(updateAction);
            };


            _appDataService.OnClockChange += () =>
            {
                TextBoxConsole.AddLine($" Recieved Clock Update ({_appDataService.WhiteClock}) ({_appDataService.BlackClock})", TEXTBOX_MAX_LINES);
            };

            _appDataService.OnUserMessageArrived += (source, message) =>
            {
                TextBoxConsole.AddLine($" From {source}::{message}", TEXTBOX_MAX_LINES);
            };
        }

        private void CheckBoxOnTop_CheckedChanged(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"DGT Cherub {((CheckBoxOnTop.Checked)?"will always be on top.":"will no longer be on top.")}", TEXTBOX_MAX_LINES);
            ((Form)TopLevelControl).TopMost = CheckBoxOnTop.Checked;
        }

        private void ButtonRabbitConfig_Click(object sender, EventArgs e)
        {
            _dgtEbDllFacade.HideCongigDialog();
            TextBoxConsole.AddLine($"Showing Rabbit Configuration", TEXTBOX_MAX_LINES);
            _dgtEbDllFacade.ShowCongigDialog();
        }

        private void CheckBoxShowInbound_CheckedChanged(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"DGT Cherub {((CheckBoxShowInbound.Checked) ? "will" : "WILL NOT")} display notification messages from DGT Angel.", TEXTBOX_MAX_LINES);
            _appDataService.EchoExternalMessagesToConsole = CheckBoxShowInbound.Checked;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.SuspendLayout();

            PictureBoxLocal.ImageLocation = $"{CHESS_DOT_COM_DYN_BOARD_URL}{EMPTY_BOARD_FEN}";
            PictureBoxRemote.ImageLocation = $"{CHESS_DOT_COM_DYN_BOARD_URL}{EMPTY_BOARD_FEN}";
            PictureBoxLocal.Load();
            PictureBoxRemote.Load();

            ToolStripStatusLabelVersion.Text = $"Ver. {VERSION_NUMBER}";
            TabControlSidePanel.SelectedTab = TabPageConfig;

            LinkLabelAbout1.Text = "GitHub Project Page";
            LinkLabelAbout1.LinkArea = new LinkArea(0, LinkLabelAbout1.Text.Length);
            LinkLabelAbout1.Visible = true;
            LinkLabelAbout1.Click += (object sender, EventArgs e) =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"{PROJECT_URL}",
                    UseShellExecute = true //required on .Net Core 
                });
            };

            this.ResumeLayout();
        }
    }
}
