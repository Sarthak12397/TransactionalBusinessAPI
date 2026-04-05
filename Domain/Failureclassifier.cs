public static class FailureClassifier
{
     public static bool IsTransient(string reason)
    {
        return reason.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
               reason.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
               reason.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
               reason.Contains("gateway", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPermanent(string reason)
    {
        return reason.Contains("insufficient", StringComparison.OrdinalIgnoreCase) ||
               reason.Contains("expired", StringComparison.OrdinalIgnoreCase) ||
               reason.Contains("blocked", StringComparison.OrdinalIgnoreCase) ||
               reason.Contains("invalid", StringComparison.OrdinalIgnoreCase);
    }
}