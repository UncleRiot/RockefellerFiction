using ClosedXML.Excel;

namespace RockefellerFiction;

public static class ExcelExportService
{
 private const string TitleFill = "DDEBF7";
 private const string SectionFill = "EAF2F8";
 private const string BasisFill = "E2F0D9";
 private const string StressFill = "FCE4D6";
 private const string HeaderFill = "D9EAF7";
 private const string ExpenseFill = "FCE8E6";
 private const string IncomeFill = "E2F0D9";
 private const string TaxFill = "FCE8E6";
 private const string WealthFill = "EAF2F8";
 private const string StatusFill = "F2F2F2";
 private const string BorderColor = "B7C9D6";
 private const string TextColor = "1F1F1F";
 private const string MutedTextColor = "666666";
 private const string SuccessColor = "008A45";
 private const string DangerColor = "C62828";
 private const string BasisLineColor = "2F75B5";
 private const string StressLineColor = "BF9000";

 private const string EuroFormat =
  "#,##0 \"€\";[Red](#,##0 \"€\");-";
 private const string PercentFormat =
  "0.0%;[Red](0.0%);-";

 public static void Export(
  string path,
  PlannerSettings settings,
  ProjectionResult baseResult,
  ProjectionResult stressResult,
  string baseStatus,
  string baseText,
  string stressStatus,
  string stressText,
  string strategyText,
  string recommendationText,
  string sensitivityText)
 {
  using var workbook = new XLWorkbook();

  BuildOverviewSheet(
   workbook,
   settings,
   baseResult,
   stressResult,
   baseStatus,
   baseText,
   stressStatus,
   stressText,
   strategyText,
   recommendationText,
   sensitivityText);

  BuildBasisSheet(workbook, settings, baseResult);
  BuildStressSheet(workbook, settings, stressResult);
  BuildFundingSheet(workbook, settings, baseResult);
  BuildAssetsSheet(workbook, baseResult);
  BuildReserveSheet(workbook, baseResult);

  workbook.SaveAs(path);
 }

