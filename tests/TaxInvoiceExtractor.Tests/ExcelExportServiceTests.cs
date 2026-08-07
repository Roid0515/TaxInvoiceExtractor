using ClosedXML.Excel;
using TaxInvoiceExtractor.Models;
using TaxInvoiceExtractor.Services;

namespace TaxInvoiceExtractor.Tests;

public sealed class ExcelExportServiceTests
{
    [Fact]
    public void Export_WritesNumericAmountsAndRequiredHeaders()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tax-invoice-{Guid.NewGuid():N}.xlsx");
        try
        {
            new ExcelExportService().Export(path,
            [
                new TaxInvoiceData { Sequence = 1, Description = "유지보수", SupplyAmount = 1000000, VatAmount = 100000,
                    SupplierName = "공급자", BuyerName = "구매자", IssueMonthDay = "08월 07일" }
            ]);
            using var workbook = new XLWorkbook(path);
            var sheet = workbook.Worksheet("추출결과");
            Assert.Equal("순번", sheet.Cell("A1").GetString());
            Assert.Equal("작성월일", sheet.Cell("G1").GetString());
            Assert.Equal(1000000d, sheet.Cell("C2").GetDouble());
            Assert.Equal("#,##0", sheet.Cell("C2").Style.NumberFormat.Format);
            Assert.True(sheet.AutoFilter.IsEnabled);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
