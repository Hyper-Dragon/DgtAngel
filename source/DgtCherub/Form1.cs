using DgtCherub.Helpers;
using DgtCherub.Services;
using DgtLiveChessWrapper;
using DgtRabbitWrapper;
using DgtRabbitWrapper.DgtEbDll;
using DynamicBoard;
using DynamicBoard.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QRCoder;
using System.Drawing;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UciComms;
using UciComms.Data;

/*
 YOUR TURN LANGUAGE

English 	Connected. Your turn.
Dutch		Verbonden. Uw zet.
Danish	    Tilsluttet. Du er i trækket.
Estonian	Ühendatud. Sinu käik.
French 	    Conectado. Te toca mover.
Italy		Connesso. Tocca a te.
Latvian	    Savienots. Tavs gājiens.
Norwegian	Tilkoblet. Din tur.
Polish	    Szachownica podłączona. Twój ruch.
Spain 	    Connecté. À vous de jouer.
Ukranian	Підключено. Ваш хід.
 */

//Example Firewall Config
/*
netsh advfirewall firewall add rule name="Dgt Angel ALLOW Tcp Port 37964" dir=in action=allow protocol=TCP localport=37964
netsh advfirewall firewall show rule name="Dgt Angel ALLOW Tcp Port 37964"
netsh advfirewall firewall delete rule name="Dgt Angel ALLOW Tcp Port 37964"
 */

