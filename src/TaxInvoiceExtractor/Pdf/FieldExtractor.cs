using System.Text.RegularExpressions;
using TaxInvoiceExtractor.Utils;

namespace TaxInvoiceExtractor.Pdf;

public sealed partial class FieldExtractor
{
    private const double SameLineTolerance = 0.014;
    private static readonly string[] ItemColumnHeaders = ["월", "일", "품목", "규격", "수량", "단가", "공급가액", "세액", "비고"];
    private static readonly string[] ItemHeaderFragments = ["월", "일", "품", "목", "규", "격", "수", "량", "단", "가", "세", "액", "비고"];
    private static readonly string[] SummaryHeaders = ["작성일자", "작성일", "발행일자", "공급가액", "공급가액합계", "부가세", "세액", "부가가치세", "수정사유", "당초승인번호"];

    public string ExtractCompanyName(IReadOnlyList<PdfWord> words, bool supplier)
    {
        var region = words.Where(w => supplier ? w.CenterX < 0.52 : w.CenterX >= 0.48).ToList();
        foreach (var label in FindCompanyLabels(region).OrderByDescending(w => w.CenterY))
        {
            var candidateLines = GroupWordLines(region.Where(w =>
                    w.PageNumber == label.PageNumber &&
                    Math.Abs(w.CenterY - label.CenterY) <= 0.022 &&
                    w.Left >= label.Right - 0.005 &&
                    w.Left - label.Right <= 0.34).ToList())
                .Select(line => TakeContiguousCompanyWords(line.OrderBy(w => w.Left).ToList(), label))
                .Where(line => line.Count > 0)
                .OrderByDescending(line => line[0].CenterY)
                .ToList();

            if (candidateLines.Count == 0) continue;
            var anchorX = candidateLines[0][0].Left;
            var aligned = candidateLines
                .Where(line => Math.Abs(line[0].Left - anchorX) <= 0.035)
                .Select(line => DataNormalizer.CleanText(string.Join(" ", line.Select(w => w.Text))))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct()
                .ToList();
            if (aligned.Count > 0) return JoinCompanyLines(aligned);
        }
        return string.Empty;
    }

    public string ExtractDescription(IReadOnlyList<PdfWord> words)
    {
        foreach (var label in FindCompositeLabels(words, "품목", "품", "목").OrderByDescending(w => w.CenterY))
        {
            var headerLine = words.Where(w => w.PageNumber == label.PageNumber &&
                                              Math.Abs(w.CenterY - label.CenterY) <= SameLineTolerance)
                .OrderBy(w => w.Left).ToList();
            var leftBoundary = headerLine
                .Where(w => w.Right <= label.Left && IsItemHeader(w.Text))
                .Select(w => w.Right).DefaultIfEmpty(Math.Max(0, label.Left - 0.18)).Max();
            var rightBoundary = headerLine
                .Where(w => w.Left > label.Right && IsItemHeader(w.Text))
                .Select(w => w.Left).DefaultIfEmpty(Math.Min(1, label.Right + 0.30)).Min();

            var footerY = words.Where(w => w.PageNumber == label.PageNumber && w.CenterY < label.CenterY &&
                                            (Compact(w.Text) == "합계" || Compact(w.Text) == "합계금액"))
                .Select(w => w.CenterY).DefaultIfEmpty(label.CenterY - 0.16).Max();
            var itemWords = words.Where(w => w.PageNumber == label.PageNumber &&
                                             w.CenterY < label.CenterY - 0.004 && w.CenterY > footerY + 0.004 &&
                                             w.CenterX > leftBoundary && w.CenterX < rightBoundary).ToList();

            var descriptions = GroupWordLines(itemWords)
                .Select(line => DataNormalizer.CleanText(string.Join(" ", line.OrderBy(w => w.Left).Select(w => w.Text))))
                .Where(value => !string.IsNullOrWhiteSpace(value) && !LooksLikeLabel(value))
                .Distinct().Take(10).ToList();
            if (descriptions.Count > 0) return string.Join(" / ", descriptions);
        }

        foreach (var label in words.Where(w => IsAny(w.Text, "적요")).OrderByDescending(w => w.CenterY))
        {
            var value = WordsRightOfLabel(words, label, 0.7);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }
        return string.Empty;
    }

