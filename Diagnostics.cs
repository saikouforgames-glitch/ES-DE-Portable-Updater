namespace ESDEUpdater;

public static class Diagnostics
{
    private static volatile Action<string>? _log;

    public static Action<string>? Log
    {
        get => _log;
        set => _log = value;
    }

    public static void Report(string message)
    {
        try
        {
            _log?.Invoke(message);
        }
        catch
        {
        }
    }
}
