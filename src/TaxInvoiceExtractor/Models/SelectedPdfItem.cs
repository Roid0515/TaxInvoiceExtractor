namespace TaxInvoiceExtractor.Models;

public sealed class SelectedPdfItem
{
    public int Sequence { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = "대기";
    public string FullPath { get; set; } = string.Empty;
}
