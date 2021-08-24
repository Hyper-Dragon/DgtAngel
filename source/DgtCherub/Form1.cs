﻿using DgtCherub.Helpers;
using DgtCherub.Services;
using DgtEbDllWrapper;
using DgtLiveChessWrapper;
using DynamicBoard;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DgtCherub.Helpers.ISequentialVoicePlayer;


//TODO: Add Firewall Config
/*
netsh advfirewall firewall add rule name="Dgt Angel ALLOW Tcp Port 37964" dir=in action=allow protocol=TCP localport=37964
netsh advfirewall firewall show rule name="Dgt Angel ALLOW Tcp Port 37964"
netsh advfirewall firewall delete rule name="Dgt Angel ALLOW Tcp Port 37964"
 */

namespace DgtCherub
{
    public partial class Form1 : Form
    {
        private const int TEXTBOX_MAX_LINES = 200;
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

        private readonly IHost _iHost;
        private readonly ILogger _logger;
        private readonly IAppDataService _appDataService;
        private readonly IDgtEbDllFacade _dgtEbDllFacade;
        private readonly IDgtLiveChess _dgtLiveChess;
        private readonly IBoardRenderer _boardRenderer;
        private readonly ISequentialVoicePlayer _voicePlayer;
        private readonly ISequentialVoicePlayer _voicePlayerMoves;

        private int LastFormWidth = 705;
        private int CollapsedWidth = 705;
        private Size InitialMinSize = new(420, 420);
        private Size InitialMaxSize = new(0, 0);
        private Color BoredLabelsInitialColor = Color.Silver;
        private Image PictureBoxLocalInitialImage;
        private Image PictureBoxRemoteInitialImage;

        private bool EchoExternalMessagesToConsole { get; set; } = true;

        private readonly bool IsRabbitInstalled = false;

        //TODO: Finish the testers tab
        //TODO:add note - is your clock on option 25 and set (play button)  - the time wont work otherwise
        //TODO:The startup order seems to matter - if you want the clock get a bluetooth connection 1st then plug in the board
        //TODO:Own board maker

        public Form1(IHost iHost, ILogger<Form1> logger, IAppDataService appData, IDgtEbDllFacade dgtEbDllFacade,
                     IDgtLiveChess dgtLiveChess, IBoardRenderer boardRenderer, ISequentialVoicePlayer voicePlayer,
                     ISequentialVoicePlayer voicePlayerMoves)
        {
            _iHost = iHost;
            _logger = logger;
            _appDataService = appData;

            _dgtLiveChess = dgtLiveChess;

            _dgtEbDllFacade = dgtEbDllFacade;
            _boardRenderer = boardRenderer;
            _voicePlayer = voicePlayer;
            _voicePlayerMoves = voicePlayerMoves;

            InitializeComponent();

            // Start the Rabbit Plugin if we can...
            try
            {
                _dgtEbDllFacade.Init();
                IsRabbitInstalled = true;
            }
            catch (DllNotFoundException)
            {
                _dgtEbDllFacade = null;
                IsRabbitInstalled = false;
            }
        }

        //TODO:  THIS HAS REAL CLOCK CODE IN IT - check before delete
        //

        ///        LabelLocalDgt.BackColor = Color.Yellow;
        ///                    LabelRemoteBoard.BackColor = Color.Yellow;
        ///                    Update();

        /*private async Task FenChangedMatchTest()
        {
            //TODO: prob should be canceled if fen changes while we wait
            if (!_appDataService.IsMismatchDetected)
            {
                //if we have no mismatch delay before test
                await Task.Delay(MISMATCH_DELAY);
            }

            //TODO: Add mismatch speech - clocks must be running
            if (_appDataService.LocalBoardFEN != _appDataService.ChessDotComBoardFEN)
            {
                _dgtEbDllFacade?.DisplayForeverMessage("SYNC ERR");

                if (!_appDataService.IsMismatchDetected)
                {
                    _voicePlayer.Speak(AudioClip.MISMATCH);
                    _appDataService.IsMismatchDetected = true;
                }
            }
            else
            {
                if (_appDataService.IsMismatchDetected)
                {
                    _dgtEbDllFacade?.StopForeverMessage();
                    _dgtEbDllFacade?.DisplayMessage(" MATCH ", 2000);
                    _appDataService.IsMismatchDetected = false;
                    _voicePlayer.Speak(AudioClip.MATCH);
                }
            }

            LabelLocalDgt.BackColor = _appDataService.LocalBoardFEN != _appDataService.ChessDotComBoardFEN ? Color.Red : BoredLabelsInitialColor;
            LabelRemoteBoard.BackColor = _appDataService.LocalBoardFEN != _appDataService.ChessDotComBoardFEN ? Color.Red : BoredLabelsInitialColor;
        }
        */

