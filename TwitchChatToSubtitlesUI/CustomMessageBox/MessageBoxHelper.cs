namespace TwitchChatToSubtitlesUI.CustomMessageBox
{
    using System.Windows.Forms;

    internal static class MessageBoxHelper
    {
        public static DialogResult Show(string text, string caption, MessageBoxIcon icon, Color? foreColor = null)
        {
            return Show(null, text, caption, icon, foreColor);
        }

        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxIcon icon, Color? foreColor = null)
        {
            return CustomMessageBox.Show(
                owner, text, caption, CustomMessageBoxButtons.OK | CustomMessageBoxButtons.Copy, icon,
                GetAppearance(foreColor: foreColor)
            );
        }

        public static DialogResult ShowInformation(IWin32Window owner, string text, Color? foreColor = null)
        {
            return CustomMessageBox.Show(
                owner, text, null, CustomMessageBoxButtons.OK | CustomMessageBoxButtons.Copy, MessageBoxIcon.Information,
                GetAppearance(foreColor: foreColor, textAlign: ContentAlignment.MiddleLeft)
            );
        }

        private static CustomAppearance GetAppearance(Color? foreColor = null, ContentAlignment? textAlign = null)
        {
            return new CustomAppearance(
                foreColor: foreColor,
                textAlign: textAlign,
                buttonsAppearance: new CustomButtonAppearance(
                     buttonsPanelBackColor: Color.FromArgb(238, 244, 249),
                     backColor: Color.White,
                     foreColor: Color.Black
                )
            );
        }
    }
}