 private static void BuildOverviewSheet(
  XLWorkbook workbook,
  PlannerSettings settings,
  ProjectionResult baseResult,
  ProjectionResult stressResult,
  string baseStatus,
  string baseText,
  string stressStatus,
  string stressText,
  string strategyText,
  string recommendationText,
  string sensitivityText)
 {
  IXLWorksheet sheet = workbook.Worksheets.Add("Übersicht");
  PrepareSheet(sheet);

  sheet.Column(1).Width = 14;
  sheet.Column(2).Width = 18;
  sheet.Column(3).Width = 18;
  sheet.Column(4).Width = 4;
  sheet.Column(5).Width = 18;
  sheet.Column(6).Width = 18;
  sheet.Column(7).Width = 18;
  sheet.Column(8).Width = 4;
  sheet.Column(9).Width = 18;
  sheet.Column(10).Width = 18;
  sheet.Column(11).Width = 18;
  sheet.Column(12).Width = 18;

  MergeAndStyleTitle(sheet, "A1:L2", "Ergebnisse");

  WriteCard(
   sheet,
   "A4:F9",
   "Basisszenario",
   baseStatus,
   baseText,
   baseResult.ReachesPlanEnd);

  WriteCard(
   sheet,
   "G4:L9",
   "Stressszenario",
   stressStatus,
   stressText,
   stressResult.ReachesPlanEnd);

  MergeAndStyleSection(sheet, "A11:L11", "Strategie");
  IXLRange strategyRange = sheet.Range("A12:L13");
  strategyRange.Merge();
  strategyRange.FirstCell().Value = strategyText;
  StyleBodyRange(strategyRange);
  strategyRange.Style.Alignment.WrapText = true;

  MergeAndStyleSection(sheet, "A15:C15", "Vermögensentwicklung");
  sheet.Cell("A16").Value = "Jahr";
  sheet.Cell("B16").Value = "Basis";
  sheet.Cell("C16").Value = "Stress";
  StyleTableHeader(sheet.Range("A16:C16"), HeaderFill);

  int chartRow = 17;
  int yearCount = Math.Max(baseResult.Years.Count, stressResult.Years.Count);
  for (int index = 0; index < yearCount; index++)
  {
   YearResult? baseYear =
    index < baseResult.Years.Count ? baseResult.Years[index] : null;
   YearResult? stressYear =
    index < stressResult.Years.Count ? stressResult.Years[index] : null;

   sheet.Cell(chartRow + index, 1).Value =
    baseYear?.Year ?? stressYear?.Year ?? 0;
   sheet.Cell(chartRow + index, 2).Value =
    baseYear?.TotalPortfolioEnd ?? 0m;
   sheet.Cell(chartRow + index, 3).Value =
    stressYear?.TotalPortfolioEnd ?? 0m;

   sheet.Cell(chartRow + index, 2).Style.NumberFormat.Format = EuroFormat;
   sheet.Cell(chartRow + index, 3).Style.NumberFormat.Format = EuroFormat;
  }

  if (yearCount > 0)
  {
   IXLRange capitalRange =
    sheet.Range(chartRow, 1, chartRow + yearCount - 1, 3);
   StyleDataRange(capitalRange);
   capitalRange.Column(2).Style.Font.FontColor =
    XLColor.FromHtml("#" + BasisLineColor);
   capitalRange.Column(3).Style.Font.FontColor =
    XLColor.FromHtml("#" + StressLineColor);
  }

  MergeAndStyleSection(sheet, "E15:L15", "Handlungsempfehlung");
  IXLRange recommendationRange = sheet.Range("E16:L24");
  recommendationRange.Merge();
  recommendationRange.FirstCell().Value = recommendationText;
  StyleBodyRange(recommendationRange);
  recommendationRange.Style.Alignment.WrapText = true;
  recommendationRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

  MergeAndStyleSection(sheet, "E26:L26", "Ausgaben ±10 %");
  IXLRange sensitivityRange = sheet.Range("E27:L30");
  sensitivityRange.Merge();
  sensitivityRange.FirstCell().Value = sensitivityText;
  StyleBodyRange(sensitivityRange);
  sensitivityRange.Style.Alignment.WrapText = true;
  sensitivityRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

  MergeAndStyleSection(sheet, "E32:L32", "Planungsdaten");
  WriteLabelValue(sheet, 33, 5, "Simulationsstart", settings.PlanningYear);
  WriteLabelValue(sheet, 34, 5, "Startvermögen", settings.StartCapital, true);
  WriteLabelValue(sheet, 35, 5, "Monatliche Lebenshaltung", settings.MonthlyLivingCosts, true);
  WriteLabelValue(sheet, 36, 5, "Inflation", settings.InflationRate, false, true);

  sheet.SheetView.FreezeRows(2);
 }

