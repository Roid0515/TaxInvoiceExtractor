using TaxInvoiceExtractor.Models;
using TaxInvoiceExtractor.Utils;

namespace TaxInvoiceExtractor.Pdf;

public sealed class TaxInvoiceParser
{
    private readonly FieldExtractor _fields;

    public TaxInvoiceParser(FieldExtractor fields) => _fields = fields;

    public TaxInvoiceData Parse(PdfLayoutDocument document, int sequence, string sourceFileName)
    {
        if (!document.HasText)
            throw new InvalidDataException("PDF에서 텍스트 데이터를 읽을 수 없습니다. OCR은 지원하지 않습니다.");

        var words = document.Pages.SelectMany(p => p.Words).ToList();
        var result = new TaxInvoiceData
        {
            Sequence = sequence,
            SourceFileName = sourceFileName,
            Description = _fields.ExtractDescription(words),
            SupplyAmount = _fields.ExtractAmount(words, vat: false),
            VatAmount = _fields.ExtractAmount(words, vat: true),
            SupplierName = _fields.ExtractCompanyName(words, supplier: true),
            BuyerName = _fields.ExtractCompanyName(words, supplier: false),
            IssueMonthDay = _fields.ExtractIssueMonthDay(words)
        };

        var missing = Validator.Validate(result);
        result.ExtractionStatus = missing.Count == 0 ? "완료" : "확인 필요";
        result.ErrorMessage = missing.Count == 0 ? string.Empty : $"추출 또는 형식 확인 필요: {string.Join(", ", missing)}";
        return result;
    }
}
