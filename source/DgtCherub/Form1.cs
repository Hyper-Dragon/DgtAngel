using DgtEbDllWrapper;
using DgtLiveChessWrapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Reflection;
using System.Threading;
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


        public enum AudioClip { MISMATCH = 0, MATCH, DGT_LC_CONNECTED, DGT_LC_DISCONNECTED, DGT_CONNECTED, DGT_DISCONNECTED, CDC_WATCHING, CDC_NOTWATCHING };

        private const string RESOURCE_VOICE_ROOT = "DgtCherub.Assets.Audio";
        private const string RESOURCE_VOICE_NAME = "Speech_en_01";

        private readonly string[] AudioFiles = { "Mismatch.wav" ,
                                                 "Match.wav" ,
                                                 "DgtLcConnected.wav" ,
                                                 "DgtLcDisconnected.wav" ,
                                                 "DgtConnected.wav" ,
                                                 "DgtDisconnected.wav" ,
                                                 "CdcWatching.wav" ,
                                                 "CdcStoppedWatching.wav" ,
                                                };

        private readonly ILogger _logger;
        private readonly IAppDataService _appDataService;
        private readonly IDgtEbDllFacade _dgtEbDllFacade;
        private readonly IDgtLiveChess _dgtLiveChess;
        private readonly SoundPlayer _soundPlayer;

        private readonly ConcurrentQueue<AudioClip> playList = new();

        private int LastFormWidth = 705;
        private int CollapsedWidth = 705;
        private Size InitialMinSize = new(420, 420);
        private Size InitialMaxSize = new(0, 0);
        private Color BoredLabelsInitialColor = Color.Silver;

        //TODO: Finish the testers tab
        //TODO: option to kill voice
        //TODO: Sync all speech
        //TODO: Mismatch to clock
        //TODO:check if live chess is running
        //TODO:check if rabbit connected
        //TODO:mutex not really working only on second try!
        //TODO:add note - is your clock on option 25 and set (play button)  - the time wont work otherwise
        //TODO:check if chrome is installes
        //TODO:add kill/restart live chess exe
        //TODO:The startup order seems to matter - if you want the clock get a bluetooth connection 1st then plug in the board
        //TODO:Angel...Icon changes
        //TODO:Angel sending lots of duplicate boards
        //TODO:Logging
        //TODO: stop saying disconnected from the live chess

        public Form1(ILogger<Form1> logger, SoundPlayer soundPlayer, IAppDataService appData, IDgtEbDllFacade dgtEbDllFacade, IDgtLiveChess dgtLiveChess)
        {
            //TODO: Sort out the logging in here
            _logger = logger;
            _soundPlayer = soundPlayer;
            _appDataService = appData;
            _dgtEbDllFacade = dgtEbDllFacade;
            _dgtLiveChess = dgtLiveChess;

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

               
        private void Speak(AudioClip clipName)
        {
            if (playList.IsEmpty)
            {
                playList.Enqueue(clipName);

                Thread playListPlayer = new(() => { while (!playList.IsEmpty)
                                                    {
                                                        if (playList.TryDequeue(out AudioClip result))
                                                        {
                                                            using var audioStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{RESOURCE_VOICE_ROOT}.{RESOURCE_VOICE_NAME}.{AudioFiles[((int)result)]}");
                                                            _soundPlayer.Stream = audioStream;
                                                            _soundPlayer.PlaySync();
                                                            _soundPlayer.Stream = null;
                                                        }
                                                    }
                                                  });

                playListPlayer.Start();
            }
            else
            {
                playList.Enqueue(clipName);
            }
        }

        
        private void ClearConsole()
        {
            TextBoxConsole.Text = "";
            TextBoxConsole.Update();

            TextBoxConsole.AddLine($"  Welcome to...                                                                  ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"  ██████╗  ██████╗ ████████╗     ██████╗██╗  ██╗███████╗██████╗ ██╗   ██╗██████╗ ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"  ██╔══██╗██╔════╝ ╚══██╔══╝    ██╔════╝██║  ██║██╔════╝██╔══██╗██║   ██║██╔══██╗", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"  ██║  ██║██║  ███╗   ██║       ██║     ███████║█████╗  ██████╔╝██║   ██║██████╔╝", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"  ██║  ██║██║   ██║   ██║       ██║     ██╔══██║██╔══╝  ██╔══██╗██║   ██║██╔══██╗", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"  ██████╔╝╚██████╔╝   ██║       ╚██████╗██║  ██║███████╗██║  ██║╚██████╔╝██████╔╝", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"  ╚═════╝  ╚═════╝    ╚═╝        ╚═════╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝ ╚═════╝ ╚═════╝ ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"     Hyper-Dragon :: Version {VERSION_NUMBER} :: {PROJECT_URL}", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"  -------------------------------------------------------------------------------", TEXTBOX_MAX_LINES, false);
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
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            ClearConsole();

            _appDataService.OnLocalFenChange += () =>
            {
                Action updateAction = new(() =>
                {
                    LabelLocalDgt.BackColor = Color.Yellow;
                    LabelRemoteBoard.BackColor = Color.Yellow;
                    this.Update();

                    ToolStripStatusLabelLastUpdate.Text = $"[Updated@{System.DateTime.Now.ToLongTimeString()}]";
                    PictureBoxLocal.ImageLocation = $"{CHESS_DOT_COM_DYN_BOARD_URL}{HttpUtility.UrlEncode(_appDataService.LocalBoardFEN)}";
                    
                    //TODO: catch failure;
                    PictureBoxLocal.Load();

                    //TODO: Add mismatch speach
                    LabelLocalDgt.BackColor = _appDataService.LocalBoardFEN != _appDataService.ChessDotComBoardFEN ? Color.Red : BoredLabelsInitialColor;
                    LabelRemoteBoard.BackColor = _appDataService.LocalBoardFEN != _appDataService.ChessDotComBoardFEN ? Color.Red : BoredLabelsInitialColor;
                });

                PictureBoxRemote.BeginInvoke(updateAction);
            };

            _appDataService.OnChessDotComFenChange += () =>
            {
                Action updateAction = new(() =>
                {
                    LabelLocalDgt.BackColor = Color.Yellow;
                    LabelRemoteBoard.BackColor = Color.Yellow;
                    this.Update();

                    ToolStripStatusLabelLastUpdate.Text = $"[Updated@{System.DateTime.Now.ToLongTimeString()}]";
                    PictureBoxRemote.ImageLocation = $"{CHESS_DOT_COM_DYN_BOARD_URL}{HttpUtility.UrlEncode(_appDataService.ChessDotComBoardFEN)}";
                    PictureBoxRemote.Load();

                    //TODO: Add mismatch speach
                    LabelLocalDgt.BackColor = _appDataService.LocalBoardFEN != _appDataService.ChessDotComBoardFEN ? Color.Red : BoredLabelsInitialColor;
                    LabelRemoteBoard.BackColor = _appDataService.LocalBoardFEN != _appDataService.ChessDotComBoardFEN ? Color.Red : BoredLabelsInitialColor;
                });

                PictureBoxRemote.BeginInvoke(updateAction);
            };

            _appDataService.OnClockChange += () =>
            {
                TextBoxConsole.AddLine($" Recieved Clock Update ({_appDataService.WhiteClock}) ({_appDataService.BlackClock}) ({_appDataService.RunWhoString})", TEXTBOX_MAX_LINES);

                this.Invoke((Action)(() =>
                {
                    LabelWhiteClock.Text = $"{ ((_appDataService.RunWhoString == "3" || _appDataService.RunWhoString == "1") ? "*" : " ")}{_appDataService.WhiteClock}";
                    LabelBlackClock.Text = $"{ ((_appDataService.RunWhoString == "3" || _appDataService.RunWhoString == "2") ? "*" : " ")}{_appDataService.BlackClock}";
                    ToolStripStatusLabelLastUpdate.Text = $"[Updated@{System.DateTime.Now.ToLongTimeString()}]";
                }));
            };
     
            _appDataService.OnUserMessageArrived += (source, message) =>
            {
                TextBoxConsole.AddLine($"From {source}::{message}", TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnLiveChessConnected += (source, eventArgs) =>
            {
                Speak(AudioClip.DGT_LC_CONNECTED);
                TextBoxConsole.AddLine($"Live Chess running [{eventArgs.ResponseOut}]", TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnLiveChessDisconnected += (source, eventArgs) =>
            {
                Speak(AudioClip.DGT_LC_DISCONNECTED);
                TextBoxConsole.AddLine($"Live Chess DISCONNECTED [{eventArgs.ResponseOut}]", TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnBoardConnected += (source, eventArgs) =>
            {
                Speak(AudioClip.DGT_CONNECTED);
                TextBoxConsole.AddLine($"Board found [{eventArgs.ResponseOut}]", TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnBoardDisconnected += (source, eventArgs) =>
            {
                Speak(AudioClip.DGT_DISCONNECTED);
                TextBoxConsole.AddLine($"Board DISCONNECTED [{eventArgs.ResponseOut}]", TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnResponseRecieved += (obj, eventArgs) =>
            {
                TextBoxConsole.AddLine($"Local DGT board changed [{eventArgs.ResponseOut}]", TEXTBOX_MAX_LINES);
                _appDataService.LocalBoardFEN = eventArgs.ResponseOut;
            };

            //All the Events are set up so we can start watching the local board
            _dgtLiveChess.PollDgtBoard();
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

            // Store changeable form params and Dynamically Calculate Size of the Collapsed Form 
            BoredLabelsInitialColor = LabelLocalDgt.BackColor;
            LastFormWidth = this.Width;
            InitialMinSize = this.MinimumSize;
            InitialMaxSize = this.MaximumSize;
            CollapsedWidth = (TabControlSidePanel.Width + TabControlSidePanel.ItemSize.Height) - TabControlSidePanel.Padding.X; //TabControlSidePanel.ItemSize.Height is correct!
        }

        private void CheckBoxShowConsole_CheckedChanged(object sender, EventArgs e)
        {
            this.SuspendLayout();

            if (CheckBoxShowConsole.Checked)
            {
                TextBoxConsole.Visible = true;
                this.MinimumSize = InitialMinSize;
                this.MaximumSize = InitialMaxSize;
                this.Width = LastFormWidth;
                this.MaximizeBox = true;
                this.ControlBox = true;
                this.ToolStripStatusLabelVersion.Visible = true;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
                LastFormWidth = this.Width;
                TextBoxConsole.Visible = false;
                this.MinimumSize = new Size(CollapsedWidth, this.MinimumSize.Height);
                this.MaximumSize = new Size(CollapsedWidth, Screen.PrimaryScreen.Bounds.Height);
                this.Width = CollapsedWidth;
                this.MaximizeBox = false;
                this.ControlBox = false;
                this.ToolStripStatusLabelVersion.Visible = false;
            }

            this.ResumeLayout();
        }

        private void ButtonClearConsole_Click(object sender, EventArgs e)
        {
            ClearConsole();
        }
    }
}
