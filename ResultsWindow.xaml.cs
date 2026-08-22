using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RockefellerFiction;

public partial class ResultsWindow : Window
{
 private PlannerSettings _settings;
 private StrategyAllocation _allocation;
 private ProjectionResult _baseResult;
 private ProjectionResult _stressResult;

 public ResultsWindow(
  PlannerSettings settings,
  StrategyAllocation allocation,
  ProjectionResult baseResult,
  ProjectionResult stressResult)
 {
  InitializeComponent();
  Background = (Brush)FindResource("BgBrush");
  Foreground = (Brush)FindResource("TextBrush");
  UiLayout.ApplyResultsWindow(this);
  WindowBehavior.ApplyDarkTitleBar(this);

  _settings = settings;
  _allocation = allocation;
  _baseResult = baseResult;
  _stressResult = stressResult;

  UpdateResults(settings, allocation, baseResult, stressResult);

  ApplyGroupHeaderColors();

  CapitalChart.SizeChanged += (_, _) => DrawCapitalChart();
  Loaded += (_, _) =>
  {
   DrawCapitalChart();
   UpdateYearOverviewLayout();
  };

  YearGrid.LayoutUpdated += (_, _) => UpdateYearOverviewLayout();
  StressYearGrid.LayoutUpdated += (_, _) => UpdateYearOverviewLayout();
 }

 public void UpdateResults(
  PlannerSettings settings,
  StrategyAllocation allocation,
  ProjectionResult baseResult,
  ProjectionResult stressResult)
 {
  _settings = settings;
  _allocation = allocation;
  _baseResult = baseResult;
  _stressResult = stressResult;

  BaseStatus.Text = baseResult.OverallStatus;
  StressStatus.Text = stressResult.OverallStatus;

  BaseStatus.Foreground = baseResult.ReachesPlanEnd
   ? (Brush)FindResource("SuccessBrush")
   : (Brush)FindResource("DangerBrush");

  StressStatus.Foreground = stressResult.ReachesPlanEnd
   ? (Brush)FindResource("SuccessBrush")
   : (Brush)FindResource("DangerBrush");

  BaseText.Text = BuildText(baseResult, "Basis");
  StressText.Text = BuildText(stressResult, "Stress");

  string recommended = StrategyService.Recommend(settings);
  StrategyText.Text =
   $"Gewählt: {settings.Strategy} | Empfohlen: {recommended} | " +
   $"Aufteilung: Tages/Festgeld {_allocation.Cash:P0}, Welt-ETF {_allocation.WorldEtf:P0}, " +
   $"Div.-ETF {_allocation.DividendEtf:P0}, Div.-Aktien {_allocation.DividendStocks:P0}";

  YearGrid.ItemsSource = baseResult.Years;
  StressYearGrid.ItemsSource = stressResult.Years;
  FundingGrid.ItemsSource = baseResult.Years;
  AssetGrid.ItemsSource = baseResult.Years;
  ReserveGrid.ItemsSource = baseResult.Years;

  ApplyHouseholdVisibility();

  RecommendationText.Text =
   RecommendationService.Build(settings, allocation, baseResult, stressResult);

  SensitivityText.Text = BuildSensitivityText(settings, allocation);

  DrawCapitalChart();
  UpdateYearOverviewLayout();
 }

 private void ApplyHouseholdVisibility()
 {
  Visibility person2Visibility =
   _settings.HouseholdPersonCount == 1
    ? Visibility.Collapsed
    : Visibility.Visible;

  SetColumnVisibility(
   YearGrid,
   person2Visibility,
   "Alter 2",
   "freiw. GKV/Pflege P2 mtl.",
   "Nettoeinkommen P2 verwendet p.a.");
  SetColumnVisibility(
   StressYearGrid,
   person2Visibility,
   "Alter 2",
   "freiw. GKV/Pflege P2 mtl.",
   "Nettoeinkommen P2 verwendet p.a.");
  SetColumnVisibility(
   FundingGrid,
   person2Visibility,
   "Nettoeinkommen P2");
 }

 private static void SetColumnVisibility(
  DataGrid dataGrid,
  Visibility visibility,
  params string[] headers)
 {
  foreach (DataGridColumn column in dataGrid.Columns)
  {
   string header = column.Header?.ToString() ?? "";

   if (headers.Contains(header, StringComparer.Ordinal))
    column.Visibility = visibility;
  }
 }