 private static void BuildBasisSheet(
  XLWorkbook workbook,
  PlannerSettings settings,
  ProjectionResult result)
 {
  IXLWorksheet sheet = workbook.Worksheets.Add("Basis Jahresübersicht");
  PrepareSheet(sheet);

  ExcelColumn[] columns =
  [
   new("Jahr", y => y.Year, ExcelValueKind.Integer),
   new("Alter 1", y => y.Age1, ExcelValueKind.Integer),
   new("Alter 2", y => y.Age2, ExcelValueKind.Integer, true),
   new("Gesamtbedarf p.a.", y => y.TotalAnnualNeed, ExcelValueKind.Currency),
   new("davon Lebenshaltung p.a.", y => y.LivingCosts, ExcelValueKind.Currency),
   new("Inflationsanstieg p.a.", y => y.LivingCostIncrease, ExcelValueKind.Currency),
   new("davon freiwillige GKV/Pflege gesamt p.a.", y => y.HealthCareCosts, ExcelValueKind.Currency),
   new("freiw. GKV/Pflege P1 mtl.", y => y.HealthCareCostsPerson1Monthly, ExcelValueKind.Currency),
   new("freiw. GKV/Pflege P2 mtl.", y => y.HealthCareCostsPerson2Monthly, ExcelValueKind.Currency, true),
   new("Rente brutto p.a.", y => y.PensionGross, ExcelValueKind.Currency),
   new("Rente P1 brutto p.a.", y => y.PensionPerson1Gross, ExcelValueKind.Currency),
   new("Rente P2 brutto p.a.", y => y.PensionPerson2Gross, ExcelValueKind.Currency, true),
   new("GKV/Pflege aus Rente p.a.", y => y.PensionHealthAndCareDeductions, ExcelValueKind.Currency),
   new("Rente netto p.a.", y => y.PensionNet, ExcelValueKind.Currency),
   new("Nettoeinkommen P2 verwendet p.a.", y => y.FundingFromPerson2Income, ExcelValueKind.Currency, true),
   new("Dividenden brutto p.a.", y => y.DividendsGross, ExcelValueKind.Currency),
   new("Zinsen brutto p.a.", y => y.InterestGross, ExcelValueKind.Currency),
   new("Entnahme p.a.", y => y.WithdrawnFromCapital, ExcelValueKind.Currency),
   new("Steuern Kapital p.a.", y => y.TaxesOnCapital, ExcelValueKind.Currency),
   new("Vermögen Ende", y => y.TotalPortfolioEnd, ExcelValueKind.Currency),
   new("Status", y => y.YearStatus, ExcelValueKind.Text)
  ];

  string[] groups =
  [
   "Planungsjahr & Alter", "Planungsjahr & Alter", "Planungsjahr & Alter",
   "Ausgaben & Vorsorge", "Ausgaben & Vorsorge", "Ausgaben & Vorsorge",
   "Ausgaben & Vorsorge", "Ausgaben & Vorsorge", "Ausgaben & Vorsorge",
   "Gesetzliche Rente", "Gesetzliche Rente", "Gesetzliche Rente",
   "Gesetzliche Rente", "Gesetzliche Rente",
   "Einnahmen & Mittelzufluss", "Einnahmen & Mittelzufluss",
   "Einnahmen & Mittelzufluss", "Einnahmen & Mittelzufluss",
   "Steuern", "Vermögen", "Planstatus"
  ];

  string[] fills =
  [
   HeaderFill, HeaderFill, HeaderFill,
   ExpenseFill, ExpenseFill, ExpenseFill, ExpenseFill, ExpenseFill, ExpenseFill,
   IncomeFill, IncomeFill, IncomeFill, IncomeFill, IncomeFill,
   IncomeFill, IncomeFill, IncomeFill, IncomeFill,
   TaxFill, WealthFill, StatusFill
  ];

  BuildYearTable(
   sheet,
   "Basis: Jahresübersicht",
   "Gesamtbedarf p.a. enthält Lebenshaltung, freiwillige GKV/Pflege, Haus-Instandhaltung und ggf. Auto-Ersatz.",
   result.Years,
   columns,
   groups,
   fills,
   settings.HouseholdPersonCount == 1);
 }

