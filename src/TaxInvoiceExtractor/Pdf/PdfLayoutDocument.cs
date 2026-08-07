namespace TaxInvoiceExtractor.Pdf;

public sealed record PdfWord(string Text, double Left, double Bottom, double Right, double Top, int PageNumber)
{
    public double CenterX => (Left + Right) / 2d;
    public double CenterY => (Bottom + Top) / 2d;
}

public sealed record PdfPageLayout(int PageNumber, IReadOnlyList<PdfWord> Words);

public sealed record PdfLayoutDocument(IReadOnlyList<PdfPageLayout> Pages)
{
    public bool HasText => Pages.Any(p => p.Words.Any(w => !string.IsNullOrWhiteSpace(w.Text)));
}