 private void UpdateYearOverviewLayout()
 {
  SetGroupWidth(BasePlanGroupColumn, YearGrid, 0, 3);
  SetGroupWidth(BaseExpenseGroupColumn, YearGrid, 3, 6);
  SetGroupWidth(BasePensionGroupColumn, YearGrid, 9, 3);
  SetGroupWidth(BaseIncomeGroupColumn, YearGrid, 12, 4);
  SetGroupWidth(BaseTaxGroupColumn, YearGrid, 16, 1);
  SetGroupWidth(BaseWealthGroupColumn, YearGrid, 17, 1);
  SetGroupWidth(BaseStatusGroupColumn, YearGrid, 18, 1);
  SetGridWidth(YearGrid, BaseGroupHeaderGrid);

  SetGroupWidth(StressPlanGroupColumn, StressYearGrid, 0, 3);
  SetGroupWidth(StressExpenseGroupColumn, StressYearGrid, 3, 5);
  SetGroupWidth(StressPensionGroupColumn, StressYearGrid, 8, 2);
  SetGroupWidth(StressIncomeGroupColumn, StressYearGrid, 10, 3);
  SetGroupWidth(StressTaxGroupColumn, StressYearGrid, 13, 1);
  SetGroupWidth(StressWealthGroupColumn, StressYearGrid, 14, 2);
  SetGroupWidth(StressStatusGroupColumn, StressYearGrid, 16, 1);
  SetGridWidth(StressYearGrid, StressGroupHeaderGrid);
 }

 private static void SetGroupWidth(
  ColumnDefinition groupColumn,
  DataGrid dataGrid,
  int firstColumnIndex,
  int columnCount)
 {
  double width = 0d;

  for (int index = firstColumnIndex; index < firstColumnIndex + columnCount; index++)
   width += dataGrid.Columns[index].ActualWidth;

  if (width > 0d)
   groupColumn.Width = new GridLength(width);
 }

 private static void SetGridWidth(DataGrid dataGrid, Grid groupHeaderGrid)
 {
  double width = 0d;

  foreach (DataGridColumn column in dataGrid.Columns)
   width += column.ActualWidth;

  if (width <= 0d)
   return;

  dataGrid.Width = width;
  groupHeaderGrid.Width = width;
 }

 private void Window_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
 {
  if (ResultsScrollViewer.ScrollableHeight <= 0)
   return;

  double newOffset = ResultsScrollViewer.VerticalOffset - e.Delta;
  newOffset = Math.Max(0, Math.Min(newOffset, ResultsScrollViewer.ScrollableHeight));

  ResultsScrollViewer.ScrollToVerticalOffset(newOffset);
  e.Handled = true;
 }

 private void ApplyGroupHeaderColors()
 {
  Brush success = CreateGroupHeaderBrush((Brush)FindResource("SuccessBrush"));
  Brush danger = CreateGroupHeaderBrush((Brush)FindResource("DangerBrush"));

  BaseExpenseGroupBorder.Background = danger;
  StressExpenseGroupBorder.Background = danger;

  BasePensionGroupBorder.Background = success;
  StressPensionGroupBorder.Background = success;

  BaseWealthGroupBorder.Background = _baseResult.FinalCapital > 0m ? success : danger;
  StressWealthGroupBorder.Background = _stressResult.FinalCapital > 0m ? success : danger;
 }

 private static Brush CreateGroupHeaderBrush(Brush source)
 {
  if (source is SolidColorBrush solid)
  {
   Color color = solid.Color;
   return new SolidColorBrush(Color.FromArgb(72, color.R, color.G, color.B));
  }

  return source.CloneCurrentValue();
 }

 private static string BuildText(ProjectionResult result, string name)
 {
  if (result.ReachesPlanEnd)
  {
   return $"{name}: Vermögen reicht bis zum Planungsende.\n" +
          $"Vermögen am Ende: {result.FinalCapital:N0} €\n" +
          $"Mindest-Startvermögen: {result.MinimumRequiredStartCapital:N0} €\n" +
          $"Sichere Reserve zu Beginn: {result.InitialRequiredCash:N0} €";
  }

  return $"{name}: Vermögen reicht nicht bis zum Planungsende.\n" +
         $"Voraussichtlich aufgebraucht: {result.DepletionYear?.ToString() ?? "unbekannt"}\n" +
         $"Mindest-Startvermögen: {result.MinimumRequiredStartCapital:N0} €\n" +
         $"Zusätzlich nötig: {result.RequiredAdditionalStartCapital:N0} €";
 }

 private static string BuildSensitivityText(
  PlannerSettings settings,
  StrategyAllocation allocation)
 {
  PlannerSettings minus = SettingsClone.Clone(settings);
  minus.MonthlyLivingCosts = settings.MonthlyLivingCosts * 0.90m;

  PlannerSettings plus = SettingsClone.Clone(settings);
  plus.MonthlyLivingCosts = settings.MonthlyLivingCosts * 1.10m;

  bool minusBasis = ProjectionService.ReachesPlanEnd(minus, allocation, false);
  bool minusStress = ProjectionService.ReachesPlanEnd(minus, allocation, true);
  bool plusBasis = ProjectionService.ReachesPlanEnd(plus, allocation, false);
  bool plusStress = ProjectionService.ReachesPlanEnd(plus, allocation, true);

  return $"-10 % Ausgaben: Basis {(minusBasis ? "Grün" : "Rot")}, Stress {(minusStress ? "Grün" : "Rot")}\n" +
         $"+10 % Ausgaben: Basis {(plusBasis ? "Grün" : "Rot")}, Stress {(plusStress ? "Grün" : "Rot")}";
 }

