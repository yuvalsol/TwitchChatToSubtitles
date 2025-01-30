using TwitchChatToSubtitlesUI.CustomMessageBox;

namespace TwitchChatToSubtitlesUI
{
    internal static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                // UI exceptions
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);

                // Non-UI exceptions
                AppDomain.CurrentDomain.UnhandledException += (sender, e) => UnhandledException((Exception)e.ExceptionObject);

                ApplicationConfiguration.Initialize();
                Application.Run(new TwitchChatToSubtitlesForm(args));
                return 0;
            }
            catch (Exception ex)
            {
                UnhandledException(ex);
                return -1;
            }
        }

        private static void UnhandledException(Exception ex)
        {
            try
            {
                MessageBoxHelper.Show(
                    GetUnhandledExceptionMessage(ex),
                    $"Unhandled Error - {Version()}",
                    MessageBoxIcon.Error
                );
            }
            catch
            {
            }

            Application.Exit();
        }

        internal static string Version()
        {
            return $"Twitch Chat To Subtitles {Assembly.GetExecutingAssembly().GetName().Version.ToString(2)}";
        }

        private static string GetUnhandledExceptionMessage(Exception ex)
        {
            var errorMessage = new StringBuilder();
            errorMessage.AppendLine($"Unhandled Error - {Version()}");

            try
            {
                errorMessage.AppendLine(ex.GetUnhandledExceptionErrorWithApplicationTerminationMessage());
            }
            catch
            {
                while (ex != null)
                {
                    errorMessage.AppendLine();
                    errorMessage.AppendLine($"ERROR TYPE: {ex.GetType()}");
                    errorMessage.AppendLine($"ERROR: {ex.Message}");

                    ex = ex.InnerException;
                }
            }

            return errorMessage.ToString();
        }
    }
}