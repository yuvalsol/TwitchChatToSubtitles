namespace TwitchChatToSubtitlesUI
{
    partial class TwitchChatToSubtitlesForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new Container();
            ComponentResourceManager resources = new ComponentResourceManager(typeof(TwitchChatToSubtitlesForm));
            txtConsole = new RichTextBox();
            ddlSubtitlesFontSize = new ComboBox();
            splitContainer1 = new SplitContainer();
            ddlSubtitlesRollingDirection = new ComboBox();
            lblSubtitlesRollingDirection = new Label();
            lblTextColor = new Label();
            flowLayoutPanelColors = new FlowLayoutPanel();
            rdbNoColor = new RadioButton();
            rdbWhite = new RadioButton();
            rdbBlack = new RadioButton();
            btnTextColor = new Button();
            btnCommandLine = new Button();
            btnCopy = new Button();
            chkCloseWhenFinishedSuccessfully = new CheckBox();
            btnClose = new Button();
            btnWriteTwitchSubtitles = new Button();
            txtJsonFile = new TextBox();
            btnJsonFile = new Button();
            chkColorUserNames = new CheckBox();
            chkRemoveEmoticonNames = new CheckBox();
            chkShowTimestamps = new CheckBox();
            lblTimeOffset = new Label();
            nudTimeOffset = new NumericUpDown();
            lblSubtitleShowDuration = new Label();
            nudSubtitleShowDuration = new NumericUpDown();
            lblSubtitlesType = new Label();
            ddlSubtitlesType = new ComboBox();
            lblSubtitlesSpeed = new Label();
            ddlSubtitlesSpeed = new ComboBox();
            lblSubtitlesLocation = new Label();
            ddlSubtitlesLocation = new ComboBox();
            lblSubtitlesFontSize = new Label();
            toolTip = new ToolTip(components);
            openJsonFileDialog = new OpenFileDialog();
            colorDialog = new ColorDialog();
            ((ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            flowLayoutPanelColors.SuspendLayout();
            ((ISupportInitialize)nudTimeOffset).BeginInit();
            ((ISupportInitialize)nudSubtitleShowDuration).BeginInit();
            SuspendLayout();
            // 
            // txtConsole
            // 
            txtConsole.BackColor = Color.Black;
            txtConsole.BorderStyle = BorderStyle.None;
            txtConsole.DetectUrls = false;
            txtConsole.Dock = DockStyle.Fill;
            txtConsole.Font = new Font("Consolas", 11.25F);
            txtConsole.ForeColor = Color.White;
            txtConsole.Location = new Point(0, 0);
            txtConsole.Name = "txtConsole";
            txtConsole.ReadOnly = true;
            txtConsole.Size = new Size(714, 279);
            txtConsole.TabIndex = 1;
            txtConsole.TabStop = false;
            txtConsole.Text = "";
            txtConsole.WordWrap = false;
            // 
            // ddlSubtitlesFontSize
            // 
            ddlSubtitlesFontSize.DropDownStyle = ComboBoxStyle.DropDownList;
            ddlSubtitlesFontSize.FormattingEnabled = true;
            ddlSubtitlesFontSize.Location = new Point(512, 10);
            ddlSubtitlesFontSize.Name = "ddlSubtitlesFontSize";
            ddlSubtitlesFontSize.Size = new Size(120, 28);
            ddlSubtitlesFontSize.TabIndex = 6;
            toolTip.SetToolTip(ddlSubtitlesFontSize, "The font size of the subtitles.");
            ddlSubtitlesFontSize.SelectedIndexChanged += ddl_SelectedIndexChanged;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.IsSplitterFixed = true;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(txtConsole);
            splitContainer1.Panel1.RightToLeft = RightToLeft.No;
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(ddlSubtitlesRollingDirection);
            splitContainer1.Panel2.Controls.Add(lblSubtitlesRollingDirection);
            splitContainer1.Panel2.Controls.Add(lblTextColor);
            splitContainer1.Panel2.Controls.Add(flowLayoutPanelColors);
            splitContainer1.Panel2.Controls.Add(btnTextColor);
            splitContainer1.Panel2.Controls.Add(btnCommandLine);
            splitContainer1.Panel2.Controls.Add(btnCopy);
            splitContainer1.Panel2.Controls.Add(chkCloseWhenFinishedSuccessfully);
            splitContainer1.Panel2.Controls.Add(btnClose);
            splitContainer1.Panel2.Controls.Add(btnWriteTwitchSubtitles);
            splitContainer1.Panel2.Controls.Add(txtJsonFile);
            splitContainer1.Panel2.Controls.Add(btnJsonFile);
            splitContainer1.Panel2.Controls.Add(chkColorUserNames);
            splitContainer1.Panel2.Controls.Add(chkRemoveEmoticonNames);
            splitContainer1.Panel2.Controls.Add(chkShowTimestamps);
            splitContainer1.Panel2.Controls.Add(lblTimeOffset);
            splitContainer1.Panel2.Controls.Add(nudTimeOffset);
            splitContainer1.Panel2.Controls.Add(lblSubtitleShowDuration);
            splitContainer1.Panel2.Controls.Add(nudSubtitleShowDuration);
            splitContainer1.Panel2.Controls.Add(lblSubtitlesType);
            splitContainer1.Panel2.Controls.Add(ddlSubtitlesType);
            splitContainer1.Panel2.Controls.Add(lblSubtitlesSpeed);
            splitContainer1.Panel2.Controls.Add(ddlSubtitlesSpeed);
            splitContainer1.Panel2.Controls.Add(lblSubtitlesLocation);
            splitContainer1.Panel2.Controls.Add(ddlSubtitlesLocation);
            splitContainer1.Panel2.Controls.Add(lblSubtitlesFontSize);
            splitContainer1.Panel2.Controls.Add(ddlSubtitlesFontSize);
            splitContainer1.Panel2.RightToLeft = RightToLeft.No;
            splitContainer1.Size = new Size(714, 561);
            splitContainer1.SplitterDistance = 279;
            splitContainer1.TabIndex = 0;
            splitContainer1.TabStop = false;
            // 
            // ddlSubtitlesRollingDirection
            // 
            ddlSubtitlesRollingDirection.DropDownStyle = ComboBoxStyle.DropDownList;
            ddlSubtitlesRollingDirection.FormattingEnabled = true;
            ddlSubtitlesRollingDirection.Location = new Point(564, 41);
            ddlSubtitlesRollingDirection.Name = "ddlSubtitlesRollingDirection";
            ddlSubtitlesRollingDirection.Size = new Size(127, 28);
            ddlSubtitlesRollingDirection.TabIndex = 7;
            toolTip.SetToolTip(ddlSubtitlesRollingDirection, "The direction that the subtitles roll.");
            ddlSubtitlesRollingDirection.SelectedIndexChanged += ddl_SelectedIndexChanged;
            // 
            // lblSubtitlesRollingDirection
            // 
            lblSubtitlesRollingDirection.AutoSize = true;
            lblSubtitlesRollingDirection.Location = new Point(374, 45);
            lblSubtitlesRollingDirection.Name = "lblSubtitlesRollingDirection";
            lblSubtitlesRollingDirection.Size = new Size(182, 20);
            lblSubtitlesRollingDirection.TabIndex = 0;
            lblSubtitlesRollingDirection.Text = "Subtitles Rolling Direction";
            toolTip.SetToolTip(lblSubtitlesRollingDirection, "The direction that the subtitles roll.");
            // 
            // lblTextColor
            // 
            lblTextColor.BorderStyle = BorderStyle.Fixed3D;
            lblTextColor.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold);
            lblTextColor.Location = new Point(374, 165);
            lblTextColor.Name = "lblTextColor";
            lblTextColor.Size = new Size(317, 30);
            lblTextColor.TabIndex = 0;
            lblTextColor.Text = "Media Player's Default Text Color";
            lblTextColor.TextAlign = ContentAlignment.MiddleCenter;
            toolTip.SetToolTip(lblTextColor, "For how long a subtitle is visible on the screen, in seconds.");
            // 
            // flowLayoutPanelColors
            // 
            flowLayoutPanelColors.AutoSize = true;
            flowLayoutPanelColors.Controls.Add(rdbNoColor);
            flowLayoutPanelColors.Controls.Add(rdbWhite);
            flowLayoutPanelColors.Controls.Add(rdbBlack);
            flowLayoutPanelColors.Location = new Point(117, 165);
            flowLayoutPanelColors.Name = "flowLayoutPanelColors";
            flowLayoutPanelColors.Size = new Size(233, 30);
            flowLayoutPanelColors.TabIndex = 12;
            // 
            // rdbNoColor
            // 
            rdbNoColor.AutoSize = true;
            rdbNoColor.Checked = true;
            rdbNoColor.Location = new Point(3, 3);
            rdbNoColor.Name = "rdbNoColor";
            rdbNoColor.Size = new Size(87, 24);
            rdbNoColor.TabIndex = 1;
            rdbNoColor.TabStop = true;
            rdbNoColor.Text = "No Color";
            rdbNoColor.UseVisualStyleBackColor = true;
            rdbNoColor.CheckedChanged += rdbNoColor_CheckedChanged;
            // 
            // rdbWhite
            // 
            rdbWhite.AutoSize = true;
            rdbWhite.Location = new Point(96, 3);
            rdbWhite.Name = "rdbWhite";
            rdbWhite.Size = new Size(66, 24);
            rdbWhite.TabIndex = 2;
            rdbWhite.Text = "White";
            rdbWhite.UseVisualStyleBackColor = true;
            rdbWhite.CheckedChanged += rdbWhite_CheckedChanged;
            // 
            // rdbBlack
            // 
            rdbBlack.AutoSize = true;
            rdbBlack.Location = new Point(168, 3);
            rdbBlack.Name = "rdbBlack";
            rdbBlack.Size = new Size(62, 24);
            rdbBlack.TabIndex = 3;
            rdbBlack.Text = "Black";
            rdbBlack.UseVisualStyleBackColor = true;
            rdbBlack.CheckedChanged += rdbBlack_CheckedChanged;
            // 
            // btnTextColor
            // 
            btnTextColor.AutoSize = true;
            btnTextColor.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnTextColor.Location = new Point(15, 165);
            btnTextColor.Name = "btnTextColor";
            btnTextColor.Size = new Size(86, 30);
            btnTextColor.TabIndex = 11;
            btnTextColor.Text = "Text Color";
            toolTip.SetToolTip(btnTextColor, "The color of the subtitles text.");
            btnTextColor.UseVisualStyleBackColor = true;
            btnTextColor.Click += btnTextColor_Click;
            // 
            // btnCommandLine
            // 
            btnCommandLine.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnCommandLine.AutoSize = true;
            btnCommandLine.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnCommandLine.Location = new Point(485, 236);
            btnCommandLine.Name = "btnCommandLine";
            btnCommandLine.Size = new Size(119, 30);
            btnCommandLine.TabIndex = 17;
            btnCommandLine.Text = "Command Line";
            btnCommandLine.UseVisualStyleBackColor = true;
            btnCommandLine.Click += btnCommandLine_Click;
            // 
            // btnCopy
            // 
            btnCopy.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCopy.AutoSize = true;
            btnCopy.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnCopy.Font = new Font("Segoe UI", 8F);
            btnCopy.Location = new Point(668, 2);
            btnCopy.Name = "btnCopy";
            btnCopy.Size = new Size(43, 23);
            btnCopy.TabIndex = 18;
            btnCopy.Text = "Copy";
            btnCopy.UseVisualStyleBackColor = true;
            btnCopy.Click += btnCopy_Click;
            // 
            // chkCloseWhenFinishedSuccessfully
            // 
            chkCloseWhenFinishedSuccessfully.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            chkCloseWhenFinishedSuccessfully.AutoSize = true;
            chkCloseWhenFinishedSuccessfully.CheckAlign = ContentAlignment.MiddleRight;
            chkCloseWhenFinishedSuccessfully.Location = new Point(208, 239);
            chkCloseWhenFinishedSuccessfully.Name = "chkCloseWhenFinishedSuccessfully";
            chkCloseWhenFinishedSuccessfully.Size = new Size(246, 24);
            chkCloseWhenFinishedSuccessfully.TabIndex = 16;
            chkCloseWhenFinishedSuccessfully.Text = "Close When Finished Successfully";
            toolTip.SetToolTip(chkCloseWhenFinishedSuccessfully, "Whether to show chat message timestamps.");
            chkCloseWhenFinishedSuccessfully.UseVisualStyleBackColor = true;
            chkCloseWhenFinishedSuccessfully.CheckedChanged += chk_CheckedChanged;
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.AutoSize = true;
            btnClose.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnClose.DialogResult = DialogResult.Cancel;
            btnClose.Location = new Point(635, 236);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(55, 30);
            btnClose.TabIndex = 19;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // btnWriteTwitchSubtitles
            // 
            btnWriteTwitchSubtitles.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnWriteTwitchSubtitles.AutoSize = true;
            btnWriteTwitchSubtitles.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnWriteTwitchSubtitles.Location = new Point(15, 236);
            btnWriteTwitchSubtitles.Name = "btnWriteTwitchSubtitles";
            btnWriteTwitchSubtitles.Size = new Size(162, 30);
            btnWriteTwitchSubtitles.TabIndex = 15;
            btnWriteTwitchSubtitles.Text = "Write Twitch Subtitles";
            btnWriteTwitchSubtitles.UseVisualStyleBackColor = true;
            btnWriteTwitchSubtitles.Click += btnWriteTwitchSubtitles_Click;
            // 
            // txtJsonFile
            // 
            txtJsonFile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtJsonFile.Location = new Point(97, 202);
            txtJsonFile.Name = "txtJsonFile";
            txtJsonFile.Size = new Size(594, 27);
            txtJsonFile.TabIndex = 14;
            txtJsonFile.TextChanged += txtJsonFile_TextChanged;
            // 
            // btnJsonFile
            // 
            btnJsonFile.AutoSize = true;
            btnJsonFile.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnJsonFile.Location = new Point(15, 200);
            btnJsonFile.Name = "btnJsonFile";
            btnJsonFile.Size = new Size(74, 30);
            btnJsonFile.TabIndex = 13;
            btnJsonFile.Text = "Json File";
            btnJsonFile.UseVisualStyleBackColor = true;
            btnJsonFile.Click += btnJsonFile_Click;
            // 
            // chkColorUserNames
            // 
            chkColorUserNames.AutoSize = true;
            chkColorUserNames.CheckAlign = ContentAlignment.MiddleRight;
            chkColorUserNames.Location = new Point(15, 43);
            chkColorUserNames.Name = "chkColorUserNames";
            chkColorUserNames.Size = new Size(147, 24);
            chkColorUserNames.TabIndex = 2;
            chkColorUserNames.Text = "Color User Names";
            toolTip.SetToolTip(chkColorUserNames, "Whether to color user names.");
            chkColorUserNames.UseVisualStyleBackColor = true;
            chkColorUserNames.CheckedChanged += chk_CheckedChanged;
            // 
            // chkRemoveEmoticonNames
            // 
            chkRemoveEmoticonNames.AutoSize = true;
            chkRemoveEmoticonNames.CheckAlign = ContentAlignment.MiddleRight;
            chkRemoveEmoticonNames.Location = new Point(15, 74);
            chkRemoveEmoticonNames.Name = "chkRemoveEmoticonNames";
            chkRemoveEmoticonNames.Size = new Size(199, 24);
            chkRemoveEmoticonNames.TabIndex = 3;
            chkRemoveEmoticonNames.Text = "Remove Emoticon Names";
            toolTip.SetToolTip(chkRemoveEmoticonNames, "Remove emoticon and badge names.");
            chkRemoveEmoticonNames.UseVisualStyleBackColor = true;
            chkRemoveEmoticonNames.CheckedChanged += chk_CheckedChanged;
            // 
            // chkShowTimestamps
            // 
            chkShowTimestamps.AutoSize = true;
            chkShowTimestamps.CheckAlign = ContentAlignment.MiddleRight;
            chkShowTimestamps.Location = new Point(15, 104);
            chkShowTimestamps.Name = "chkShowTimestamps";
            chkShowTimestamps.Size = new Size(148, 24);
            chkShowTimestamps.TabIndex = 4;
            chkShowTimestamps.Text = "Show Timestamps";
            toolTip.SetToolTip(chkShowTimestamps, "Whether to show chat message timestamps.");
            chkShowTimestamps.UseVisualStyleBackColor = true;
            chkShowTimestamps.CheckedChanged += chk_CheckedChanged;
            // 
            // lblTimeOffset
            // 
            lblTimeOffset.AutoSize = true;
            lblTimeOffset.Location = new Point(374, 106);
            lblTimeOffset.Name = "lblTimeOffset";
            lblTimeOffset.Size = new Size(153, 20);
            lblTimeOffset.TabIndex = 0;
            lblTimeOffset.Text = "Time Offset (seconds)";
            toolTip.SetToolTip(lblTimeOffset, "Time offset for all subtitles, in seconds.");
            // 
            // nudTimeOffset
            // 
            nudTimeOffset.Location = new Point(535, 103);
            nudTimeOffset.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            nudTimeOffset.Minimum = new decimal(new int[] { int.MinValue, 0, 0, int.MinValue });
            nudTimeOffset.Name = "nudTimeOffset";
            nudTimeOffset.Size = new Size(80, 27);
            nudTimeOffset.TabIndex = 9;
            nudTimeOffset.TextAlign = HorizontalAlignment.Center;
            toolTip.SetToolTip(nudTimeOffset, "Time offset for all subtitles, in seconds.");
            nudTimeOffset.ValueChanged += nud_ValueChanged;
            // 
            // lblSubtitleShowDuration
            // 
            lblSubtitleShowDuration.AutoSize = true;
            lblSubtitleShowDuration.Location = new Point(374, 137);
            lblSubtitleShowDuration.Name = "lblSubtitleShowDuration";
            lblSubtitleShowDuration.Size = new Size(229, 20);
            lblSubtitleShowDuration.TabIndex = 0;
            lblSubtitleShowDuration.Text = "Subtitle Show Duration (seconds)";
            toolTip.SetToolTip(lblSubtitleShowDuration, "For how long a subtitle is visible on the screen, in seconds.");
            // 
            // nudSubtitleShowDuration
            // 
            nudSubtitleShowDuration.Location = new Point(611, 134);
            nudSubtitleShowDuration.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            nudSubtitleShowDuration.Minimum = new decimal(new int[] { int.MinValue, 0, 0, int.MinValue });
            nudSubtitleShowDuration.Name = "nudSubtitleShowDuration";
            nudSubtitleShowDuration.Size = new Size(80, 27);
            nudSubtitleShowDuration.TabIndex = 10;
            nudSubtitleShowDuration.TextAlign = HorizontalAlignment.Center;
            toolTip.SetToolTip(nudSubtitleShowDuration, "For how long a subtitle is visible on the screen, in seconds.");
            nudSubtitleShowDuration.Value = new decimal(new int[] { 5, 0, 0, 0 });
            nudSubtitleShowDuration.ValueChanged += nud_ValueChanged;
            // 
            // lblSubtitlesType
            // 
            lblSubtitlesType.AutoSize = true;
            lblSubtitlesType.Location = new Point(15, 14);
            lblSubtitlesType.Name = "lblSubtitlesType";
            lblSubtitlesType.Size = new Size(101, 20);
            lblSubtitlesType.TabIndex = 0;
            lblSubtitlesType.Text = "Subtitles Type";
            toolTip.SetToolTip(lblSubtitlesType, resources.GetString("lblSubtitlesType.ToolTip"));
            // 
            // ddlSubtitlesType
            // 
            ddlSubtitlesType.DropDownStyle = ComboBoxStyle.DropDownList;
            ddlSubtitlesType.FormattingEnabled = true;
            ddlSubtitlesType.Location = new Point(124, 10);
            ddlSubtitlesType.Name = "ddlSubtitlesType";
            ddlSubtitlesType.Size = new Size(180, 28);
            ddlSubtitlesType.TabIndex = 1;
            toolTip.SetToolTip(ddlSubtitlesType, resources.GetString("ddlSubtitlesType.ToolTip"));
            ddlSubtitlesType.SelectedIndexChanged += ddlSubtitlesType_SelectedIndexChanged;
            // 
            // lblSubtitlesSpeed
            // 
            lblSubtitlesSpeed.AutoSize = true;
            lblSubtitlesSpeed.Location = new Point(374, 76);
            lblSubtitlesSpeed.Name = "lblSubtitlesSpeed";
            lblSubtitlesSpeed.Size = new Size(112, 20);
            lblSubtitlesSpeed.TabIndex = 0;
            lblSubtitlesSpeed.Text = "Subtitles Speed";
            toolTip.SetToolTip(lblSubtitlesSpeed, "How fast the subtitles roll.");
            // 
            // ddlSubtitlesSpeed
            // 
            ddlSubtitlesSpeed.DropDownStyle = ComboBoxStyle.DropDownList;
            ddlSubtitlesSpeed.FormattingEnabled = true;
            ddlSubtitlesSpeed.Location = new Point(494, 72);
            ddlSubtitlesSpeed.Name = "ddlSubtitlesSpeed";
            ddlSubtitlesSpeed.Size = new Size(120, 28);
            ddlSubtitlesSpeed.TabIndex = 8;
            toolTip.SetToolTip(ddlSubtitlesSpeed, "How fast the subtitles roll.");
            ddlSubtitlesSpeed.SelectedIndexChanged += ddl_SelectedIndexChanged;
            // 
            // lblSubtitlesLocation
            // 
            lblSubtitlesLocation.AutoSize = true;
            lblSubtitlesLocation.Location = new Point(15, 137);
            lblSubtitlesLocation.Name = "lblSubtitlesLocation";
            lblSubtitlesLocation.Size = new Size(127, 20);
            lblSubtitlesLocation.TabIndex = 0;
            lblSubtitlesLocation.Text = "Subtitles Location";
            toolTip.SetToolTip(lblSubtitlesLocation, "The location of the subtitles on the screen.");
            // 
            // ddlSubtitlesLocation
            // 
            ddlSubtitlesLocation.DropDownStyle = ComboBoxStyle.DropDownList;
            ddlSubtitlesLocation.FormattingEnabled = true;
            ddlSubtitlesLocation.Location = new Point(150, 133);
            ddlSubtitlesLocation.Name = "ddlSubtitlesLocation";
            ddlSubtitlesLocation.Size = new Size(200, 28);
            ddlSubtitlesLocation.TabIndex = 5;
            toolTip.SetToolTip(ddlSubtitlesLocation, "The location of the subtitles on the screen.");
            ddlSubtitlesLocation.SelectedIndexChanged += ddl_SelectedIndexChanged;
            // 
            // lblSubtitlesFontSize
            // 
            lblSubtitlesFontSize.AutoSize = true;
            lblSubtitlesFontSize.Location = new Point(374, 14);
            lblSubtitlesFontSize.Name = "lblSubtitlesFontSize";
            lblSubtitlesFontSize.Size = new Size(130, 20);
            lblSubtitlesFontSize.TabIndex = 0;
            lblSubtitlesFontSize.Text = "Subtitles Font Size";
            toolTip.SetToolTip(lblSubtitlesFontSize, "The font size of the subtitles.");
            // 
            // toolTip
            // 
            toolTip.AutomaticDelay = 1500;
            // 
            // openJsonFileDialog
            // 
            openJsonFileDialog.Filter = "Json files (*.json)|*.json";
            // 
            // TwitchChatToSubtitlesForm
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnClose;
            ClientSize = new Size(714, 561);
            Controls.Add(splitContainer1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "TwitchChatToSubtitlesForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Twitch Chat To Subtitles";
            FormClosing += TwitchChatToSubtitlesForm_FormClosing;
            Load += TwitchChatToSubtitlesForm_Load;
            Shown += TwitchChatToSubtitlesForm_Shown;
            DragDrop += TwitchChatToSubtitlesForm_DragDrop;
            DragEnter += TwitchChatToSubtitlesForm_DragEnter;
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            flowLayoutPanelColors.ResumeLayout(false);
            flowLayoutPanelColors.PerformLayout();
            ((ISupportInitialize)nudTimeOffset).EndInit();
            ((ISupportInitialize)nudSubtitleShowDuration).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private RichTextBox txtConsole;
        private ComboBox ddlSubtitlesFontSize;
        private SplitContainer splitContainer1;
        private Label lblSubtitlesFontSize;
        private Label lblSubtitlesSpeed;
        private ComboBox ddlSubtitlesSpeed;
        private Label lblSubtitlesLocation;
        private ComboBox ddlSubtitlesLocation;
        private Label lblSubtitlesType;
        private ComboBox ddlSubtitlesType;
        private NumericUpDown nudSubtitleShowDuration;
        private Label lblSubtitleShowDuration;
        private Label lblTimeOffset;
        private NumericUpDown nudTimeOffset;
        private CheckBox chkColorUserNames;
        private CheckBox chkRemoveEmoticonNames;
        private CheckBox chkShowTimestamps;
        private ToolTip toolTip;
        private OpenFileDialog openJsonFileDialog;
        private TextBox txtJsonFile;
        private Button btnJsonFile;
        private Button btnClose;
        private Button btnWriteTwitchSubtitles;
        private CheckBox chkCloseWhenFinishedSuccessfully;
        private Button btnCopy;
        private Button btnCommandLine;
        private Button btnTextColor;
        private ColorDialog colorDialog;
        private FlowLayoutPanel flowLayoutPanelColors;
        private RadioButton rdbNoColor;
        private RadioButton rdbWhite;
        private RadioButton rdbBlack;
        private Label lblTextColor;
        private Label lblSubtitlesRollingDirection;
        private ComboBox ddlSubtitlesRollingDirection;
    }
}
