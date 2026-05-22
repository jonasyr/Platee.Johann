namespace Platee.Johann.UI.ViewModels;

public static class ToastToneHelper
{
    public static ToastTone DeriveFromCompletion(string message)
    {
        if (message.StartsWith("Fehler:", StringComparison.OrdinalIgnoreCase))
            return ToastTone.Error;
        if (message.StartsWith("Kein API-Schlüssel", StringComparison.OrdinalIgnoreCase))
            return ToastTone.Warn;
        return ToastTone.Ok;
    }

    public static ToastTone DeriveFromAdd(string message)
    {
        if (message.StartsWith("Kein API-Schlüssel", StringComparison.OrdinalIgnoreCase))
            return ToastTone.Warn;
        return ToastTone.Ok;
    }
}
