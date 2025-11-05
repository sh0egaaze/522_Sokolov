using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.DataVisualization.Charting;
using Word = Microsoft.Office.Interop.Word;
using Excel = Microsoft.Office.Interop.Excel;

namespace _522_Sokolov.Pages
{
    /// <summary>
    /// Страница для визуализации данных платежей в виде диаграмм и экспорта отчетов
    /// </summary>
    public partial class DiagrammPage : Page
    {
        private Entities _context = new Entities();

        public DiagrammPage()
        {
            InitializeComponent();

            ChartPayments.ChartAreas.Add(new ChartArea("Main"));

            var currentSeries = new Series("Платежи")
            {
                IsValueShownAsLabel = true
            };
            ChartPayments.Series.Add(currentSeries);

            CmbUser.ItemsSource = _context.User.ToList();
            CmbDiagram.ItemsSource = Enum.GetValues(typeof(SeriesChartType));
        }

        /// <summary>
        /// Обновляет диаграмму при изменении пользователя или типа диаграммы
        /// </summary>
        private void UpdateChart(object sender, SelectionChangedEventArgs e)
        {
            if (CmbUser.SelectedItem is User currentUser && CmbDiagram.SelectedItem is SeriesChartType currentType)
            {
                Series currentSeries = ChartPayments.Series.FirstOrDefault();
                currentSeries.ChartType = currentType;
                currentSeries.Points.Clear();

                var categoriesList = _context.Category.ToList();
                foreach (var category in categoriesList)
                {
                    var sum = _context.Payment.ToList()
                        .Where(u => u.UserID == currentUser.ID && u.CategoryID == category.ID)
                        .Sum(u => u.Price * u.Num);

                    currentSeries.Points.AddXY(category.Name, sum);
                }
            }
        }

        /// <summary>
        /// Экспортирует данные платежей в Excel с группировкой по пользователям
        /// </summary>
        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var allUsers = _context.User.OrderBy(u => u.FIO).ToList();
                if (!allUsers.Any())
                {
                    MessageBox.Show("Нет пользователей для экспорта.");
                    return;
                }

                var application = new Excel.Application();
                application.SheetsInNewWorkbook = allUsers.Count;
                Excel.Workbook workbook = application.Workbooks.Add(Type.Missing);
                decimal grandTotal = 0;

                foreach (var user in allUsers)
                {
                    int startRowIndex = 1;
                    Excel.Worksheet worksheet = (Excel.Worksheet)application.Worksheets.Item[allUsers.IndexOf(user) + 1];
                    worksheet.Name = user.FIO;

                    worksheet.Cells[1, 1] = "Дата платежа";
                    worksheet.Cells[1, 2] = "Название";
                    worksheet.Cells[1, 3] = "Стоимость";
                    worksheet.Cells[1, 4] = "Количество";
                    worksheet.Cells[1, 5] = "Сумма";

                    Excel.Range columnHeaderRange = worksheet.Range[worksheet.Cells[1, 1], worksheet.Cells[1, 5]];
                    columnHeaderRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                    columnHeaderRange.Font.Bold = true;
                    startRowIndex++;

                    var userPayments = _context.Payment
                        .Where(p => p.UserID == user.ID)
                        .OrderBy(p => p.Date)
                        .ToList();

                    var userCategories = userPayments
                        .GroupBy(p => p.Category)
                        .OrderBy(g => g.Key.Name)
                        .ToList();

                    foreach (var groupCategory in userCategories)
                    {
                        Excel.Range headerRange = worksheet.Range[worksheet.Cells[startRowIndex, 1], worksheet.Cells[startRowIndex, 5]];
                        headerRange.Merge();
                        headerRange.Value = groupCategory.Key.Name;
                        headerRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                        headerRange.Font.Italic = true;
                        startRowIndex++;

                        foreach (var payment in groupCategory)
                        {
                            worksheet.Cells[startRowIndex, 1] = payment.Date.ToString("dd.MM.yyyy");
                            worksheet.Cells[startRowIndex, 2] = payment.Name;
                            worksheet.Cells[startRowIndex, 3] = payment.Price;
                            (worksheet.Cells[startRowIndex, 3] as Excel.Range).NumberFormat = "0.00";
                            worksheet.Cells[startRowIndex, 4] = payment.Num;
                            worksheet.Cells[startRowIndex, 5].Formula = $"=C{startRowIndex}*D{startRowIndex}";
                            (worksheet.Cells[startRowIndex, 5] as Excel.Range).NumberFormat = "0.00";

                            grandTotal += payment.Price * payment.Num;
                            startRowIndex++;
                        }

                        Excel.Range sumRange = worksheet.Range[worksheet.Cells[startRowIndex, 1], worksheet.Cells[startRowIndex, 4]];
                        sumRange.Merge();
                        sumRange.Value = "ИТОГО:";
                        sumRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignRight;

                        worksheet.Cells[startRowIndex, 5].Formula = $"=SUM(E{startRowIndex - groupCategory.Count()}:E{startRowIndex - 1})";
                        sumRange.Font.Bold = true;
                        (worksheet.Cells[startRowIndex, 5] as Excel.Range).Font.Bold = true;
                        startRowIndex++;
                    }

                    Excel.Range rangeBorders = worksheet.Range[worksheet.Cells[1, 1], worksheet.Cells[startRowIndex - 1, 5]];
                    rangeBorders.Borders[Excel.XlBordersIndex.xlEdgeBottom].LineStyle =
                    rangeBorders.Borders[Excel.XlBordersIndex.xlEdgeLeft].LineStyle =
                    rangeBorders.Borders[Excel.XlBordersIndex.xlEdgeRight].LineStyle =
                    rangeBorders.Borders[Excel.XlBordersIndex.xlEdgeTop].LineStyle =
                    rangeBorders.Borders[Excel.XlBordersIndex.xlInsideHorizontal].LineStyle =
                    rangeBorders.Borders[Excel.XlBordersIndex.xlInsideVertical].LineStyle = Excel.XlLineStyle.xlContinuous;

                    worksheet.Columns.AutoFit();
                }

