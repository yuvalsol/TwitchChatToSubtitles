namespace System;

public static partial class ExceptionExtensions
{
    public static string GetUnhandledExceptionErrorWithApplicationTerminationMessage(this Exception ex)
    {
        return GetExceptionErrorMessage(ex,
            string.Format("An unhandled error occurred.{0}The application will terminate now.{0}", Environment.NewLine)
        );
    }

    public static string GetUnhandledExceptionErrorMessage(this Exception ex)
    {
        return GetExceptionErrorMessage(ex,
            string.Format("An unhandled error occurred.{0}", Environment.NewLine)
        );
    }

    public static string GetExceptionErrorMessage(this Exception ex, string mainMessage = null)
    {
        if (ex == null)
            return null;

        string errorMessage = mainMessage;

        AppendToExceptionErrorMessage(ref errorMessage, ex);

        while (ex.InnerException != null)
        {
            ex = ex.InnerException;
            AppendToExceptionErrorMessage(ref errorMessage, ex);
        }

        errorMessage = errorMessage.Trim();

        return errorMessage;
    }

    private static void AppendToExceptionErrorMessage(ref string errorMessage, Exception ex)
    {
        errorMessage += string.Format("{0}ERROR: {1}{0}ERROR TYPE: {2}",
            Environment.NewLine,
            ex.Message,
            ex.GetType()
        );

        string formattedStackTrace = ex.GetFormattedStackTrace();
        if (string.IsNullOrEmpty(formattedStackTrace) == false)
        {
            errorMessage += string.Format("{0}STACK TRACE:{0}{1}",
                Environment.NewLine,
                formattedStackTrace
            );
        }
    }

    public static string GetFormattedStackTrace(this Exception ex)
    {
        var sb = new StringBuilder();
        bool isFoundNamespaceNotSystem = false;

        var st = new StackTrace(ex, true);
        for (int i = 0; i < st.FrameCount; i++)
        {
            StackFrame sf = st.GetFrame(i);
            if (sf != null)
            {
                MethodBase method = sf.GetMethod();
                if (method != null)
                {
                    Type reflectedType = method.ReflectedType;
                    if (reflectedType != null)
                    {
                        if ((isFoundNamespaceNotSystem == false) ||
                            (string.IsNullOrEmpty(reflectedType.Namespace) == false && reflectedType.Namespace.StartsWith("System") == false))
                        {
                            isFoundNamespaceNotSystem = reflectedType.Namespace.StartsWith("System") == false;

                            MethodInfo mi = method as MethodInfo;
                            if (mi != null)
                            {
                                sb.Append((i + 1) + ". ");
                                sb.Append(mi.GetSignature());

                                int lineNumber = sf.GetFileLineNumber();
                                if (lineNumber > 0)
                                {
                                    sb.Append(" at line ");
                                    sb.Append(lineNumber);
                                }

                                sb.Append(Environment.NewLine);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        if (sb.Length == 0)
            return null;

        return sb.ToString().Trim();
    }
}
