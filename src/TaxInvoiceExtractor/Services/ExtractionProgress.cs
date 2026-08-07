namespace TaxInvoiceExtractor.Services;

public sealed record ExtractionProgress(int Current, int Total, string FileName, string Status);