    public long? ExtractAmount(IReadOnlyList<PdfWord> words, bool vat)
    {
        var labelTerms = vat ? new[] { "부가세", "세액", "부가가치세" } : new[] { "공급가액", "공급가액합계" };
        var labels = words.Where(w => labelTerms.Any(t => Compact(w.Text).Contains(t)))
            .OrderBy(w => w.PageNumber).ThenByDescending(w => w.CenterY);

        foreach (var label in labels)
        {
            var headerLine = words.Where(w => w.PageNumber == label.PageNumber &&
                                               Math.Abs(w.CenterY - label.CenterY) <= SameLineTolerance &&
                                               IsSummaryHeader(w.Text))
                .OrderBy(w => w.Left).ToList();
            var previous = headerLine.LastOrDefault(w => w.Right <= label.Left);
            var next = headerLine.FirstOrDefault(w => w.Left > label.Right);
            var leftBoundary = previous is null ? Math.Max(0, label.Left - 0.05) : (previous.Right + label.Left) / 2;
            var rightBoundary = next?.Left ?? Math.Min(1, label.Right + 0.38);

            var valueLines = GroupWordLines(words.Where(w => w.PageNumber == label.PageNumber &&
                    w.CenterY < label.CenterY - 0.003 && label.CenterY - w.CenterY < 0.065 &&
                    w.CenterX > leftBoundary && w.CenterX < rightBoundary).ToList())
                .OrderByDescending(line => line.Average(w => w.CenterY));
            foreach (var line in valueLines)
            {
                var joined = string.Concat(line.OrderBy(w => w.Left).Select(w => w.Text));
                if (DigitRegex().IsMatch(joined) && DataNormalizer.ParseAmount(joined) is { } amount) return amount;
            }

            var rightWords = words.Where(w => w.PageNumber == label.PageNumber &&
                                               Math.Abs(w.CenterY - label.CenterY) <= SameLineTolerance &&
                                               w.Left >= label.Right - 0.005 && w.Left - label.Right < 0.35)
                .OrderBy(w => w.Left).ToList();
            var rightText = string.Concat(rightWords.Select(w => w.Text));
            if (DigitRegex().IsMatch(rightText) && DataNormalizer.ParseAmount(rightText) is { } rightAmount) return rightAmount;
        }
        return null;
    }

    public string ExtractIssueMonthDay(IReadOnlyList<PdfWord> words)
    {
        var labels = words.Where(w => IsAny(w.Text, "작성일자", "작성일", "발행일자"));
        foreach (var label in labels)
        {
            var line = string.Join(" ", words.Where(w => w.PageNumber == label.PageNumber && Math.Abs(w.CenterY - label.CenterY) < 0.025)
                .OrderBy(w => w.Left).Select(w => w.Text));
            var parsed = DataNormalizer.ParseIssueMonthDay(line);
            if (parsed is not null) return parsed;
        }

        foreach (var line in GroupLines(words))
        {
            var parsed = DataNormalizer.ParseIssueMonthDay(line);
            if (parsed is not null) return parsed;
        }
        return string.Empty;
    }

    private static string JoinCompanyLines(IReadOnlyList<string> lines)
    {
        var result = lines[0];
        foreach (var line in lines.Skip(1))
        {
            var separator = line.StartsWith("㈜") || line.StartsWith("(주)") || line.StartsWith("주식회사") ? " " : string.Empty;
            result += separator + line;
        }
        return result;
    }
    private static IEnumerable<PdfWord> FindCompanyLabels(IReadOnlyList<PdfWord> words)
    {
        foreach (var word in words.Where(w => IsAny(w.Text, "상호", "(법인명)", "상호법인명") || Compact(w.Text).StartsWith("(법인")))
            yield return word;

        foreach (var first in words.Where(w => Compact(w.Text) == "상"))
        {
            var second = words.FirstOrDefault(w => w.PageNumber == first.PageNumber && Compact(w.Text) == "호" &&
                                                   Math.Abs(w.CenterY - first.CenterY) <= SameLineTolerance &&
                                                   w.Left >= first.Right - 0.005 && w.Left - first.Right <= 0.07);
            if (second is not null)
                yield return new PdfWord("상호", first.Left, Math.Min(first.Bottom, second.Bottom), second.Right, Math.Max(first.Top, second.Top), first.PageNumber);
        }
    }

