namespace System;

public static partial class EventHandlerExtensions
{
    public static void Raise(this EventHandler handler, object sender, Func<EventArgs> argsHandler)
    {
        if (handler == null)
            return;

        EventArgs args = argsHandler();
        foreach (EventHandler listener in handler.GetInvocationList().Cast<EventHandler>())
            listener.Invoke(sender, args);
    }

    public static void RaiseAsync(this EventHandler handler, object sender, Func<EventArgs> argsHandler)
    {
        if (handler == null)
            return;

        EventArgs args = argsHandler();
        foreach (EventHandler listener in handler.GetInvocationList().Cast<EventHandler>())
            Task.Run(() => listener(sender, args));
    }

    public static void Raise<TEventArgs>(this EventHandler<TEventArgs> handler, object sender, Func<TEventArgs> argsHandler) where TEventArgs : EventArgs
    {
        if (handler == null)
            return;

        TEventArgs args = argsHandler();
        foreach (EventHandler<TEventArgs> listener in handler.GetInvocationList().Cast<EventHandler<TEventArgs>>())
            listener.Invoke(sender, args);
    }

    public static void RaiseAsync<TEventArgs>(this EventHandler<TEventArgs> handler, object sender, Func<TEventArgs> argsHandler) where TEventArgs : EventArgs
    {
        if (handler == null)
            return;

        TEventArgs args = argsHandler();
        foreach (EventHandler<TEventArgs> listener in handler.GetInvocationList().Cast<EventHandler<TEventArgs>>())
            Task.Run(() => listener(sender, args));
    }
}
