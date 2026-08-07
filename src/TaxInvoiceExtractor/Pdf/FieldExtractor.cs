using System.Text.RegularExpressions;
using TaxInvoiceExtractor.Utils;

namespace TaxInvoiceExtractor.Pdf;

public sealed partial class FieldExtractor
{
    private const double SameLineTolerance = 0.014;
    private static readonly string[] ItemColumnHeaders = ["월", "일", "품목", "규격", "수량", "단가", "공급가액", "세액", "비고"];

    public string ExtractCompanyName(IReadOnlyList<PdfWord> words, bool supplier)
    {
        var region = words.Where(w => supplier ? w.CenterX < 0.52 : w.CenterX >= 0.48).ToList();
        var labels = region.Where(w =>
            Compact(w.Text).Contains("상호(법인명)") ||
            Compact(w.Text).Contains("상호법인명") ||
            Compact(w.Text) == "(법인명)");

        foreach (var label in labels.OrderByDescending(w => w.CenterY))
        {
            var value = WordsRightOfLabel(region, label, 0.32);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        return string.Empty;
    }

    public string ExtractDescription(IReadOnlyList<PdfWord> words)
    {
        // '품목'은 표 열의 가운데에 있고 실제 데이터는 아래 행에 있다.
        foreach (var label in words.Where(w => IsAny(w.Text, "품목", "품목명")).OrderByDescending(w => w.CenterY))
        {
            var headerLine = words.Where(w => w.PageNumber == label.PageNumber &&
                                              Math.Abs(w.CenterY - label.CenterY) <= SameLineTolerance)
                .OrderBy(w => w.Left)
                .ToList();
            var leftBoundary = headerLine
                .Where(w => w.Right <= label.Left && ItemColumnHeaders.Any(h => Compact(w.Text) == h))
                .Select(w => w.Right).DefaultIfEmpty(Math.Max(0, label.Left - 0.18)).Max();
            var rightBoundary = headerLine
                .Where(w => w.Left > label.Right && ItemColumnHeaders.Any(h => Compact(w.Text) == h))
                .Select(w => w.Left).DefaultIfEmpty(Math.Min(1, label.Right + 0.30)).Min();

            var footerY = words.Where(w => w.PageNumber == label.PageNumber && w.CenterY < label.CenterY &&
                                            (Compact(w.Text) == "합계" || Compact(w.Text) == "합계금액"))
                .Select(w => w.CenterY).DefaultIfEmpty(label.CenterY - 0.16).Max();
            var itemWords = words.Where(w => w.PageNumber == label.PageNumber &&
                                             w.CenterY < label.CenterY - 0.004 && w.CenterY > footerY + 0.004 &&
                                             w.CenterX > leftBoundary && w.CenterX < rightBoundary)
                .ToList();

            var descriptions = GroupWordLines(itemWords)
                .Select(line => DataNormalizer.CleanText(string.Join(" ", line.OrderBy(w => w.Left).Select(w => w.Text))))
                .Where(value => !string.IsNullOrWhiteSpace(value) && !LooksLikeLabel(value))
                .Distinct()
                .Take(10)
                .ToList();
            if (descriptions.Count > 0) return string.Join(" / ", descriptions);
        }

        // '적요'와 값이 같은 행에 있는 다른 양식을 위한 제한적인 대체 규칙.
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
            // 총액 표의 제목 아래, 같은 열에 있는 숫자를 가장 먼저 사용한다.
            var below = words.Where(w => w.PageNumber == label.PageNumber &&
                                         w.CenterY < label.CenterY - 0.003 && label.CenterY - w.CenterY < 0.055 &&
                                         Math.Abs(w.CenterX - label.CenterX) < 0.10)
                .Select(w => new { Word = w, Amount = DataNormalizer.ParseAmount(w.Text) })
                .Where(x => x.Amount is not null && DigitRegex().IsMatch(x.Word.Text))
                .OrderBy(x => label.CenterY - x.Word.CenterY)
                .ThenBy(x => Math.Abs(x.Word.CenterX - label.CenterX))
                .FirstOrDefault();
            if (below is not null) return below.Amount;

            // 라벨과 값이 같은 행인 양식을 위한 대체 규칙.
            var right = words.Where(w => w.PageNumber == label.PageNumber &&
                                         Math.Abs(w.CenterY - label.CenterY) <= SameLineTolerance &&
                                         w.Left >= label.Right - 0.005 && w.Left - label.Right < 0.35)
                .Select(w => new { Word = w, Amount = DataNormalizer.ParseAmount(w.Text) })
                .Where(x => x.Amount is not null && DigitRegex().IsMatch(x.Word.Text))
                .OrderBy(x => x.Word.Left - label.Right)
                .FirstOrDefault();
            if (right is not null) return right.Amount;
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

    private static string WordsRightOfLabel(IReadOnlyList<PdfWord> words, PdfWord label, double maxDistance)
    {
        var right = words.Where(w => w.PageNumber == label.PageNumber && w.Left >= label.Right - 0.005 &&
                    w.Left - label.Right <= maxDistance && Math.Abs(w.CenterY - label.CenterY) <= SameLineTolerance)
            .OrderBy(w => w.Left)
            .TakeWhile(w => !LooksLikeLabel(w.Text))
            .Select(w => w.Text);
        return DataNormalizer.CleanText(string.Join(" ", right));
    }

    private static IEnumerable<string> GroupLines(IReadOnlyList<PdfWord> words) =>
        GroupWordLines(words).Select(g => string.Join(" ", g.OrderBy(w => w.Left).Select(w => w.Text)));

    private static IEnumerable<IGrouping<(int PageNumber, double Bucket), PdfWord>> GroupWordLines(IReadOnlyList<PdfWord> words) =>
        words.GroupBy(w => (w.PageNumber, Bucket: Math.Round(w.CenterY / 0.012)))
            .OrderBy(g => g.Key.PageNumber).ThenByDescending(g => g.Key.Bucket);

    private static bool LooksLikeLabel(string value) =>
        new[] { "등록번호", "상호", "성명", "사업장", "업태", "종목", "이메일", "규격", "수량", "단가", "공급가액", "세액", "비고" }
            .Any(term => Compact(value).Contains(term));

    private static bool IsAny(string value, params string[] terms) => terms.Any(t => Compact(value).Contains(t));
    private static string Compact(string value) => Regex.Replace(value, @"\s+", string.Empty).Replace("：", ":");

    [GeneratedRegex(@"\d")]
    private static partial Regex DigitRegex();
}
