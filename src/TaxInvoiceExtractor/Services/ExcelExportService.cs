using ClosedXML.Excel;
using TaxInvoiceExtractor.Models;

namespace TaxInvoiceExtractor.Services;

public sealed class ExcelExportService
{
    private static readonly string[] Headers =
    [
        "순번", "적요", "공급가액", "부가세", "공급자 상호(법인명)", "공급받는자 상호(법인명)", "작성월일"
    ];

    public void Export(string path, IReadOnlyList<TaxInvoiceData> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("추출결과");
        for (var column = 0; column < Headers.Length; column++) sheet.Cell(1, column + 1).Value = Headers[column];

        for (var index = 0; index < rows.Count; index++)
        {
            var row = index + 2;
            var item = rows[index];
            sheet.Cell(row, 1).Value = item.Sequence;
            sheet.Cell(row, 2).Value = item.Description;
            if (item.SupplyAmount is long supply) sheet.Cell(row, 3).Value = supply;
            if (item.VatAmount is long vat) sheet.Cell(row, 4).Value = vat;
            sheet.Cell(row, 5).Value = item.SupplierName;
            sheet.Cell(row, 6).Value = item.BuyerName;
            sheet.Cell(row, 7).Value = item.IssueMonthDay;
        }

        var range = sheet.Range(1, 1, Math.Max(1, rows.Count + 1), Headers.Length);
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sheet.Range(1, 1, 1, Headers.Length).Style.Font.Bold = true;
        sheet.Range(1, 1, 1, Headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#DCE6F1");
        sheet.Range(1, 1, 1, Headers.Length).SetAutoFilter();
        sheet.SheetView.FreezeRows(1);
        if (rows.Count > 0) sheet.Range(2, 3, rows.Count + 1, 4).Style.NumberFormat.Format = "#,##0";

        sheet.Column(1).Width = 8;
        sheet.Column(2).Width = 38;
        sheet.Columns(3, 4).Width = 16;
        sheet.Columns(5, 6).Width = 28;
        sheet.Column(7).Width = 14;
        sheet.Rows().AdjustToContents();
        workbook.SaveAs(path);
    }
}
