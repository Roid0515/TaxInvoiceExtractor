using TaxInvoiceExtractor.Logging;
using TaxInvoiceExtractor.Models;
using TaxInvoiceExtractor.Pdf;

namespace TaxInvoiceExtractor.Services;

public sealed class ExtractionService
{
    private readonly IPdfTextExtractor _extractor;
    private readonly TaxInvoiceParser _parser;

    public ExtractionService(IPdfTextExtractor extractor, TaxInvoiceParser parser)
    {
        _extractor = extractor;
        _parser = parser;
    }

    public async Task<IReadOnlyList<TaxInvoiceData>> ExtractAsync(
        IReadOnlyList<SelectedPdfItem> files,
        IProgress<ExtractionProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        AppLogger.Info($"데이터 추출 시작: {files.Count}개 파일");
        var results = new List<TaxInvoiceData>(files.Count);

        for (var index = 0; index < files.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var file = files[index];
            progress?.Report(new ExtractionProgress(index + 1, files.Count, file.FileName, "분석중"));
            AppLogger.Info($"PDF 처리 시작: {file.FileName}");

            try
            {
                var data = await Task.Run(() =>
                {
                    var document = _extractor.Extract(file.FullPath);
                    return _parser.Parse(document, index + 1, file.FileName);
                }, cancellationToken);
                results.Add(data);
                file.Status = data.ExtractionStatus;
                if (!string.IsNullOrWhiteSpace(data.ErrorMessage)) AppLogger.Warn($"{file.FileName}: {data.ErrorMessage}");
                AppLogger.Info($"PDF 처리 완료: {file.FileName}, 상태={data.ExtractionStatus}");
            }
            catch (Exception ex)
            {
                file.Status = "읽기 실패";
                results.Add(new TaxInvoiceData
                {
                    Sequence = index + 1,
                    SourceFileName = file.FileName,
                    ExtractionStatus = "읽기 실패",
                    ErrorMessage = ex.Message
                });
                AppLogger.Error($"PDF 처리 실패: {file.FileName}", ex);
            }
        }

        return results;
    }
}
