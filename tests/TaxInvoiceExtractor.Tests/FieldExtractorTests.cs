using TaxInvoiceExtractor.Pdf;

namespace TaxInvoiceExtractor.Tests;

public sealed class FieldExtractorTests
{
    private readonly FieldExtractor _extractor = new();

    [Fact]
    public void ExtractCompanyName_SeparatesLeftSupplierAndRightBuyer()
    {
        PdfWord[] words =
        [
            W("상호(법인명)", .08, .80, .20), W("공급자주식회사", .22, .80, .42),
            W("상호(법인명)", .55, .80, .68), W("구매자주식회사", .70, .80, .92)
        ];

        Assert.Equal("공급자주식회사", _extractor.ExtractCompanyName(words, true));
        Assert.Equal("구매자주식회사", _extractor.ExtractCompanyName(words, false));
    }

    [Fact]
    public void ExtractAmount_UsesValueOnLabelLine()
    {
        PdfWord[] words = [W("공급가액", .15, .60, .25), W("1,250,000", .30, .60, .42), W("세액", .55, .60, .61), W("125,000", .66, .60, .76)];
        Assert.Equal(1250000, _extractor.ExtractAmount(words, false));
        Assert.Equal(125000, _extractor.ExtractAmount(words, true));
    }

    [Fact]
    public void TrusBillLayout_ExtractsItemTotalsAndSplitCompanyLabels()
    {
        PdfWord[] words =
        [
            W("상", .0771, .8799, .0909), W("호", .1288, .8799, .1426), W("(법인명)", .0840, .8686, .1358),
            W("고진모터스(주)", .1578, .8700, .2696),
            W("상", .4912, .8799, .5050), W("호", .5428, .8799, .5567), W("(법인명)", .4981, .8686, .5499),
            W("지케이모빌리티", .5718, .8700, .6856), W("주식회사", .6929, .8697, .7574),
            W("공급가액", .1961, .7575, .2556), W("2026/08/07", .0594, .7421, .1330), W("32,000,000", .2367, .7417, .3051),
            W("세액", .3775, .7575, .4067), W("3,200,000", .4086, .7417, .4695),
            W("월", .0553, .6978, .0694), W("일", .0815, .6978, .0956), W("품목", .2043, .6979, .2336),
            W("규격", .3530, .6978, .3823), W("수량", .4159, .6978, .4451), W("단가", .4924, .6979, .5217),
            W("공급가액", .5774, .6978, .6369), W("세액", .7016, .6978, .7309), W("비고", .8094, .6981, .8386),
            W("08", .0553, .6802, .0708), W("07", .0815, .6802, .0970),
            W("임대료", .1154, .6804, .1570), W("청구", .1620, .6804, .1898), W("8월", .1948, .6804, .2162),
            W("1", .4492, .6802, .4572), W("32,000,000", .4790, .6802, .5474),
            W("32,000,000", .5888, .6802, .6573), W("3,200,000", .7048, .6802, .7657),
            W("합계", .0860, .5919, .1152), W("금액", .1225, .5919, .1518)
        ];

        Assert.Equal("임대료 청구 8월", _extractor.ExtractDescription(words));
        Assert.Equal(32000000, _extractor.ExtractAmount(words, false));
        Assert.Equal(3200000, _extractor.ExtractAmount(words, true));
        Assert.Equal("고진모터스(주)", _extractor.ExtractCompanyName(words, true));
        Assert.Equal("지케이모빌리티 주식회사", _extractor.ExtractCompanyName(words, false));
    }

    [Fact]
    public void SplitLabelsAndDigitWords_AreReassembledByColumnAndLine()
    {
        PdfWord[] words =
        [
            W("상", .095, .865, .110), W("호", .128, .865, .143), W("(법인명)", .094, .852, .142),
            W("현대엘리베이터서비스", .151, .865, .290), W("㈜충청센터", .151, .852, .222), W("성명", .311, .865, .345),
            W("상", .486, .865, .501), W("호", .518, .865, .533), W("(법인명)", .485, .852, .533),
            W("고진모터스(주)", .542, .859, .636), W("성명", .701, .865, .735),
            W("작성일자", .081, .755, .132), W("공급가액", .276, .755, .327), W("세액", .579, .755, .605), W("수정사유", .762, .755, .813),
            W("2026", .066, .719, .101), W("7", .111, .719, .118), W("25", .130, .719, .147),
            W("2", .316, .719, .324), W("1", .342, .719, .350), W("0", .367, .719, .375), W("0", .392, .719, .400), W("0", .417, .719, .425), W("0", .442, .719, .450),
            W("2", .619, .719, .627), W("1", .644, .719, .652), W("0", .669, .719, .677), W("0", .694, .719, .702), W("0", .720, .719, .728),
            W("일", .090, .671, .109), W("품", .196, .671, .215), W("목", .217, .671, .230), W("규격", .326, .671, .365),
            W("월정보수", .122, .653, .178), W("합계", .080, .590, .120)
        ];

        Assert.Equal("현대엘리베이터서비스 ㈜충청센터", _extractor.ExtractCompanyName(words, true));
        Assert.Equal("고진모터스(주)", _extractor.ExtractCompanyName(words, false));
        Assert.Equal("월정보수", _extractor.ExtractDescription(words));
        Assert.Equal(210000, _extractor.ExtractAmount(words, false));
        Assert.Equal(21000, _extractor.ExtractAmount(words, true));
        Assert.Equal("07월 25일", _extractor.ExtractIssueMonthDay(words));
    }
    private static PdfWord W(string text, double left, double y, double right) => new(text, left, y - .005, right, y + .005, 1);
}