 private void DrawCapitalChart()
 {
  CapitalChart.Children.Clear();

  double width = CapitalChart.ActualWidth;
  double height = CapitalChart.ActualHeight;

  if (width < 80 || height < 80 || _baseResult.Years.Count == 0)
  {
   ChartEmptyText.Visibility = Visibility.Visible;
   return;
  }

  ChartEmptyText.Visibility = Visibility.Collapsed;

  const double left = 56;
  const double right = 18;
  const double top = 14;
  const double bottom = 28;

  double plotWidth = Math.Max(1, width - left - right);
  double plotHeight = Math.Max(1, height - top - bottom);

  decimal maxCapital = Math.Max(
   _baseResult.Years.Max(x => x.TotalPortfolioEnd),
   _stressResult.Years.Max(x => x.TotalPortfolioEnd));

  if (maxCapital <= 0m)
   maxCapital = 1m;

  DrawGrid(width, height, left, right, top, bottom, maxCapital);

  AddSeries(_baseResult.Years, Colors.DodgerBlue, left, top, plotWidth, plotHeight, maxCapital);
  AddSeries(_stressResult.Years, Colors.Goldenrod, left, top, plotWidth, plotHeight, maxCapital);
 }

 private void DrawGrid(
  double width,
  double height,
  double left,
  double right,
  double top,
  double bottom,
  decimal maxCapital)
 {
  Brush gridBrush = new SolidColorBrush(Color.FromRgb(55, 55, 55));
  Brush textBrush = (Brush)FindResource("MutedTextBrush");

  for (int i = 0; i <= 4; i++)
  {
   double y = top + ((height - top - bottom) * i / 4d);

   var line = new Line
   {
    X1 = left,
    X2 = width - right,
    Y1 = y,
    Y2 = y,
    Stroke = gridBrush,
    StrokeThickness = 1
   };
   CapitalChart.Children.Add(line);

   decimal value = maxCapital * (4 - i) / 4m;
   var label = new System.Windows.Controls.TextBlock
   {
    Text = FormatCompactEuro(value),
    Foreground = textBrush,
    FontSize = 11
   };

   System.Windows.Controls.Canvas.SetLeft(label, 2);
   System.Windows.Controls.Canvas.SetTop(label, Math.Max(0, y - 8));
   CapitalChart.Children.Add(label);
  }

  int firstYear = _baseResult.Years.First().Year;
  int lastYear = _baseResult.Years.Last().Year;

  var first = new System.Windows.Controls.TextBlock
  {
   Text = firstYear.ToString(CultureInfo.InvariantCulture),
   Foreground = textBrush,
   FontSize = 11
  };
  System.Windows.Controls.Canvas.SetLeft(first, left);
  System.Windows.Controls.Canvas.SetTop(first, height - bottom + 6);
  CapitalChart.Children.Add(first);

  var last = new System.Windows.Controls.TextBlock
  {
   Text = lastYear.ToString(CultureInfo.InvariantCulture),
   Foreground = textBrush,
   FontSize = 11
  };
  System.Windows.Controls.Canvas.SetLeft(last, Math.Max(left, width - right - 34));
  System.Windows.Controls.Canvas.SetTop(last, height - bottom + 6);
  CapitalChart.Children.Add(last);
 }

 private void AddSeries(
  IReadOnlyList<YearResult> years,
  Color color,
  double left,
  double top,
  double plotWidth,
  double plotHeight,
  decimal maxCapital)
 {
  if (years.Count == 0)
   return;

  var points = new PointCollection();

  for (int i = 0; i < years.Count; i++)
  {
   double x = left + (years.Count == 1 ? 0 : plotWidth * i / (years.Count - 1d));
   double ratio = (double)(years[i].TotalPortfolioEnd / maxCapital);
   double y = top + plotHeight * (1d - Math.Clamp(ratio, 0d, 1d));
   points.Add(new Point(x, y));
  }

  CapitalChart.Children.Add(new Polyline
  {
   Points = points,
   Stroke = new SolidColorBrush(color),
   StrokeThickness = 2.5,
   StrokeLineJoin = PenLineJoin.Round
  });
 }

 private static string FormatCompactEuro(decimal value)
 {
  if (value >= 1000000m)
   return $"{value / 1000000m:0.#} Mio.";
  if (value >= 1000m)
   return $"{value / 1000m:0} Tsd.";
  return $"{value:0} €";
 }
}