 private static void BuildStressSheet(
  XLWorkbook workbook,
  PlannerSettings settings,
  ProjectionResult result)
 {
  IXLWorksheet sheet = workbook.Worksheets.Add("Stress Jahresübersicht");
  PrepareSheet(sheet);

  ExcelColumn[] columns =
  [
   new("Jahr", y => y.Year, ExcelValueKind.Integer),
   new("Alter 1", y => y.Age1, ExcelValueKind.Integer),
   new("Alter 2", y => y.Age2, ExcelValueKind.Integer, true),
   new("Gesamtbedarf p.a.", y => y.TotalAnnualNeed, ExcelValueKind.Currency),
   new("davon Lebenshaltung p.a.", y => y.LivingCosts, ExcelValueKind.Currency),
   new("davon freiwillige GKV/Pflege gesamt p.a.", y => y.HealthCareCosts, ExcelValueKind.Currency),
   new("freiw. GKV/Pflege P1 mtl.", y => y.HealthCareCostsPerson1Monthly, ExcelValueKind.Currency),
   new("freiw. GKV/Pflege P2 mtl.", y => y.HealthCareCostsPerson2Monthly, ExcelValueKind.Currency, true),
   new("GKV/Pflege aus Rente p.a.", y => y.PensionHealthAndCareDeductions, ExcelValueKind.Currency),
   new("Rente netto p.a.", y => y.PensionNet, ExcelValueKind.Currency),
   new("Nettoeinkommen P2 verwendet p.a.", y => y.FundingFromPerson2Income, ExcelValueKind.Currency, true),
   new("Dividenden brutto p.a.", y => y.DividendsGross, ExcelValueKind.Currency),
   new("Entnahme p.a.", y => y.WithdrawnFromCapital, ExcelValueKind.Currency),
   new("Steuern Kapital p.a.", y => y.TaxesOnCapital, ExcelValueKind.Currency),
   new("Reserve Ist", y => y.ReserveActual, ExcelValueKind.Currency),
   new("Vermögen Ende", y => y.TotalPortfolioEnd, ExcelValueKind.Currency),
   new("Status", y => y.YearStatus, ExcelValueKind.Text)
  ];

  string[] groups =
  [
   "Planungsjahr & Alter", "Planungsjahr & Alter", "Planungsjahr & Alter",
   "Ausgaben & Vorsorge", "Ausgaben & Vorsorge", "Ausgaben & Vorsorge",
   "Ausgaben & Vorsorge", "Ausgaben & Vorsorge",
   "Gesetzliche Rente", "Gesetzliche Rente",
   "Einnahmen & Mittelzufluss", "Einnahmen & Mittelzufluss", "Einnahmen & Mittelzufluss",
   "Steuern", "Reserve", "Vermögen", "Planstatus"
  ];

  string[] fills =
  [
   HeaderFill, HeaderFill, HeaderFill,
   ExpenseFill, ExpenseFill, ExpenseFill, ExpenseFill, ExpenseFill,
   IncomeFill, IncomeFill,
   IncomeFill, IncomeFill, IncomeFill,
   TaxFill, WealthFill, WealthFill, StatusFill
  ];

  BuildYearTable(
   sheet,
   "Stress: Jahresübersicht",
   "Stresslauf mit den aktuell eingestellten Stressannahmen.",
   result.Years,
   columns,
   groups,
   fills,
   settings.HouseholdPersonCount == 1);
 }

 private static void BuildFundingSheet(
  XLWorkbook workbook,
  PlannerSettings settings,
  ProjectionResult result)
 {
  IXLWorksheet sheet = workbook.Worksheets.Add("Finanzierung");
  PrepareSheet(sheet);

  ExcelColumn[] columns =
  [
   new("Jahr", y => y.Year, ExcelValueKind.Integer),
   new("Gesamtbedarf p.a.", y => y.TotalAnnualNeed, ExcelValueKind.Currency),
   new("aus Rente", y => y.FundingFromPension, ExcelValueKind.Currency),
   new("Nettoeinkommen P2", y => y.FundingFromPerson2Income, ExcelValueKind.Currency, true),
   new("aus Dividenden", y => y.FundingFromDividends, ExcelValueKind.Currency),
   new("sonstige Einnahmen", y => y.FundingFromOtherIncome, ExcelValueKind.Currency),
   new("aus Vermögen", y => y.FundingFromCapital, ExcelValueKind.Currency),
   new("Finanzierungslücke", y => y.FundingGap, ExcelValueKind.Currency),
   new("Kapitalsteuer", y => y.TaxesOnCapital, ExcelValueKind.Currency)
  ];

  BuildSimpleYearTable(
   sheet,
   "Finanzierung der Ausgaben",
   result.Years,
   columns,
   settings.HouseholdPersonCount == 1);
 }