                Excel.Worksheet summarySheet = (Excel.Worksheet)workbook.Worksheets.Add(After: workbook.Worksheets[workbook.Worksheets.Count]);
                summarySheet.Name = "Общий итог";
                summarySheet.Cells[1, 1] = "Общий итог:";
                summarySheet.Cells[1, 2] = grandTotal;

                Excel.Range summaryRange = summarySheet.Range[summarySheet.Cells[1, 1], summarySheet.Cells[1, 2]];
                summaryRange.Font.Color = Excel.XlRgbColor.rgbRed;
                summaryRange.Font.Bold = true;
                summarySheet.Columns.AutoFit();

                application.Visible = true;
                MessageBox.Show("Данные экспортированы в Excel");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта в Excel: {ex.Message}");
            }
        }

        /// <summary>
        /// Экспортирует данные платежей в Word и PDF форматы
        /// </summary>
        private void BtnExportWord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var allUsers = _context.User.ToList();
                var allCategories = _context.Category.ToList();

                if (!allUsers.Any())
                {
                    MessageBox.Show("Нет пользователей для экспорта.");
                    return;
                }

                var application = new Word.Application();
                Word.Document document = application.Documents.Add();

                foreach (Word.Section section in document.Sections)
                {
                    Word.Range headerRange = section.Headers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary].Range;
                    headerRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    headerRange.Font.ColorIndex = Word.WdColorIndex.wdBlack;
                    headerRange.Font.Size = 10;
                    headerRange.Text = DateTime.Now.ToString("dd/MM/yyyy");
                }

                foreach (var user in allUsers)
                {
                    Word.Paragraph userParagraph = document.Paragraphs.Add();
                    Word.Range userRange = userParagraph.Range;
                    userRange.Text = user.FIO;
                    userParagraph.set_Style("Заголовок 1");
                    userRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    userRange.InsertParagraphAfter();
                    document.Paragraphs.Add();

                    Word.Paragraph tableParagraph = document.Paragraphs.Add();
                    Word.Range tableRange = tableParagraph.Range;
                    Word.Table paymentsTable = document.Tables.Add(tableRange, allCategories.Count() + 1, 2);
                    paymentsTable.Borders.InsideLineStyle = paymentsTable.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                    paymentsTable.Range.Cells.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;

                    Word.Range cellRange;
                    cellRange = paymentsTable.Cell(1, 1).Range;
                    cellRange.Text = "Категория";
                    cellRange = paymentsTable.Cell(1, 2).Range;
                    cellRange.Text = "Сумма расходов";

                    paymentsTable.Rows[1].Range.Font.Name = "Times New Roman";
                    paymentsTable.Rows[1].Range.Font.Size = 14;
                    paymentsTable.Rows[1].Range.Bold = 1;
                    paymentsTable.Rows[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                    for (int i = 0; i < allCategories.Count(); i++)
                    {
                        var currentCategory = allCategories[i];
                        cellRange = paymentsTable.Cell(i + 2, 1).Range;
                        cellRange.Text = currentCategory.Name;
                        cellRange.Font.Name = "Times New Roman";
                        cellRange.Font.Size = 12;

                        var sum = _context.Payment.ToList()
                            .Where(u => u.UserID == user.ID && u.CategoryID == currentCategory.ID)
                            .Sum(u => u.Num * u.Price);

                        cellRange = paymentsTable.Cell(i + 2, 2).Range;
                        cellRange.Text = sum.ToString("N2") + " руб.";
                        cellRange.Font.Name = "Times New Roman";
                        cellRange.Font.Size = 12;
                    }

                    document.Paragraphs.Add();

                    var maxPayment = _context.Payment
                        .Where(p => p.UserID == user.ID)
                        .OrderByDescending(u => u.Price * u.Num)
                        .FirstOrDefault();

                    if (maxPayment != null)
                    {
                        Word.Paragraph maxPaymentParagraph = document.Paragraphs.Add();
                        Word.Range maxPaymentRange = maxPaymentParagraph.Range;
                        maxPaymentRange.Text = $"Самый дорогостоящий платеж - {maxPayment.Name} за {(maxPayment.Price * maxPayment.Num).ToString("N2")} руб. от {maxPayment.Date.ToString("dd.MM.yyyy")}";
                        maxPaymentParagraph.set_Style("Заголовок 2");
                        maxPaymentRange.Font.Color = Word.WdColor.wdColorDarkRed;
                        maxPaymentRange.InsertParagraphAfter();
                    }

                    var minPayment = _context.Payment
                        .Where(p => p.UserID == user.ID)
                        .OrderBy(u => u.Price * u.Num)
                        .FirstOrDefault();

                    if (minPayment != null)
                    {
                        Word.Paragraph minPaymentParagraph = document.Paragraphs.Add();
                        Word.Range minPaymentRange = minPaymentParagraph.Range;
                        minPaymentRange.Text = $"Самый дешевый платеж - {minPayment.Name} за {(minPayment.Price * minPayment.Num).ToString("N2")} руб. от {minPayment.Date.ToString("dd.MM.yyyy")}";
                        minPaymentParagraph.set_Style("Заголовок 2");
                        minPaymentRange.Font.Color = Word.WdColor.wdColorDarkGreen;
                        minPaymentRange.InsertParagraphAfter();
                    }

                    if (user != allUsers.LastOrDefault())
                        document.Words.Last.InsertBreak(Word.WdBreakType.wdPageBreak);
                }

                foreach (Word.Section section in document.Sections)
                {
                    Word.HeaderFooter footer = section.Footers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary];
                    footer.PageNumbers.Add(Word.WdPageNumberAlignment.wdAlignPageNumberCenter);
                }

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string docxPath = Path.Combine(desktopPath, "Payments.docx");
                string pdfPath = Path.Combine(desktopPath, "Payments.pdf");

                try
                {
                    document.SaveAs2(docxPath);
                    document.ExportAsFixedFormat(pdfPath, Word.WdExportFormat.wdExportFormatPDF);
                }
                catch (Exception saveEx)
                {
                    string altPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    docxPath = Path.Combine(altPath, "Payments.docx");
                    pdfPath = Path.Combine(altPath, "Payments.pdf");
                    document.SaveAs2(docxPath);
                    document.ExportAsFixedFormat(pdfPath, Word.WdExportFormat.wdExportFormatPDF);
                    MessageBox.Show($"Файлы сохранены в альтернативном пути: {altPath}");
                }

                application.Visible = true;
                MessageBox.Show($"Данные экспортированы в Word и PDF. Файлы сохранены на рабочем столе: {docxPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта в Word: {ex.Message}");
            }
        }
    }
}