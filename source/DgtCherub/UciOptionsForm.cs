using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;
using UciComms.Data;


namespace DgtCherub
{
    public class UciOptionSettings
    {
        public Dictionary<string, List<UciOption>> Options { get; set; } = new Dictionary<string, List<UciOption>>();

        public string SerializeSettings()
        {
            return JsonSerializer.Serialize(Options);
        }

        public static UciOptionSettings DeserializeSettings(string json)
        {
            UciOptionSettings settings = new();

            if (!string.IsNullOrEmpty(json))
            {
                settings.Options = JsonSerializer.Deserialize<Dictionary<string, List<UciOption>>>(json);
            }

            return settings;
        }
    }

    public class UciOptionsForm : Form
    {
        private const int CONTROL_OFFSET = 250;
        private const int CONTROL_Y_OFFSET = 15;
        private readonly List<UciOption> _uciOptions;
        private readonly Dictionary<string, Control> _controls;
        private Button _saveButton;
        private readonly Button _resetButton;
        private readonly string _engineName;
        private readonly List<(TextBox control, string defaultVal)> _stringDefaults = new();
        private readonly List<(CheckBox control, bool defaultVal)> _checkDefaults = new();
        private readonly List<(NumericUpDown control, int defaultVal)> _spinDefaults = new();

        public UciOptionsForm(string engineName, List<UciOption> uciOptions)
        {
            _uciOptions = uciOptions;
            _controls = new Dictionary<string, Control>();
            _engineName = engineName;

            InitializeForm();
            CreateControls();
        }

        private void InitializeForm()
        {
            Text = $"UCI - {_engineName}";
            Padding = new Padding(10);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
        }

        private void CreateControls()
        {
            Font textFont = new("Segoe UI", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            int yOffset = 15;


            Panel panel = new Panel
            {
                Location = new System.Drawing.Point(0, 0),
                Size = new System.Drawing.Size(800,600),
                AutoScroll = true
            };
            Controls.Add(panel);


            foreach (UciOption option in _uciOptions)
            {
                if (option.DefaultValue == null || option.VarValue == null)
                {
                    continue;
                }

                Label label = new() { Text = option.Name, Height = 35, Font = textFont, Width = CONTROL_OFFSET - 10, Location = new System.Drawing.Point(10, yOffset) };
                panel.Controls.Add(label);

                Control control = null;

                try
                {
                    switch (option.Type)
                    {
                        case "spin":
                            NumericUpDown numericUpDown = new()
                            {
                                Font = textFont,
                                Minimum = int.Parse(option.MinValue),
                                Maximum = int.Parse(option.MaxValue),
                                Value = int.Parse(string.IsNullOrEmpty(option.VarValue) ? option.MinValue : option.VarValue),
                                Location = new System.Drawing.Point(CONTROL_OFFSET, yOffset)
                            };
                            control = numericUpDown;
                            _spinDefaults.Add((control as NumericUpDown, int.Parse(option.DefaultValue)));
                            break;
                        case "check":
                            CheckBox checkBox = new()
                            {
                                Font = textFont,
                                Checked = bool.Parse(option.VarValue),
                                Location = new System.Drawing.Point(CONTROL_OFFSET, yOffset)
                            };
                            control = checkBox;
                            _checkDefaults.Add((control as CheckBox, bool.Parse(option.DefaultValue)));
                            break;
                        case "button":
                            Button button = new()
                            {
                                Font = textFont,
                                Text = option.Name,
                                Enabled = false,
                                Location = new System.Drawing.Point(CONTROL_OFFSET, yOffset)
                            };
                            control = button;
                            break;
                        case "string":
                            TextBox textBox = new()
                            {
                                Font = textFont,
                                Text = option.VarValue,
                                Width = 300,
                                Location = new System.Drawing.Point(CONTROL_OFFSET, yOffset)
                            };
                            control = textBox;
                            _stringDefaults.Add((control as TextBox, option.DefaultValue));
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Label errLabel = new()
                    {
                        Font = textFont,
                        Text = $"ERR:{ex.Message}",
                        Location = new System.Drawing.Point(CONTROL_OFFSET, yOffset)
                    };

                    control = errLabel;
                }


                _controls.Add(option.Name, control);
                panel.Controls.Add(control);


                yOffset += 40;
            }

            //yOffset = 15 + panel.AutoScrollPosition.Y;

            yOffset = 650;

            _saveButton = new Button
            {
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = true,
                Size = new System.Drawing.Size(137, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = textFont,
                Text = "Save",
                Location = new System.Drawing.Point(CONTROL_OFFSET, yOffset)
            };
            _saveButton.Click += SaveButton_Click;
            Controls.Add(_saveButton);

            Button _resetButton = new()
            {
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = true,
                Size = new System.Drawing.Size(137, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = textFont,
                Text = "Reset",
                Location = new System.Drawing.Point(10, yOffset)
            };
            _resetButton.Click += ResetButton_Click;
            Controls.Add(_resetButton);

        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            foreach ((TextBox control, string defaultVal) in _stringDefaults) { control.Text = defaultVal; };
            foreach ((CheckBox control, bool defaultVal) in _checkDefaults) { control.Checked = defaultVal; };
            foreach ((NumericUpDown control, int defaultVal) in _spinDefaults) { control.Value = defaultVal; };
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            foreach (UciOption option in _uciOptions)
            {
                if (!_controls.ContainsKey(option.Name))
                {
                    continue;
                }

                Control control = _controls[option.Name];

                switch (option.Type)
                {
                    case "spin":
                        option.VarValue = ((NumericUpDown)control).Value.ToString();
                        break;
                    case "check":
                        option.VarValue = ((CheckBox)control).Checked.ToString();
                        break;
                    case "string":
                        option.VarValue = ((TextBox)control).Text;
                        break;
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        public List<UciOption> GetModifiedUciOptions()
        {
            return _uciOptions;
        }
    }
}