 private static void BuildAssetsSheet(
  XLWorkbook workbook,
  ProjectionResult result)
 {
  IXLWorksheet sheet = workbook.Worksheets.Add("Anlageklassen");
  PrepareSheet(sheet);

  ExcelColumn[] columns =
  [
   new("Jahr", y => y.Year, ExcelValueKind.Integer),
   new("Tages/Festgeld", y => y.CashEnd, ExcelValueKind.Currency),
   new("Welt-ETF", y => y.WorldEtfEnd, ExcelValueKind.Currency),
   new("Dividenden-ETF", y => y.DividendEtfEnd, ExcelValueKind.Currency),
   new("Dividenden-Aktien", y => y.DividendStocksEnd, ExcelValueKind.Currency),
   new("Zins Tages/Festgeld", y => y.CashReturnContribution, ExcelValueKind.Currency),
   new("Zins netto", y => y.CashNetIncome, ExcelValueKind.Currency),
   new("Welt-ETF Kurs", y => y.WorldPriceReturnContribution, ExcelValueKind.Currency),
   new("Welt-ETF Ausschüttung", y => y.WorldDistributionContribution, ExcelValueKind.Currency),
   new("Welt-ETF Ausschüttung netto", y => y.WorldNetIncome, ExcelValueKind.Currency),
   new("Div.-ETF Kurs", y => y.DividendEtfPriceReturnContribution, ExcelValueKind.Currency),
   new("Div.-ETF Ausschüttung", y => y.DividendEtfDistributionContribution, ExcelValueKind.Currency),
   new("Div.-ETF Ausschüttung netto", y => y.DividendEtfNetIncome, ExcelValueKind.Currency),
   new("Div.-Aktien Kurs", y => y.DividendStocksPriceReturnContribution, ExcelValueKind.Currency),
   new("Div.-Aktien Dividende", y => y.DividendStocksDistributionContribution, ExcelValueKind.Currency),
   new("Div.-Aktien Dividende netto", y => y.DividendStocksNetIncome, ExcelValueKind.Currency)
  ];

  BuildSimpleYearTable(
   sheet,
   "Anlageklassen",
   result.Years,
   columns,
   false);
 }

 private static void BuildReserveSheet(
  XLWorkbook workbook,
  ProjectionResult result)
 {
  IXLWorksheet sheet = workbook.Worksheets.Add("Reserve & Rücklagen");
  PrepareSheet(sheet);

  ExcelColumn[] columns =
  [
   new("Jahr", y => y.Year, ExcelValueKind.Integer),
   new("Haus-Instandhaltung p.a.", y => y.HouseMaintenanceExpense, ExcelValueKind.Currency),
   new("Auto-Ersatz Ausgabe", y => y.CarReplacementExpense, ExcelValueKind.Currency),
   new("Gesundheit", y => y.HealthReserveTarget, ExcelValueKind.Currency),
   new("Reisen", y => y.TravelReserveTarget, ExcelValueKind.Currency),
   new("Sonstiges", y => y.OtherReserveTarget, ExcelValueKind.Currency),
   new("Reserve Soll gesamt", y => y.ReserveTarget, ExcelValueKind.Currency),
   new("Reserve Ist", y => y.ReserveActual, ExcelValueKind.Currency),
   new("Status", y => y.YearStatus, ExcelValueKind.Text)
  ];

  BuildSimpleYearTable(
   sheet,
   "Reserve & Rücklagen",
   result.Years,
   columns,
   false);
 }

