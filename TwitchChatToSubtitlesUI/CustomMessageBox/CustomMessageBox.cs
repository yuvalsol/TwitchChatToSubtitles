namespace System.Windows.Forms
{
    public partial class CustomMessageBox : Form
    {
        public bool Quiet { get; set; }

        private CustomMessageBox() : base()
        {
            InitializeComponent();
            this.BackColor = lblMessage.BackColor;
        }

        private CustomMessageBox(bool quiet) : this()
        {
            this.Quiet = quiet;
        }

        private void CustomMessageBox_Shown(object sender, EventArgs e)
        {
            if (this.Quiet == false)
                SystemSounds.Beep.Play();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }
            else if ((keyData & ~(Keys.ControlKey | Keys.C)) == Keys.None)
            {
                CopyToClipboard();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void CopyToClipboard(bool copyText = false)
        {
            try
            {
                Clipboard.SetText(((copyText ? string.Empty : this.Text + Environment.NewLine + Environment.NewLine) + lblMessage.Text).Trim());
                if (this.Quiet == false)
                    SystemSounds.Hand.Play();
            }
            catch { }
        }

        public static DialogResult Show(
            string text,
            string caption = null,
            CustomMessageBoxButtons buttons = CustomMessageBoxButtons.None,
            MessageBoxIcon icon = MessageBoxIcon.None,
            CustomAppearance appearance = null,
            CustomButton[] customButtons = null,
            CustomButtonText[] buttonTexts = null,
            bool quiet = false)
        {
            return Show(null, text, caption, buttons, icon, appearance, customButtons, buttonTexts, quiet);
        }

        public static DialogResult Show(
            IWin32Window owner,
            string text,
            string caption = null,
            CustomMessageBoxButtons buttons = CustomMessageBoxButtons.None,
            MessageBoxIcon icon = MessageBoxIcon.None,
            CustomAppearance appearance = null,
            CustomButton[] customButtons = null,
            CustomButtonText[] buttonTexts = null,
            bool quiet = false)
        {
            var messageBox = new CustomMessageBox(quiet);
            messageBox.Init(text, caption, buttons, icon, appearance, customButtons, buttonTexts);

            if (owner != null)
                return messageBox.ShowDialog(owner);
            else
                return messageBox.ShowDialog();
        }

        private void Init(
            string text,
            string caption = null,
            CustomMessageBoxButtons buttons = CustomMessageBoxButtons.None,
            MessageBoxIcon icon = MessageBoxIcon.None,
            CustomAppearance appearance = null,
            CustomButton[] customButtons = null,
            CustomButtonText[] buttonTexts = null)
        {
            SetCaption(caption);
            SetIcon(icon);
            List<Button> btns = SetButtons(buttons, customButtons, buttonTexts);
            SetAppearance(appearance, icon, btns);
            SetMessage(text, caption);
            SetRightToLeft(appearance, icon, btns);
        }

        private void SetCaption(string caption)
        {
            this.Text = caption;
        }

        private void SetIcon(MessageBoxIcon icon)
        {
            if (icon == MessageBoxIcon.None)
            {
                int iconX = pbxIcon.Location.X;
                int diffX = lblMessage.Location.X - iconX;
                pbxIcon.Hide();
                lblMessage.Location = new Point(iconX, lblMessage.Location.Y);
                this.Width -= diffX;
            }
            else if (icon == MessageBoxIcon.Error || icon == MessageBoxIcon.Stop || icon == MessageBoxIcon.Hand)
            {
                pbxIcon.Image = SystemIcons.Error.ToBitmap();
            }
            else if (icon == MessageBoxIcon.Question)
            {
                pbxIcon.Image = SystemIcons.Question.ToBitmap();
            }
            else if (icon == MessageBoxIcon.Exclamation || icon == MessageBoxIcon.Warning)
            {
                pbxIcon.Image = SystemIcons.Exclamation.ToBitmap();
            }
            else if (icon == MessageBoxIcon.Information || icon == MessageBoxIcon.Asterisk)
            {
                pbxIcon.Image = SystemIcons.Information.ToBitmap();
            }
        }

        private List<Button> SetButtons(CustomMessageBoxButtons buttons, CustomButton[] customButtons, CustomButtonText[] buttonTexts)
        {
            List<Button> btns = null;

            bool hasCustomButtons = customButtons != null && customButtons.Length > 0;

            if (buttons == CustomMessageBoxButtons.None && hasCustomButtons == false)
            {
                int pnlH = pnlButtons.Height;
                int lblY = lblMessage.Location.Y;
                pnlButtons.Hide();
                this.Height -= pnlH - lblY;
            }
            else
            {
                btns = [];

                AddButton(btns, buttons, CustomMessageBoxButtons.OK, DialogResult.OK, "OK", buttonTexts);
                AddButton(btns, buttons, CustomMessageBoxButtons.Abort, DialogResult.Abort, "Abort", buttonTexts);
                AddButton(btns, buttons, CustomMessageBoxButtons.Retry, DialogResult.Retry, "Retry", buttonTexts);
                AddButton(btns, buttons, CustomMessageBoxButtons.Ignore, DialogResult.Ignore, "Ignore", buttonTexts);
                AddButton(btns, buttons, CustomMessageBoxButtons.Yes, DialogResult.Yes, "Yes", buttonTexts);
                AddButton(btns, buttons, CustomMessageBoxButtons.No, DialogResult.No, "No", buttonTexts);
                AddButton(btns, buttons, CustomMessageBoxButtons.Cancel, DialogResult.Cancel, "Cancel", buttonTexts);

                AddButton(btns, buttons, CustomMessageBoxButtons.Copy, DialogResult.None, "Copy", buttonTexts, (object sender, CustomMessageBoxEventArgs e) =>
                {
                    CopyToClipboard();
                });

                AddButton(btns, buttons, CustomMessageBoxButtons.CopyText, DialogResult.None, "Copy", buttonTexts, (object sender, CustomMessageBoxEventArgs e) =>
                {
                    CopyToClipboard(true);
                });

                if (hasCustomButtons)
                {
                    foreach (var customButton in customButtons)
                    {
                        if (string.IsNullOrEmpty(customButton.Text) == false)
                            btns.Add(GetButton(customButton.Text, customButton.DialogResult ?? DialogResult.None, customButton.Click));
                    }
                }

                int tabIndex = 1;
                foreach (var btn in btns)
                    btn.TabIndex = tabIndex++;

                btns.Reverse();
                pnlButtons.Controls.AddRange([.. btns]);
            }

            return btns;
        }

        private void SetAppearance(CustomAppearance appearance, MessageBoxIcon icon, List<Button> btns)
        {
            if (appearance != null)
            {
                SetColors(appearance);
                SetFont(appearance);
                SetTextAlign(appearance, icon);
                SetButtonsAppearance(appearance, btns);
            }
        }

        private void SetColors(CustomAppearance appearance)
        {
            if (appearance.ForeColor != null)
                this.ForeColor = lblMessage.ForeColor = appearance.ForeColor.Value;

            if (appearance.BackColor != null)
                this.BackColor = lblMessage.BackColor = appearance.BackColor.Value;
        }

        private void SetFont(CustomAppearance appearance)
        {
            Font newFont = GetFont(
                this.Font,
                appearance.Font,
                appearance.FontFamilyName,
                appearance.FontSize,
                appearance.FontStyle
            );

            if (this.Font != newFont)
                this.Font = lblMessage.Font = newFont;
        }

        private void SetTextAlign(CustomAppearance appearance, MessageBoxIcon icon)
        {
            if (appearance.TextAlign != null)
            {
                lblMessage.TextAlign = appearance.TextAlign.Value;

                if (icon != MessageBoxIcon.None)
                {
                    if (lblMessage.TextAlign == ContentAlignment.MiddleLeft ||
                        lblMessage.TextAlign == ContentAlignment.MiddleCenter ||
                        lblMessage.TextAlign == ContentAlignment.MiddleRight)
                    {
                        pbxIcon.Location = new Point(
                            pbxIcon.Location.X,
                            ((2 * lblMessage.Location.Y) + lblMessage.Height - pbxIcon.Height) / 2
                        );
                    }
                    else if (lblMessage.TextAlign == ContentAlignment.BottomLeft ||
                        lblMessage.TextAlign == ContentAlignment.BottomCenter ||
                        lblMessage.TextAlign == ContentAlignment.BottomRight)
                    {
                        pbxIcon.Location = new Point(
                            pbxIcon.Location.X,
                            lblMessage.Location.Y + lblMessage.Height - pbxIcon.Height
                        );
                    }
                }
            }
        }

        private void SetButtonsAppearance(CustomAppearance appearance, List<Button> btns)
        {
            if (appearance.ButtonsAppearance != null)
            {
                SetButtonsPanelColors(appearance);

                if (btns != null && btns.Count > 0)
                {
                    foreach (var btn in btns)
                    {
                        SetButtonColors(appearance, btn);
                        SetButtonFont(appearance, btn);
                    }
                }
            }
        }

        private void SetButtonsPanelColors(CustomAppearance appearance)
        {
            if (appearance.ButtonsAppearance.ButtonsPanelBackColor != null)
                pnlButtons.BackColor = appearance.ButtonsAppearance.ButtonsPanelBackColor.Value;
        }

        private static void SetButtonColors(CustomAppearance appearance, Button btn)
        {
            if (appearance.ButtonsAppearance.ForeColor != null)
                btn.ForeColor = appearance.ButtonsAppearance.ForeColor.Value;

            if (appearance.ButtonsAppearance.BackColor != null)
                btn.BackColor = appearance.ButtonsAppearance.BackColor.Value;
        }

        private static void SetButtonFont(CustomAppearance appearance, Button btn)
        {
            Font newFont = GetFont(
                btn.Font,
                appearance.ButtonsAppearance.Font,
                appearance.ButtonsAppearance.FontFamilyName,
                appearance.ButtonsAppearance.FontSize,
                appearance.ButtonsAppearance.FontStyle
            );

            if (btn.Font != newFont)
                btn.Font = newFont;
        }

        private void SetMessage(string text, string caption)
        {
            int lblW = lblMessage.Size.Width;
            int lblH = lblMessage.Size.Height;

            int captionW = 0;
            if (string.IsNullOrEmpty(caption) == false)
            {
                lblMessage.Text = caption;
                captionW = lblMessage.Size.Width;
            }

            lblMessage.Text = text;

            int lblNewW = lblMessage.Size.Width;
            int lblNewH = lblMessage.Size.Height;

            if (captionW != 0 && lblNewW < captionW)
                lblNewW = captionW;

            if (lblNewW > lblW)
            {
                this.Width += lblNewW - lblW;
            }

            if (lblNewH > lblH)
            {
                using var g = this.CreateGraphics();
                int lineSpacing = lblMessage.Font.FontFamily.GetLineSpacing(lblMessage.Font.Style);
                float lineSpacingPixel = lblMessage.Font.Size * lineSpacing / lblMessage.Font.FontFamily.GetEmHeight(lblMessage.Font.Style);
                this.Height += (int)(lblNewH - lblH + lineSpacingPixel);
            }
        }

        private void SetRightToLeft(CustomAppearance appearance, MessageBoxIcon icon, List<Button> btns)
        {
            if (appearance != null)
            {
                if (appearance.RightToLeft == true)
                {
                    lblMessage.RightToLeft = RightToLeft.Yes;

                    if (icon != MessageBoxIcon.None)
                    {
                        int iconX = pbxIcon.Location.X;
                        int lblX1 = lblMessage.Location.X;
                        int lblX2 = lblX1 + lblMessage.Width;
                        int iconNewX = lblX2 - pbxIcon.Width;
                        int lblNewX = iconX;

                        pbxIcon.Location = new Point(iconNewX, pbxIcon.Location.Y);
                        lblMessage.Location = new Point(lblNewX, lblMessage.Location.Y);
                    }

                    pnlButtons.RightToLeft = RightToLeft.No;

                    if (btns != null && btns.Count > 0)
                    {
                        foreach (var btn in btns)
                            btn.RightToLeft = RightToLeft.Yes;
                    }
                }
            }
        }

        private static Font GetFont(Font controlFont, Font font, string fontFamilyName, float? fontSize, FontStyle? fontStyle)
        {
            Font newFont = controlFont;

            if (font != null)
                newFont = (Font)font.Clone();

            if ((string.IsNullOrEmpty(fontFamilyName) &&
                fontSize == null &&
                fontStyle == null) == false)
            {
                newFont = new Font
                (
                    string.IsNullOrEmpty(fontFamilyName) ? newFont.FontFamily.Name : fontFamilyName,
                    fontSize == null ? newFont.Size : fontSize.Value,
                    fontStyle == null ? newFont.Style : fontStyle.Value
                );
            }

            return newFont;
        }

        private void AddButton(
            List<Button> btns,
            CustomMessageBoxButtons buttons,
            CustomMessageBoxButtons button,
            DialogResult dialogResult,
            string text,
            CustomButtonText[] buttonTexts,
            CustomMessageBoxEventHandler click = null)
        {
            if ((buttons & button) == button)
            {
                var newText = buttonTexts?.FirstOrDefault(x => x.Button == button)?.Text;
                if (string.IsNullOrEmpty(newText) == false)
                    text = newText;

                btns.Add(GetButton(text, dialogResult, click));
            }
        }

        private Button GetButton(
            string text,
            DialogResult dialogResult = DialogResult.None,
            CustomMessageBoxEventHandler click = null)
        {
            Button btn = new()
            {
                Text = text,
                Size = new Size(90, 40),
                DialogResult = dialogResult
            };

            if (click != null)
            {
                btn.Click += (object sender, EventArgs e) =>
                {
                    foreach (CustomMessageBoxEventHandler listener in click.GetInvocationList().Cast<CustomMessageBoxEventHandler>())
                        listener.Invoke(sender, new CustomMessageBoxEventArgs(lblMessage.Text, this.Text));
                };
            }

            return btn;
        }

        public static CustomAppearance GetDefaultCustomAppearance()
        {
            var messageBox = new CustomMessageBox();

            var appearance = new CustomAppearance()
            {
                ForeColor = messageBox.ForeColor,
                BackColor = messageBox.BackColor,
                Font = (Font)messageBox.Font.Clone(),
                FontFamilyName = messageBox.Font.FontFamily.Name,
                FontSize = messageBox.Font.Size,
                FontStyle = messageBox.Font.Style,
                TextAlign = messageBox.lblMessage.TextAlign,
                RightToLeft = messageBox.lblMessage.RightToLeft == RightToLeft.Yes
            };

            appearance.ButtonsAppearance = new CustomButtonAppearance()
            {
                ButtonsPanelBackColor = appearance.BackColor,
                ForeColor = appearance.ForeColor,
                BackColor = appearance.BackColor,
                Font = (Font)appearance.Font.Clone(),
                FontFamilyName = appearance.Font.FontFamily.Name,
                FontSize = appearance.Font.Size,
                FontStyle = appearance.Font.Style
            };

            return appearance;
        }
    }

    #region CustomMessageBoxButtons

    [Flags]
    public enum CustomMessageBoxButtons
    {
        None = 0,
        OK = 1,
        Abort = 1 << 1,
        Retry = 1 << 2,
        Ignore = 1 << 3,
        Yes = 1 << 4,
        No = 1 << 5,
        Cancel = 1 << 6,
        Copy = 1 << 7,
        CopyText = 1 << 8,
        OKCancel = OK | Cancel,
        AbortRetryIgnore = Abort | Retry | Ignore,
        YesNoCancel = Yes | No | Cancel,
        YesNo = Yes | No,
        RetryCancel = Retry | Cancel,
    }

    #endregion

    #region CustomMessageBoxEventHandler

    public delegate void CustomMessageBoxEventHandler(object sender, CustomMessageBoxEventArgs e);

    public class CustomMessageBoxEventArgs(string text, string caption) : EventArgs()
    {
        public string Text { get; private set; } = text;
        public string Caption { get; private set; } = caption;

        public new static readonly CustomMessageBoxEventArgs Empty = new(null, null);
    }

    #endregion

    #region CustomAppearance

    public class CustomAppearance(
        Color? foreColor = null,
        Color? backColor = null,
        Font font = null,
        string fontFamilyName = null,
        float? fontSize = null,
        FontStyle? fontStyle = null,
        ContentAlignment? textAlign = null,
        bool? rightToLeft = null,
        CustomButtonAppearance buttonsAppearance = null) : ICloneable
    {
        public Color? ForeColor { get; set; } = foreColor;
        public Color? BackColor { get; set; } = backColor;
        public Font Font { get; set; } = font;
        public string FontFamilyName { get; set; } = fontFamilyName;
        public float? FontSize { get; set; } = fontSize;
        public FontStyle? FontStyle { get; set; } = fontStyle;
        public ContentAlignment? TextAlign { get; set; } = textAlign;
        public bool? RightToLeft { get; set; } = rightToLeft;
        public CustomButtonAppearance ButtonsAppearance { get; set; } = buttonsAppearance;

        public CustomAppearance(CustomAppearance prototype) : this(
            prototype.ForeColor,
            prototype.BackColor,
            (prototype.Font != null ? (Font)prototype.Font.Clone() : null),
            prototype.FontFamilyName,
            prototype.FontSize,
            prototype.FontStyle,
            prototype.TextAlign,
            prototype.RightToLeft,
            (prototype.ButtonsAppearance != null ? (CustomButtonAppearance)prototype.ButtonsAppearance.Clone() : null))
        { }

        public object Clone()
        {
            return new CustomAppearance(this);
        }
    }

    #endregion

    #region CustomButtonAppearance

    public class CustomButtonAppearance(
        Color? buttonsPanelBackColor = null,
        Color? foreColor = null,
        Color? backColor = null,
        Font font = null,
        string fontFamilyName = null,
        float? fontSize = null,
        FontStyle? fontStyle = null) : ICloneable
    {
        public Color? ButtonsPanelBackColor { get; set; } = buttonsPanelBackColor;
        public Color? ForeColor { get; set; } = foreColor;
        public Color? BackColor { get; set; } = backColor;
        public Font Font { get; set; } = font;
        public string FontFamilyName { get; set; } = fontFamilyName;
        public float? FontSize { get; set; } = fontSize;
        public FontStyle? FontStyle { get; set; } = fontStyle;

        public CustomButtonAppearance(CustomButtonAppearance prototype) : this(
            prototype.ButtonsPanelBackColor,
            prototype.ForeColor,
            prototype.BackColor,
            (prototype.Font != null ? (Font)prototype.Font.Clone() : null),
            prototype.FontFamilyName,
            prototype.FontSize,
            prototype.FontStyle)
        { }

        public object Clone()
        {
            return new CustomButtonAppearance(this);
        }
    }

    #endregion

    #region CustomButton

    public class CustomButton(
        string text,
        DialogResult? dialogResult = null,
        CustomMessageBoxEventHandler click = null) : ICloneable
    {
        public string Text { get; set; } = text;
        public DialogResult? DialogResult { get; set; } = dialogResult;
        public CustomMessageBoxEventHandler Click { get; set; } = click;

        public CustomButton(CustomButton prototype) : this(
            prototype.Text,
            prototype.DialogResult,
            (prototype.Click != null ? (CustomMessageBoxEventHandler)prototype.Click.Clone() : null))
        { }

        public object Clone()
        {
            return new CustomButton(this);
        }
    }

    #endregion

    #region CustomButtonText

    public class CustomButtonText
    {
        public CustomMessageBoxButtons Button { get; private set; }
        public string Text { get; private set; }

        public CustomButtonText(CustomMessageBoxButtons button, string text)
        {
            if ((button == CustomMessageBoxButtons.OK ||
                button == CustomMessageBoxButtons.Abort ||
                button == CustomMessageBoxButtons.Retry ||
                button == CustomMessageBoxButtons.Ignore ||
                button == CustomMessageBoxButtons.Yes ||
                button == CustomMessageBoxButtons.No ||
                button == CustomMessageBoxButtons.Cancel ||
                button == CustomMessageBoxButtons.Copy ||
                button == CustomMessageBoxButtons.CopyText) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(button), "Valid values are OK, Abort, Retry, Ignore, Yes, No, Cancel, Copy, CopyText");
            }

            Button = button;
            Text = text;
        }
    }

    #endregion
}