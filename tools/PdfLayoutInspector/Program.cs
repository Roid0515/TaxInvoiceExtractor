using TaxInvoiceExtractor.Pdf;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: PdfLayoutInspector <pdf-path>");
    return 1;
}

var document = new PdfTextExtractor().Extract(args[0]);
foreach (var page in document.Pages)
{
    Console.WriteLine($"--- PAGE {page.PageNumber} ---");
    foreach (var word in page.Words.OrderByDescending(w => w.CenterY).ThenBy(w => w.Left))
        Console.WriteLine($"{word.Left:F4}\t{word.Bottom:F4}\t{word.Right:F4}\t{word.Top:F4}\t{word.Text}");
}

return 0;
