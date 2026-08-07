using System.ComponentModel;

namespace TaxInvoiceExtractor.Models;

public sealed class TaxInvoiceData : INotifyPropertyChanged
{
    private int _sequence;
    private string _description = string.Empty;
    private long? _supplyAmount;
    private long? _vatAmount;
    private string _supplierName = string.Empty;
    private string _buyerName = string.Empty;
    private string _issueMonthDay = string.Empty;
    private string _extractionStatus = "대기";
    private string _errorMessage = string.Empty;

    public int Sequence { get => _sequence; set => Set(ref _sequence, value); }
    public string Description { get => _description; set => Set(ref _description, value ?? string.Empty); }
    public long? SupplyAmount { get => _supplyAmount; set => Set(ref _supplyAmount, value); }
    public long? VatAmount { get => _vatAmount; set => Set(ref _vatAmount, value); }
    public string SupplierName { get => _supplierName; set => Set(ref _supplierName, value ?? string.Empty); }
    public string BuyerName { get => _buyerName; set => Set(ref _buyerName, value ?? string.Empty); }
    public string IssueMonthDay { get => _issueMonthDay; set => Set(ref _issueMonthDay, value ?? string.Empty); }
    [Browsable(false)] public string SourceFileName { get; set; } = string.Empty;
    [Browsable(false)] public string ExtractionStatus { get => _extractionStatus; set => Set(ref _extractionStatus, value); }
    [Browsable(false)] public string ErrorMessage { get => _errorMessage; set => Set(ref _errorMessage, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
