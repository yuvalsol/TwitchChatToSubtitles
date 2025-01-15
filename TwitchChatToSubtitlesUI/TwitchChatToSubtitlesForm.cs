using System.Text.Json;
using System.Text.RegularExpressions;
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
            this.Text = Program.Version();
        }

        private void TwitchChatToSubtitlesForm_Shown(object sender, EventArgs e)
        {
            LoadUISettings();

            openJsonFileDialog.InitialDirectory = AppContext.BaseDirectory;

            if (string.IsNullOrEmpty(argsJsonFile))
            {
                string[] jsonFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.json");
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

        #region Controls

        private void ddlSubtitlesType_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblSubtitlesLocation.Enabled = false;
            ddlSubtitlesLocation.Enabled = false;

            lblSubtitlesSpeed.Enabled = false;
            ddlSubtitlesSpeed.Enabled = false;

            lblSubtitleShowDuration.Enabled = false;
            nudSubtitleShowDuration.Enabled = false;

            var subtitlesType = (SubtitlesType)ddlSubtitlesType.SelectedValue;
            if (subtitlesType == SubtitlesType.RegularSubtitles)
            {
                lblSubtitleShowDuration.Enabled = true;
                nudSubtitleShowDuration.Enabled = true;
            }
            else if (subtitlesType == SubtitlesType.RollingChatSubtitles)
            {
                lblSubtitlesLocation.Enabled = true;
                ddlSubtitlesLocation.Enabled = true;

                lblSubtitlesSpeed.Enabled = true;
                ddlSubtitlesSpeed.Enabled = true;
            }
            else if (subtitlesType == SubtitlesType.StaticChatSubtitles)
            {
                lblSubtitlesLocation.Enabled = true;
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
            this.Close();
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
            this.Text = openJsonFileDialog.FileName + " - " + this.Text;
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
                SubtitlesType = (SubtitlesType)ddlSubtitlesType.SelectedValue,
                SubtitleShowDuration = Convert.ToInt32(nudSubtitleShowDuration.Value),
                SubtitlesSpeed = (SubtitlesSpeed)ddlSubtitlesSpeed.SelectedValue,
                SubtitlesLocation = (SubtitlesLocation)ddlSubtitlesLocation.SelectedValue,
                SubtitlesFontSize = (SubtitlesFontSize)ddlSubtitlesFontSize.SelectedValue,
                ShowTimestamps = chkShowTimestamps.Checked,
                TimeOffset = Convert.ToInt32(nudTimeOffset.Value),
                RemoveEmoticonNames = chkRemoveEmoticonNames.Checked,
                ColorUserNames = chkColorUserNames.Checked
            };

            var twitchSubtitles = new TwitchSubtitles(settings);

            twitchSubtitles.Start += (object sender, EventArgs e) =>
            {
                btnCopy.Enabled = false;
                txtConsole.Clear();
                Application.DoEvents();

                if (settings.RegularSubtitles)
                    WriteLine("Regular Subtitles.");
                else if (settings.RollingChatSubtitles)
                    WriteLine("Rolling Chat Subtitles.");
                else if (settings.StaticChatSubtitles)
                    WriteLine("Static Chat Subtitles.");
            };

            twitchSubtitles.StartLoadingJsonFile += (object sender, StartLoadingJsonFileEventArgs e) =>
            {
                WriteLine("Loading JSON file...");
            };

            twitchSubtitles.FinishLoadingJsonFile += (object sender, FinishLoadingJsonFileEventArgs e) =>
            {
                if (e.Error == null)
                    WriteLine("JSON file loaded successfully.");
                else
                    WriteLine("Could not load JSON file.");
                WriteLine("JSON file: " + e.JsonFile);
            };

            twitchSubtitles.StartWritingPreparations += (object sender, StartWritingPreparationsEventArgs e) =>
            {
                string preparations =
                    (e.RemoveEmoticonNames ? "emoticons" : string.Empty) +
                    (e.RemoveEmoticonNames && e.ColorUserNames ? ", " : string.Empty) +
                    (e.ColorUserNames ? "user colors" : string.Empty);

                WriteLine($"Begin writing preparations ({preparations})...");
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
                WriteLine("Subtitles: 0");

                selectionLength = txtConsole.TextLength - selectionStart;
            };

            var lockObj = new object();

            void PrintProgress(object sender, ProgressEventArgs e)
            {
                var strMessages = $"Chat Messages: {e.MessagesCount:N0} / {e.TotalMessages:N0}";
                if (e.DiscardedMessagesCount > 0)
                    strMessages += $" (discarded messages {e.DiscardedMessagesCount:N0})";

                var strSubtitles = $"Subtitles: {e.SubtitlesCount:N0}";

                var strSelection = $"{strMessages}{Environment.NewLine}{strSubtitles}{Environment.NewLine}";

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
                    WriteLine("Subtitles file: " + e.SrtFile);

                    string processTime = e.ProcessTime.ToString(e.ProcessTime.Days > 0 ? "d':'hh':'mm':'ss'.'fff" : e.ProcessTime.Hours > 0 ? "h':'mm':'ss'.'fff" : "m':'ss'.'fff");
                    WriteLine("Process Time: " + processTime);

                    Application.DoEvents();

                    if (chkCloseWhenFinishedSuccessfully.Checked)
                        this.Close();
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
                    WriteErrorLine("Failed to write subtitles.");
                    WriteErrorLine("Error: " + e.Error.Message);

                    Exception ex = e.Error.InnerException;
                    while (ex != null)
                    {
                        WriteErrorLine("Error: " + ex.Message);
                        ex = ex.InnerException;
                    }
#elif DEBUG
                    WriteErrorLine(e.Error.GetExceptionErrorMessage("Failed to write subtitles."));
#endif
                }

                btnCopy.Enabled = true;
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

        #region UI Settings

        [Serializable]
        private class UISettings
        {
            public SubtitlesType ddlSubtitlesType_SelectedValue { get; set; }
            public decimal nudSubtitleShowDuration_Value { get; set; }
            public SubtitlesSpeed ddlSubtitlesSpeed_SelectedValue { get; set; }
            public SubtitlesLocation ddlSubtitlesLocation_SelectedValue { get; set; }
            public SubtitlesFontSize ddlSubtitlesFontSize_SelectedValue { get; set; }
            public bool chkShowTimestamps_Checked { get; set; }
            public decimal nudTimeOffset_Value { get; set; }
            public bool chkRemoveEmoticonNames_Checked { get; set; }
            public bool chkColorUserNames_Checked { get; set; }
            public bool chkCloseWhenFinishedSuccessfully_Checked { get; set; }
        }

        private const string settingsFileName = "TwitchChatToSubtitlesUI.settings";

        private void LoadUISettings()
        {
            UISettings settings = DeserializeUISettings();
            if (settings == null)
                return;

            ddlSubtitlesType.SelectedValue = settings.ddlSubtitlesType_SelectedValue;
            nudSubtitleShowDuration.Value = settings.nudSubtitleShowDuration_Value;
            ddlSubtitlesSpeed.SelectedValue = settings.ddlSubtitlesSpeed_SelectedValue;
            ddlSubtitlesLocation.SelectedValue = settings.ddlSubtitlesLocation_SelectedValue;
            ddlSubtitlesFontSize.SelectedValue = settings.ddlSubtitlesFontSize_SelectedValue;
            chkShowTimestamps.Checked = settings.chkShowTimestamps_Checked;
            nudTimeOffset.Value = settings.nudTimeOffset_Value;
            chkRemoveEmoticonNames.Checked = settings.chkRemoveEmoticonNames_Checked;
            chkColorUserNames.Checked = settings.chkColorUserNames_Checked;
            chkCloseWhenFinishedSuccessfully.Checked = settings.chkCloseWhenFinishedSuccessfully_Checked;
        }

        private static UISettings DeserializeUISettings()
        {
            try
            {
                if (File.Exists(settingsFileName) == false)
                    return null;

                return JsonSerializer.Deserialize<UISettings>(File.ReadAllText(settingsFileName));
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
            return new UISettings()
            {
                ddlSubtitlesType_SelectedValue = (SubtitlesType)ddlSubtitlesType.SelectedValue,
                nudSubtitleShowDuration_Value = nudSubtitleShowDuration.Value,
                ddlSubtitlesSpeed_SelectedValue = (SubtitlesSpeed)ddlSubtitlesSpeed.SelectedValue,
                ddlSubtitlesLocation_SelectedValue = (SubtitlesLocation)ddlSubtitlesLocation.SelectedValue,
                ddlSubtitlesFontSize_SelectedValue = (SubtitlesFontSize)ddlSubtitlesFontSize.SelectedValue,
                chkShowTimestamps_Checked = chkShowTimestamps.Checked,
                nudTimeOffset_Value = nudTimeOffset.Value,
                chkRemoveEmoticonNames_Checked = chkRemoveEmoticonNames.Checked,
                chkColorUserNames_Checked = chkColorUserNames.Checked,
                chkCloseWhenFinishedSuccessfully_Checked = chkCloseWhenFinishedSuccessfully.Checked
            };
        }

        private static void SerializeUISettings(UISettings settings)
        {
            try
            {
                File.WriteAllText(settingsFileName, JsonSerializer.Serialize(settings));
            }
            catch
            {
            }
        }

        #endregion
    }
}