 private static void BuildYearTable(
  IXLWorksheet sheet,
  string title,
  string subtitle,
  IReadOnlyList<YearResult> years,
  IReadOnlyList<ExcelColumn> allColumns,
  IReadOnlyList<string> allGroups,
  IReadOnlyList<string> allFills,
  bool hidePerson2Columns)
 {
  List<int> visibleIndexes = [];

  for (int index = 0; index < allColumns.Count; index++)
  {
   if (hidePerson2Columns && allColumns[index].Person2Only)
    continue;

   visibleIndexes.Add(index);
  }

  int columnCount = visibleIndexes.Count;
  MergeAndStyleTitle(sheet, 1, 1, 1, columnCount, title);
  sheet.Range(2, 1, 2, columnCount).Merge();
  sheet.Cell(2, 1).Value = subtitle;
  sheet.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#" + MutedTextColor);
  sheet.Cell(2, 1).Style.Alignment.WrapText = true;

  int groupRow = 4;
  int headerRow = 5;
  int outputColumn = 1;

  while (outputColumn <= columnCount)
  {
   int originalIndex = visibleIndexes[outputColumn - 1];
   string group = allGroups[originalIndex];
   string fill = allFills[originalIndex];

   int endOutputColumn = outputColumn;
   while (endOutputColumn < columnCount)
   {
    int nextOriginalIndex = visibleIndexes[endOutputColumn];
    if (!string.Equals(allGroups[nextOriginalIndex], group, StringComparison.Ordinal))
     break;

    endOutputColumn++;
   }

   IXLRange groupRange =
    sheet.Range(groupRow, outputColumn, groupRow, endOutputColumn);
   groupRange.Merge();
   groupRange.FirstCell().Value = group;
   StyleTableHeader(groupRange, fill);

   outputColumn = endOutputColumn + 1;
  }

  List<ExcelColumn> visibleColumns =
   visibleIndexes.Select(index => allColumns[index]).ToList();

  WriteHeaderRow(sheet, headerRow, visibleColumns, HeaderFill);
  WriteRows(sheet, headerRow + 1, years, visibleColumns);
  FormatYearTable(sheet, headerRow, years.Count, visibleColumns.Count);
 }

 private static void BuildSimpleYearTable(
  IXLWorksheet sheet,
  string title,
  IReadOnlyList<YearResult> years,
  IReadOnlyList<ExcelColumn> allColumns,
  bool hidePerson2Columns)
 {
  List<ExcelColumn> columns =
   allColumns
    .Where(column => !(hidePerson2Columns && column.Person2Only))
    .ToList();

  MergeAndStyleTitle(sheet, 1, 1, 1, columns.Count, title);
  WriteHeaderRow(sheet, 3, columns, HeaderFill);
  WriteRows(sheet, 4, years, columns);
  FormatYearTable(sheet, 3, years.Count, columns.Count);
 }

 private static void WriteHeaderRow(
  IXLWorksheet sheet,
  int row,
  IReadOnlyList<ExcelColumn> columns,
  string fill)
 {
  for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
  {
   IXLCell cell = sheet.Cell(row, columnIndex + 1);
   cell.Value = columns[columnIndex].Header;
  }

  StyleTableHeader(sheet.Range(row, 1, row, columns.Count), fill);
 }

 private static void WriteRows(
  IXLWorksheet sheet,
  int firstRow,
  IReadOnlyList<YearResult> years,
  IReadOnlyList<ExcelColumn> columns)
 {
  for (int rowIndex = 0; rowIndex < years.Count; rowIndex++)
  {
   YearResult year = years[rowIndex];

   for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
   {
    ExcelColumn column = columns[columnIndex];
    IXLCell cell = sheet.Cell(firstRow + rowIndex, columnIndex + 1);
    SetCellValue(cell, column.Value(year));
    ApplyNumberFormat(cell, column.Kind);

    if (column.Kind == ExcelValueKind.Text &&
        string.Equals(column.Header, "Status", StringComparison.Ordinal))
    {
     ApplyStatusFont(cell, year.YearStatus);
    }
   }
  }

  if (years.Count > 0)
  {
   IXLRange dataRange =
    sheet.Range(firstRow, 1, firstRow + years.Count - 1, columns.Count);
   StyleDataRange(dataRange);
  }
 }

 private static void FormatYearTable(
  IXLWorksheet sheet,
  int headerRow,
  int dataRowCount,
  int columnCount)
 {
  sheet.SheetView.FreezeRows(headerRow);
  sheet.SheetView.FreezeColumns(1);

  for (int column = 1; column <= columnCount; column++)
  {
   sheet.Column(column).AdjustToContents(1, headerRow + dataRowCount);
   double width = sheet.Column(column).Width;
   sheet.Column(column).Width = Math.Min(Math.Max(width + 2d, 10d), 28d);
  }

  if (dataRowCount > 0)
  {
   sheet.Range(
    headerRow,
    1,
    headerRow + dataRowCount,
    columnCount).Style.Alignment.WrapText = true;
  }
 }

