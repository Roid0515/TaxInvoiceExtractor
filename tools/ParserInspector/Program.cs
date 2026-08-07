using System.Text.Json;
using TaxInvoiceExtractor.Pdf;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: ParserInspector <pdf-path>");
    return 1;
}

var path = args[0];
var layout = new PdfTextExtractor().Extract(path);
var result = new TaxInvoiceParser(new FieldExtractor()).Parse(layout, 1, Path.GetFileName(path));
Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
return 0;
