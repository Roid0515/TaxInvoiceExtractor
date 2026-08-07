using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace TaxInvoiceExtractor.Pdf;

public sealed class PdfTextExtractor : IPdfTextExtractor
{
    public PdfLayoutDocument Extract(string filePath)
    {
        var pages = new List<PdfPageLayout>();
        using var document = PdfDocument.Open(filePath);

        foreach (var page in document.GetPages())
        {
            var width = Math.Max(1d, (double)page.Width);
            var height = Math.Max(1d, (double)page.Height);
            var words = page.GetWords(NearestNeighbourWordExtractor.Instance)
                .Where(w => !string.IsNullOrWhiteSpace(w.Text))
                .Select(w => new PdfWord(
                    w.Text.Trim(),
                    (double)w.BoundingBox.Left / width,
                    (double)w.BoundingBox.Bottom / height,
                    (double)w.BoundingBox.Right / width,
                    (double)w.BoundingBox.Top / height,
                    page.Number))
                .ToList();
            pages.Add(new PdfPageLayout(page.Number, words));
        }

        return new PdfLayoutDocument(pages);
    }
}