 private static void WriteCard(
  IXLWorksheet sheet,
  string rangeAddress,
  string title,
  string status,
  string body,
  bool success)
 {
  IXLRange range = sheet.Range(rangeAddress);
  range.Style.Fill.BackgroundColor =
   XLColor.FromHtml("#" + (success ? BasisFill : StressFill));
  range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
  range.Style.Border.OutsideBorderColor = XLColor.FromHtml("#" + BorderColor);

  int firstRow = range.RangeAddress.FirstAddress.RowNumber;
  int lastRow = range.RangeAddress.LastAddress.RowNumber;
  int firstColumn = range.RangeAddress.FirstAddress.ColumnNumber;
  int lastColumn = range.RangeAddress.LastAddress.ColumnNumber;

  IXLRange titleRange =
   sheet.Range(firstRow, firstColumn, firstRow, lastColumn);
  titleRange.Merge();
  titleRange.FirstCell().Value = title;
  titleRange.FirstCell().Style.Font.Bold = true;
  titleRange.FirstCell().Style.Font.FontSize = 14;
  titleRange.FirstCell().Style.Font.FontColor = XLColor.FromHtml("#" + TextColor);

  IXLRange statusRange =
   sheet.Range(firstRow + 1, firstColumn, firstRow + 1, lastColumn);
  statusRange.Merge();
  statusRange.FirstCell().Value = status;
  statusRange.FirstCell().Style.Font.Bold = true;
  statusRange.FirstCell().Style.Font.FontSize = 18;
  statusRange.FirstCell().Style.Font.FontColor =
   XLColor.FromHtml("#" + (success ? SuccessColor : DangerColor));

  IXLRange bodyRange =
   sheet.Range(firstRow + 2, firstColumn, lastRow, lastColumn);
  bodyRange.Merge();
  bodyRange.FirstCell().Value = body;
  bodyRange.Style.Alignment.WrapText = true;
  bodyRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
  bodyRange.Style.Font.FontColor = XLColor.FromHtml("#" + TextColor);
 }

 private static void WriteLabelValue(
  IXLWorksheet sheet,
  int row,
  int firstColumn,
  string label,
  decimal value,
  bool currency = false,
  bool percentage = false)
 {
  sheet.Range(row, firstColumn, row, firstColumn + 2).Merge();
  sheet.Cell(row, firstColumn).Value = label;
  sheet.Cell(row, firstColumn).Style.Font.Bold = true;

  sheet.Range(row, firstColumn + 3, row, firstColumn + 5).Merge();
  sheet.Cell(row, firstColumn + 3).Value = value;

  if (currency)
   sheet.Cell(row, firstColumn + 3).Style.NumberFormat.Format = EuroFormat;
  else if (percentage)
   sheet.Cell(row, firstColumn + 3).Style.NumberFormat.Format = PercentFormat;
 }

 private static void WriteLabelValue(
  IXLWorksheet sheet,
  int row,
  int firstColumn,
  string label,
  int value)
 {
  sheet.Range(row, firstColumn, row, firstColumn + 2).Merge();
  sheet.Cell(row, firstColumn).Value = label;
  sheet.Cell(row, firstColumn).Style.Font.Bold = true;

  sheet.Range(row, firstColumn + 3, row, firstColumn + 5).Merge();
  sheet.Cell(row, firstColumn + 3).Value = value;
 }

 private static void SetCellValue(IXLCell cell, object value)
 {
  switch (value)
  {
   case int intValue:
    cell.Value = intValue;
    break;
   case decimal decimalValue:
    cell.Value = decimalValue;
    break;
   case double doubleValue:
    cell.Value = doubleValue;
    break;
   case bool boolValue:
    cell.Value = boolValue;
    break;
   case string stringValue:
    cell.Value = stringValue;
    break;
   default:
    cell.Value = value?.ToString() ?? "";
    break;
  }
 }

