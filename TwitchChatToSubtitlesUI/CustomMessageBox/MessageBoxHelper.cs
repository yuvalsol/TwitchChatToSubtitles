namespace TwitchChatToSubtitlesUI.CustomMessageBox
{
    using System.Windows.Forms;

    internal static class MessageBoxHelper
    {
        public static DialogResult ShowError(string text, string caption)
        {
            return ShowError(null, text, caption);
        }

        public static DialogResult ShowError(IWin32Window owner, string text, string caption)
        {
            return Show(owner, text, caption, CustomMessageBoxButtons.OK | CustomMessageBoxButtons.Copy, MessageBoxIcon.Error, Color.DarkRed);
        }

        public static DialogResult ShowInformation(IWin32Window owner, string text, string caption)
        {
            return Show(owner, text, caption, CustomMessageBoxButtons.OK, MessageBoxIcon.Information, textAlign: ContentAlignment.MiddleLeft,
                customButtons: [
                    new CustomButton("Copy", DialogResult.None, (sender, e) =>
                    {
                        try
                        {
                            Clipboard.SetText((e.Text).Trim());
                            SystemSounds.Hand.Play();
                        }
                        catch { }
                    })
                ]
            );
        }

        private static DialogResult Show(IWin32Window owner, string text, string caption, CustomMessageBoxButtons buttons, MessageBoxIcon icon, Color? foreColor = null, ContentAlignment? textAlign = null, CustomButton[] customButtons = null)
        {
            if (textAlign == null)
            {
                int linesCount = text.ToCharArray().Count(c => c == '\n') + 1;
                if (linesCount <= 4)
                    textAlign = ContentAlignment.MiddleLeft;
            }

            return CustomMessageBox.Show(
                owner, text, caption, buttons, icon,
                new CustomAppearance(
                    foreColor: foreColor,
                    textAlign: textAlign,
                    buttonsAppearance: new CustomButtonAppearance(
                        buttonsPanelBackColor: Color.FromArgb(238, 244, 249),
                        backColor: Color.White,
                        foreColor: Color.Black
                    )
                ),
                customButtons
            );
        }
    }
}
