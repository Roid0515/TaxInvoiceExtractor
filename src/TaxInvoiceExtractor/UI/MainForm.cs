using System.ComponentModel;
using TaxInvoiceExtractor.Logging;
using TaxInvoiceExtractor.Models;
using TaxInvoiceExtractor.Pdf;
using TaxInvoiceExtractor.Services;
using TaxInvoiceExtractor.Utils;

namespace TaxInvoiceExtractor.UI;

public sealed class MainForm : Form
{

    private readonly BindingList<SelectedPdfItem> _files = [];
    private readonly BindingList<TaxInvoiceData> _results = [];
    private readonly DataGridView _fileGrid = new();
    private readonly DataGridView _resultGrid = new();
    private readonly Button _addButton = new();
    private readonly Button _extractButton = new();
    private readonly Button _saveButton = new();
    private readonly ProgressBar _progressBar = new();
    private readonly Label _progressLabel = new();
    private readonly Label _statusLabel = new();
    private readonly ExtractionService _extractionService;
    private readonly ExcelExportService _excelService = new();

    public MainForm()
    {
        _extractionService = new ExtractionService(
            new PdfTextExtractor(),
            new TaxInvoiceParser(new FieldExtractor()));

        Text = "전자세금계산서 PDF → Excel 변환";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1050, 720);
        Size = new Size(1280, 820);
        Font = new Font("맑은 고딕", 9F);
        AllowDrop = true;
        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
        BuildUi();
        BindData();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), RowCount = 6, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = new Label
        {
            Text = "전자세금계산서 PDF → Excel 변환",
            Font = new Font("맑은 고딕", 16F, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        };
        root.Controls.Add(title, 0, 0);

        ConfigureFileGrid();
        root.Controls.Add(_fileGrid, 0, 1);

        var fileButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 6, 0, 4) };
        _addButton.Text = "PDF 폴더 불러오기";
        _addButton.AutoSize = true;
        _addButton.Click += (_, _) => ChooseFolder();
        fileButtons.Controls.Add(_addButton);
        fileButtons.Controls.Add(MakeButton("위로", (_, _) => MoveSelected(-1)));
        fileButtons.Controls.Add(MakeButton("아래로", (_, _) => MoveSelected(1)));
        fileButtons.Controls.Add(MakeButton("선택 삭제", (_, _) => RemoveSelected()));
        fileButtons.Controls.Add(MakeButton("전체 삭제", (_, _) => ClearFiles()));
        root.Controls.Add(fileButtons, 0, 2);

        var actionPanel = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3 };
        actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _extractButton.Text = "데이터 추출";
        _extractButton.AutoSize = true;
        _extractButton.Padding = new Padding(12, 5, 12, 5);
        _extractButton.Click += async (_, _) => await ExtractAsync();
        actionPanel.Controls.Add(_extractButton, 0, 0);
        var progressPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Margin = new Padding(12, 0, 12, 0) };
        _progressBar.Dock = DockStyle.Fill;
        _progressBar.Visible = false;
        _progressLabel.AutoSize = true;
        _progressLabel.Visible = false;
        progressPanel.Controls.Add(_progressBar, 0, 0);
        progressPanel.Controls.Add(_progressLabel, 0, 1);
        actionPanel.Controls.Add(progressPanel, 1, 0);
        _saveButton.Text = "Excel로 저장";
        _saveButton.AutoSize = true;
        _saveButton.Padding = new Padding(12, 5, 12, 5);
        _saveButton.Enabled = false;
        _saveButton.Click += (_, _) => SaveExcel();
        actionPanel.Controls.Add(_saveButton, 2, 0);
        root.Controls.Add(actionPanel, 0, 3);

        ConfigureResultGrid();
        root.Controls.Add(_resultGrid, 0, 4);

        _statusLabel.Text = "PDF가 들어 있는 폴더를 선택하세요. (끌어놓기 가능)";
        _statusLabel.AutoSize = true;
        _statusLabel.Padding = new Padding(0, 8, 0, 0);
        root.Controls.Add(_statusLabel, 0, 5);
        Controls.Add(root);
    }

    private void ConfigureFileGrid()
    {
        _fileGrid.Dock = DockStyle.Fill;
        _fileGrid.AutoGenerateColumns = false;
        _fileGrid.AllowUserToAddRows = false;
        _fileGrid.AllowUserToDeleteRows = false;
        _fileGrid.ReadOnly = true;
        _fileGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _fileGrid.MultiSelect = false;
        _fileGrid.RowHeadersVisible = false;
        _fileGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(SelectedPdfItem.Sequence), HeaderText = "순번", Width = 65 });
        _fileGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(SelectedPdfItem.FileName), HeaderText = "파일명", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _fileGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(SelectedPdfItem.Status), HeaderText = "상태", Width = 130 });
    }

    private void ConfigureResultGrid()
    {
        _resultGrid.Dock = DockStyle.Fill;
        _resultGrid.AutoGenerateColumns = false;
        _resultGrid.AllowUserToAddRows = false;
        _resultGrid.RowHeadersVisible = false;
        _resultGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _resultGrid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        _resultGrid.DataError += (_, e) =>
        {
            e.ThrowException = false;
            _statusLabel.Text = "공급가액과 부가세는 숫자로 입력해주세요.";
        };
        _resultGrid.CellValidating += ValidateCell;
        AddResultColumn(nameof(TaxInvoiceData.Sequence), "순번", 60, true);
        AddResultColumn(nameof(TaxInvoiceData.Description), "적요", 190);
        AddResultColumn(nameof(TaxInvoiceData.SupplyAmount), "공급가액", 110, format: "N0");
        AddResultColumn(nameof(TaxInvoiceData.VatAmount), "부가세", 100, format: "N0");
        AddResultColumn(nameof(TaxInvoiceData.SupplierName), "공급자 상호(법인명)", 190);
        AddResultColumn(nameof(TaxInvoiceData.BuyerName), "공급받는자 상호(법인명)", 210);
        AddResultColumn(nameof(TaxInvoiceData.IssueMonthDay), "작성월일", 105);
    }

    private void AddResultColumn(string property, string header, int width, bool readOnly = false, string? format = null)
    {
        _resultGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = property,
            Name = property,
            HeaderText = header,
            Width = width,
            ReadOnly = readOnly,
            DefaultCellStyle = new DataGridViewCellStyle { Format = format ?? string.Empty }
        });
    }

    private void BindData()
    {
        _fileGrid.DataSource = _files;
        _resultGrid.DataSource = _results;
    }

    private static Button MakeButton(string text, EventHandler handler)
    {
        var button = new Button { Text = text, AutoSize = true, Margin = new Padding(4, 0, 0, 0) };
        button.Click += handler;
        return button;
    }

    private void ChooseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "전자세금계산서 PDF가 들어 있는 폴더를 선택하세요.",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var pdfFiles = PdfFolderService.GetPdfFiles(dialog.SelectedPath);
            if (pdfFiles.Count == 0)
            {
                MessageBox.Show(this, "선택한 폴더에 PDF 파일이 없습니다.", "PDF 없음", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _files.Clear();
            AddFiles(pdfFiles);
            _statusLabel.Text = $"'{dialog.SelectedPath}' 폴더에서 PDF {pdfFiles.Count}개를 불러왔습니다.";
        }
        catch (Exception ex)
        {
            AppLogger.Error($"PDF 폴더 읽기 실패: {dialog.SelectedPath}", ex);
            MessageBox.Show(this, $"선택한 폴더를 읽지 못했습니다.\n\n{ex.Message}", "폴더 읽기 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    private void AddFiles(IEnumerable<string> paths)
    {
        var valid = paths.Where(p => string.Equals(Path.GetExtension(p), ".pdf", StringComparison.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var newPaths = valid.Where(p => _files.All(f => !string.Equals(f.FullPath, p, StringComparison.OrdinalIgnoreCase))).ToList();
        foreach (var path in newPaths)
            _files.Add(new SelectedPdfItem { FullPath = path, FileName = Path.GetFileName(path), Sequence = _files.Count + 1 });
        ResetResults();
        _statusLabel.Text = $"PDF {_files.Count}개가 선택되었습니다.";
    }

    private void MoveSelected(int direction)
    {
        if (_fileGrid.CurrentRow?.DataBoundItem is not SelectedPdfItem item) return;
        var index = _files.IndexOf(item);
        var target = index + direction;
        if (target < 0 || target >= _files.Count) return;
        _files.RaiseListChangedEvents = false;
        _files.RemoveAt(index);
        _files.Insert(target, item);
        _files.RaiseListChangedEvents = true;
        RenumberAndRefresh();
        _fileGrid.CurrentCell = _fileGrid.Rows[target].Cells[0];
        ResetResults();
    }

    private void RemoveSelected()
    {
        if (_fileGrid.CurrentRow?.DataBoundItem is not SelectedPdfItem item) return;
        _files.Remove(item);
        RenumberAndRefresh();
        ResetResults();
    }

    private void ClearFiles()
    {
        _files.Clear();
        ResetResults();
        _statusLabel.Text = "PDF가 들어 있는 폴더를 선택하세요. (끌어놓기 가능)";
    }

    private void RenumberAndRefresh()
    {
        for (var i = 0; i < _files.Count; i++) _files[i].Sequence = i + 1;
        _fileGrid.Refresh();
    }

    private void ResetResults()
    {
        _results.Clear();
        foreach (var file in _files) file.Status = "대기";
        _fileGrid.Refresh();
        _saveButton.Enabled = false;
    }

    private async Task ExtractAsync()
    {
        if (_files.Count == 0)
        {
            MessageBox.Show(this, "PDF 파일을 먼저 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetBusy(true);
        _results.Clear();
        var progress = new Progress<ExtractionProgress>(p =>
        {
            _progressBar.Maximum = p.Total;
            _progressBar.Value = p.Current;
            _progressLabel.Text = $"데이터 추출, 변환중...  {p.Current} / {p.Total} 파일 처리중  |  {p.FileName}";
            if (p.Current - 1 < _files.Count) _files[p.Current - 1].Status = p.Status;
            _fileGrid.Refresh();
        });

        try
        {
            var rows = await _extractionService.ExtractAsync(_files.ToList(), progress);
            foreach (var row in rows) _results.Add(row);
            _saveButton.Enabled = _results.Count > 0;
            var reviewCount = rows.Count(r => r.ExtractionStatus != "완료");
            _statusLabel.Text = reviewCount == 0
                ? $"추출이 완료되었습니다. {_results.Count}개 결과를 확인한 뒤 Excel로 저장하세요."
                : $"추출 완료: {_results.Count}개 중 {reviewCount}개는 확인 및 수정이 필요합니다.";
            ShowFirstError(rows);
        }
        catch (Exception ex)
        {
            AppLogger.Error("전체 추출 작업 오류", ex);
            MessageBox.Show(this, $"데이터 추출 중 오류가 발생했습니다.\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ShowFirstError(IReadOnlyList<TaxInvoiceData> rows)
    {
        var first = rows.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.ErrorMessage));
        if (first is not null)
            _statusLabel.Text += $"  첫 확인 항목: {first.SourceFileName} — {first.ErrorMessage}";
    }

    private void SetBusy(bool busy)
    {
        _addButton.Enabled = !busy;
        _extractButton.Enabled = !busy;
        _fileGrid.Enabled = !busy;
        _resultGrid.Enabled = !busy;
        _progressBar.Visible = busy;
        _progressLabel.Visible = busy;
        if (busy) { _progressBar.Value = 0; _progressLabel.Text = "데이터 추출, 변환중..."; }
    }

    private void SaveExcel()
    {
        _resultGrid.EndEdit();
        var invalid = _results.Where(r => Validator.Validate(r).Count > 0).ToList();
        if (invalid.Count > 0)
        {
            var answer = MessageBox.Show(this,
                $"{invalid.Count}개 행에 비어 있거나 형식이 맞지 않는 값이 있습니다. 빈 셀을 유지한 채 저장할까요?",
                "검증 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Excel 파일로 저장",
            Filter = "Excel 통합 문서 (*.xlsx)|*.xlsx",
            DefaultExt = "xlsx",
            AddExtension = true,
            FileName = $"전자세금계산서_추출결과_{DateTime.Now:yyyyMMdd}.xlsx",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            _excelService.Export(dialog.FileName, _results.ToList());
            AppLogger.Info($"Excel 저장 성공: {Path.GetFileName(dialog.FileName)}");
            _statusLabel.Text = $"Excel 저장 완료: {dialog.FileName}";
            MessageBox.Show(this, "Excel 파일을 저장했습니다.", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Excel 저장 실패: {Path.GetFileName(dialog.FileName)}", ex);
            MessageBox.Show(this,
                $"Excel 파일을 저장하지 못했습니다. 파일이 Excel에서 열려 있거나 저장 권한이 없는지 확인해주세요.\n\n{ex.Message}",
                "저장 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ValidateCell(object? sender, DataGridViewCellValidatingEventArgs e)
    {
        var property = _resultGrid.Columns[e.ColumnIndex].DataPropertyName;
        var text = Convert.ToString(e.FormattedValue)?.Trim() ?? string.Empty;
        if (property is nameof(TaxInvoiceData.SupplyAmount) or nameof(TaxInvoiceData.VatAmount))
        {
            if (text.Length > 0 && DataNormalizer.ParseAmount(text) is null)
            {
                e.Cancel = true;
                _resultGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].ErrorText = "숫자로 입력해주세요.";
            }
            else _resultGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].ErrorText = string.Empty;
        }
        else if (property == nameof(TaxInvoiceData.IssueMonthDay) && text.Length > 0 &&
                 !System.Text.RegularExpressions.Regex.IsMatch(text, @"^(0[1-9]|1[0-2])월\s(0[1-9]|[12]\d|3[01])일$"))
        {
            e.Cancel = true;
            _resultGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].ErrorText = "예: 08월 07일";
        }
        else _resultGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].ErrorText = string.Empty;
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) e.Effect = DragDropEffects.Copy;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths) AddFiles(paths);
    }
}
