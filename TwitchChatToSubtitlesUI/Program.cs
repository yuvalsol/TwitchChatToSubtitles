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
                MessageBoxHelper.ShowError(
                    GetUnhandledExceptionMessage(ex),
                    $"Unhandled Error - {Version()}"
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
            var errorMessage = new StringBuilder()
                .AppendLine($"Unhandled Error - {Version()}");

            try
            {
                errorMessage.Append(ex.GetUnhandledExceptionErrorWithApplicationTerminationMessage()).AppendLine();
            }
            catch
            {
                while (ex != null)
                {
                    errorMessage.AppendLine().Append("ERROR: ").AppendLine(ex.Message);

#if DEBUG
                    errorMessage.Append("ERROR TYPE: ").AppendLine(ex.GetType().ToString());
#endif

                    ex = ex.InnerException;
                }
            }

            return errorMessage.ToString();
        }
    }
}