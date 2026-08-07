namespace TaxInvoiceExtractor.Pdf;

public interface IPdfTextExtractor
{
    PdfLayoutDocument Extract(string filePath);
}
