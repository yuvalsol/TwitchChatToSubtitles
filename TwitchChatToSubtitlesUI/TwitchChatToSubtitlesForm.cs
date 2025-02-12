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

        #region Form Load

        private void TwitchChatToSubtitlesForm_Load(object sender, EventArgs e)
        {
            ResetFormTitle();
            BindDdlDataSource<SubtitlesType>(ddlSubtitlesType);
            BindDdlDataSource<SubtitlesLocation>(ddlSubtitlesLocation);
            BindDdlDataSource<SubtitlesFontSize>(ddlSubtitlesFontSize);
            BindDdlDataSource<SubtitlesSpeed>(ddlSubtitlesSpeed);
            ddlSubtitlesType.SelectedValue = SubtitlesType.RegularSubtitles;
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

        #endregion

        #region Enum ComboBox

        private class EnumItem<TEnum> where TEnum : Enum
        {
            public TEnum Value { get; set; }
            public string Name { get; set; }
        }

        private static void BindDdlDataSource<TEnum>(ComboBox ddl) where TEnum : Enum
        {
            ddl.ValueMember = "Value";
            ddl.DisplayMember = "Name";
            ddl.DataSource = GetEnumDataSource<TEnum>().ToList();
        }

        [GeneratedRegex(@"([a-z])([A-Z])")]
        private static partial Regex RegexCamelCase();

        private static IEnumerable<EnumItem<TEnum>> GetEnumDataSource<TEnum>() where TEnum : Enum
        {
            foreach (TEnum value in Enum.GetValues(typeof(TEnum)))
            {
                yield return new EnumItem<TEnum>
                {
                    Value = value,
                    Name = RegexCamelCase().Replace(
                        Enum.GetName(typeof(TEnum), value),
                        "$1 $2"
                    )
                };
            }
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
            if (color == null || color.Value.IsEmpty)
            {
                rdbNoColor.Checked = true;
            }
            else if (color == Color.White)
            {
                rdbWhite.Checked = true;
            }
            else if (color == Color.Black)
            {
                rdbBlack.Checked = true;
            }
            else
            {
                rdbNoColor.Checked =
                rdbWhite.Checked =
                rdbBlack.Checked = false;

                SetTextColor(color);
            }
        }

        private void rdbNoColor_CheckedChanged(object sender, EventArgs e)
        {
            SetTextColor(null);
        }

        private void rdbWhite_CheckedChanged(object sender, EventArgs e)
        {
            SetTextColor(Color.White);
        }

        private void rdbBlack_CheckedChanged(object sender, EventArgs e)
        {
            SetTextColor(Color.Black);
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

        private void ddlSubtitlesType_SelectedIndexChanged(object sender, EventArgs e)
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

                lblSubtitlesSpeed.Enabled =
                ddlSubtitlesSpeed.Enabled = true;
            }
            else if (subtitlesType == SubtitlesType.StaticChatSubtitles)
            {
                lblSubtitlesLocation.Enabled =
                ddlSubtitlesLocation.Enabled = true;
            }
        }

        private void txtJsonFile_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtJsonFile.Text))
                ResetFormTitle();
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

        private void LoadJsonFile(string jsonFile)
        {
            if (IsJsonFile(jsonFile) == false)
                return;

            txtJsonFile.Text = jsonFile;

            openJsonFileDialog.FileName = Path.GetFileName(jsonFile);
            openJsonFileDialog.InitialDirectory = Path.GetDirectoryName(jsonFile);

            ResetFormTitle();
            Text = openJsonFileDialog.FileName + " - " + Text;
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
                MessageBoxHelper.Show(
                    this,
                    "Not a JSON file." + Environment.NewLine + jsonFile,
                    $"File Error - {Program.Version()}",
                    MessageBoxIcon.Error
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
            var sb = new StringBuilder("TwitchChatToSubtitles.exe");

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

            MessageBoxHelper.ShowInformation(this, sb.ToString());
        }

        #endregion

        #region UI Settings

        [Serializable]
        private class UISettings
        {
            public SubtitlesType ddlSubtitlesType_SelectedValue { get; set; }
            public bool chkColorUserNames_Checked { get; set; }
            public bool chkRemoveEmoticonNames_Checked { get; set; }
            public bool chkShowTimestamps_Checked { get; set; }
            public SubtitlesLocation ddlSubtitlesLocation_SelectedValue { get; set; }
            public SubtitlesFontSize ddlSubtitlesFontSize_SelectedValue { get; set; }
            public SubtitlesSpeed ddlSubtitlesSpeed_SelectedValue { get; set; }
            public decimal nudTimeOffset_Value { get; set; }
            public decimal nudSubtitleShowDuration_Value { get; set; }
            public string TextColor { get; set; }
            public bool chkCloseWhenFinishedSuccessfully_Checked { get; set; }
            public string JsonDirectory { get; set; }
        }

        private const string settingsFileName = "TwitchChatToSubtitlesUI.settings";

        private void LoadUISettings()
        {
            UISettings settings = DeserializeUISettings();
            if (settings == null)
                return;

            ddlSubtitlesType.SelectedValue = settings.ddlSubtitlesType_SelectedValue;
            chkColorUserNames.Checked = settings.chkColorUserNames_Checked;
            chkRemoveEmoticonNames.Checked = settings.chkRemoveEmoticonNames_Checked;
            chkShowTimestamps.Checked = settings.chkShowTimestamps_Checked;
            ddlSubtitlesLocation.SelectedValue = settings.ddlSubtitlesLocation_SelectedValue;
            ddlSubtitlesFontSize.SelectedValue = settings.ddlSubtitlesFontSize_SelectedValue;
            ddlSubtitlesSpeed.SelectedValue = settings.ddlSubtitlesSpeed_SelectedValue;
            nudTimeOffset.Value = settings.nudTimeOffset_Value;
            nudSubtitleShowDuration.Value = settings.nudSubtitleShowDuration_Value;
            chkCloseWhenFinishedSuccessfully.Checked = settings.chkCloseWhenFinishedSuccessfully_Checked;

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

        private static UISettings DeserializeUISettings()
        {
            try
            {
                var settingsFile = Path.Combine(AppContext.BaseDirectory, settingsFileName);
                if (File.Exists(settingsFile) == false)
                    return null;

                return JsonSerializer.Deserialize<UISettings>(File.ReadAllText(settingsFile));
            }
            catch
            {
                return null;
            }
        }

        private void TwitchChatToSubtitlesForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SerializeUISettings(GetUISettings());
        }

        private UISettings GetUISettings()
        {
            string jsonDirectory = null;
            if (string.IsNullOrEmpty(txtJsonFile.Text) == false)
            {
                try
                {
                    jsonDirectory = Path.GetDirectoryName(txtJsonFile.Text);
                }
                catch { }
            }

            return new UISettings()
            {
                ddlSubtitlesType_SelectedValue = (SubtitlesType)ddlSubtitlesType.SelectedValue,
                chkColorUserNames_Checked = chkColorUserNames.Checked,
                chkRemoveEmoticonNames_Checked = chkRemoveEmoticonNames.Checked,
                chkShowTimestamps_Checked = chkShowTimestamps.Checked,
                ddlSubtitlesLocation_SelectedValue = (SubtitlesLocation)ddlSubtitlesLocation.SelectedValue,
                ddlSubtitlesFontSize_SelectedValue = (SubtitlesFontSize)ddlSubtitlesFontSize.SelectedValue,
                ddlSubtitlesSpeed_SelectedValue = (SubtitlesSpeed)ddlSubtitlesSpeed.SelectedValue,
                nudTimeOffset_Value = nudTimeOffset.Value,
                nudSubtitleShowDuration_Value = nudSubtitleShowDuration.Value,
                TextColor = (textColor != null ? ColorToHex(textColor.Value) : null),
                chkCloseWhenFinishedSuccessfully_Checked = chkCloseWhenFinishedSuccessfully.Checked,
                JsonDirectory = jsonDirectory
            };
        }

        private static void SerializeUISettings(UISettings settings)
        {
            try
            {
                var settingsFile = Path.Combine(AppContext.BaseDirectory, settingsFileName);
                File.WriteAllText(settingsFile, JsonSerializer.Serialize(settings));
            }
            catch
            {
            }
        }

        #endregion
    }
}
