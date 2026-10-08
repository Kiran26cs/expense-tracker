using System.Text.RegularExpressions;

namespace ExpensesBackend.API.Services.BankSync;

/// <summary>
/// Cleans up raw bank statement narrations. Indian UPI/NEFT/RTGS/IMPS narrations mash a
/// payee name together with a VPA/account ref, bank name, and transaction reference —
/// e.g. "UPI/AZHAGAPPAN/aka639439-1@ok/UPI/INDUSINDB/651829130787/...". This extracts:
///  - a short, human-readable description (just the payee name)
///  - a stable "payee key" so the same payee can be recognized across future imports and
///    have a previously-chosen category reapplied automatically
/// </summary>
public static class BankNarrationParser
{
    // UPI/<name>/<vpa>/UPI/<bank>/<ref>/...
    private static readonly Regex UpiPattern = new(
        @"^UPI/(?<name>[^/]+)/(?<vpa>[^/]+)/UPI/",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // NEFT-<ref>-<name>...  or  NEFT/<ref>/<name>...  (also RTGS, IMPS)
    private static readonly Regex NeftPattern = new(
        @"^(?:NEFT|RTGS|IMPS)[-/](?<ref>[^-/]+)[-/](?<name>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static (string CleanDescription, string? PayeeKey) Parse(string? rawDescription)
    {
        var raw = (rawDescription ?? string.Empty).Trim();
        if (raw.Length == 0) return (raw, null);

        var upi = UpiPattern.Match(raw);
        if (upi.Success)
        {
            var name = upi.Groups["name"].Value.Trim();
            var vpa  = upi.Groups["vpa"].Value.Trim().ToLowerInvariant();
            return (name.Length > 0 ? name : raw, vpa.Length > 0 ? $"upi:{vpa}" : null);
        }

        var neft = NeftPattern.Match(raw);
        if (neft.Success)
        {
            var name = neft.Groups["name"].Value.Trim().TrimEnd('.', '-', '/', ' ');
            return (name.Length > 0 ? name : raw,
                    name.Length > 0 ? $"name:{name.ToUpperInvariant()}" : null);
        }

        // Unrecognized format — leave the narration as-is; no reliable key to memorize it by.
        return (raw, null);
    }
}