namespace DgtCherub
{
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    public partial class Form1 : Form
    {
        private LiveChessServer fakeLiveChessServer;

        private readonly string[] YOUR_TURN_LANG = { "Your turn",
                                                     "Uw zet",
                                                     "Du er i trækket",
                                                     "Sinu käik",
                                                     "Te toca mover",
                                                     "Tocca a te",
                                                     "Tavs gājiens",
                                                     "Din tur",
                                                     "Twój ruch",
                                                     "À vous de jouer",
                                                     "Ваш хід"};

        private const int TEXTBOX_MAX_LINES = 200;
        private const string VERSION_NUMBER = "0.4.6-IVOR-01";
        private const string PROJECT_URL = "https://hyper-dragon.github.io/DgtAngel/";
        private const int LIVE_CHESS_LISTEN_PORT = 1982;
        private const string VIRTUAL_CLOCK_PORT = "37964";
        private const string VIRTUAL_CLOCK_LINK = @$"http://127.0.0.1:{VIRTUAL_CLOCK_PORT}";
        private const string VIRTUAL_CLOCK_WH_LINK = $"{VIRTUAL_CLOCK_LINK}/CherubVirtualClock/GetClock/WingedHorse";
        private const string CHESS_DOT_COM_PLAY_LINK = @"https://www.chess.com/play/online";
        private const string LICHESS_PLAY_LINK = @"https://lichess.org/dgt/play";
        private const string CHESS_DOT_COM_DGT_FORUM = @"https://www.chess.com/clubs/forum/dgt-chess-club";
        private const string CHESS_DOT_COM_PEGASUS_FORUM = @"https://www.chess.com/clubs/forum/dgt-pegasus-centaur-e-board-users";
        private const string PROJECT_LINK = @"https://hyper-dragon.github.io/DgtAngel/";
        private const string PROJECT_ISSUES = @"https://github.com/Hyper-Dragon/DgtAngel/issues/new/choose";
        private const string PROJECT_RELEASES = @"https://github.com/Hyper-Dragon/DgtAngel/releases";
        private const string PROJECT_CHESS_STATS = @"https://hyper-dragon.github.io/ChessStats/";
        private const string DL_LIVE_CHESS = @"http://www.livechesscloud.com/";
        private const string DL_RABBIT = @"https://digitalgametechnology.com/support/software/software-downloads";
        private const string DL_CHROME_CDC_PLUGIN = @"https://chrome.google.com/webstore/detail/dgt-angel-cdc-play/mbkgcknkcljokhinimibaminlolgoecc";
        private const string DL_CHROME_LICHESS_PLUGIN = @"https://chrome.google.com/webstore/detail/dgt-angel-lichess/iapbigkgggibablinlgoabjkfaejlfhi";
        private const string PP_CODE = "QNKADKV5BAM5C"; //Not a secret
        private const string PP_LINK = @$"https://www.paypal.com/donate?hosted_button_id={PP_CODE}&source=url";
        private const string GITHUB_SPN_LINK = @"https://github.com/sponsors/Hyper-Dragon";
        private const string holegg08 = @"                 '='      '='      '='      '='      '='      '='                    ";
        private const string holegg03 = @"                [___]    [___]    [___]    [___]    [___]    [___]      ************ ";
        private const string holegg01 = @"                   .--._.--.--.__.--.--.__.--.--.__.--.--._.--.                      ";
        private const string holegg07 = @"           jgs  \::./    \::./    \::./    \::./    \::./    \::./                   ";
        private const string holegg05 = @"               |::   |  |::   |  |::   |  |::   |  |::   |  |::   |     * HOLIDAYS * ";
        private const string holegg06 = @"               \::.  /  \::.  /  \::.  /  \::.  /  \::.  /  \::.  /     ************ ";
        private const string holegg02 = @"                 _(_      _Y_      _Y_      _Y_      _Y_      _)_                    ";
        private const string holegg04 = @"                /:' \    /:' \    /:' \    /:' \    /:' \    /:' \      *  HAPPY   * ";


        private readonly System.Resources.ResourceManager DEFAULT_MOVE_VOICE = DgtCherub.Assets.Moves_en_02.ResourceManager;
        private System.Resources.ResourceManager VoiceMoveResManager;

        private IDgtEbDllFacade _dgtEbDllFacade;

        private readonly IHost _iHost;
        private readonly ILogger _logger;
        private readonly IAngelHubService _angelHubService;
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
        private bool isEngineActivationRequired = true;

        private UciChessEngine currentUciChessEngine;
        private string lastUciExe = "";
        private UciOptionSettings uciOptionSettings = new();

        private readonly Dictionary<string, Bitmap> qrCodeImageDictionary;

        private bool EchoInternallMessagesToConsole { get; set; } = true;
        private bool EchoExternalMessagesToConsole { get; set; } = true;
        private bool IncludeSecs { get; set; } = true;
        private bool PlayerBeepOnly { get; set; } = false;
        private bool IsSilentBeep { get; set; } = false;

        private bool IsUsingRabbit = false;

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

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool SetDllDirectory(string lpPathName);


        public Form1(IHost iHost, ILogger<Form1> logger, IAngelHubService appData, IDgtEbDllFacade dgtEbDllFacade,
                     IDgtLiveChess dgtLiveChess, IBoardRenderer boardRenderer, IUciEngineManager uciEngineManager, ISequentialVoicePlayer voicePlayer,
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

        private void StartBoardComms()
        {
            if (IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(endpoint => endpoint.Port == LIVE_CHESS_LISTEN_PORT))
            {
                //If Live Chess is running don't try to start rabbit 
            }
            else if (DgtCherub.Properties.UserSettings.Default.IsRabbitDisabled)
            {
                //If Rabbit is disabled don't try to start rabbit
            }
            else
            {
                //Set an alternate search path for the DGT DLLs - used to configure alternative board drivers
                string altDllPath = DgtCherub.Properties.UserSettings.Default.AltDgtDllPath;

                if (!string.IsNullOrEmpty(altDllPath) && Directory.Exists(altDllPath))
                {
                    TextBoxConsole.AddLine($"RABBIT : Alt board driver search path set to [{altDllPath}]", timeStamp: false);
                    TextBoxConsole.AddLine($"         WARNING:: This is an experimental feature and your results may vary.", timeStamp: false);
                    TextBoxConsole.AddLine($"                   3rd party drivers are NOT supported - contact the author for support.", timeStamp: false);

                    SetDllDirectory(altDllPath);
                }
                else
                {
                    TextBoxConsole.AddLine($"RABBIT : No alt driver path set...using native DGT driver.",timeStamp:false); 
                }


                TextBoxConsole.AddLine("---------------------------------------------------------------------------------------", timeStamp: false);

                try
                {
                    if (_dgtEbDllFacade.Init(_dgtEbDllFacade))
                    {
                        IsUsingRabbit = true;
                        string trackRunwho = "";

                        /*
                        _dgtEbDllFacade.OnStatusMessage += (object sender, StatusMessageEventArgs e) => {TextBoxConsole.AddLine($"RABBIT: {e.Message}");};
                        _dgtEbDllFacade.OnFenChanged += (object sender, FenChangedEventArgs e) => { TextBoxConsole.AddLine($"RABBIT: {e.FEN}"); };
                        _dgtEbDllFacade.OnBClock += (object sender, StatusMessageEventArgs e) => { TextBoxConsole.AddLine($"RABBIT: {e.Message}"); };
                        _dgtEbDllFacade.OnBlackMoveInput += (object sender, StatusMessageEventArgs e) => { TextBoxConsole.AddLine($"RABBIT: {e.Message}"); };
                        _dgtEbDllFacade.OnBlackMoveNow += (object sender, StatusMessageEventArgs e) => { TextBoxConsole.AddLine($"RABBIT: {e.Message}"); };
                        _dgtEbDllFacade.OnNewGame += (object sender, StatusMessageEventArgs e) => { TextBoxConsole.AddLine($"RABBIT: {e.Message}"); };
                        _dgtEbDllFacade.OnResult += (object sender, StatusMessageEventArgs e) => { TextBoxConsole.AddLine($"RABBIT: {e.Message}"); };
                        _dgtEbDllFacade.OnStartSetup += (object sender, StatusMessageEventArgs e) => { TextBoxConsole.AddLine($"RABBIT: {e.Message}"); };
                        _dgtEbDllFacade.OnStopSetupBTM += (object sender, StatusMessageEventArgs e) => { TextBoxConsole.AddLine($"RABBIT: {e.Message}"); };
                        _dgtEbDllFacade.OnStopSetupWTM += (object sender, StatusMessageEventArgs e) => { TextBoxConsole.AddLine($"RABBIT: {e.Message}"); };
                        _dgtEbDllFacade.OnWClock += (object sender, StatusMessageEventArgs e) => { TextBoxConsole.AddLine($"RABBIT: {e.Message}"); };
                        _dgtEbDllFacade.OnWhiteMoveInput += (object sender, StatusMessageEventArgs e) => { TextBoxConsole.AddLine($"RABBIT: {e.Message}"); };
                        _dgtEbDllFacade.OnWhiteMoveNow += (object sender, StatusMessageEventArgs e) => { TextBoxConsole.AddLine($"RABBIT: {e.Message}"); };
                        */

                        _angelHubService.OnClockChange += () =>
                        {
                            if (trackRunwho != _angelHubService.RunWhoString)
                            {
                                TextBoxConsole.AddLine($"DGT3000: [{_angelHubService.WhiteClock}] [{_angelHubService.BlackClock}] [{_angelHubService.RunWhoString}]");

                                trackRunwho = _angelHubService.RunWhoString;
                                _dgtEbDllFacade.SetClock(_angelHubService.WhiteClock, _angelHubService.BlackClock, int.Parse(_angelHubService.RunWhoString));
                            }
                        };

                        fakeLiveChessServer = new LiveChessServer(_dgtEbDllFacade, 23456, 1, 25, "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR");

                        _dgtEbDllFacade.SetClock("0:15:00", "0:15:00", 1);
                        _dgtEbDllFacade.SetClock("0:15:00", "0:15:00", 0);

                        fakeLiveChessServer.OnLiveChessSrvMessage += (object o, string message) => TextBoxConsole.AddLine($"LiveSRV: {message}");

                        _angelHubService.OnRemoteWatchStarted += (remoteSource) =>
                        {
                            //If on CDC set the drop fix mode
                            fakeLiveChessServer.DropFix = LiveChessServer.PlayDropFix.NONE;
                            fakeLiveChessServer.DropFix = !remoteSource.Contains("CDC") ?
                                                          LiveChessServer.PlayDropFix.NONE :
                                                          (_angelHubService.IsWhiteOnBottom ? LiveChessServer.PlayDropFix.FROMWHITE :
                                                          LiveChessServer.PlayDropFix.FROMBLACK);
                        };

                        _angelHubService.OnOrientationFlipped += () =>
                        {
                            //fakeLiveChessServer.DropFix = LiveChessServer.PlayDropFix.NONE;
                            //Flip drop fix if the dropfix is applied
                            fakeLiveChessServer.DropFix = fakeLiveChessServer.DropFix == LiveChessServer.PlayDropFix.NONE ?
                                                          LiveChessServer.PlayDropFix.NONE :
                                                          (_angelHubService.IsWhiteOnBottom ? LiveChessServer.PlayDropFix.FROMWHITE :
                                                          LiveChessServer.PlayDropFix.FROMBLACK);
                        };

                        _angelHubService.OnPluginDisconnect += () =>
                        {
                            fakeLiveChessServer.DropFix = LiveChessServer.PlayDropFix.NONE;
                        };

                        _angelHubService.OnRemoteFenChange += (string _, string toRemoteFen, string _, string _, string _, string _, bool _) =>
                        {
                            fakeLiveChessServer.RemoteFEN = toRemoteFen;
                        };

                        _angelHubService.OnRemoteBoardStatusChange += (string boardMsg, bool isWhiteOnBottom) =>
                    {
                        if (fakeLiveChessServer.DropFix != LiveChessServer.PlayDropFix.NONE)
                        {
                            //Test for "DGT: Connected. Your turn." in the UI
                            //Language list at the top of this file
                            if (YOUR_TURN_LANG.Any(s => boardMsg.Contains(s)))
                            {
                                fakeLiveChessServer.SideToPlay = isWhiteOnBottom ? "WHITE" : "BLACK";
                                fakeLiveChessServer.BlockSendToRemote = false;
                            }
                            else
                            {
                                fakeLiveChessServer.SideToPlay = isWhiteOnBottom ? "BLACK" : "WHITE";
                                fakeLiveChessServer.BlockSendToRemote = true;
                            }
                        }
                    };

                        ButtonRabbitConfig1.Visible = true;
                        ButtonRabbitConf2.Visible = true;
                        GroupBoxClockTest.Visible = true;
                        ButtonSendTestMsg1.Visible = true;
                        ButtonSendTestMsg2.Visible = true;

                        //Only do this when rabbit is setup
                        fakeLiveChessServer.RunLiveChessServer();
                    }
                    else
                    {
                        _dgtEbDllFacade = null;
                        IsUsingRabbit = false;
                    }
                }
                catch (DllNotFoundException)
                {
                    _dgtEbDllFacade = null;
                    IsUsingRabbit = false;
                }

                TextBoxConsole.AddLine($"Board  : {(IsUsingRabbit ? $"Using {_dgtEbDllFacade.GetRabbitVersionString()} [{(Environment.Is64BitProcess ? "64" : "32")} bit]." : $"Using Live Chess. {(DgtCherub.Properties.UserSettings.Default.IsRabbitDisabled ? "[Rabbit is Always Disabled]" : "")}")}", TEXTBOX_MAX_LINES, false);
                if (IsUsingRabbit)
                {
                    TextBoxConsole.AddLine($"         {(IsUsingRabbit ? $"To use Live Chess you need to start it before running Cherub." : $"DGT Rabbit [{(Environment.Is64BitProcess ? "64" : "32")} bit] is either not installed or Live Chess was running")}", TEXTBOX_MAX_LINES, false);
                    TextBoxConsole.AddLine($"         {(IsUsingRabbit ? $"Your DGT 3000 must be in mode 25 for time updates (+ press play)" : "No clock updates will be sent to the DGT 3000")}", TEXTBOX_MAX_LINES, false);
                }
                TextBoxConsole.AddLine($"---------------------------------------------------------------------------------------", TEXTBOX_MAX_LINES, false);
            }

            //Init complete so notify the hub we can
            //start accepting external connections 
            Thread.Sleep(500);
            _angelHubService.NotifyInitComplete();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            PreventScreensaver(false);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            currentUciChessEngine?.Stop();

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
            DgtCherub.Properties.UserSettings.Default.IsRabbitDisabled = CheckBoxNeverUseRabbit.Checked;
            DgtCherub.Properties.UserSettings.Default.StartingWidth = Width;
            DgtCherub.Properties.UserSettings.Default.LastUciExe = lastUciExe;
            DgtCherub.Properties.UserSettings.Default.UciOptions = uciOptionSettings?.SerializeSettings();
            DgtCherub.Properties.UserSettings.Default.DebugUciIn = CheckBoxKibitzerShowUciIn.Checked;
            DgtCherub.Properties.UserSettings.Default.DebugUciOut = CheckBoxKibitzerShowUciOut.Checked;
            DgtCherub.Properties.UserSettings.Default.Save();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            SuspendLayout();

            lastUciExe = DgtCherub.Properties.UserSettings.Default.LastUciExe;
            uciOptionSettings = UciOptionSettings.DeserializeSettings(DgtCherub.Properties.UserSettings.Default.UciOptions);

            ButtonSetAltDriver.Enabled = string.IsNullOrEmpty(DgtCherub.Properties.UserSettings.Default.AltDgtDllPath);
            ButtonClearAltDriver.Enabled = !string.IsNullOrEmpty(DgtCherub.Properties.UserSettings.Default.AltDgtDllPath);

            CheckBoxKibitzerShowUciIn.Checked = DgtCherub.Properties.UserSettings.Default.DebugUciIn;
            CheckBoxKibitzerShowUciOut.Checked = DgtCherub.Properties.UserSettings.Default.DebugUciOut;

            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;

            //Set Appsettings from the designer values...
            EchoInternallMessagesToConsole = CheckBoxRecieveLog.Checked;
            EchoExternalMessagesToConsole = CheckBoxDisableRabbit.Checked;

            ToolStripStatusLabelVersion.Text = $"Ver. {VERSION_NUMBER}";


            TabControlSidePanel.SelectedTab = TabPageConfig;

            Width = DgtCherub.Properties.UserSettings.Default.StartingWidth;

            CheckBoxIncludeSecs.Checked = DgtCherub.Properties.UserSettings.Default.IncludeSeconds;
            CheckBoxPlayerBeep.Checked = DgtCherub.Properties.UserSettings.Default.BeepMode;
            CheckboxSilentBeep.Checked = DgtCherub.Properties.UserSettings.Default.Silent;
            CheckBoxNeverUseRabbit.Checked = DgtCherub.Properties.UserSettings.Default.IsRabbitDisabled;

            CheckBoxOnTop.Checked = DgtCherub.Properties.UserSettings.Default.AlwaysOnTop;
            CheckBoxOnTop_CheckedChanged(this, null);

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
            if (!IsUsingRabbit)
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

        private async void Form1_Shown(object sender, EventArgs e)
        {
            ClearConsole();

            _angelHubService.OnOrientationFlipped += DisplayBoardImages;

            _angelHubService.OnKibitzerActivated += () =>
            {
                TextBoxConsole.AddLine($"KIBITZER:: Turned ON - RESTART Cherub for online play");
            };

            _angelHubService.OnKibitzerFenChange += (string fen) =>
            {
                TextBoxConsole.AddLine($"KIBITZER:: Running eval for {fen}");
            };

            _angelHubService.OnKibitzerDeactivated += () =>
            {
                TextBoxConsole.AddLine($"KIBITZER:: Turned OFF");
            };

            _angelHubService.OnUciEngineStartError += (string errorMsg) =>
            {
                TextBoxConsole.AddLine($"UCI: Engine Start Error:: {errorMsg}");

                if (currentUciChessEngine != null)
                {
                    //We still have a loaded engine so enable the buttons
                    ButtonEngineConfig.Enabled = true;
                    CheckBoxKibitzerEnabled.Enabled = true;
                }
            };

            _angelHubService.OnUciEngineReleased += (string engineName) =>
            {
                TextBoxConsole.AddLine($"UCI: Released Engine:: {engineName}");
            };

            _angelHubService.OnUciEngineLoaded += (UciChessEngine engine) =>
            {
                LabelEngine.Text = engine.EngineName;
                lastUciExe = engine.Executable.FullName;

                TextBoxConsole.AddLine($"UCI: Loaded Engine:: {engine.EngineName} [{engine.EngineAuthor}]");

                if (currentUciChessEngine != null)
                {
                    currentUciChessEngine.OnOutputRecievedRaw -= Eng_OnOutputRecievedRawOut;
                    currentUciChessEngine.OnErrorRecievedRaw -= Eng_OnOutputRecievedRawError;
                    currentUciChessEngine.OnInputSentRaw -= Eng_OnOutputRecievedRawIn;
                    //currentUciChessEngine.OnOutputRecieved -= Eng_OnOutputRecieved;
                    engine.OnBoardEvalChanged -= Engine_OnBoardEvalChanged;
                }

                currentUciChessEngine = engine;

                currentUciChessEngine.OnOutputRecievedRaw += Eng_OnOutputRecievedRawOut;
                currentUciChessEngine.OnErrorRecievedRaw += Eng_OnOutputRecievedRawError;
                currentUciChessEngine.OnInputSentRaw += Eng_OnOutputRecievedRawIn;
                engine.OnBoardEvalChanged += Engine_OnBoardEvalChanged;

                if (uciOptionSettings.Options.ContainsKey(currentUciChessEngine.Executable.FullName))
                {
                    ApplyUciSettings(uciOptionSettings.Options[currentUciChessEngine.Executable.FullName]);
                }

                ButtonEngineConfig.Enabled = true;
                CheckBoxKibitzerEnabled.Enabled = true;
            };

            _angelHubService.OnLocalFenChange += (string localFen) =>
            {
                TextBoxConsole.AddLine($"Local board changed [{localFen}]");
                DisplayBoardImages();
            };

            _angelHubService.OnRemoteFenChange += (string fromRemoteFen, string toRemoteFen, string lastMove, string clockFen, string boardFen, string boardMsg, bool isWhiteOnBottom) =>
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

            _angelHubService.OnRemoteWatchStarted += (remoteSource) =>
            {
                if (remoteSource.Contains("CDC"))
                {
                    _voicePlayeStatus.Speak(Assets.Speech_en_01.CdcWatching_AP);

                    if (!IsUsingRabbit)
                    {
                        TextBoxConsole.AddLine("         ***********************************************************************", TEXTBOX_MAX_LINES, false);
                        TextBoxConsole.AddLine("         PLAYING ON CHESS.COM IN LIVE CHESS MODE IS NOT RECOMMENDED AT THIS TIME", TEXTBOX_MAX_LINES, false);
                        TextBoxConsole.AddLine("          To play enable Rabbit mode and restart Cherub - DO NOT run Live Chess ", TEXTBOX_MAX_LINES, false);
                        TextBoxConsole.AddLine("         ***********************************************************************", TEXTBOX_MAX_LINES, false);
                    }
                }
                else if (remoteSource.Contains("Lichess"))
                {
                    if (IsUsingRabbit)
                    {
                        TextBoxConsole.AddLine("         **********************************************************************", TEXTBOX_MAX_LINES, false);
                        TextBoxConsole.AddLine("         PLAYING ON LICHESS.ORG IN RABBIT MODE IS NOT SUPPORTED IN THIS VERSION", TEXTBOX_MAX_LINES, false);
                        TextBoxConsole.AddLine("            To play close Cherub, run Live Chess and then start Cherub again   ", TEXTBOX_MAX_LINES, false);
                        TextBoxConsole.AddLine("         **********************************************************************", TEXTBOX_MAX_LINES, false);
                    }

                    _voicePlayeStatus.Speak(Assets.Speech_en_01.LichessWatching_AP);
                }
                else
                {
                    _voicePlayeStatus.Speak(Assets.Speech_en_01.RemoteWatching_AP);
                }
            };

            _angelHubService.OnRemoteWatchStopped += (remoteSource) =>
            {

                if (remoteSource.Contains("CDC"))
                {
                    _voicePlayeStatus.Speak(Assets.Speech_en_01.CdcStoppedWatching_AP);
                }
                else if (remoteSource.Contains("Lichess"))
                {
                    _voicePlayeStatus.Speak(Assets.Speech_en_01.LichessStoppedWatching_AP);
                }
                else
                {
                    _voicePlayeStatus.Speak(Assets.Speech_en_01.RemoteStoppedWatching_AP);
                }
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

            _angelHubService.OnBoardMatch += (_, _) =>
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
                if (IsUsingRabbit)
                {
                    _voicePlayeStatus.Speak(Assets.Speech_en_01.ConnectedToRabbit_AP);
                    TextBoxConsole.AddLines(new string[]{$"{"".PadRight(67,'-')}",
                                                     $"Connected to Rabbit...",
                                                     $"{"".PadRight(67,'-')}"}, TEXTBOX_MAX_LINES);
                }
                else
                {
                    _voicePlayeStatus.Speak(Assets.Speech_en_01.DgtLcConnected_AP);
                    TextBoxConsole.AddLines(new string[]{$"{"".PadRight(67,'-')}",
                                                     $"Live Chess running [{eventArgs.ResponseOut}]",
                                                     $"{"".PadRight(67,'-')}"}, TEXTBOX_MAX_LINES);
                }
            };

            _dgtLiveChess.OnBoardConnected += (source, eventArgs) =>
            {
                if (IsUsingRabbit)
                {
                    TextBoxConsole.AddLines(new string[]{$"{"".PadRight(67,'-')}",
                                                     $"Verify the Rabbit to board connection in the Rabbit config screen.",
                                                     $"{"".PadRight(67,'-')}"}, TEXTBOX_MAX_LINES);
                }
                else
                {
                    PictureBoxLocal.Image = PictureBoxLocalInitialImage;
                    _voicePlayeStatus.Speak(Assets.Speech_en_01.DgtConnected_AP);
                    TextBoxConsole.AddLines(new string[]{$"{"".PadRight(67,'-')}",
                                                     $"Board found [{eventArgs.ResponseOut}]",
                                                     $"{"".PadRight(67,'-')}"}, TEXTBOX_MAX_LINES);
                }
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
                if (IsUsingRabbit)
                {
                    TextBoxConsole.AddLine($"WARNING: Battery status unavailable when using Rabbit.", TEXTBOX_MAX_LINES);
                }
                else
                {
                    _voicePlayeStatus.Speak(Assets.Speech_en_01.BatteryCritical_AP);
                    TextBoxConsole.AddLine($"{eventArgs.ResponseOut}", TEXTBOX_MAX_LINES);
                }
            };

            _dgtLiveChess.OnBatteryLow += (obj, eventArgs) =>
            {
                if (IsUsingRabbit)
                {
                    TextBoxConsole.AddLine($"WARNING: Battery status unavailable when using Rabbit.", TEXTBOX_MAX_LINES);
                }
                else
                {
                    _voicePlayeStatus.Speak(Assets.Speech_en_01.BatteryLow_AP);
                    TextBoxConsole.AddLine($"{eventArgs.ResponseOut}", TEXTBOX_MAX_LINES);
                }
            };

            _dgtLiveChess.OnBatteryOk += (obj, eventArgs) =>
            {
                if (IsUsingRabbit)
                {
                    TextBoxConsole.AddLine($"WARNING: Battery status unavailable when using Rabbit.", TEXTBOX_MAX_LINES);
                }
                else
                {
                    _voicePlayeStatus.Speak(Assets.Speech_en_01.BatteryOk_AP);
                    TextBoxConsole.AddLine($"{eventArgs.ResponseOut}", TEXTBOX_MAX_LINES);
                }
            };

            _dgtLiveChess.OnFenRecieved += (obj, eventArgs) =>
            {
                //TextBoxConsole.AddLine($"Local DGT board changed [{eventArgs.ResponseOut}]", TEXTBOX_MAX_LINES);
                _angelHubService.LocalBoardUpdate(eventArgs.ResponseOut);
            };

            //All the Events are set up so we can start watching the local board and running the inbound API
            _ = Task.Run(_dgtLiveChess.PollDgtBoard);
            _ = Task.Run(_iHost.Run);

            await Task.Delay(500); //Short delay for the form to fully render
            StartBoardComms();
        }

        int lasteval = 0;
        private void Engine_OnBoardEvalChanged(UciEngineEval obj)
        {
            if (obj.Depth > 20)
            {
                if (lasteval != obj.Eval)
                {
                    lasteval = obj.Eval;
                    //TextBoxConsole.AddLine($"{eval.GetBestMove()} @{info.Depth} {eval.GetBoardEval() / 100f}");
                    TextBoxConsole.AddLine($"KIBITZER:: {obj.Eval / 100f}cp at depth {obj.Depth} - Best Move {obj.BestMove}");
                }
            }
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
            TextBoxConsole.AddLine($"Cherub {(CheckBoxDisableRabbit.Checked ? "will" : "WILL NOT")} display notification messages from DGT Angel.", TEXTBOX_MAX_LINES);
            EchoExternalMessagesToConsole = CheckBoxDisableRabbit.Checked;
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

        private void PlayLiChessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ = TextBoxConsole.RunProcessWithComments("chrome",
                                       LICHESS_PLAY_LINK,
                                       $"Trying to open the Lichess DTG connection page in Chrome....",
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
            _ = TextBoxConsole.RunProcessWithComments(DL_CHROME_CDC_PLUGIN,
                                                  "",
                                                  $"Trying to open the download page for the Chess.com Chrome Plugin....",
                                                  $"...the download page opened.",
                                                  TEXTBOX_MAX_LINES);
        }

        private void DgtAngelLichessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ = TextBoxConsole.RunProcessWithComments(DL_CHROME_LICHESS_PLUGIN,
                                      "",
                                      $"Trying to open the download page for the Lichess.org Chrome Plugin....",
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

        private void StartWingedHorseToolStripMenu_Click(object sender, EventArgs e)
        {
            _ = TextBoxConsole.RunProcessWithComments("chrome",
                          $"--app={VIRTUAL_CLOCK_WH_LINK}",
                          $"Trying to start Winged Horse Mode in Chrome....",
                          $"...Winged Horse Mode openend.",
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
            TextBoxConsole.AddLine($"---------------------------------------------------------------------------------------", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"    Welcome to...                                                                      ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"                      ██████╗██╗  ██╗███████╗██████╗ ██╗   ██╗██████╗                  ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"                     ██╔════╝██║  ██║██╔════╝██╔══██╗██║   ██║██╔══██╗                 ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"                     ██║     ███████║█████╗  ██████╔╝██║   ██║██████╔╝                 ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"                     ██║     ██╔══██║██╔══╝  ██╔══██╗██║   ██║██╔══██╗                 ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"                     ╚██████╗██║  ██║███████╗██║  ██║╚██████╔╝██████╔╝                 ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"                      ╚═════╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝ ╚═════╝ ╚═════╝                  ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($" Hyper-Dragon :: Version {VERSION_NUMBER} :: {PROJECT_URL}", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"---------------------------------------------------------------------------------------", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"NOTE   : This project IS NOT affiliated with DGT, Chess.com or Lichess any way.", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"WARNING: I think that this release can now be considered a beta version.  I can", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         confidently say that it works not only on my machine but also those", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         machines of others...as a beta your mileage may vary but please report", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         any defects via the links menu.", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"PreReq : You will need a physical DGT Board and either Live Chess or the Rabbit ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         plug-in installed on this machine.  You will also need the Chrome browser", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         with an 'Angel' plug-in installed.  Don't forget to enable your board", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         in the Chess.com options.", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"Thanks : Thanks go to BaronVonChickenpants, Hamilton53, MancombSeepgood, er642,", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         danielbaechli, KevinTheChessGnome, CFossa and qnuti for their support and ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         testing and to Fake-Angel for the new move voice (en-02).", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"V.Clock: IP Addresses for [{(string.IsNullOrEmpty(hostName) ? "NO HOST!" : hostName)}] are [{(string.IsNullOrEmpty(hostName) ? "" : string.Join(',', thisMachineIpV4Addrs))}]", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         The Virtual Clock is available on http://<Your IP>:{VIRTUAL_CLOCK_PORT}/", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         Alternatively, point your phone at the QR code on the clock tab (don't", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         forget that you will need to open port 37964 on the windows firewall", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         for this to work).", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"---------------------------------------------------------------------------------------", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"*** PLAY BOARD/LICHESS CHANGE NOTE ***", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         For previous Live board users, you must update your Chrome extension to the", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         'Play' version.  Go to Links->Downloads->DGT Angel Chrome Extension", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         At the time of this release there were outstanding problems with the", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         DGT board on the 'Play' interface. To castle move your King 2 squares", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         very, very fast and NEVER move your rook until the move has been", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         acknowledged and keep a mouse handy as CDC sometimes refuses to accept", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         your move on the physical board.", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         Workarounds have been applied in Rabbit mode so my advice would be to", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         use that instead of Live Chess.  You should still keep a mouse handy ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         though, just in case.  For the new Lichess Extension you will need to", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         to be in Live Chess mode to play a game.", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         ", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         Finally, if you have the 'ghost move' issue open up the Rabbit", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         config and change the 'stableboard' slider on the extra tab.  Check", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"         for update news on the Chess.com DGT Club forum.", TEXTBOX_MAX_LINES, false);
            TextBoxConsole.AddLine($"---------------------------------------------------------------------------------------", TEXTBOX_MAX_LINES, false);
            if (DateTime.Now.Month == 12 && DateTime.Now.Day > 20)
            {
                TextBoxConsole.AddLine(holegg01, TEXTBOX_MAX_LINES, false);
                TextBoxConsole.AddLine(holegg02, TEXTBOX_MAX_LINES, false);
                TextBoxConsole.AddLine(holegg03, TEXTBOX_MAX_LINES, false);
                TextBoxConsole.AddLine(holegg04, TEXTBOX_MAX_LINES, false);
                TextBoxConsole.AddLine(holegg05, TEXTBOX_MAX_LINES, false);
                TextBoxConsole.AddLine(holegg06, TEXTBOX_MAX_LINES, false);
                TextBoxConsole.AddLine(holegg07, TEXTBOX_MAX_LINES, false);
                TextBoxConsole.AddLine(holegg08, TEXTBOX_MAX_LINES, false);
                TextBoxConsole.AddLine($"---------------------------------------------------------------------------------------", TEXTBOX_MAX_LINES, false);
            }
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
                    try
                    {
                        ToolStripStatusLabelLastUpdate.Text = $"[Updated@{System.DateTime.Now.ToLongTimeString()}]";

                        string local = _angelHubService.IsLocalBoardAvailable ? _angelHubService.LocalBoardFEN : _angelHubService.RemoteBoardFEN;
                        string remote = _angelHubService.IsRemoteBoardAvailable ? _angelHubService.RemoteBoardFEN : _angelHubService.LocalBoardFEN;

                        PictureBoxLocal.Image = _angelHubService.IsLocalBoardAvailable ? (await _boardRenderer.GetPngImageDiffFromFenAsync(local, remote, PictureBoxRemote.Width, _angelHubService.IsWhiteOnBottom)).ConvertPngByteArrayToBitmap()
                                                                                       : PictureBoxLocal.Image = PictureBoxLocalInitialImage;

                        PictureBoxRemote.Image = _angelHubService.IsRemoteBoardAvailable ? (await _boardRenderer.GetPngImageDiffFromFenAsync(remote, local, PictureBoxRemote.Width, _angelHubService.IsWhiteOnBottom)).ConvertPngByteArrayToBitmap()
                                                                                           : PictureBoxRemote.Image = PictureBoxRemoteInitialImage;
                    }
                    catch (Exception ex)
                    {
                        TextBoxConsole.AddLine($"ERROR:: Image update failed [{ex.Message}]");
                    }
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

        private void CheckBoxNeverUseRabbit_CheckedChanged(object sender, EventArgs e)
        {
            if (CheckBoxNeverUseRabbit.Checked)
            {
                TextBoxConsole.AddLine($"WARNING: No attempt will be made to use Rabbit on future restarts");
            }
            else
            {
                TextBoxConsole.AddLine($"WARNING: An attempt will be made to use Rabbit on future restarts.");
                TextBoxConsole.AddLine($"         To use Rabbit DO NOT run Live Chess or that will take precedence.");
            }
        }


        private void Eng_OnOutputRecievedRawIn(object sender, string e)
        {
            if (CheckBoxKibitzerShowUciIn.Checked)
            {
                TextBoxConsole.AddLine($"UCI__IN :: {currentUciChessEngine?.EngineName} :: {e}");
            }
        }

        private void Eng_OnOutputRecievedRawOut(object sender, string e)
        {
            if (e == null) { } // DO NOTHING
            else if (e.Contains("currmove")) { } // DO NOTHING 
            else if (e.Contains("score")) Invoke(() => { if (!LabelKibitzerInfo.IsDisposed) LabelKibitzerInfo.Text = e; });
            else if (CheckBoxKibitzerShowUciOut.Checked) TextBoxConsole.AddLine($"UCI_OUT :: {currentUciChessEngine?.EngineName} :: {e}");
        }

        private void Eng_OnOutputRecievedRawError(object sender, string e)
        {
            TextBoxConsole.AddLine($"UCI_ERR :: {currentUciChessEngine?.EngineName} :: {e}");
        }

        private void CheckBoxKibitzerEnabled_CheckedChanged(object sender, EventArgs e)
        {
            if (_angelHubService.IsRemoteBoardAvailable && ((CheckBox)sender).Checked)
            {
                TextBoxConsole.AddLine($"KIBITZER:: Can't enable when the remote board is active.");
                ((CheckBox)sender).Checked = false;
            }
            else if (currentUciChessEngine == null)
            {
                TextBoxConsole.AddLine($"KIBITZER:: Can't enable without engine running.");
                ((CheckBox)sender).Checked = false;
            }
            else
            {
                _angelHubService.SwitchKibitzer(((CheckBox)sender).Checked);
            }
        }

        private void ButtonEngineConfig_Click(object sender, EventArgs e)
        {
            try
            {
                List<UciOption> uciOptions = currentUciChessEngine.Options.Values.ToList();
                UciOptionsForm form = new(currentUciChessEngine.EngineName, uciOptions);

                if (form.ShowDialog() == DialogResult.OK)
                {
                    List<UciOption> modifiedUciOptions = form.GetModifiedUciOptions();

                    currentUciChessEngine.Stop();

                    if (uciOptionSettings.Options.ContainsKey(currentUciChessEngine.Executable.FullName))
                    {
                        uciOptionSettings.Options[currentUciChessEngine.Executable.FullName] = modifiedUciOptions;
                    }
                    else
                    {
                        uciOptionSettings.Options.Add(currentUciChessEngine.Executable.FullName, modifiedUciOptions);
                    }

                    ApplyUciSettings(modifiedUciOptions);
                }
            }
            catch (Exception ex)
            {
                TextBoxConsole.AddLine($"UCI: Error setting options {ex.Message}");
            }
        }

        private void ApplyUciSettings(List<UciOption> options)
        {
            foreach (var option in options)
            {
                currentUciChessEngine.SetOption(option.Name, option.VarValue);
            }
        }

        private void ButtonEngineSelect_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Executable Files (*.exe)|*.exe";

            DialogResult result = openFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                ButtonEngineConfig.Enabled = false;
                CheckBoxKibitzerEnabled.Enabled = false;
                _angelHubService.LoadEngineAsync(openFileDialog.FileName);
            }
        }


        private void LabelEngine_VisibleChanged(object sender, EventArgs e)
        {
            //Try and load the last engine when the 'Offline' tab is selected
            if (isEngineActivationRequired)
            {
                isEngineActivationRequired = false;
                if (!string.IsNullOrEmpty(lastUciExe)) _angelHubService.LoadEngineAsync(lastUciExe);
            }
        }

        private void ButtonSetAltDriver_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "DGT Board Driver (DGTEBDLL.dll)|DGTEBDLL.dll";
            openFileDialog.Multiselect = false;
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;

            DialogResult result = openFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                ButtonSetAltDriver.Enabled = false;
                ButtonClearAltDriver.Enabled = true;

                var dir = Path.GetDirectoryName(openFileDialog.FileName);
                DgtCherub.Properties.UserSettings.Default.AltDgtDllPath = dir;
                TextBoxConsole.AddLine($"RABBIT:: Alt board driver search path set to [{dir}]");
                TextBoxConsole.AddLine($"         WARNING:: This is an experimental feature and your results may vary.");
                TextBoxConsole.AddLine($"                   3rd party drivers are NOT supported - contact the author for support.");
                TextBoxConsole.AddLine($"         *** RESTART REQUIRED *** to Activte");
            }
        }

        private void ButtonClearAltDriver_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to proceed?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                ButtonSetAltDriver.Enabled = true;
                ButtonClearAltDriver.Enabled = false;
                DgtCherub.Properties.UserSettings.Default.AltDgtDllPath = "";
                TextBoxConsole.AddLine($"RABBIT:: Alt board driver cleared *** RESTART REQUIRED ***");
            }
        }
    }
}