        private void Form1_Load(object sender, EventArgs e)
        {
            SuspendLayout();

            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;

            //Set Appsettings from the designer values...
            EchoExternalMessagesToConsole = CheckBoxShowInbound.Checked;
            _voicePlayer.Volume = CheckBoxPlayStatus.Checked ? 10 : 0;

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

            //If no rabbit disable rabbit things..
            if (!IsRabbitInstalled)
            {
                ButtonSendTestMsg1.Enabled = false;
                ButtonSendTestMsg2.Enabled = false;
                ButtonRabbitConfig1.Enabled = false;
                ButtonRabbitConf2.Enabled = false;
            }

            //Make sure this is set
            DoubleBuffered = true;

            ResumeLayout();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            ClearConsole();

            _appDataService.OnOrientationFlipped += () =>
            {
                DisplayBoardImages();
            };

            _appDataService.OnLocalFenChange += () =>
            {
                DisplayBoardImages();
            };

            _appDataService.OnRemoteFenChange += () =>
            {
                DisplayBoardImages();
            };

            _appDataService.OnBoardMissmatch += () =>
            {
                TextBoxConsole.AddLine($"The boards DO NOT match", TEXTBOX_MAX_LINES);

                LabelLocalDgt.BackColor = Color.Red;
                LabelRemoteBoard.BackColor = Color.Red;

                _voicePlayer.Speak(AudioClip.MISMATCH);
            };

            _appDataService.OnBoardMatcherStarted += () =>
            {
                LabelLocalDgt.BackColor = Color.Yellow;
                LabelRemoteBoard.BackColor = Color.Yellow;
            };

            _appDataService.OnBoardMatchFromMissmatch += () =>
            {
                TextBoxConsole.AddLine($"The boards now match", TEXTBOX_MAX_LINES);
                _voicePlayer.Speak(AudioClip.MATCH);
            };

            _appDataService.OnBoardMatch += () =>
            {
                LabelLocalDgt.BackColor = BoredLabelsInitialColor;
                LabelRemoteBoard.BackColor = BoredLabelsInitialColor;
            };

            _appDataService.OnChessDotComDisconnect += () =>
            {
                DisplayBoardImages();
            };

            _dgtLiveChess.OnLiveChessDisconnected += (source, eventArgs) =>
             {
                 _appDataService.ResetBoardState();
                 _voicePlayer.Speak(AudioClip.DGT_LC_DISCONNECTED);
                 DisplayBoardImages();
                 TextBoxConsole.AddLine($"Live Chess DISCONNECTED [{eventArgs.ResponseOut}]", TEXTBOX_MAX_LINES);
             };

            _appDataService.OnPlayWhiteClockAudio += (audioFilename) =>
            {
                if (CheckBoxPlayTime.Checked && _appDataService.IsWhiteOnBottom)
                {
                    _voicePlayer.Speak(DgtCherub.Assets.Time_en_01.ResourceManager.GetStream($"{audioFilename}_AP"));
                }
            };

            _appDataService.OnPlayBlackClockAudio += (audioFilename) =>
            {
                if (CheckBoxPlayTime.Checked && !_appDataService.IsWhiteOnBottom)
                {
                    _voicePlayer.Speak(DgtCherub.Assets.Time_en_01.ResourceManager.GetStream($"{audioFilename}_AP"));
                }
            };

            _appDataService.OnClockChange += () =>
            {
                //TODO: replace the runwho + LabelWhiteClock.IsHandleCreated????
                //TextBoxConsole.AddLine($">>Recieved Clock Update ({_appDataService.WhiteClock}) ({_appDataService.BlackClock}) ({_appDataService.RunWhoString})", TEXTBOX_MAX_LINES); 
                Invoke((Action)(() =>
                {
                    LabelWhiteClock.Text = $"{ ((_appDataService.RunWhoString == "3" || _appDataService.RunWhoString == "1") ? "*" : " ")}{_appDataService.WhiteClock}";
                    LabelBlackClock.Text = $"{ ((_appDataService.RunWhoString == "3" || _appDataService.RunWhoString == "2") ? "*" : " ")}{_appDataService.BlackClock}";
                    ToolStripStatusLabelLastUpdate.Text = $"[Updated@{System.DateTime.Now.ToLongTimeString()}]";
                }));
            };

            _appDataService.OnNewMoveDetected += (moveString) =>
            {
                if (CheckBoxPlayMoves.Checked)
                {
                    string soundName = "";
                    soundName = moveString switch
                    {
                        "O-O" => "Words_CastlesShort",
                        "O-O-O" => "Words_CastlesLong",
                        "1/2-1/2" => "Words_GameDrawn",
                        "1-0" => "Words_WhiteWins",
                        "0-1" => "Words_BlackWins",
                        _ => ""
                    };

                    if (!string.IsNullOrEmpty(soundName))
                    {
                        _voicePlayerMoves.Speak(DgtCherub.Assets.Moves_en_01.ResourceManager.GetStream($"{soundName}_AP"));
                    }
                    else
                    {
                        List<UnmanagedMemoryStream> playlist = new();
                        foreach (char ch in moveString.ToCharArray())
                        {
                            soundName = ch switch
                            {
                                'Q' => "Pieces_Queen",
                                'K' => "Pieces_King",
                                'N' => "Pieces_Knight",
                                'B' => "Pieces_Bishop",
                                'R' => "Pieces_Rook",
                                'P' => "Pieces_Pawn",
                                'a' => "Letters_A",
                                'b' => "Letters_B",
                                'c' => "Letters_C",
                                'd' => "Letters_D",
                                'e' => "Letters_E",
                                'f' => "Letters_F",
                                'g' => "Letters_G",
                                'h' => "Letters_H",
                                '1' => "Numbers_1",
                                '2' => "Numbers_2",
                                '3' => "Numbers_3",
                                '4' => "Numbers_4",
                                '5' => "Numbers_5",
                                '6' => "Numbers_6",
                                '7' => "Numbers_7",
                                '8' => "Numbers_8",
                                'x' => "Words_Takes",
                                '+' => "Words_Check",
                                '=' => "Words_PromotesTo",
                                _ => "Words_Missing",
                            };

                            playlist.Add(DgtCherub.Assets.Moves_en_01.ResourceManager.GetStream($"{soundName}_AP"));
                        }

                        _voicePlayerMoves.Speak(playlist);
                    }
                }
            };

            _appDataService.OnUserMessageArrived += (source, message) =>
                {
                    //TODO: Remove condition
                    if (source == "LMOVE" || source == "INGEST")
                    {
                        if (EchoExternalMessagesToConsole && CheckBoxRecieveLog.Checked)
                        {
                            TextBoxConsole.AddLine($"From {source}::{message}", TEXTBOX_MAX_LINES);
                        }
                    }
                };

            _dgtLiveChess.OnLiveChessConnected += (source, eventArgs) =>
            {
                _voicePlayer.Speak(AudioClip.DGT_LC_CONNECTED);
                TextBoxConsole.AddLines(new string[]{$"{"".PadRight(67,'-')}",
                                                     $"Live Chess running [{eventArgs.ResponseOut}]",
                                                     $"{"".PadRight(67,'-')}"}, TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnBoardConnected += (source, eventArgs) =>
            {
                PictureBoxLocal.Image = PictureBoxLocalInitialImage;
                _voicePlayer.Speak(AudioClip.DGT_CONNECTED);
                TextBoxConsole.AddLines(new string[]{$"{"".PadRight(67,'-')}",
                                                     $"Board found [{eventArgs.ResponseOut}]",
                                                     $"{"".PadRight(67,'-')}"}, TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnBoardDisconnected += (source, eventArgs) =>
            {
                DisplayBoardImages();

                _voicePlayer.Speak(AudioClip.DGT_DISCONNECTED);
                _appDataService.ResetBoardState();
                TextBoxConsole.AddLine($"Board DISCONNECTED [{eventArgs.ResponseOut}]", TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnCantFindBoard += (source, eventArgs) =>
            {
                _voicePlayer.Speak(AudioClip.DGT_CANT_FIND);
                TextBoxConsole.AddLine($"Board DISCONNECTED [{eventArgs.ResponseOut}]", TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnResponseRecieved += (obj, eventArgs) =>
            {
                TextBoxConsole.AddLine($"{eventArgs.ResponseOut}", TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnFenRecieved += (obj, eventArgs) =>
            {
                TextBoxConsole.AddLine($"Local DGT board changed [{eventArgs.ResponseOut}]", TEXTBOX_MAX_LINES);
                _appDataService.LocalBoardUpdate(eventArgs.ResponseOut);
            };

            //All the Events are set up so we can start watching the local board and running the inbound API
            Task.Run(() => _dgtLiveChess.PollDgtBoard());
            Task.Run(() => _iHost.Run());
        }



        //*********************************************//
        #region Form Control Events
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

        private void ButtonSendTestMsg1_Click(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"Sending a test message to the clock. You should see '{"  *DGT*  "}' ", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"{" ",11}and '{" *ANGEL*"}'.  If not then check your settings.", TEXTBOX_MAX_LINES, false);

            _dgtEbDllFacade?.DisplayMessageSeries("  *DGT*  ", " *ANGEL*");
        }

        private void ButtonSendTestMsg2_Click(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"Sending a test message to the clock. You should see '{"ABCDEFGH"}'", TEXTBOX_MAX_LINES);
            TextBoxConsole.AddLine($"{" ",11}and '{"12345678"}'.  If not then check your settings.", TEXTBOX_MAX_LINES, false);

            _dgtEbDllFacade?.DisplayMessageSeries("ABCDEFGH", "12345678");
        }

        private void ButtonRabbitConfig_Click(object sender, EventArgs e)
        {
            _dgtEbDllFacade?.HideCongigDialog();
            TextBoxConsole.AddLine($"Showing Rabbit Configuration", TEXTBOX_MAX_LINES);
            _dgtEbDllFacade?.ShowCongigDialog();
        }

        private void CheckBoxShowInbound_CheckedChanged(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"DGT Cherub {(CheckBoxShowInbound.Checked ? "will" : "WILL NOT")} display notification messages from DGT Angel.", TEXTBOX_MAX_LINES);
            EchoExternalMessagesToConsole = CheckBoxShowInbound.Checked;
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
            TextBoxConsole.AddLine($"Audio messages from DGT Cherub {((CheckBoxPlayStatus.Checked) ? "are enabled" : "have been disabled.")}", TEXTBOX_MAX_LINES);
            _voicePlayer.Volume = CheckBoxPlayStatus.Checked ? 10 : 0;
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to exit?", "DGT Cherub", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }
        #endregion
        //*********************************************//

        //*********************************************//
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

        //TODO: Create 'Play' Menu - leave this on tasks
        private void KillLiveChessMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to kill the Live Chess process?", "DGT Cherub", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                TextBoxConsole.RunProcessWithComments(@"Taskkill",
                                                      "/IM \"DGT LiveChess.exe\" /F",
                                                      $"Trying to kill 'DGT LiveChess.exe'....",
                                                      $"...done. 'DGT LiveChess.exe' is no longer running",
                                                      TEXTBOX_MAX_LINES,
                                                      useShellExecute: false);
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

        private void PlayWindowlessMenuItem_Click(object sender, EventArgs e)
        {
            TextBoxConsole.RunProcessWithComments("chrome",
                                                  $"--app={CHESS_DOT_COM_PLAY_LINK}",
                                                  $"Trying to open Chess.com in Chrome....",
                                                  $"...Chess.com openend.",
                                                  TEXTBOX_MAX_LINES);
        }
        #endregion
        //*********************************************//

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
            TextBoxConsole.AddLine($"Using { ((IsRabbitInstalled) ? _dgtEbDllFacade.GetRabbitVersionString() : "DGT Rabbit is not installed on this machine.")     }", TEXTBOX_MAX_LINES, true);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES, false);
        }

        private void DisplayBoardImages()
        {
            Action updateAction = new(async () =>
            {
                ToolStripStatusLabelLastUpdate.Text = $"[Updated@{System.DateTime.Now.ToLongTimeString()}]";

                string local = _appDataService.IsLocalBoardAvailable ? _appDataService.LocalBoardFEN : _appDataService.ChessDotComBoardFEN;
                string remote = _appDataService.IsRemoteBoardAvailable ? _appDataService.ChessDotComBoardFEN : _appDataService.LocalBoardFEN;

                PictureBoxLocal.Image = _appDataService.IsLocalBoardAvailable ? await _boardRenderer.GetImageDiffFromFenAsync(local, remote, PictureBoxRemote.Width, _appDataService.IsWhiteOnBottom) : PictureBoxLocal.Image = PictureBoxLocalInitialImage;
                PictureBoxRemote.Image = _appDataService.IsRemoteBoardAvailable ? await _boardRenderer.GetImageDiffFromFenAsync(remote, local, PictureBoxRemote.Width, _appDataService.IsWhiteOnBottom) : PictureBoxRemote.Image = PictureBoxRemoteInitialImage;
            });

            BeginInvoke(updateAction);
        }
    }
}