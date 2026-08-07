using System.Globalization;
using System.Text.RegularExpressions;

namespace TaxInvoiceExtractor.Utils;

public static partial class DataNormalizer
{
    [GeneratedRegex(@"[^0-9-]")]
    private static partial Regex NonNumericRegex();

    [GeneratedRegex(@"(?<year>20\d{2}|\d{2})(?:\s*년\s*|\s*[.\-/]\s*|\s+)(?<month>\d{1,2})(?:\s*월\s*|\s*[.\-/]\s*|\s+)(?<day>\d{1,2})\s*일?")]
    private static partial Regex DateRegex();

    public static long? ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = NonNumericRegex().Replace(value, string.Empty);
        return long.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : null;
    }

    public static string? ParseIssueMonthDay(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var match = DateRegex().Match(value);
        if (!match.Success || !int.TryParse(match.Groups["year"].Value, out var year) ||
            !int.TryParse(match.Groups["month"].Value, out var month) ||
            !int.TryParse(match.Groups["day"].Value, out var day) || month is < 1 or > 12)
            return null;

        var normalizedYear = year < 100 ? 2000 + year : year;
        if (day < 1 || day > DateTime.DaysInMonth(normalizedYear, month)) return null;
        return $"{month:00}월 {day:00}일";
    }
    public static string CleanText(string? value) =>
        Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim(' ', ':', '：');
}

