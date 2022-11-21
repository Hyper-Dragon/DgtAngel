using DgtCherub.Helpers;
using DgtCherub.Services;
using DgtEbDllWrapper;
using DgtLiveChessWrapper;
using DynamicBoard;
using DynamicBoard.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QRCoder;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows.Forms;


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
        private const string VERSION_NUMBER = "0.4.3 PLAY-RC-01";
        private const string PROJECT_URL = "https://hyper-dragon.github.io/DgtAngel/";
        private const string VIRTUAL_CLOCK_PORT = "37964";
        private const string VIRTUAL_CLOCK_LINK = @$"http://127.0.0.1:{VIRTUAL_CLOCK_PORT}";
        private const string CHESS_DOT_COM_PLAY_LINK = @"https://www.chess.com/play/online";
        private const string CHESS_DOT_COM_DGT_FORUM = @"https://www.chess.com/clubs/forum/dgt-chess-club";
        private const string CHESS_DOT_COM_PEGASUS_FORUM = @"https://www.chess.com/clubs/forum/dgt-pegasus-centaur-e-board-users";
        private const string PROJECT_LINK = @"https://hyper-dragon.github.io/DgtAngel/";
        private const string PROJECT_ISSUES = @"https://github.com/Hyper-Dragon/DgtAngel/issues/new/choose";
        private const string PROJECT_RELEASES = @"https://github.com/Hyper-Dragon/DgtAngel/releases";
        private const string PROJECT_CHESS_STATS = @"https://hyper-dragon.github.io/ChessStats/";
        private const string DL_LIVE_CHESS = @"http://www.livechesscloud.com/";
        private const string DL_RABBIT = @"https://digitalgametechnology.com/support/software/software-downloads";
        private const string DL_CHROME_PLUGIN = @"https://chrome.google.com/webstore/detail/dgt-angel-cdc-play/mbkgcknkcljokhinimibaminlolgoecc";
        private const string PP_CODE = "QNKADKV5BAM5C";
        private const string PP_LINK = @$"https://www.paypal.com/donate?hosted_button_id={PP_CODE}&source=url";
        private const string GITHUB_SPN_LINK = @"https://github.com/sponsors/Hyper-Dragon";

        private readonly System.Resources.ResourceManager DEFAULT_MOVE_VOICE = DgtCherub.Assets.Moves_en_02.ResourceManager;
        private System.Resources.ResourceManager VoiceMoveResManager;

        private readonly IHost _iHost;
        private readonly ILogger _logger;
        private readonly IAngelHubService _angelHubService;
        private readonly IDgtEbDllFacade _dgtEbDllFacade;
        private readonly IDgtLiveChess _dgtLiveChess;
        private readonly IBoardRenderer _boardRenderer;
        private readonly ISequentialVoicePlayer _voicePlayeStatus;
        private readonly ISequentialVoicePlayer _voicePlayerMoves;
        private readonly ISequentialVoicePlayer _voicePlayerMovesNoDrop;
        private readonly ISequentialVoicePlayer _voicePlayerTime;

        private int LastFormWidth = 705;
        private int CollapsedWidth = 705;
        private Size InitialMinSize = new(420, 420);
        private Size InitialMaxSize = new(0, 0);
        private Color BoredLabelsInitialColor = Color.Silver;
        private Image PictureBoxLocalInitialImage;
        private Image PictureBoxRemoteInitialImage;

        private readonly Dictionary<string, Bitmap> qrCodeImageDictionary;

        private bool EchoInternallMessagesToConsole { get; set; } = true;
        private bool EchoExternalMessagesToConsole { get; set; } = true;

        private bool IncludeSecs { get; set; } = true;
        private bool PlayerBeepOnly { get; set; } = false;
        private bool IsSilentBeep { get; set; } = false;

        private readonly bool IsRabbitInstalled = false;

        // Get Hostname 
        private readonly string hostName;
        private readonly string[] thisMachineIpV4Addrs;


        [FlagsAttribute]
        public enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
            // Legacy flag, should not be used.
            // ES_USER_PRESENT = 0x00000004
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        [DllImport("user32")]
        private static extern bool HideCaret(IntPtr hWnd);

        public Form1(IHost iHost, ILogger<Form1> logger, IAngelHubService appData, IDgtEbDllFacade dgtEbDllFacade,
                     IDgtLiveChess dgtLiveChess, IBoardRenderer boardRenderer, ISequentialVoicePlayer voicePlayer,
                     ISequentialVoicePlayer voicePlayerMoves, ISequentialVoicePlayer voicePlayerMovesNoDrop, ISequentialVoicePlayer voicePlayerTime)
        {
            _iHost = iHost;
            _logger = logger;
            _angelHubService = appData;

            _dgtLiveChess = dgtLiveChess;

            _dgtEbDllFacade = dgtEbDllFacade;
            _boardRenderer = boardRenderer;
            _voicePlayeStatus = voicePlayer;
            _voicePlayerMoves = voicePlayerMoves;
            _voicePlayerTime = voicePlayerTime;
            _voicePlayerMovesNoDrop = voicePlayerMovesNoDrop;

            _voicePlayeStatus.Start();
            _voicePlayerMoves.Start();
            _voicePlayerMovesNoDrop.Start(20);
            _voicePlayerTime.Start();

            InitializeComponent();



            if (Process.GetProcessesByName("DGT LiveChess").Length > 0)
            {
                //TODO: Prep for Live Chess removal update
                //Console.WriteLine("Process");
            }
            else
            {
                //TODO: Start the Rabbit Plugin if we can...
                //      add note - is your clock on option 25 and set (play button)  - the time wont work otherwise
                //      The startup order seems to matter - if you want the clock get a bluetooth connection 1st then plug in the board
                try
                {
                    //_dgtEbDllFacade.Init();
                    //IsRabbitInstalled = true;
                }
                catch (DllNotFoundException)
                {
                    _dgtEbDllFacade = null;
                    IsRabbitInstalled = false;
                }
            }

            // Get Hostname and v4 IP Addrs
            try
            {
                hostName = Dns.GetHostName();
                thisMachineIpV4Addrs = Dns.GetHostEntry(hostName).AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork).Select(x => x.ToString()).ToArray();
                qrCodeImageDictionary = new Dictionary<string, Bitmap>();
            }
            catch
            {
                //If this fails don't error - UI Quality of life only
                hostName = "";
                thisMachineIpV4Addrs = Array.Empty<string>();
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            PreventScreensaver(false);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DgtCherub.Properties.UserSettings.Default.VolStatus = UpDownVolStatus.Value;
            DgtCherub.Properties.UserSettings.Default.VolMoves = UpDownVolMoves.Value;
            DgtCherub.Properties.UserSettings.Default.VolTime = UpDownVolTime.Value;
            DgtCherub.Properties.UserSettings.Default.AlwaysOnTop = CheckBoxOnTop.Checked;
            DgtCherub.Properties.UserSettings.Default.PreventSleep = CheckBoxPreventSleep.Checked;
            DgtCherub.Properties.UserSettings.Default.IncludeSeconds = CheckBoxIncludeSecs.Checked;
            DgtCherub.Properties.UserSettings.Default.BeepMode = CheckBoxPlayerBeep.Checked;
            DgtCherub.Properties.UserSettings.Default.Silent = CheckboxSilentBeep.Checked;
            DgtCherub.Properties.UserSettings.Default.FontSize = UpDownFontSize.Value;
            DgtCherub.Properties.UserSettings.Default.MoveVoiceIdx = ComboBoxMoveVoice.SelectedIndex;
            DgtCherub.Properties.UserSettings.Default.MatcherDelay = UpDownVoiceDelay.Value;
            DgtCherub.Properties.UserSettings.Default.MatcherLocalDelay = UpDownLocalDelay.Value;
            DgtCherub.Properties.UserSettings.Default.MatcherLocalFromMismatchDelay = UpDownFromMismatchDelay.Value;
            DgtCherub.Properties.UserSettings.Default.Save();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            SuspendLayout();

            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;

            //Set Appsettings from the designer values...
            EchoInternallMessagesToConsole = CheckBoxRecieveLog.Checked;
            EchoExternalMessagesToConsole = CheckBoxShowInbound.Checked;

            ToolStripStatusLabelVersion.Text = $"Ver. {VERSION_NUMBER}";

            //Remove the about tab for now (reserved for new content)
            TabControlSidePanel.TabPages.RemoveAt(0);

            TabControlSidePanel.SelectedTab = TabPageConfig;

            CheckBoxIncludeSecs.Checked = DgtCherub.Properties.UserSettings.Default.IncludeSeconds;
            CheckBoxPlayerBeep.Checked = DgtCherub.Properties.UserSettings.Default.BeepMode;
            CheckboxSilentBeep.Checked = DgtCherub.Properties.UserSettings.Default.Silent;

            CheckBoxOnTop.Checked = DgtCherub.Properties.UserSettings.Default.AlwaysOnTop;
            CheckBoxOnTop_CheckedChanged(this, null);

            LinkLabelAbout1.Text = "GitHub Project Page";
            LinkLabelAbout1.LinkArea = new LinkArea(0, LinkLabelAbout1.Text.Length);
            LinkLabelAbout1.Visible = true;
            LinkLabelAbout1.Click += (object sender, EventArgs e) =>
            {
                _ = Process.Start(new ProcessStartInfo
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
            CollapsedWidth = TabControlSidePanel.Width + TabControlSidePanel.ItemSize.Height - TabControlSidePanel.Padding.X;

            //If no rabbit disable rabbit things..
            if (!IsRabbitInstalled)
            {
                ButtonRabbitConfig1.Visible = false;
                ButtonRabbitConf2.Visible = false;
                GroupBoxClockTest.Visible = false;
            }

            // Generate the clock QR Codes + set images
            if (thisMachineIpV4Addrs.Length > 0)
            {
                QRCodeGenerator qrGenerator = new();
                thisMachineIpV4Addrs.OrderByDescending(item => item.ToString())
                    .ToList<string>()
                    .ForEach(addr =>
                    {
                        _ = DomainUpDown.Items.Add(addr);
                        QRCode qrCode = new(qrGenerator.CreateQrCode($@"http://{addr}:{VIRTUAL_CLOCK_PORT}/", QRCodeGenerator.ECCLevel.Q));
                        qrCodeImageDictionary.Add(addr, qrCode.GetGraphic(20));
                    });

                DomainUpDown.SelectedIndex = 0;
            }

            //Set voice/volume to default
            UpDownVolStatus.Value = DgtCherub.Properties.UserSettings.Default.VolStatus;
            UpDownVolMoves.Value = DgtCherub.Properties.UserSettings.Default.VolMoves;
            UpDownVolTime.Value = DgtCherub.Properties.UserSettings.Default.VolTime;

            ComboBoxMoveVoice.SelectedIndex = DgtCherub.Properties.UserSettings.Default.MoveVoiceIdx;
            ComboBoxMoveVoice_SelectedValueChanged(this, null);

            UpDownVoiceDelay.Value = DgtCherub.Properties.UserSettings.Default.MatcherDelay;
            UpDownVoiceDelay_ValueChanged(this, null);

            UpDownLocalDelay.Value = DgtCherub.Properties.UserSettings.Default.MatcherLocalDelay;
            UpDownLocalDelay_ValueChanged(this, null);

            UpDownFromMismatchDelay.Value = DgtCherub.Properties.UserSettings.Default.MatcherLocalFromMismatchDelay;
            UpDownFromMismatchDelay_ValueChanged(this, null);

            UpDownFontSize.Value = DgtCherub.Properties.UserSettings.Default.FontSize;
            UpDownFontSize_ValueChanged(this, null);

            //Hides the caret from up/down boxes
            _ = HideCaret(UpDownVolStatus.Controls[1].Handle);
            _ = HideCaret(UpDownVolMoves.Controls[1].Handle);
            _ = HideCaret(UpDownVolTime.Controls[1].Handle);
            _ = HideCaret(DomainUpDown.Controls[1].Handle);

            CheckBoxPreventSleep.Checked = DgtCherub.Properties.UserSettings.Default.PreventSleep;
            PreventScreensaver(CheckBoxPreventSleep.Checked);

            //Make sure this is set
            DoubleBuffered = true;

            Update();
            ResumeLayout();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            ClearConsole();

            _angelHubService.OnOrientationFlipped += DisplayBoardImages;

            _angelHubService.OnLocalFenChange += (string localFen) =>
            {
                TextBoxConsole.AddLine($"Local board changed [{localFen}]");
                DisplayBoardImages();
            };

            _angelHubService.OnRemoteFenChange += (string fromRemoteFen, string toRemoteFen, string lastMove, string clockFen, string boardFen) =>
            {
                TextBoxConsole.AddLine($"Remote board changed to [{toRemoteFen}] from [{fromRemoteFen}] [{lastMove}] [clk={clockFen[..1]}::brd={boardFen[..1]}]");
                DisplayBoardImages();
            };

            _angelHubService.OnBoardMissmatch += (long timeTicks, int diffCount, string lastLocalFenMatch, string localFen) =>
            {
                TextBoxConsole.AddLine($"The boards DO NOT match [Diff:{diffCount}] [Last Local Match:{lastLocalFenMatch}]", TEXTBOX_MAX_LINES);

                LabelLocalDgt.BackColor = Color.Red;
                LabelRemoteBoard.BackColor = Color.Red;

                //If the board difference is a single move and the remote board has not changed since the last match
                //then we can assume the player has not moved their opponants piece.  In this case we can play the alternative
                //audio
                _voicePlayeStatus.Speak((diffCount == 2 && lastLocalFenMatch == localFen) ? Assets.Speech_en_01.NotReplayed_AP : Assets.Speech_en_01.Mismatch_AP);
            };

            _angelHubService.OnRemoteWatchStarted += () =>
            {
                _voicePlayeStatus.Speak(Assets.Speech_en_01.CdcWatching_AP);
            };

            _angelHubService.OnRemoteWatchStopped += () =>
            {
                _voicePlayeStatus.Speak(Assets.Speech_en_01.CdcStoppedWatching_AP);
            };

            _angelHubService.OnBoardMatcherStarted += () =>
            {
                LabelLocalDgt.BackColor = Color.Yellow;
                LabelRemoteBoard.BackColor = Color.Yellow;
            };

            _angelHubService.OnBoardMatchFromMissmatch += (_) =>
            {
                TextBoxConsole.AddLine($"The boards now match", TEXTBOX_MAX_LINES);
                _voicePlayeStatus.Speak(Assets.Speech_en_01.Match_AP);
            };

            _angelHubService.OnBoardMatch += (_,_) =>
            {
                LabelLocalDgt.BackColor = BoredLabelsInitialColor;
                LabelRemoteBoard.BackColor = BoredLabelsInitialColor;
            };

            _angelHubService.OnRemoteDisconnect += DisplayBoardImages;

            _angelHubService.OnPlayWhiteClockAudio += (audioFilename) =>
            {

                if ((_angelHubService.IsWhiteOnBottom && IncludeSecs) ||
                    (_angelHubService.IsWhiteOnBottom && !IncludeSecs && _angelHubService.WhiteClockMsRemaining > 55 * 1000))
                {
                    _voicePlayerTime.Speak(DgtCherub.Assets.Time_en_01.ResourceManager.GetStream($"{audioFilename}_AP"));
                }
            };

            _angelHubService.OnPlayBlackClockAudio += (audioFilename) =>
            {
                if ((!_angelHubService.IsWhiteOnBottom && IncludeSecs) ||
                     (!_angelHubService.IsWhiteOnBottom && !IncludeSecs && _angelHubService.BlackClockMsRemaining > 55 * 1000))
                {
                    _voicePlayerTime.Speak(DgtCherub.Assets.Time_en_01.ResourceManager.GetStream($"{audioFilename}_AP"));
                }
            };

            _angelHubService.OnClockChange += () =>
            {
                //TODO: replace the runwho + LabelWhiteClock.IsHandleCreated????
                //_logger?.LogTrace($">>Recieved Clock Update ({_angelHubService.WhiteClock}) ({_angelHubService.BlackClock}) ({_angelHubService.RunWhoString})", TEXTBOX_MAX_LINES);
                _logger?.LogTrace($">>Recieved Clock Update", TEXTBOX_MAX_LINES);

                if (!IsDisposed && IsHandleCreated && !TopLevelControl.IsDisposed)
                {
                    Invoke(() =>
                    {
                        LabelWhiteClock.Text = $"{((_angelHubService.RunWhoString is "3" or "1") ? "*" : " ")}{_angelHubService.WhiteClock}";
                        LabelBlackClock.Text = $"{((_angelHubService.RunWhoString is "3" or "2") ? "*" : " ")}{_angelHubService.BlackClock}";
                        ToolStripStatusLabelLastUpdate.Text = $"[Updated@{System.DateTime.Now.ToLongTimeString()}]";
                    });
                }
            };

            _angelHubService.OnNewMoveDetected += (moveString, isPlayerTurn) =>
            {
                if (PlayerBeepOnly && isPlayerTurn)
                {
                    string soundName = "";
                    soundName = moveString switch
                    {
                        "1/2-1/2" => "Words_GameDrawn",
                        "1-0" => "Words_WhiteWins",
                        "0-1" => "Words_BlackWins",
                        _ => ""
                    };

                    if (!string.IsNullOrEmpty(soundName))
                    {
                        _voicePlayerMovesNoDrop.Speak(VoiceMoveResManager.GetStream($"{soundName}_AP"));
                    }
                    else
                    {
                        if (!IsSilentBeep)
                        {
                            _voicePlayerMovesNoDrop.Speak(DgtCherub.Assets.Speech_en_01.ResourceManager.GetStream("Beep_AP"));
                        }
                    }
                }
                else
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
                        (PlayerBeepOnly ? _voicePlayerMovesNoDrop : _voicePlayerMoves).Speak(VoiceMoveResManager.GetStream($"{soundName}_AP"));
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
                                '#' => "Words_Check",
                                '=' => "Words_PromotesTo",
                                _ => "Words_Missing",
                            };

                            playlist.Add(VoiceMoveResManager.GetStream($"{soundName}_AP"));
                        }

                        (PlayerBeepOnly ? _voicePlayerMovesNoDrop : _voicePlayerMoves).Speak(playlist);
                    }
                }
            };

            _angelHubService.OnNotification += (source, message) =>
            {
                if ((source == "ANGEL" && EchoExternalMessagesToConsole) ||
                    (source != "ANGEL" && EchoInternallMessagesToConsole))
                {
                    TextBoxConsole.AddLine($"{message}", TEXTBOX_MAX_LINES);
                }
            };

            _dgtLiveChess.OnLiveChessDisconnected += (source, eventArgs) =>
            {
                _angelHubService.ResetLocalBoardState();
                _voicePlayeStatus.Speak(Assets.Speech_en_01.DgtLcDisconnected_AP);
                DisplayBoardImages();
                TextBoxConsole.AddLine($"Live Chess DISCONNECTED [{eventArgs.ResponseOut}]", TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnLiveChessConnected += (source, eventArgs) =>
            {
                _voicePlayeStatus.Speak(Assets.Speech_en_01.DgtLcConnected_AP);
                TextBoxConsole.AddLines(new string[]{$"{"".PadRight(67,'-')}",
                                                     $"Live Chess running [{eventArgs.ResponseOut}]",
                                                     $"{"".PadRight(67,'-')}"}, TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnBoardConnected += (source, eventArgs) =>
            {
                PictureBoxLocal.Image = PictureBoxLocalInitialImage;
                _voicePlayeStatus.Speak(Assets.Speech_en_01.DgtConnected_AP);
                TextBoxConsole.AddLines(new string[]{$"{"".PadRight(67,'-')}",
                                                     $"Board found [{eventArgs.ResponseOut}]",
                                                     $"{"".PadRight(67,'-')}"}, TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnBoardDisconnected += (source, eventArgs) =>
            {
                DisplayBoardImages();
                _voicePlayeStatus.Speak(Assets.Speech_en_01.DgtDisconnected_AP);
                _angelHubService.ResetLocalBoardState();
                TextBoxConsole.AddLine($"Board DISCONNECTED [{eventArgs.ResponseOut}]", TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnCantFindBoard += (source, eventArgs) =>
            {
                _voicePlayeStatus.Speak(Assets.Speech_en_01.DgtCantFindBoard_AP);
                TextBoxConsole.AddLine($"Board DISCONNECTED [{eventArgs.ResponseOut}]", TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnError += (obj, eventArgs) =>
            {
                TextBoxConsole.AddLine($"{eventArgs.ResponseOut}", TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnBatteryCritical += (obj, eventArgs) =>
            {
                _voicePlayeStatus.Speak(Assets.Speech_en_01.BatteryCritical_AP);
                TextBoxConsole.AddLine($"{eventArgs.ResponseOut}", TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnBatteryLow += (obj, eventArgs) =>
            {
                _voicePlayeStatus.Speak(Assets.Speech_en_01.BatteryLow_AP);
                TextBoxConsole.AddLine($"{eventArgs.ResponseOut}", TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnBatteryOk += (obj, eventArgs) =>
            {
                _voicePlayeStatus.Speak(Assets.Speech_en_01.BatteryOk_AP);
                TextBoxConsole.AddLine($"{eventArgs.ResponseOut}", TEXTBOX_MAX_LINES);
            };

            _dgtLiveChess.OnFenRecieved += (obj, eventArgs) =>
            {
                //TextBoxConsole.AddLine($"Local DGT board changed [{eventArgs.ResponseOut}]", TEXTBOX_MAX_LINES);
                _angelHubService.LocalBoardUpdate(eventArgs.ResponseOut);
            };

            //All the Events are set up so we can start watching the local board and running the inbound API
            _ = Task.Run(_dgtLiveChess.PollDgtBoard);
            _ = Task.Run(_iHost.Run);
        }

        //*********************************************//
        #region Form Control Events
        private void CheckBoxShowConsole_CheckedChanged(object sender, EventArgs e)
        {
            SuspendLayout();

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

            Update();
            ResumeLayout();
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
        private void CheckBoxRecieveLog_CheckedChanged(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"Cherub {(CheckBoxRecieveLog.Checked ? "will" : "WILL NOT")} display notification messages.", TEXTBOX_MAX_LINES);
            EchoExternalMessagesToConsole = CheckBoxRecieveLog.Checked;
        }

        private void CheckBoxShowInbound_CheckedChanged(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"Cherub {(CheckBoxShowInbound.Checked ? "will" : "WILL NOT")} display notification messages from DGT Angel.", TEXTBOX_MAX_LINES);
            EchoExternalMessagesToConsole = CheckBoxShowInbound.Checked;
        }

        private void ButtonClearConsole_Click(object sender, EventArgs e)
        {
            ClearConsole();
        }

        private void TabPageBoards_Enter(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"Selected the Board Tab...you {(CheckBoxOnTop.Checked ? "will always be on top." : "will not be on top.")}", TEXTBOX_MAX_LINES);
            ((Form)TopLevelControl).TopMost = CheckBoxOnTop.Checked;
        }

        private void TabPageBoards_Leave(object sender, EventArgs e)
        {
            ((Form)TopLevelControl).TopMost = false;
        }

        private void CheckBoxOnTop_CheckedChanged(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"The Board tab {(CheckBoxOnTop.Checked ? "will always be on top." : "will no longer be on top.")}", TEXTBOX_MAX_LINES);

            if (CheckBoxOnTop.Checked)
            {
                TopMost = true;
            }
            else
            {
                TopMost = false;

                TextBoxConsole.AddLines(new string[] { $"Keeping the board tab on top is handy when playing since you are able",
                                                       $"to see it without Angel losing focus on the game board."}, TEXTBOX_MAX_LINES);
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to exit?", "Cherub", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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
            _ = TextBoxConsole.RunProcessWithComments("chrome",
                                                   CHESS_DOT_COM_PLAY_LINK,
                                                   $"Trying to open Chess.com in Chrome....",
                                                   $"...Chess.com openend.",
                                                   TEXTBOX_MAX_LINES);
        }

        private void ChesscomDgtForumsMenuItem_Click(object sender, EventArgs e)
        {
            _ = TextBoxConsole.RunProcessWithComments(CHESS_DOT_COM_DGT_FORUM,
                                                   "",
                                                   $"Trying to open the Chess.com DGT forum....",
                                                   $"...the Chess.com DGT forum opened.",
                                                   TEXTBOX_MAX_LINES);
        }

        private void ChessStatsMenuItem_Click(object sender, EventArgs e)
        {
            _ = TextBoxConsole.RunProcessWithComments(PROJECT_CHESS_STATS,
                                       "",
                                       $"Trying to open the ChessStats site....",
                                       $"...the ChessStats site opened.",
                                       TEXTBOX_MAX_LINES);
        }

        private void ChesscomPegasusForumsMenuItem_Click(object sender, EventArgs e)
        {
            _ = TextBoxConsole.RunProcessWithComments(CHESS_DOT_COM_PEGASUS_FORUM,
                                                   "",
                                                   $"Trying to open the Chess.com DGT Pegasus Centaur e-Board Users forum....",
                                                   $"...the Chess.com forum opened.",
                                                   TEXTBOX_MAX_LINES);
        }

        private void KillLiveChessMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to kill the Live Chess process?", "Cherub", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _ = TextBoxConsole.RunProcessWithComments(@"Taskkill",
                                                      "/IM \"DGT LiveChess.exe\" /F",
                                                      $"Trying to kill 'DGT LiveChess.exe'....",
                                                      $"...done. 'DGT LiveChess.exe' is no longer running",
                                                      TEXTBOX_MAX_LINES,
                                                      useShellExecute: false);
            }
        }

        private void DgtAngelChromeExtensionMenuItem_Click(object sender, EventArgs e)
        {
            _ = TextBoxConsole.RunProcessWithComments(DL_CHROME_PLUGIN,
                                                  "",
                                                  $"Trying to open the download page for the Chrome Plugin....",
                                                  $"...the download page opened.",
                                                  TEXTBOX_MAX_LINES);
        }

        private void DgtLiveChessSoftwareMenuItem_Click(object sender, EventArgs e)
        {
            _ = TextBoxConsole.RunProcessWithComments(DL_LIVE_CHESS,
                                                   "",
                                                   $"Trying to open the download page for the Live Chess Software....",
                                                   $"...the download page opened.",
                                                   TEXTBOX_MAX_LINES);
        }

        private void DgtDriversRabbitPluginMenuItem_Click(object sender, EventArgs e)
        {
            _ = TextBoxConsole.RunProcessWithComments(DL_RABBIT,
                                                   "",
                                                   $"Trying to open the download page for the DGT drivers....",
                                                   $"...the download page is opened.",
                                                   TEXTBOX_MAX_LINES);
        }

        private void ProjectPageMenuItem_Click(object sender, EventArgs e)
        {
            _ = TextBoxConsole.RunProcessWithComments(PROJECT_LINK,
                                                  "",
                                                  $"Trying to open DGT Angel project page....",
                                                  $"...the project page is opened.",
                                                  TEXTBOX_MAX_LINES);
        }

        private void ReportIssuesMenuItem_Click(object sender, EventArgs e)
        {
            _ = TextBoxConsole.RunProcessWithComments(PROJECT_ISSUES,
                                                  "",
                                                  $"Trying to open DGT Angel issues page....",
                                                  $"...the issues page is opened.",
                                                  TEXTBOX_MAX_LINES);
        }

        private void ReleasesMenuItem_Click(object sender, EventArgs e)
        {
            _ = TextBoxConsole.RunProcessWithComments(PROJECT_RELEASES,
                                                  "",
                                                  $"Trying to open the DGT Angel beta releases page....",
                                                  $"...the releases page is opened.",
                                                  TEXTBOX_MAX_LINES);
        }

        private void VirtualClockMenuItem_Click(object sender, EventArgs e)
        {
            _ = TextBoxConsole.RunProcessWithComments(VIRTUAL_CLOCK_LINK,
                                      "",
                                      $"Trying to open the Virtual Clock....",
                                      $"...the Virtual Clock is opened.",
                                      TEXTBOX_MAX_LINES);
        }

        private void VirtualClockWindowlessMenuItem_Click(object sender, EventArgs e)
        {
            _ = TextBoxConsole.RunProcessWithComments("chrome",
                                      $"--app={VIRTUAL_CLOCK_LINK}",
                                      $"Trying to open the Virtual Clock in Chrome....",
                                      $"...virtual clock openend.",
                                      TEXTBOX_MAX_LINES);
        }

        private void DonateViaPayPalMenuItem_Click(object sender, EventArgs e)
        {
            _ = TextBoxConsole.RunProcessWithComments(PP_LINK,
                                      "",
                                      $"Thank you very much for thinking about donating....",
                                      $"...PayPal should be open now.",
                                      TEXTBOX_MAX_LINES);
        }

        private void DonateViaGitHubMenuItem_Click(object sender, EventArgs e)
        {
            _ = TextBoxConsole.RunProcessWithComments(GITHUB_SPN_LINK,
                          "",
                          $"Thank you very much for thinking about donating....",
                          $"...GitHub should be open now.",
                          TEXTBOX_MAX_LINES);
        }


        #endregion
        //*********************************************//

        //*********************************************//
        #region Volume Controls
        private void UpDownVolStatus_ValueChanged(object sender, EventArgs e)
        {
            _voicePlayeStatus.Volume = ((float)((NumericUpDown)sender).Value) / 10f;
            _ = HideCaret(((NumericUpDown)sender).Controls[1].Handle);
        }

        private void UpDownVolMoves_ValueChanged(object sender, EventArgs e)
        {
            _voicePlayerMoves.Volume = ((float)((NumericUpDown)sender).Value) / 10f;
            _voicePlayerMovesNoDrop.Volume = ((float)((NumericUpDown)sender).Value) / 10f;
            _ = HideCaret(((NumericUpDown)sender).Controls[1].Handle);
        }

        private void UpDownVolTime_ValueChanged(object sender, EventArgs e)
        {
            _voicePlayerTime.Volume = ((float)((NumericUpDown)sender).Value) / 10f;
            _ = HideCaret(((NumericUpDown)sender).Controls[1].Handle);
        }

        private void UpDownVolHideCaret(object sender, EventArgs e)
        {
            _ = HideCaret(((NumericUpDown)sender).Controls[1].Handle);
        }
        #endregion
        //*********************************************//

        //*********************************************//
        #region QR Code Change
        private void DomainUpDown_SelectedItemChanged(object sender, EventArgs e)
        {
            PictureBoxQrCode.Image = qrCodeImageDictionary[((DomainUpDown)sender).SelectedItem.ToString()];
        }

        private void UpDownDomainHideCaret(object sender, EventArgs e)
        {
            _ = HideCaret(((DomainUpDown)sender).Controls[1].Handle);
        }
        #endregion
        //*********************************************//


        private void ClearConsole()
        {
            TextBoxConsole.Text = "";
            TextBoxConsole.Update();

            TextBoxConsole.AddLine($"---------------------------------------------------------------------------------", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($" Welcome to...                                                                   ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($" ██████╗  ██████╗ ████████╗     ██████╗██╗  ██╗███████╗██████╗ ██╗   ██╗██████╗  ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($" ██╔══██╗██╔════╝ ╚══██╔══╝    ██╔════╝██║  ██║██╔════╝██╔══██╗██║   ██║██╔══██╗ ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($" ██║  ██║██║  ███╗   ██║       ██║     ███████║█████╗  ██████╔╝██║   ██║██████╔╝ ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($" ██║  ██║██║   ██║   ██║       ██║     ██╔══██║██╔══╝  ██╔══██╗██║   ██║██╔══██╗ ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($" ██████╔╝╚██████╔╝   ██║       ╚██████╗██║  ██║███████╗██║  ██║╚██████╔╝██████╔╝ ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($" ╚═════╝  ╚═════╝    ╚═╝        ╚═════╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝ ╚═════╝ ╚═════╝  ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"   Hyper-Dragon :: Version {VERSION_NUMBER} :: {PROJECT_URL}", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"---------------------------------------------------------------------------------", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"NOTE   : This project IS NOT affiliated with either DGT or Chess.com in any way.", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"WARNING: I think that this release can now be considered a beta version.  I can", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         confidently say that it works not only on my machine but also those", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         machines of others...as a beta your mileage may vary but please report", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         any defects via the links menu.", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"PreReq : You will need A DGT Board and the Live Chess Software installed on this", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         machine, just as you would for playing on Chess.com.  You will also need", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         the Chrome browser with the 'DTG Angel' plugin installed.  Don't forget", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         to enable your board in the Chess.com options.", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"Thanks : Thanks go to BaronVonChickenpants, Hamilton53, er642, danielbaechli and", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         KevinTheChessGnome for their support and feedback and to Fake-Angel for", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         for the new move voice (en-02).", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"Rabbit : {(IsRabbitInstalled ? $"Using {_dgtEbDllFacade.GetRabbitVersionString()}" : "DGT Rabbit is not installed or is not required in this version.")}", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"V.Clock: IP Addresses for [{(string.IsNullOrEmpty(hostName) ? "NO HOST!" : hostName)}] are [{(string.IsNullOrEmpty(hostName) ? "" : string.Join(',', thisMachineIpV4Addrs))}]", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         The Virtual Clock is available on http://<Your IP>:{VIRTUAL_CLOCK_PORT}/", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         Alternatively, point your phone at the QR code on the clock tab (don't", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         forget that you will need to open port 37964 on the windows firewall", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         for this to work).", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"---------------------------------------------------------------------------------", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"*** PLAY BOARD CHANGE NOTE ***", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         For previous Live board users you will must update your chrome extension...", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"            Go to Links->Downloads->DGT Angel Chrome Extension", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         At the time of this release there were outstanding problems with the", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         DGT board on the 'Play' interface. To castle move your King 2 squares", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         very, very fast and never move your rook until the move has been", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         acknowledged.  Always pick up your pieces (never slide them).  Keep a", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         mouse handy as CDC sometimes refuses to accept your move on the physical   ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         board.  Finally, if you have the 'ghost move' problem you will need the", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         next release (a pre-release is available from the GitHub page).  Check", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         for news on the Chess.com DGT Club forum.", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"---------------------------------------------------------------------------------", TEXTBOX_MAX_LINES, false);
        }


        private static void PreventScreensaver(bool preventSleep)
        {
            // Use 'powercfg -requests' to test if the power settings are set correctly

            _ = preventSleep
                ? SetThreadExecutionState(EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_CONTINUOUS)
                : SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        }


        private void DisplayBoardImages()
        {
            if (!IsDisposed && IsHandleCreated && !TopLevelControl.IsDisposed)
            {
                Action updateAction = new(async () =>
                {
                    ToolStripStatusLabelLastUpdate.Text = $"[Updated@{System.DateTime.Now.ToLongTimeString()}]";

                    string local = _angelHubService.IsLocalBoardAvailable ? _angelHubService.LocalBoardFEN : _angelHubService.RemoteBoardFEN;
                    string remote = _angelHubService.IsRemoteBoardAvailable ? _angelHubService.RemoteBoardFEN : _angelHubService.LocalBoardFEN;

                    PictureBoxLocal.Image = _angelHubService.IsLocalBoardAvailable ? (await _boardRenderer.GetPngImageDiffFromFenAsync(local, remote, PictureBoxRemote.Width, _angelHubService.IsWhiteOnBottom)).ConvertPngByteArrayToBitmap()
                                                                                   : PictureBoxLocal.Image = PictureBoxLocalInitialImage;

                    PictureBoxRemote.Image = _angelHubService.IsRemoteBoardAvailable ? (await _boardRenderer.GetPngImageDiffFromFenAsync(remote, local, PictureBoxRemote.Width, _angelHubService.IsWhiteOnBottom)).ConvertPngByteArrayToBitmap()
                                                                                       : PictureBoxRemote.Image = PictureBoxRemoteInitialImage;
                });

                _ = BeginInvoke(updateAction);
            }
        }

        private void TabPageConfig_Click(object sender, EventArgs e)
        {

        }


        private void UpDownFontSize_ValueChanged(object sender, EventArgs e)
        {
            TextBoxConsole.Font = new Font("Consolas",
                                            (float)UpDownFontSize.Value,
                                             GraphicsUnit.Pixel);
        }

        private void UpDownVoiceDelay_ValueChanged(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"You have {(int)UpDownVoiceDelay.Value} seconds to make your opponents move");
            _angelHubService.MatcherRemoteTimeDelayMs = (int)UpDownVoiceDelay.Value * 1000;
        }

        private void UpDownLocalDelay_ValueChanged(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"You have {UpDownLocalDelay.Value} seconds to make your move");
            _angelHubService.MatcherLocalDelayMs = (int)(UpDownLocalDelay.Value * 1000);
        }

        private void UpDownFromMismatchDelay_ValueChanged(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"When the board is not in sync you have {UpDownFromMismatchDelay.Value} seconds from change to re-check");
            _angelHubService.FromMismatchDelayMs = (int)(UpDownFromMismatchDelay.Value * 1000);
        }


        private void ComboBoxMoveVoice_SelectedValueChanged(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"Using Voice {ComboBoxMoveVoice.Text} for move announcements");

            VoiceMoveResManager = ComboBoxMoveVoice.Text switch
            {
                "en-01" => DgtCherub.Assets.Moves_en_01.ResourceManager,
                "en-02" => DgtCherub.Assets.Moves_en_02.ResourceManager,
                "en-03" => DgtCherub.Assets.Moves_en_03.ResourceManager,
                "en-04" => DgtCherub.Assets.Moves_en_04.ResourceManager,
                "en-05" => DgtCherub.Assets.Moves_en_05.ResourceManager,
                "en-06" => DgtCherub.Assets.Moves_en_06.ResourceManager,
                _ => DEFAULT_MOVE_VOICE

            };
        }

        private void CheckBoxPreventSleep_CheckedChanged(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"Windows {(((CheckBox)sender).Checked ? "WILL NOT sleep" : "MAY sleep")} while Cherub is running");
            PreventScreensaver(((CheckBox)sender).Checked);
        }

        private void CheckBoxIncludeSecs_CheckedChanged(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"Time Countdown {(((CheckBox)sender).Checked ? "WILL" : "WILL NOT")} include seconds in the final minute");
            IncludeSecs = ((CheckBox)sender).Checked;
        }

        private void CheckBoxPlayerBeep_CheckedChanged(object sender, EventArgs e)
        {
            TextBoxConsole.AddLine($"Player moves {(((CheckBox)sender).Checked ? "WILL" : "WILL NOT")} be vocalised");


            if (((CheckBox)sender).Checked)
            {
                TextBoxConsole.AddLine($"{(IsSilentBeep ? "No sound will be played" : "A sound will play instead of the move announcement")}");
            }

            PlayerBeepOnly = ((CheckBox)sender).Checked;
        }

        private void CheckboxSilentBeep_CheckedChanged(object sender, EventArgs e)
        {
            if (PlayerBeepOnly)
            {
                TextBoxConsole.AddLine($"Player moves WILL be vocalised");
                TextBoxConsole.AddLine($"{(((CheckBox)sender).Checked ? "No sound will be played" : "A sound will play instead of the move announcement")}");
            }
            else
            {
                TextBoxConsole.AddLine($"WARNING: Beep Mode is disabled - This option will have no effect");
            }

            IsSilentBeep = ((CheckBox)sender).Checked;
        }
    }
}
