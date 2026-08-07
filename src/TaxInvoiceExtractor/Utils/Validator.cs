using System.Text.RegularExpressions;
using TaxInvoiceExtractor.Models;

namespace TaxInvoiceExtractor.Utils;

public static partial class Validator
{
    [GeneratedRegex(@"^(0[1-9]|1[0-2])월\s(0[1-9]|[12]\d|3[01])일$")]
    private static partial Regex MonthDayRegex();

    public static IReadOnlyList<string> Validate(TaxInvoiceData data)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(data.Description)) errors.Add("적요");
        if (data.SupplyAmount is null) errors.Add("공급가액");
        if (data.VatAmount is null) errors.Add("부가세");
        if (string.IsNullOrWhiteSpace(data.SupplierName)) errors.Add("공급자 상호");
        if (string.IsNullOrWhiteSpace(data.BuyerName)) errors.Add("공급받는자 상호");
        if (!MonthDayRegex().IsMatch(data.IssueMonthDay)) errors.Add("작성월일");
        return errors;
    }
}
