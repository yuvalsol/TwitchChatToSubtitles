#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static partial class ExceptionExtensions
{
    public static StringBuilder GetUnhandledExceptionErrorWithApplicationTerminationMessage(this Exception ex, bool withoutDebuggingInfo = false, bool callable = false)
    {
        if (ex == null)
            return null;

        return GetExceptionErrorMessage(new StringBuilder()
            .AppendLine("An unhandled error occurred.")
            .AppendLine("The application will terminate now.")
            .AppendLine(), ex, withoutDebuggingInfo, callable);
    }

    public static StringBuilder GetUnhandledExceptionErrorMessage(this Exception ex, bool withoutDebuggingInfo = false, bool callable = false)
    {
        if (ex == null)
            return null;

        return GetExceptionErrorMessage(new StringBuilder()
            .AppendLine("An unhandled error occurred.")
            .AppendLine(), ex, withoutDebuggingInfo, callable);
    }

    public static StringBuilder GetExceptionErrorMessage(this Exception ex, string mainMessage = null, bool withoutDebuggingInfo = false, bool callable = false)
    {
        if (ex == null)
            return null;

        return GetExceptionErrorMessage(
            string.IsNullOrEmpty(mainMessage) ? new StringBuilder() : new StringBuilder().AppendLine(mainMessage),
            ex, withoutDebuggingInfo, callable
        );
    }

    public static StringBuilder GetExceptionErrorMessage(this Exception ex, StringBuilder errorMessage, bool withoutDebuggingInfo = false, bool callable = false)
    {
        if (ex == null)
            return errorMessage;

        return GetExceptionErrorMessage(
            errorMessage ?? new StringBuilder(),
            ex, withoutDebuggingInfo, callable
        );
    }

    private static StringBuilder GetExceptionErrorMessage(StringBuilder errorMessage, Exception ex, bool withoutDebuggingInfo, bool callable)
    {
        AppendToExceptionErrorMessage(errorMessage, ex, withoutDebuggingInfo, callable);

        while (ex.InnerException != null)
        {
            ex = ex.InnerException;
            AppendToExceptionErrorMessage(errorMessage, ex, withoutDebuggingInfo, callable);
        }

        TrimEndNewLine(errorMessage);

        return errorMessage;
    }

    private static void AppendToExceptionErrorMessage(StringBuilder errorMessage, Exception ex, bool withoutDebuggingInfo, bool callable)
    {
        errorMessage.Append("ERROR: ").AppendLine(ex.Message);

        if (withoutDebuggingInfo)
            return;

        errorMessage.Append("ERROR TYPE: ").AppendLine(ex.GetType().ToString());

        var stackTrace = ex.GetFormattedStackTrace(callable);
        if (stackTrace?.Length > 0)
            errorMessage.AppendLine("STACK TRACE:").Append(stackTrace).AppendLine();
    }

    public static StringBuilder GetFormattedStackTrace(this Exception ex, bool callable = false)
    {
        if (ex == null)
            return null;

        var stackTrace = new StringBuilder();

        var st = new StackTrace(ex, true);

        var frames =
            Enumerable.Range(0, st.FrameCount)
            .Select(i => new
            {
                frameNumber = i + 1,
                frame = st.GetFrame(i)
            })
            .Select(f => new
            {
                f.frameNumber,
                f.frame,
                method = f.frame.GetMethod()
            })
            .Where(f => f.method != null)
            .Select(f => new
            {
                f.frameNumber,
                f.frame,
                f.method,
                isSystemNamespace = f.method.ReflectedType?.Namespace?.StartsWith("System") ?? false
            });

        bool isFoundNamespaceNotSystem = false;

        foreach (var f in frames)
        {
            if (isFoundNamespaceNotSystem && f.isSystemNamespace)
                break;

            isFoundNamespaceNotSystem = f.isSystemNamespace == false;

            StringBuilder signature = null;
            if (f.method is MethodInfo mi)
                signature = mi.GetSignature(callable);
            else if (f.method is ConstructorInfo ci)
                signature = ci.GetSignature(callable);

            if (signature == null)
                continue;

            int lineNumber = f.frame.GetFileLineNumber();
            if (lineNumber > 0)
                stackTrace.Append(f.frameNumber).Append(". ").Append(signature).Append(" at line ").Append(lineNumber).AppendLine();
            else
                stackTrace.Append(f.frameNumber).Append(". ").Append(signature).AppendLine();
        }

        TrimEndNewLine(stackTrace);

        return stackTrace;
    }

    private static void TrimEndNewLine(StringBuilder sb)
    {
        int countCharsToRemove = 0;

        if (sb.Length >= 2 && sb[^2] == '\r')
            countCharsToRemove++;

        if (sb.Length >= 1 && sb[^1] == '\n')
            countCharsToRemove++;

        if (countCharsToRemove == 0)
            return;

        sb.Length -= countCharsToRemove;
    }
}
