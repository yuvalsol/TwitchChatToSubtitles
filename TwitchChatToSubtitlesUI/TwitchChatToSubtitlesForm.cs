using TwitchChatToSubtitles.Library;
using TwitchChatToSubtitlesUI.CustomMessageBox;

namespace TwitchChatToSubtitlesUI
{
    public partial class TwitchChatToSubtitlesForm : Form
    {
        #region Form Init

        public TwitchChatToSubtitlesForm(string[] args)
        {
            InitializeComponent();
            ParseArgs(args);
        }

        private string argsJsonFile;

        private void ParseArgs(string[] args)
        {
            if (args.HasSingle() == false)
                return;

            string jsonFile = args[0];
            if (IsJsonFile(jsonFile) == false)
                return;

            argsJsonFile = jsonFile;
        }

        #endregion

        #region Form Events

        private void TwitchChatToSubtitlesForm_Load(object sender, EventArgs e)
        {
            ResetFormTitle();
            BindDdlDataSource<SubtitlesType>(ddlSubtitlesType, ddlSubtitlesType_SelectedIndexChanged, SubtitlesType.RegularSubtitles);
            BindDdlDataSource<SubtitlesLocation>(ddlSubtitlesLocation, ddl_SelectedIndexChanged);
            BindDdlDataSource<SubtitlesFontSize>(ddlSubtitlesFontSize, ddl_SelectedIndexChanged);
            BindDdlDataSource<SubtitlesRollingDirection>(ddlSubtitlesRollingDirection, ddl_SelectedIndexChanged);
            BindDdlDataSource<SubtitlesSpeed>(ddlSubtitlesSpeed, ddl_SelectedIndexChanged);
            SubtitlesTypeChanged();
        }

        private void ResetFormTitle()
        {
            Text = Program.Version();
        }

        private void TwitchChatToSubtitlesForm_Shown(object sender, EventArgs e)
        {
            openJsonFileDialog.InitialDirectory = AppContext.BaseDirectory;

            LoadUISettings();

            if (string.IsNullOrEmpty(argsJsonFile))
            {
                string[] jsonFiles = Directory.GetFiles(openJsonFileDialog.InitialDirectory, "*.json");
                if (jsonFiles.HasAny())
                    LoadJsonFile(jsonFiles[0]);
            }
            else
            {
                LoadJsonFile(argsJsonFile);
            }

            ddlSubtitlesType.Focus();
        }

        private void TwitchChatToSubtitlesForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SetUISettings();
            SaveUISettings();
        }

        #endregion

        #region Enum ComboBox

        private class EnumItem<TEnum> where TEnum : Enum
        {
            public TEnum Value { get; set; }
            public string Name { get; set; }
        }

        private static void BindDdlDataSource<TEnum>(ComboBox ddl, EventHandler selectedIndexChangedHandler, object selectedValue = null) where TEnum : Enum
        {
            ddl.SelectedIndexChanged -= selectedIndexChangedHandler;
            ddl.ValueMember = "Value";
            ddl.DisplayMember = "Name";
            ddl.DataSource = GetEnumDataSource<TEnum>().ToList();
            if (selectedValue != null)
                ddl.SelectedValue = selectedValue;
            ddl.SelectedIndexChanged += selectedIndexChangedHandler;
        }

        private static IEnumerable<EnumItem<TEnum>> GetEnumDataSource<TEnum>() where TEnum : Enum
        {
            foreach (TEnum value in Enum.GetValues(typeof(TEnum)))
            {
                yield return new EnumItem<TEnum>
                {
                    Value = value,
                    Name = GetEnumName(value)
                };
            }
        }

        [GeneratedRegex(@"([a-z])([A-Z])")]
        private static partial Regex RegexCamelCase();

        public static string GetEnumName<TEnum>(TEnum value) where TEnum : Enum
        {
            return RegexCamelCase().Replace(
                Enum.GetName(typeof(TEnum), value),
                "$1 $2"
            );
        }

        #endregion

        #region Text Color

        private Color? textColor = null;

        private void btnTextColor_Click(object sender, EventArgs e)
        {
            if (colorDialog.ShowDialog(this) == DialogResult.OK)
                SetTextColorControls(colorDialog.Color);
        }