    private static List<PdfWord> TakeContiguousCompanyWords(IReadOnlyList<PdfWord> line, PdfWord label)
    {
        var start = line.ToList().FindIndex(w => w.Left - label.Right <= 0.085 && !LooksLikeLabel(w.Text) && !IsCompanyLabelFragment(w.Text));
        if (start < 0) return [];

        var result = new List<PdfWord> { line[start] };
        for (var index = start + 1; index < line.Count; index++)
        {
            var word = line[index];
            if (LooksLikeLabel(word.Text) || IsCompanyLabelFragment(word.Text) || word.Left - result[^1].Right > 0.018) break;
            result.Add(word);
        }
        return result;
    }

    private static IEnumerable<PdfWord> FindCompositeLabels(IReadOnlyList<PdfWord> words, string full, string firstPart, string secondPart)
    {
        foreach (var word in words.Where(w => Compact(w.Text).Contains(full))) yield return word;
        foreach (var first in words.Where(w => Compact(w.Text) == firstPart))
        {
            var second = words.FirstOrDefault(w => w.PageNumber == first.PageNumber && Compact(w.Text) == secondPart &&
                                                   Math.Abs(w.CenterY - first.CenterY) <= SameLineTolerance &&
                                                   w.Left >= first.Right - 0.005 && w.Left - first.Right <= 0.07);
            if (second is not null)
                yield return new PdfWord(full, first.Left, Math.Min(first.Bottom, second.Bottom), second.Right, Math.Max(first.Top, second.Top), first.PageNumber);
        }
    }

    private static string WordsRightOfLabel(IReadOnlyList<PdfWord> words, PdfWord label, double maxDistance)
    {
        var right = words.Where(w => w.PageNumber == label.PageNumber && w.Left >= label.Right - 0.005 &&
                    w.Left - label.Right <= maxDistance && Math.Abs(w.CenterY - label.CenterY) <= SameLineTolerance)
            .OrderBy(w => w.Left).TakeWhile(w => !LooksLikeLabel(w.Text)).Select(w => w.Text);
        return DataNormalizer.CleanText(string.Join(" ", right));
    }

    private static IEnumerable<string> GroupLines(IReadOnlyList<PdfWord> words) =>
        GroupWordLines(words).Select(g => string.Join(" ", g.OrderBy(w => w.Left).Select(w => w.Text)));

    private static IEnumerable<IReadOnlyList<PdfWord>> GroupWordLines(IReadOnlyList<PdfWord> words)
    {
        foreach (var page in words.GroupBy(w => w.PageNumber).OrderBy(g => g.Key))
        {
            var lines = new List<List<PdfWord>>();
            foreach (var word in page.OrderByDescending(w => w.CenterY).ThenBy(w => w.Left))
            {
                var line = lines.FirstOrDefault(candidate => Math.Abs(candidate.Average(w => w.CenterY) - word.CenterY) <= 0.006);
                if (line is null)
                {
                    line = [];
                    lines.Add(line);
                }
                line.Add(word);
            }
            foreach (var line in lines.OrderByDescending(line => line.Average(w => w.CenterY))) yield return line;
        }
    }

    private static bool IsItemHeader(string value)
    {
        var compact = Compact(value);
        return ItemColumnHeaders.Contains(compact) || ItemHeaderFragments.Contains(compact);
    }

    private static bool IsSummaryHeader(string value) => SummaryHeaders.Any(term => Compact(value).Contains(term));
    private static bool IsCompanyLabelFragment(string value) => new[] { "상", "호", "(", "법", "인", "명", ")", "(법인명)" }.Contains(Compact(value));

    private static bool LooksLikeLabel(string value) =>
        new[] { "등록번호", "상호", "성명", "사업장", "업태", "종목", "이메일", "규격", "수량", "단가", "공급가액", "세액", "비고" }
            .Any(term => Compact(value).Contains(term));

    private static bool IsAny(string value, params string[] terms) => terms.Any(t => Compact(value).Contains(t));
    private static string Compact(string value) => Regex.Replace(value, @"\s+", string.Empty).Replace("：", ":");

    [GeneratedRegex(@"\d")]
    private static partial Regex DigitRegex();
}



