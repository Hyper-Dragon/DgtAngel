﻿using DgtEbDllWrapper;
using DgtLiveChessWrapper;
using DynamicBoard;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Media;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace DgtCherub
{
    public partial class Form1 : Form
    {
        private const int TEXTBOX_MAX_LINES = 250;
        private const string VERSION_NUMBER = "0.0.1";
        private const string PROJECT_URL = "https://github.com/Hyper-Dragon/DgtAngel";
        private const string CHESS_DOT_COM_PLAY_LINK = @"http://chess.com/live";
        private const string CHESS_DOT_COM_DGT_FORUM = @"https://www.chess.com/clubs/forum/dgt-chess-club";
        private const string PROJECT_LINK = @"https://github.com/Hyper-Dragon/DgtAngel";
        private const string PROJECT_ISSUES = @"https://github.com/Hyper-Dragon/DgtAngel/issues";
        private const string PROJECT_RELEASES = @"https://github.com/Hyper-Dragon/DgtAngel/releases";
        private const string DL_LIVE_CHESS = @"http://www.livechesscloud.com/";
        private const string DL_RABBIT = @"https://www.digitalgametechnology.com/index.php/support1/dgt-software/dgt-e-board-chess-8x8";
        private const string DL_VOICE_EXT = @"https://chrome.google.com/webstore/detail/chesscom-voice-commentary/kampphbbbggcjlepmgfogpkpembcaphk";


        public enum AudioClip { MISMATCH = 0, MATCH, DGT_LC_CONNECTED, DGT_LC_DISCONNECTED, DGT_CONNECTED, DGT_DISCONNECTED, CDC_WATCHING, CDC_NOTWATCHING, DGT_CANT_FIND };

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
                                                 "DgtCantFindBoard.wav"
                                                };

        private readonly ILogger _logger;
        private readonly IAppDataService _appDataService;
        private readonly IDgtEbDllFacade _dgtEbDllFacade;
        private readonly IDgtLiveChess _dgtLiveChess;
        private readonly IBoardRenderer _boardRenderer;
        private readonly SoundPlayer _soundPlayer;

        private readonly ConcurrentQueue<AudioClip> playList = new();

        private int LastFormWidth = 705;
        private int CollapsedWidth = 705;
        private Size InitialMinSize = new(420, 420);
        private Size InitialMaxSize = new(0, 0);
        private Color BoredLabelsInitialColor = Color.Silver;
        private Image PictureBoxLocalInitialImage;
        private Image PictureBoxRemoteInitialImage;

        //TODO: Finish the testers tab
        //TODO: Mismatch to clock -
        //hours not done in time????
        //TODO:check if rabbit connected
        //TODO:mutex not really working only on second try!
        //TODO:add note - is your clock on option 25 and set (play button)  - the time wont work otherwise
        //TODO:The startup order seems to matter - if you want the clock get a bluetooth connection 1st then plug in the board
        //TODO:Remove Chessdotnet
        //TODO:Own board maker

        public Form1(ILogger<Form1> logger,
                     SoundPlayer soundPlayer,
                     IAppDataService appData,
                     IDgtEbDllFacade dgtEbDllFacade,
                     IDgtLiveChess dgtLiveChess,
                     IBoardRenderer boardRenderer)
        {

            //TODO: Sort out the logging in here
            _logger = logger;
            _soundPlayer = soundPlayer;
            _appDataService = appData;
            _dgtEbDllFacade = dgtEbDllFacade;
            _dgtLiveChess = dgtLiveChess;
            _boardRenderer = boardRenderer;

            InitializeComponent();

            // Start the Rabbit Plugin
            // TODO: or maybe not....test
            _dgtEbDllFacade.Init();
        }

        private void ButtonSendTestMsg1_Click(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"Sending a test message to the clock. You should see '{"  *DGT*  "}' ", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"{" ",11}and '{" *ANGEL*"}'.  If not then check your settings.", TEXTBOX_MAX_LINES, false);

            _dgtEbDllFacade.DisplayMessageSeries("  *DGT*  ", " *ANGEL*");
        }

        private void ButtonSendTestMsg2_Click(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"Sending a test message to the clock. You should see '{"ABCDEFGH"}'", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"{" ",11}and '{"12345678"}'.  If not then check your settings.", TEXTBOX_MAX_LINES, false);

            _dgtEbDllFacade.DisplayMessageSeries("ABCDEFGH", "12345678");
        }


        private void Speak(AudioClip clipName)
        {
            if (playList.IsEmpty)
            {
                if (_appDataService.PlayAudio)
                {
                    playList.Enqueue(clipName);

                    Thread playListPlayer = new(() =>
                    {
                        while (!playList.IsEmpty)
                        {
                            if (playList.TryDequeue(out AudioClip result))
                            {
                                if (_appDataService.PlayAudio)
                                {
                                    using System.IO.Stream audioStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{RESOURCE_VOICE_ROOT}.{RESOURCE_VOICE_NAME}.{AudioFiles[((int)result)]}");
                                    _soundPlayer.Stream = audioStream;
                                    _soundPlayer.PlaySync();
                                    _soundPlayer.Stream = null;
                                }
                            }
                        }
                    });

                    playListPlayer.Start();
                }
            }
            else
            {
                if (_appDataService.PlayAudio)
                {
                    playList.Enqueue(clipName);
                }
            }
        }

        private void ClearConsole()
        {
            TextBoxConsole.Text = "";
            TextBoxConsole.Update();

            TextBoxConsole.AddLine($"  -------------------------------------------------------------------------------", TEXTBOX_MAX_LINES, false);
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
                Action updateAction = new(async () =>
                {
                    LabelLocalDgt.BackColor = Color.Yellow;
                    LabelRemoteBoard.BackColor = Color.Yellow;
                    Update();

                    ToolStripStatusLabelLastUpdate.Text = $"[Updated@{System.DateTime.Now.ToLongTimeString()}]";
                    PictureBoxLocal.Image = await _boardRenderer.GetImageFromFenAsync(_appDataService.LocalBoardFEN, PictureBoxLocal.Width, _appDataService.IsWhiteOnBottom);

                    //TODO: Add mismatch speech - clocks must be running
                    if (_appDataService.LocalBoardFEN != _appDataService.ChessDotComBoardFEN)
                    {
                        _dgtEbDllFacade.DisplayForeverMessage("SYNC ERR");
                        
                        if (!_appDataService.IsMismatchDetected)
                        {
                            Speak(AudioClip.MISMATCH);
                            _appDataService.IsMismatchDetected = true;
                        }
                    }
                    else
                    {
                        if (_appDataService.IsMismatchDetected)
                        {
                            _dgtEbDllFacade.StopForeverMessage();
                            _dgtEbDllFacade.DisplayMessage(" MATCH ", 2000);
                            _appDataService.IsMismatchDetected = false;                           
                            Speak(AudioClip.MATCH);
                        }
                    }

                    LabelLocalDgt.BackColor = _appDataService.LocalBoardFEN != _appDataService.ChessDotComBoardFEN ? Color.Red : BoredLabelsInitialColor;
                    LabelRemoteBoard.BackColor = _appDataService.LocalBoardFEN != _appDataService.ChessDotComBoardFEN ? Color.Red : BoredLabelsInitialColor;
                });

                PictureBoxLocal.BeginInvoke(updateAction);
            };

            _appDataService.OnChessDotComDisconnect += () =>
            {
                PictureBoxRemote.Image = PictureBoxRemoteInitialImage;
            };

            _appDataService.OnChessDotComFenChange += () =>
            {
                Action updateAction = new(async () =>
                {
                    LabelLocalDgt.BackColor = Color.Yellow;
                    LabelRemoteBoard.BackColor = Color.Yellow;
                    Update();

                    ToolStripStatusLabelLastUpdate.Text = $"[Updated@{System.DateTime.Now.ToLongTimeString()}]";
                    PictureBoxRemote.Image = await _boardRenderer.GetImageFromFenAsync(_appDataService.ChessDotComBoardFEN, PictureBoxRemote.Width, _appDataService.IsWhiteOnBottom);

                    //TODO: Add mismatch speech - clocks must be running
                    if (_appDataService.LocalBoardFEN != _appDataService.ChessDotComBoardFEN)
                    {
                        _dgtEbDllFacade.DisplayForeverMessage("SYNC ERR");                       

                        if (!_appDataService.IsMismatchDetected)
                        {
                            Speak(AudioClip.MISMATCH);
                            _appDataService.IsMismatchDetected = true;
                        }
                    }
                    else
                    {
                        if (_appDataService.IsMismatchDetected)
                        {
                            _dgtEbDllFacade.StopForeverMessage();
                            _dgtEbDllFacade.DisplayMessage(" MATCH ", 2000);
                            _appDataService.IsMismatchDetected = false;
                            Speak(AudioClip.MATCH);
                        }
                    }

                    LabelLocalDgt.BackColor = _appDataService.LocalBoardFEN != _appDataService.ChessDotComBoardFEN ? Color.Red : BoredLabelsInitialColor;
                    LabelRemoteBoard.BackColor = _appDataService.LocalBoardFEN != _appDataService.ChessDotComBoardFEN ? Color.Red : BoredLabelsInitialColor;
                });

                PictureBoxRemote.BeginInvoke(updateAction);
            };

            _appDataService.OnClockChange += () =>
            {
                TextBoxConsole.AddLine($">>Recieved Clock Update ({_appDataService.WhiteClock}) ({_appDataService.BlackClock}) ({_appDataService.RunWhoString})", TEXTBOX_MAX_LINES);

                Invoke((Action)(() =>
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
                TextBoxConsole.AddLines(new string[]{$"{"".PadRight(67,'-')}",
                                                     $"Live Chess running [{eventArgs.ResponseOut}]",
                                                     $"{"".PadRight(67,'-')}"}, TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnLiveChessDisconnected += (source, eventArgs) =>
            {
                PictureBoxLocal.Image = PictureBoxLocalInitialImage;
                Speak(AudioClip.DGT_LC_DISCONNECTED);
                _appDataService.ResetChessDotComLocalBoardState();
                TextBoxConsole.AddLine($"Live Chess DISCONNECTED [{eventArgs.ResponseOut}]", TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnBoardConnected += (source, eventArgs) =>
            {
                PictureBoxLocal.Image = PictureBoxLocalInitialImage;
                Speak(AudioClip.DGT_CONNECTED);
                TextBoxConsole.AddLines(new string[]{$"{"".PadRight(67,'-')}",
                                                     $"Board found [{eventArgs.ResponseOut}]",
                                                     $"{"".PadRight(67,'-')}"}, TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnBoardDisconnected += (source, eventArgs) =>
            {
                PictureBoxLocal.Image = PictureBoxLocalInitialImage;
                Speak(AudioClip.DGT_DISCONNECTED);
                _appDataService.ResetChessDotComLocalBoardState();
                TextBoxConsole.AddLine($"Board DISCONNECTED [{eventArgs.ResponseOut}]", TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnCantFindBoard += (source, eventArgs) =>
            {
                Speak(AudioClip.DGT_CANT_FIND);
                TextBoxConsole.AddLine($"Board DISCONNECTED [{eventArgs.ResponseOut}]", TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnResponseRecieved += (obj, eventArgs) =>
            {
                TextBoxConsole.AddLine($"{eventArgs.ResponseOut}", TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnFenRecieved += (obj, eventArgs) =>
            {
                TextBoxConsole.AddLine($"Local DGT board changed [{eventArgs.ResponseOut}]", TEXTBOX_MAX_LINES);
                _appDataService.LocalBoardFEN = eventArgs.ResponseOut;
            };

            //All the Events are set up so we can start watching the local board
            _dgtLiveChess.PollDgtBoard();
        }

        private void ButtonRabbitConfig_Click(object sender, EventArgs e)
        {
            _dgtEbDllFacade.HideCongigDialog();
            TextBoxConsole.AddLine($"Showing Rabbit Configuration", TEXTBOX_MAX_LINES);
            _dgtEbDllFacade.ShowCongigDialog();
        }

        private void CheckBoxShowInbound_CheckedChanged(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"DGT Cherub {(CheckBoxShowInbound.Checked ? "will" : "WILL NOT")} display notification messages from DGT Angel.", TEXTBOX_MAX_LINES);
            _appDataService.EchoExternalMessagesToConsole = CheckBoxShowInbound.Checked;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SuspendLayout();

            //Set Appsettings from the designer values...
            _appDataService.EchoExternalMessagesToConsole = CheckBoxShowInbound.Checked;
            _appDataService.PlayAudio = CheckBoxPlayAudio.Checked;

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

            // Store changeable form params and Dynamically Calculate Size of the Collapsed Form 
            BoredLabelsInitialColor = LabelLocalDgt.BackColor;
            LastFormWidth = Width;
            InitialMinSize = MinimumSize;
            InitialMaxSize = MaximumSize;
            PictureBoxLocalInitialImage = PictureBoxLocal.Image;
            PictureBoxRemoteInitialImage = PictureBoxRemote.Image;

            // ItemSize.Height is correct - the tabs are on the side!
            CollapsedWidth = (TabControlSidePanel.Width + TabControlSidePanel.ItemSize.Height) - TabControlSidePanel.Padding.X;

            ResumeLayout();
        }

        private void CheckBoxShowConsole_CheckedChanged(object sender, EventArgs e)
        {
            TopLevelControl.SuspendLayout();

            if (CheckBoxShowConsole.Checked)
            {
                TextBoxConsole.Visible = true;
                MinimumSize = InitialMinSize;
                MaximumSize = InitialMaxSize;
                Width = LastFormWidth;
                MaximizeBox = true;
                MinimizeBox = true;
                ToolStripStatusLabelVersion.Visible = true;
            }
            else
            {
                WindowState = FormWindowState.Normal;
                LastFormWidth = Width;
                TextBoxConsole.Visible = false;
                MinimumSize = new Size(CollapsedWidth, MinimumSize.Height);
                MaximumSize = new Size(CollapsedWidth, Screen.PrimaryScreen.Bounds.Height);
                Width = CollapsedWidth;
                MaximizeBox = false;
                MinimizeBox = false;
                ToolStripStatusLabelVersion.Visible = false;
            }

            TopLevelControl.ResumeLayout();
        }

        private void ButtonClearConsole_Click(object sender, EventArgs e)
        {
            ClearConsole();
        }

        private void TabPageBoards_Enter(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"Selected the Board Tab...you {((CheckBoxOnTop.Checked) ? "will always be on top." : "will not be on top.")}", TEXTBOX_MAX_LINES);
            ((Form)TopLevelControl).TopMost = CheckBoxOnTop.Checked;
        }

        private void TabPageBoards_Leave(object sender, EventArgs e)
        {
            ((Form)TopLevelControl).TopMost = false;
        }

        private void CheckBoxOnTop_CheckedChanged(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"The Board tab {((CheckBoxOnTop.Checked) ? "will always be on top." : "will no longer be on top.")}", TEXTBOX_MAX_LINES);
            if (!CheckBoxOnTop.Checked)
            {
                TextBoxConsole.AddLines(new string[] { $"Keeping the board tab on top is handy when playing since you are able",
                                                       $"to see it without DGT Angel losing focus on the game board."}, TEXTBOX_MAX_LINES);
            }
        }
        private void CheckBoxPlayAudio_CheckedChanged(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"Audio messages from DGT Cherub {((CheckBoxPlayAudio.Checked) ? "are enabled" : "have been disabled.")}", TEXTBOX_MAX_LINES);
            _appDataService.PlayAudio = CheckBoxPlayAudio.Checked;
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to exit?", "DGT Cherub", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        //MENU LINKS REGION (Nothing Exciting) >>>>>>
        #region Menu Links Region
        private void PlayChessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextBoxConsole.RunProcessWithComments("chrome",
                                                   CHESS_DOT_COM_PLAY_LINK,
                                                   $"Trying to open Chess.com in Chrome....",
                                                   $"...Chess.com openend.",
                                                   TEXTBOX_MAX_LINES);
        }

        private void ChesscomDgtForumsMenuItem_Click(object sender, EventArgs e)
        {
            TextBoxConsole.RunProcessWithComments(CHESS_DOT_COM_DGT_FORUM,
                                                   "",
                                                   $"Trying to open the Chess.com DGT forum....",
                                                   $"...the Chess.com DGT forum opened.",
                                                   TEXTBOX_MAX_LINES);
        }

        private void KillLiveChessMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to kill the Live Chess process?", "DGT Cherub", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                TextBoxConsole.RunProcessWithComments(@"Taskkill",
                                                      "/IM \"DGT LiveChess.exe\" /F",
                                                      $"Trying to kill 'DGT LiveChess.exe'....",
                                                      $"...done. 'DGT LiveChess.exe' is no longer running",
                                                      TEXTBOX_MAX_LINES);
            }
        }

        private void DgtAngelChromeExtensionMenuItem_Click(object sender, EventArgs e)
        {
            TextBoxConsole.AddLines(new string[] { $"DGT Angel is currently only available as a developer release.  Go to",
                                                   $"the project release page and follow the instructions."}, TEXTBOX_MAX_LINES);
        }

        private void CdcChromeExtensionVoiceComentaryMenuItem_Click(object sender, EventArgs e)
        {
            TextBoxConsole.RunProcessWithComments("chrome",
                                                   DL_VOICE_EXT,
                                                   $"Trying to open the Google Chrome Web Store....",
                                                   $"...the Google Chrome Web Store is open.",
                                                   TEXTBOX_MAX_LINES);
        }

        private void DgtLiveChessSoftwareMenuItem_Click(object sender, EventArgs e)
        {
            TextBoxConsole.RunProcessWithComments(DL_LIVE_CHESS,
                                                   "",
                                                   $"Trying to open the download page for the Live Chess Software....",
                                                   $"...the download page opened.",
                                                   TEXTBOX_MAX_LINES);
        }

        private void DgtDriversRabbitPluginMenuItem_Click(object sender, EventArgs e)
        {
            TextBoxConsole.RunProcessWithComments(DL_RABBIT,
                                                   "",
                                                   $"Trying to open the download page for the DGT drivers....",
                                                   $"...the download page is opened.",
                                                   TEXTBOX_MAX_LINES);
        }

        private void ProjectPageMenuItem_Click(object sender, EventArgs e)
        {
            TextBoxConsole.RunProcessWithComments(PROJECT_LINK,
                                                  "",
                                                  $"Trying to open DGT Angel project page....",
                                                  $"...the project page is opened.",
                                                  TEXTBOX_MAX_LINES);
        }

        private void ReportIssuesMenuItem_Click(object sender, EventArgs e)
        {
            TextBoxConsole.RunProcessWithComments(PROJECT_ISSUES,
                                                  "",
                                                  $"Trying to open DGT Angel issues page....",
                                                  $"...the issues page is opened.",
                                                  TEXTBOX_MAX_LINES);
        }

        private void ReleasesMenuItem_Click(object sender, EventArgs e)
        {
            TextBoxConsole.RunProcessWithComments(PROJECT_RELEASES,
                                                  "",
                                                  $"Trying to open DGT Anget releases page....",
                                                  $"...the releases page is opened.",
                                                  TEXTBOX_MAX_LINES);
        }
        #endregion

    }
}