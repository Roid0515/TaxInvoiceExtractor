using TaxInvoiceExtractor.Utils;

namespace TaxInvoiceExtractor.Tests;

public sealed class DataNormalizerTests
{
    [Theory]
    [InlineData("1,250,000원", 1250000)]
    [InlineData(" 125 000 ", 125000)]
    [InlineData("￦99,000", 99000)]
    public void ParseAmount_NormalizesKoreanCurrency(string source, long expected) =>
        Assert.Equal(expected, DataNormalizer.ParseAmount(source));

    [Theory]
    [InlineData("2026-08-07", "08월 07일")]
    [InlineData("작성일자 2025.12.31", "12월 31일")]
    [InlineData("26년 1월 9일", "01월 09일")]
    public void ParseIssueMonthDay_FormatsExpected(string source, string expected) =>
        Assert.Equal(expected, DataNormalizer.ParseIssueMonthDay(source));

    [Theory]
    [InlineData("2026-13-01")]
    [InlineData("날짜 없음")]
    public void ParseIssueMonthDay_RejectsInvalid(string source) =>
        Assert.Null(DataNormalizer.ParseIssueMonthDay(source));
}