        private void SetTextColorControls(Color? color)
        {
            SetRadioButton(rdbNoColor, rdbNoColor_CheckedChanged, false);
            SetRadioButton(rdbWhite, rdbWhite_CheckedChanged, false);
            SetRadioButton(rdbBlack, rdbBlack_CheckedChanged, false);

            if (color == null || color.Value.IsEmpty)
            {
                color = null;
                SetRadioButton(rdbNoColor, rdbNoColor_CheckedChanged, true);
            }
            else if (color == Color.White)
            {
                SetRadioButton(rdbWhite, rdbWhite_CheckedChanged, true);
            }
            else if (color == Color.Black)
            {
                SetRadioButton(rdbBlack, rdbBlack_CheckedChanged, true);
            }

            SetTextColor(color);
            SetUISettings();
        }

        private void rdbNoColor_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbNoColor.Checked)
            {
                SetTextColor(null);
                SetUISettings();
            }
        }

        private void rdbWhite_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbWhite.Checked)
            {
                SetTextColor(Color.White);
                SetUISettings();
            }
        }

        private void rdbBlack_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbBlack.Checked)
            {
                SetTextColor(Color.Black);
                SetUISettings();
            }
        }

        private void SetTextColor(Color? color)
        {
            textColor = color;

            if (color != null)
            {
                lblTextColor.Text = ColorToHex(color.Value);

                if (color.Value.IsNamedColor)
                    lblTextColor.Text = color.Value.Name + " " + lblTextColor.Text;

                lblTextColor.ForeColor = color.Value;
                lblTextColor.BackColor = (lblTextColor.ForeColor.GetBrightness() > 0.4 ? Color.Black : Color.FromName("Control"));
            }
            else
            {
                lblTextColor.Text = "Media Player's Default Text Color";
                lblTextColor.ForeColor = Color.FromName("ControlText");
                lblTextColor.BackColor = Color.FromName("Control");
            }
        }

        private static string ColorToHex(Color color)
        {
            return "#" +
                color.R.ToString("X2") +
                color.G.ToString("X2") +
                color.B.ToString("X2");
        }

        #endregion

        #region Controls

        private static void SetComboBox(ComboBox ddl, EventHandler selectedIndexChangedHandler, object selectedValue)
        {
            if (ddl.SelectedValue == selectedValue)
                return;
            ddl.SelectedIndexChanged -= selectedIndexChangedHandler;
            ddl.SelectedValue = selectedValue;
            ddl.SelectedIndexChanged += selectedIndexChangedHandler;
        }

        private static void SetCheckBox(CheckBox chk, EventHandler checkedChangedHandler, bool isChecked)
        {
            if (chk.Checked == isChecked)
                return;
            chk.CheckedChanged -= checkedChangedHandler;
            chk.Checked = isChecked;
            chk.CheckedChanged += checkedChangedHandler;
        }

        private static void SetNumericUpDown(NumericUpDown nud, EventHandler valueChangedHandler, decimal value)
        {
            if (nud.Value == value)
                return;
            nud.ValueChanged -= valueChangedHandler;
            nud.Value = value;
            nud.ValueChanged += valueChangedHandler;
        }

        private static void SetRadioButton(RadioButton rdb, EventHandler checkedChangedHandler, bool isChecked)
        {
            if (rdb.Checked == isChecked)
                return;
            rdb.CheckedChanged -= checkedChangedHandler;
            rdb.Checked = isChecked;
            rdb.CheckedChanged += checkedChangedHandler;
        }

        private static void SetTextBox(TextBox txt, EventHandler textChangedHandler, string text)
        {
            if ((txt.Text ?? string.Empty) == (text ?? string.Empty))
                return;
            txt.TextChanged -= textChangedHandler;
            txt.Text = text;
            txt.TextChanged += textChangedHandler;
        }

        private void ddlSubtitlesType_SelectedIndexChanged(object sender, EventArgs e)
        {
            SubtitlesTypeChanged();
            SetUISettings();
        }

        private void SubtitlesTypeChanged()
        {
            var subtitlesType = (SubtitlesType)ddlSubtitlesType.SelectedValue;

            btnWriteTwitchSubtitles.Text = (subtitlesType == SubtitlesType.ChatTextFile ? "Write Chat Text File" : "Write Twitch Subtitles");

            chkColorUserNames.Enabled =
            lblSubtitlesFontSize.Enabled =
            ddlSubtitlesFontSize.Enabled =
            lblTimeOffset.Enabled =
            nudTimeOffset.Enabled =
            btnTextColor.Enabled =
            flowLayoutPanelColors.Enabled =
            lblTextColor.Enabled = (subtitlesType != SubtitlesType.ChatTextFile);

            lblSubtitlesLocation.Enabled =
            ddlSubtitlesLocation.Enabled = false;

            lblSubtitlesRollingDirection.Enabled =
            ddlSubtitlesRollingDirection.Enabled = false;

            lblSubtitlesSpeed.Enabled =
            ddlSubtitlesSpeed.Enabled = false;

            lblSubtitleShowDuration.Enabled =
            nudSubtitleShowDuration.Enabled = false;

            if (subtitlesType == SubtitlesType.RegularSubtitles)
            {
                lblSubtitleShowDuration.Enabled =
                nudSubtitleShowDuration.Enabled = true;
            }
            else if (subtitlesType == SubtitlesType.RollingChatSubtitles)
            {
                lblSubtitlesLocation.Enabled =
                ddlSubtitlesLocation.Enabled = true;

                lblSubtitlesRollingDirection.Enabled =
                ddlSubtitlesRollingDirection.Enabled = true;

                lblSubtitlesSpeed.Enabled =
                ddlSubtitlesSpeed.Enabled = true;
            }
            else if (subtitlesType == SubtitlesType.StaticChatSubtitles)
            {
                lblSubtitlesLocation.Enabled =
                ddlSubtitlesLocation.Enabled = true;
            }
        }

        private void ddl_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetUISettings();
        }

        private void chk_CheckedChanged(object sender, EventArgs e)
        {
            SetUISettings();
        }

        private void nud_ValueChanged(object sender, EventArgs e)
        {
            SetUISettings();
        }

        private void txtJsonFile_TextChanged(object sender, EventArgs e)
        {
            ResetFormTitle();
            streamerName = null;
            if (string.IsNullOrWhiteSpace(txtJsonFile.Text) == false)
                LoadJsonFile(txtJsonFile.Text);
            SetUISettings();
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(txtConsole.Text);
            }
            catch { }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        #endregion

        #region Load Json File

        private void TwitchChatToSubtitlesForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void TwitchChatToSubtitlesForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (filePaths.HasSingle())
                    LoadJsonFile(filePaths[0]);
            }
        }

        private void btnJsonFile_Click(object sender, EventArgs e)
        {
            if (openJsonFileDialog.ShowDialog(this) == DialogResult.OK)
                LoadJsonFile(openJsonFileDialog.FileName);
        }

        private string streamerName;

        private void LoadJsonFile(string jsonFile)
        {
            if (IsJsonFile(jsonFile) == false)
                return;

            streamerName = null;
            try
            {
                streamerName = TwitchSubtitles.GetStreamerName(jsonFile);
            }
            catch { }

            SetUISettingsToForm(GetUISettings());

            SetTextBox(txtJsonFile, txtJsonFile_TextChanged, jsonFile);

            openJsonFileDialog.FileName = Path.GetFileName(jsonFile);
            openJsonFileDialog.InitialDirectory = Path.GetDirectoryName(jsonFile);

            ResetFormTitle();
            Text =
                openJsonFileDialog.FileName +
                (string.IsNullOrEmpty(streamerName) ? null : " - " + streamerName) +
                " - " + Text;
        }

        private static bool IsJsonFile(string jsonFile)
        {
            return
                string.IsNullOrEmpty(jsonFile) == false &&
                string.Compare(Path.GetExtension(jsonFile), ".json") == 0 &&
                File.Exists(jsonFile);
        }

        #endregion

        #region Write Twitch Subtitles

        private void btnWriteTwitchSubtitles_Click(object sender, EventArgs e)
        {
            string jsonFile = txtJsonFile.Text;

            if (IsJsonFile(jsonFile) == false)
            {
                MessageBoxHelper.ShowError(
                    this,
                    "Not a JSON file." + Environment.NewLine + jsonFile,
                    $"File Error - {Program.Version()}"
                );

                return;
            }

            var settings = new TwitchSubtitlesSettings
            {
                SubtitlesType = (SubtitlesType)ddlSubtitlesType.SelectedValue
            };

            if (chkColorUserNames.Enabled)
                settings.ColorUserNames = chkColorUserNames.Checked;
            if (chkRemoveEmoticonNames.Enabled)
                settings.RemoveEmoticonNames = chkRemoveEmoticonNames.Checked;
            if (chkShowTimestamps.Enabled)
                settings.ShowTimestamps = chkShowTimestamps.Checked;
            if (nudSubtitleShowDuration.Enabled)
                settings.SubtitleShowDuration = Convert.ToInt32(nudSubtitleShowDuration.Value);
            if (ddlSubtitlesFontSize.Enabled)
                settings.SubtitlesFontSize = (SubtitlesFontSize)ddlSubtitlesFontSize.SelectedValue;
            if (ddlSubtitlesLocation.Enabled)
                settings.SubtitlesLocation = (SubtitlesLocation)ddlSubtitlesLocation.SelectedValue;
            if (ddlSubtitlesRollingDirection.Enabled)
                settings.SubtitlesRollingDirection = (SubtitlesRollingDirection)ddlSubtitlesRollingDirection.SelectedValue;
            if (ddlSubtitlesSpeed.Enabled)
                settings.SubtitlesSpeed = (SubtitlesSpeed)ddlSubtitlesSpeed.SelectedValue;
            if (btnTextColor.Enabled)
                settings.TextColor = textColor;
            if (nudTimeOffset.Enabled)
                settings.TimeOffset = Convert.ToInt32(nudTimeOffset.Value);

            var twitchSubtitles = new TwitchSubtitles(settings);

            twitchSubtitles.Start += (object sender, EventArgs e) =>
            {
                Cursor = Cursors.WaitCursor;

                txtConsole.Clear();

                if (settings.RegularSubtitles)
                    WriteLine("Regular Subtitles.");
                else if (settings.RollingChatSubtitles)
                    WriteLine("Rolling Chat Subtitles.");
                else if (settings.StaticChatSubtitles)
                    WriteLine("Static Chat Subtitles.");
                else if (settings.ChatTextFile)
                    WriteLine("Chat Text File.");

                Application.DoEvents();
            };

            twitchSubtitles.StartLoadingJsonFile += (object sender, StartLoadingJsonFileEventArgs e) =>
            {
                WriteLine("Loading JSON file...");

                Application.DoEvents();
            };

            twitchSubtitles.FinishLoadingJsonFile += (object sender, FinishLoadingJsonFileEventArgs e) =>
            {
                if (e.Error == null)
                    WriteLine("JSON file loaded successfully.");
                else
                    WriteLine("Could not load JSON file.");
                WriteLine("JSON file: " + e.JsonFile);

                Application.DoEvents();
            };

            twitchSubtitles.StartWritingPreparations += (object sender, StartWritingPreparationsEventArgs e) =>
            {
                string preparations =
                    (e.RemoveEmoticonNames ? "emoticons" : string.Empty) +
                    (e.RemoveEmoticonNames && e.ColorUserNames ? ", " : string.Empty) +
                    (e.ColorUserNames ? "user colors" : string.Empty);

                WriteLine($"Begin writing preparations ({preparations})...");

                Application.DoEvents();
            };

            twitchSubtitles.FinishWritingPreparations += (object sender, FinishWritingPreparationsEventArgs e) =>
            {
                if (e.Error == null)
                    WriteLine("Writing preparations finished successfully.");
                else
                    WriteLine("Failed to finish writing preparations.");

                Application.DoEvents();
            };

            int selectionStart = 0;
            int selectionLength = 0;

            twitchSubtitles.StartWritingSubtitles += (object sender, StartWritingSubtitlesEventArgs e) =>
            {
                selectionStart = txtConsole.SelectionStart;

                WriteLine("Chat Messages: 0 / 0");
                if (settings.ChatTextFile == false)
                    WriteLine("Subtitles: 0");

                selectionLength = txtConsole.TextLength - selectionStart;

                Application.DoEvents();
            };

            var lockObj = new object();

            void PrintProgress(object sender, ProgressEventArgs e)
            {
                var strMessages = $"Chat Messages: {e.MessagesCount:N0} / {e.TotalMessages:N0}";
                if (e.DiscardedMessagesCount > 0)
                    strMessages += $" (discarded messages {e.DiscardedMessagesCount:N0})";

                string strSelection = null;

                if (settings.ChatTextFile)
                {
                    strSelection = $"{strMessages}{Environment.NewLine}";
                }
                else
                {
                    var strSubtitles = $"Subtitles: {e.SubtitlesCount:N0}";

                    strSelection = $"{strMessages}{Environment.NewLine}{strSubtitles}{Environment.NewLine}";
                }

                lock (lockObj)
                {
                    txtConsole.Select(selectionStart, selectionLength);
                    txtConsole.SelectedText = strSelection;
                    selectionLength = txtConsole.TextLength - selectionStart;
                }
            }

            twitchSubtitles.ProgressAsync += PrintProgress;
            twitchSubtitles.FinishWritingSubtitles += PrintProgress;

            twitchSubtitles.Finish += (object sender, FinishEventArgs e) =>
            {
                if (e.Error == null)
                {
                    WriteLine("Finished successfully.");

                    if (settings.ChatTextFile)
                        WriteLine("Chat text file: " + e.SrtFile);
                    else
                        WriteLine("Subtitles file: " + e.SrtFile);

                    string processTime = e.ProcessTime.ToString(e.ProcessTime.Days > 0 ? "d':'hh':'mm':'ss'.'fff" : e.ProcessTime.Hours > 0 ? "h':'mm':'ss'.'fff" : "m':'ss'.'fff");
                    WriteLine("Process Time: " + processTime);

                    if (chkCloseWhenFinishedSuccessfully.Checked)
                    {
                        Application.DoEvents();
                        Close();
                    }
                }
                else
                {
                    try
                    {
                        if (File.Exists(e.SrtFile))
                            File.Delete(e.SrtFile);
                    }
                    catch { }

#if RELEASE
                    if (settings.ChatTextFile)
                        WriteErrorLine("Failed to write chat text file.");
                    else
                        WriteErrorLine("Failed to write subtitles.");
                    WriteErrorLine("Error: " + e.Error.Message);

                    Exception ex = e.Error.InnerException;
                    while (ex != null)
                    {
                        WriteErrorLine("Error: " + ex.Message);
                        ex = ex.InnerException;
                    }
#elif DEBUG
                    if (settings.ChatTextFile)
                        WriteErrorLine(e.Error.GetExceptionErrorMessage("Failed to write chat text file."));
                    else
                        WriteErrorLine(e.Error.GetExceptionErrorMessage("Failed to write subtitles."));
#endif
                }

                Cursor = Cursors.Default;

                Application.DoEvents();
            };

            twitchSubtitles.Tracepoint += (object sender, TracepointEventArgs e) =>
            {
                WriteLine(e.Message);
            };

            twitchSubtitles.WriteTwitchSubtitles(jsonFile);
        }

        private void WriteLine(string text)
        {
            txtConsole.AppendText(text);
            txtConsole.AppendText(Environment.NewLine);
        }

        private void WriteErrorLine(string text)
        {
            int selectionStart = txtConsole.SelectionStart;
            txtConsole.AppendText(text);
            int selectionLength = txtConsole.TextLength - selectionStart;
            txtConsole.Select(selectionStart, selectionLength);
            txtConsole.SelectionColor = Color.Red;
            txtConsole.AppendText(Environment.NewLine);
        }

        #endregion

        #region Command Line

        private void btnCommandLine_Click(object sender, EventArgs e)
        {
            ShowCommandLine();
        }

        private void ShowCommandLine()
        {
            var sb = new StringBuilder();

            if (OperatingSystem.IsWindows())
                sb.Append("TwitchChatToSubtitles.exe");
            else if (OperatingSystem.IsLinux())
                sb.Append("./TwitchChatToSubtitles");
            else
                sb.Append("TwitchChatToSubtitles");

            var subtitlesType = (SubtitlesType)ddlSubtitlesType.SelectedValue;
            sb.Append($" --{subtitlesType}");

            if (string.IsNullOrEmpty(txtJsonFile.Text) == false)
                sb.Append($" --JsonFile \"{txtJsonFile.Text}\"");

            if (chkColorUserNames.Enabled)
            {
                if (chkColorUserNames.Checked)
                    sb.Append($" --ColorUserNames");
            }

            if (chkRemoveEmoticonNames.Enabled)
            {
                if (chkRemoveEmoticonNames.Checked)
                    sb.Append($" --RemoveEmoticonNames");
            }

            if (chkShowTimestamps.Enabled)
            {
                if (chkShowTimestamps.Checked)
                    sb.Append($" --ShowTimestamps");
            }

            if (ddlSubtitlesLocation.Enabled)
            {
                var subtitlesLocation = (SubtitlesLocation)ddlSubtitlesLocation.SelectedValue;
                if (subtitlesLocation != SubtitlesLocation.None)
                    sb.Append($" --SubtitlesLocation {subtitlesLocation}");
            }

            if (ddlSubtitlesFontSize.Enabled)
            {
                var subtitlesFontSize = (SubtitlesFontSize)ddlSubtitlesFontSize.SelectedValue;
                if (subtitlesFontSize != SubtitlesFontSize.None)
                    sb.Append($" --SubtitlesFontSize {subtitlesFontSize}");
            }

            if (ddlSubtitlesRollingDirection.Enabled)
            {
                var subtitlesRollingDirection = (SubtitlesRollingDirection)ddlSubtitlesRollingDirection.SelectedValue;
                if (subtitlesRollingDirection != SubtitlesRollingDirection.None)
                    sb.Append($" --SubtitlesRollingDirection {subtitlesRollingDirection}");
            }

            if (ddlSubtitlesSpeed.Enabled)
            {
                var subtitlesSpeed = (SubtitlesSpeed)ddlSubtitlesSpeed.SelectedValue;
                if (subtitlesSpeed != SubtitlesSpeed.None)
                    sb.Append($" --SubtitlesSpeed {subtitlesSpeed}");
            }

            if (nudTimeOffset.Enabled)
            {
                int timeOffset = Convert.ToInt32(nudTimeOffset.Value);
                if (timeOffset != 0)
                    sb.Append($" --TimeOffset {timeOffset}");
            }

            if (nudSubtitleShowDuration.Enabled)
            {
                int subtitleShowDuration = Convert.ToInt32(nudSubtitleShowDuration.Value);
                if (subtitleShowDuration > 0 && subtitleShowDuration != 5)
                    sb.Append($" --SubtitleShowDuration {subtitleShowDuration}");
            }

            if (btnTextColor.Enabled)
            {
                if (textColor != null)
                {
                    string color = ColorToHex(textColor.Value);
                    if (textColor.Value.IsNamedColor)
                        color = textColor.Value.Name;
                    sb.Append($" --TextColor \"{color}\"");
                }
            }

            MessageBoxHelper.ShowCommandLine(this, sb.ToString(), "Command Line");
        }

        #endregion

        #region UI Settings

        private Dictionary<string, UISettings> StreamersUISettings = new(StringComparer.OrdinalIgnoreCase);
        private const string SETTINGS_KEY = "DefaultUISettings";

        private void SetUISettings()
        {
            string settingsKey = (string.IsNullOrEmpty(streamerName) ? SETTINGS_KEY : streamerName);

            if (StreamersUISettings.ContainsKey(settingsKey))
                GetUISettingsFromForm(StreamersUISettings[settingsKey]);
            else
                StreamersUISettings.Add(settingsKey, GetUISettingsFromForm());
        }

        private UISettings GetUISettings()
        {
            string settingsKey = (string.IsNullOrEmpty(streamerName) ? SETTINGS_KEY : streamerName);

            if (StreamersUISettings.TryGetValue(settingsKey, out UISettings settings))
                return settings;

            if (settingsKey != SETTINGS_KEY)
            {
                if (StreamersUISettings.TryGetValue(SETTINGS_KEY, out settings))
                    return settings;
            }

            settings = GetUISettingsFromForm();
            StreamersUISettings.Add(SETTINGS_KEY, settings);
            return settings;
        }

        private UISettings GetUISettingsFromForm(UISettings settings = null)
        {
            settings ??= new();

            string jsonDirectory = null;
            if (string.IsNullOrEmpty(txtJsonFile.Text) == false)
            {
                try
                {
                    jsonDirectory = Path.GetDirectoryName(txtJsonFile.Text);
                }
                catch { }
            }

            settings.SubtitlesType = (SubtitlesType)(ddlSubtitlesType.SelectedValue ?? SubtitlesType.RegularSubtitles);
            settings.ColorUserNames = chkColorUserNames.Checked;
            settings.RemoveEmoticonNames = chkRemoveEmoticonNames.Checked;
            settings.ShowTimestamps = chkShowTimestamps.Checked;
            settings.SubtitlesLocation = (SubtitlesLocation)(ddlSubtitlesLocation.SelectedValue ?? SubtitlesLocation.None);
            settings.SubtitlesFontSize = (SubtitlesFontSize)(ddlSubtitlesFontSize.SelectedValue ?? SubtitlesFontSize.None);
            settings.SubtitlesRollingDirection = (SubtitlesRollingDirection)(ddlSubtitlesRollingDirection.SelectedValue ?? SubtitlesRollingDirection.None);
            settings.SubtitlesSpeed = (SubtitlesSpeed)(ddlSubtitlesSpeed.SelectedValue ?? SubtitlesSpeed.None);
            settings.TimeOffset = nudTimeOffset.Value;
            settings.SubtitleShowDuration = nudSubtitleShowDuration.Value;
            settings.TextColor = (textColor != null ? ColorToHex(textColor.Value) : null);
            settings.CloseWhenFinishedSuccessfully = chkCloseWhenFinishedSuccessfully.Checked;
            settings.JsonDirectory = jsonDirectory;

            return settings;
        }

        private void SetUISettingsToForm(UISettings settings)
        {
            if (settings == null)
                return;

            SetComboBox(ddlSubtitlesType, ddlSubtitlesType_SelectedIndexChanged, settings.SubtitlesType);
            SubtitlesTypeChanged();
            SetCheckBox(chkColorUserNames, chk_CheckedChanged, settings.ColorUserNames);
            SetCheckBox(chkRemoveEmoticonNames, chk_CheckedChanged, settings.RemoveEmoticonNames);
            SetCheckBox(chkShowTimestamps, chk_CheckedChanged, settings.ShowTimestamps);
            SetComboBox(ddlSubtitlesLocation, ddl_SelectedIndexChanged, settings.SubtitlesLocation);
            SetComboBox(ddlSubtitlesFontSize, ddl_SelectedIndexChanged, settings.SubtitlesFontSize);
            SetComboBox(ddlSubtitlesRollingDirection, ddl_SelectedIndexChanged, settings.SubtitlesRollingDirection);
            SetComboBox(ddlSubtitlesSpeed, ddl_SelectedIndexChanged, settings.SubtitlesSpeed);
            SetNumericUpDown(nudTimeOffset, nud_ValueChanged, settings.TimeOffset);
            SetNumericUpDown(nudSubtitleShowDuration, nud_ValueChanged, settings.SubtitleShowDuration);
            SetCheckBox(chkCloseWhenFinishedSuccessfully, chk_CheckedChanged, settings.CloseWhenFinishedSuccessfully);

            if (string.IsNullOrEmpty(settings.TextColor) == false)
            {
                TypeConverter tc = TypeDescriptor.GetConverter(typeof(Color));
                var color = tc.ConvertFromString(settings.TextColor) as Color?;
                SetTextColorControls(color);
            }

            if (string.IsNullOrEmpty(settings.JsonDirectory) == false)
            {
                if (Directory.Exists(settings.JsonDirectory))
                    openJsonFileDialog.InitialDirectory = settings.JsonDirectory;
            }
        }

        #endregion

        #region Load UI Settings

        private const string SETTINGS_FILE_NAME = "TwitchChatToSubtitlesUI.settings";
        private static readonly JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };

        private void LoadUISettings()
        {
            var settingsFile = Path.Combine(AppContext.BaseDirectory, SETTINGS_FILE_NAME);
            Dictionary<string, UISettings> tempUISettings = DeserializeUISettings(settingsFile);
            if (tempUISettings.HasAny())
                StreamersUISettings = new(tempUISettings, StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, UISettings> DeserializeUISettings(string settingsFile)
        {
            try
            {
                if (string.IsNullOrEmpty(settingsFile))
                    return null;

                if (File.Exists(settingsFile) == false)
                    return null;

                return JsonSerializer.Deserialize<Dictionary<string, UISettings>>(File.ReadAllText(settingsFile), jsonSerializerOptions);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Save UI Settings

        private void SaveUISettings()
        {
            var settingsFile = Path.Combine(AppContext.BaseDirectory, SETTINGS_FILE_NAME);
            SerializeUISettings(settingsFile, StreamersUISettings);
        }

        private static void SerializeUISettings(string settingsFile, Dictionary<string, UISettings> settings)
        {
            try
            {
                if (string.IsNullOrEmpty(settingsFile))
                    return;

                string directory = Path.GetDirectoryName(settingsFile);
                if (Directory.Exists(directory) == false)
                    Directory.CreateDirectory(directory);

                File.WriteAllText(settingsFile, JsonSerializer.Serialize(settings, jsonSerializerOptions));
            }
            catch
            {
            }
        }

        #endregion
    }
}