 private static void ApplyNumberFormat(
  IXLCell cell,
  ExcelValueKind kind)
 {
  if (kind == ExcelValueKind.Currency)
   cell.Style.NumberFormat.Format = EuroFormat;
  else if (kind == ExcelValueKind.Percent)
   cell.Style.NumberFormat.Format = PercentFormat;
  else if (kind == ExcelValueKind.Integer)
   cell.Style.NumberFormat.Format = "0";
 }

 private static void ApplyStatusFont(
  IXLCell cell,
  string status)
 {
  cell.Style.Font.Bold = true;
  cell.Style.Font.FontColor =
   XLColor.FromHtml(
    "#" + (string.Equals(status, "Grün", StringComparison.OrdinalIgnoreCase)
     ? SuccessColor
     : DangerColor));
 }

 private static void MergeAndStyleTitle(
  IXLWorksheet sheet,
  string rangeAddress,
  string title)
 {
  IXLRange range = sheet.Range(rangeAddress);
  range.Merge();
  range.FirstCell().Value = title;
  range.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + TitleFill);
  range.Style.Font.Bold = true;
  range.Style.Font.FontSize = 20;
  range.Style.Font.FontColor = XLColor.FromHtml("#" + TextColor);
  range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
 }

 private static void MergeAndStyleTitle(
  IXLWorksheet sheet,
  int firstRow,
  int firstColumn,
  int lastRow,
  int lastColumn,
  string title)
 {
  IXLRange range =
   sheet.Range(firstRow, firstColumn, lastRow, lastColumn);
  range.Merge();
  range.FirstCell().Value = title;
  range.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + TitleFill);
  range.Style.Font.Bold = true;
  range.Style.Font.FontSize = 18;
  range.Style.Font.FontColor = XLColor.FromHtml("#" + TextColor);
 }

 private static void MergeAndStyleSection(
  IXLWorksheet sheet,
  string rangeAddress,
  string title)
 {
  IXLRange range = sheet.Range(rangeAddress);
  range.Merge();
  range.FirstCell().Value = title;
  range.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + SectionFill);
  range.Style.Font.Bold = true;
  range.Style.Font.FontSize = 12;
  range.Style.Font.FontColor = XLColor.FromHtml("#" + TextColor);
  range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
  range.Style.Border.BottomBorderColor = XLColor.FromHtml("#" + BorderColor);
 }

 private static void StyleTableHeader(
  IXLRange range,
  string fill)
 {
  range.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + fill);
  range.Style.Font.Bold = true;
  range.Style.Font.FontColor = XLColor.FromHtml("#" + TextColor);
  range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
  range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
  range.Style.Alignment.WrapText = true;
  range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
  range.Style.Border.BottomBorderColor = XLColor.FromHtml("#" + BorderColor);
 }

 private static void StyleBodyRange(IXLRange range)
 {
  range.Style.Fill.BackgroundColor = XLColor.White;
  range.Style.Font.FontColor = XLColor.FromHtml("#" + TextColor);
  range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
  range.Style.Border.OutsideBorderColor = XLColor.FromHtml("#" + BorderColor);
 }

 private static void StyleDataRange(IXLRange range)
 {
  range.Style.Font.FontColor = XLColor.FromHtml("#" + TextColor);
  range.Style.Border.BottomBorder = XLBorderStyleValues.Hair;
  range.Style.Border.BottomBorderColor = XLColor.FromHtml("#D9E2F3");
  range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
 }

 private static void PrepareSheet(IXLWorksheet sheet)
 {
  sheet.ShowGridLines = false;
  sheet.Style.Font.FontName = "Aptos";
  sheet.Style.Font.FontSize = 10;
  sheet.Style.Font.FontColor = XLColor.FromHtml("#" + TextColor);
  sheet.Style.Fill.BackgroundColor = XLColor.White;
 }

 private sealed record ExcelColumn(
  string Header,
  Func<YearResult, object> Value,
  ExcelValueKind Kind,
  bool Person2Only = false);

 private enum ExcelValueKind
 {
  Text,
  Integer,
  Currency,
  Percent
 }
}
