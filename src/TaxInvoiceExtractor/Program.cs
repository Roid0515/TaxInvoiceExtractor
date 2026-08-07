using TaxInvoiceExtractor.Logging;
using TaxInvoiceExtractor.UI;

namespace TaxInvoiceExtractor;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => AppLogger.Error("처리되지 않은 UI 오류", e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            AppLogger.Error("처리되지 않은 프로그램 오류", e.ExceptionObject as Exception);

        AppLogger.Info("프로그램 시작");
        Application.Run(new MainForm());
    }
}
