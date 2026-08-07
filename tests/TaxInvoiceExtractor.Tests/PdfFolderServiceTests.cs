using TaxInvoiceExtractor.Services;

namespace TaxInvoiceExtractor.Tests;

public sealed class PdfFolderServiceTests
{
    [Fact]
    public void GetPdfFiles_LoadsAllPdfsBeyondTenAndExcludesOtherFiles()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"tax-invoice-folder-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            for (var i = 12; i >= 1; i--)
                File.WriteAllText(Path.Combine(folder, $"계산서_{i:00}.pdf"), "test");
            File.WriteAllText(Path.Combine(folder, "메모.txt"), "not a pdf");
            Directory.CreateDirectory(Path.Combine(folder, "하위폴더"));
            File.WriteAllText(Path.Combine(folder, "하위폴더", "제외.pdf"), "nested");

            var result = PdfFolderService.GetPdfFiles(folder);

            Assert.Equal(12, result.Count);
            Assert.Equal("계산서_01.pdf", Path.GetFileName(result[0]));
            Assert.Equal("계산서_12.pdf", Path.GetFileName(result[^1]));
            Assert.DoesNotContain(result, path => path.Contains("하위폴더"));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
