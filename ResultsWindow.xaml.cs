using System.Globalization;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RockefellerFiction;

public partial class ResultsWindow : Window
{
 private PlannerSettings _settings;
 private StrategyAllocation _allocation;
 private ProjectionResult _baseResult;
 private ProjectionResult _stressResult;
 private readonly string _layoutPath =
  System.IO.Path.Combine(AppContext.BaseDirectory, "results-layout.json");
 private readonly JsonSerializerOptions _layoutJsonOptions =
  new() { WriteIndented = true };
 private ResultsLayoutState _layoutState = new();
 private bool _layoutLoaded;

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

  LoadResultsLayout();
  ApplyPaneExpansionState();
  ApplySavedColumnWidths();

  UpdateResults(settings, allocation, baseResult, stressResult);

  ApplyGroupHeaderColors();

  CapitalChart.SizeChanged += (_, _) => DrawCapitalChart();
  Loaded += (_, _) =>
  {
   ApplySavedColumnWidths();
   AttachColumnWidthPersistence();
   _layoutLoaded = true;
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
  StrategyAllocation initialAllocation =
   ProjectionService.GetInitialAllocation(settings, allocation);
  StrategyText.Text =
   $"Gewählt: {settings.Strategy} | Empfohlen: {recommended} | " +
   $"Ziel: Sichere Anlage {_allocation.Cash:P0}, Welt-ETF {_allocation.WorldEtf:P0}, " +
   $"Div.-ETF {_allocation.DividendEtf:P0}, Div.-Aktien {_allocation.DividendStocks:P0} | " +
   $"Start tatsächlich: Sichere Anlage {initialAllocation.Cash:P0}, Welt-ETF {initialAllocation.WorldEtf:P0}, " +
   $"Div.-ETF {initialAllocation.DividendEtf:P0}, Div.-Aktien {initialAllocation.DividendStocks:P0}";

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

 private void LoadResultsLayout()
 {
  try
  {
   if (!File.Exists(_layoutPath))
    return;

   string json = File.ReadAllText(_layoutPath);
   ResultsLayoutState? loaded =
    JsonSerializer.Deserialize<ResultsLayoutState>(
     json,
     _layoutJsonOptions);

   if (loaded != null)
    _layoutState = loaded;
  }
  catch
  {
   _layoutState = new ResultsLayoutState();
  }
 }

 private void SaveResultsLayout()
 {
  if (!_layoutLoaded)
   return;

  try
  {
   string json =
    JsonSerializer.Serialize(
     _layoutState,
     _layoutJsonOptions);

   File.WriteAllText(_layoutPath, json);
  }
  catch
  {
  }
 }

 private void ApplyPaneExpansionState()
 {
  OverviewPaneExpander.IsExpanded =
   GetPaneExpanded("Overview", true);
  CapitalPaneExpander.IsExpanded =
   GetPaneExpanded("Capital", true);
  BasePaneExpander.IsExpanded =
   GetPaneExpanded("Base", true);
  StressPaneExpander.IsExpanded =
   GetPaneExpanded("Stress", true);
  RecommendationPaneExpander.IsExpanded =
   GetPaneExpanded("Recommendation", true);
 }

 private bool GetPaneExpanded(
  string key,
  bool defaultValue)
 {
  return _layoutState.Panes.TryGetValue(
   key,
   out bool isExpanded)
    ? isExpanded
    : defaultValue;
 }

 private void PaneExpansion_Changed(
  object sender,
  RoutedEventArgs e)
 {
  if (sender is not Expander expander)
   return;

  string? key =
   expander.Name switch
   {
    "OverviewPaneExpander" => "Overview",
    "CapitalPaneExpander" => "Capital",
    "BasePaneExpander" => "Base",
    "StressPaneExpander" => "Stress",
    "RecommendationPaneExpander" => "Recommendation",
    _ => null
   };

  if (key == null)
   return;

  _layoutState.Panes[key] = expander.IsExpanded;
  SaveResultsLayout();

  if ((string.Equals(
        key,
        "Capital",
        StringComparison.Ordinal) ||
       string.Equals(
        key,
        "Overview",
        StringComparison.Ordinal)) &&
      expander.IsExpanded)
  {
   Dispatcher.BeginInvoke(
    new Action(DrawCapitalChart));
  }
 }

 private void ApplySavedColumnWidths()
 {
  ApplySavedColumnWidths(
   "YearGrid",
   YearGrid);
  ApplySavedColumnWidths(
   "StressYearGrid",
   StressYearGrid);
  ApplySavedColumnWidths(
   "FundingGrid",
   FundingGrid);
  ApplySavedColumnWidths(
   "AssetGrid",
   AssetGrid);
  ApplySavedColumnWidths(
   "ReserveGrid",
   ReserveGrid);
 }

 private void ApplySavedColumnWidths(
  string gridKey,
  DataGrid dataGrid)
 {
  if (!_layoutState.ColumnWidths.TryGetValue(
       gridKey,
       out Dictionary<string, double>? widths))
   return;

  foreach (DataGridColumn column in dataGrid.Columns)
  {
   string header =
    column.Header?.ToString() ?? "";

   if (!widths.TryGetValue(
        header,
        out double width) ||
       width <= 0d)
    continue;

   column.Width =
    new DataGridLength(
     width,
     DataGridLengthUnitType.Pixel);
  }
 }

 private void AttachColumnWidthPersistence()
 {
  AttachColumnWidthPersistence(
   "YearGrid",
   YearGrid);
  AttachColumnWidthPersistence(
   "StressYearGrid",
   StressYearGrid);
  AttachColumnWidthPersistence(
   "FundingGrid",
   FundingGrid);
  AttachColumnWidthPersistence(
   "AssetGrid",
   AssetGrid);
  AttachColumnWidthPersistence(
   "ReserveGrid",
   ReserveGrid);
 }

 private void AttachColumnWidthPersistence(
  string gridKey,
  DataGrid dataGrid)
 {
  foreach (DataGridColumn column in dataGrid.Columns)
  {
   DependencyPropertyDescriptor? descriptor =
    DependencyPropertyDescriptor.FromProperty(
     DataGridColumn.WidthProperty,
     typeof(DataGridColumn));

   if (descriptor == null)
    continue;

   descriptor.AddValueChanged(
    column,
    (_, _) =>
    {
     string header =
      column.Header?.ToString() ?? "";

     if (string.IsNullOrWhiteSpace(header))
      return;

     if (!_layoutState.ColumnWidths.TryGetValue(
          gridKey,
          out Dictionary<string, double>? widths))
     {
      widths = new Dictionary<string, double>(
       StringComparer.Ordinal);

      _layoutState.ColumnWidths[gridKey] =
       widths;
     }

     double width =
      column.Width.IsAbsolute
       ? column.Width.Value
       : column.ActualWidth;

     if (width <= 0d)
      return;

     widths[header] = width;
     SaveResultsLayout();
     UpdateYearOverviewLayout();
    });
  }
 }

 private void ExcelExportButton_Click(object sender, RoutedEventArgs e)
 {
  var dialog = new SaveFileDialog
  {
   Title = "Excel-Export speichern",
   Filter = "Excel-Arbeitsmappe (*.xlsx)|*.xlsx",
   DefaultExt = ".xlsx",
   AddExtension = true,
   FileName = $"RockefellerFiction_Ergebnisse_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
  };

  if (dialog.ShowDialog(this) != true)
   return;

  try
  {
   ExcelExportService.Export(
    dialog.FileName,
    _settings,
    _baseResult,
    _stressResult,
    BaseStatus.Text,
    BaseText.Text,
    StressStatus.Text,
    StressText.Text,
    StrategyText.Text,
    RecommendationText.Text,
    SensitivityText.Text);

   MessageBox.Show(
    this,
    "Excel-Export wurde gespeichert.",
    "RockefellerFiction",
    MessageBoxButton.OK,
    MessageBoxImage.Information);
  }
  catch (Exception ex)
  {
   MessageBox.Show(
    this,
    "Excel-Export fehlgeschlagen:\n\n" + ex.Message,
    "RockefellerFiction",
    MessageBoxButton.OK,
    MessageBoxImage.Error);
  }
 }

 private void ResultColumnHeader_MouseEnter(
  object sender,
  System.Windows.Input.MouseEventArgs e)
 {
  if (sender is not DataGridColumnHeader columnHeader ||
      columnHeader.Content?.ToString() is not string header ||
      string.IsNullOrWhiteSpace(header))
   return;

  string hint =
   HintService.Get($"Ergebnis - {header}");

  columnHeader.ToolTip = new ToolTip
  {
   Background = (Brush)FindResource("BgBrush"),
   BorderBrush = (Brush)FindResource("MutedTextBrush"),
   BorderThickness = new Thickness(1),
   Padding = new Thickness(10, 8, 10, 8),
   Content = new TextBlock
   {
    Text = hint,
    Foreground = (Brush)FindResource("TextBrush"),
    TextWrapping = TextWrapping.Wrap,
    MaxWidth = 420
   }
  };
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
   "P2 Alter",
   "Nettoeinkommen P2 verwendet p.a.");
  SetColumnVisibility(
   StressYearGrid,
   person2Visibility,
   "P2 Alter",
   "Nettoeinkommen P2 verwendet p.a.");
  SetColumnVisibility(
   FundingGrid,
   person2Visibility,
   "Nettoeinkommen P2");

  UpdateHealthDetailColumns();
 }

 private void HealthDetailsToggle_Changed(
  object sender,
  RoutedEventArgs e)
 {
  UpdateHealthDetailColumns();
  UpdateYearOverviewLayout();
 }

 private void UpdateHealthDetailColumns()
 {
  bool baseDetailsVisible =
   BaseHealthDetailsToggle.IsChecked == true;
  bool stressDetailsVisible =
   StressHealthDetailsToggle.IsChecked == true;

  SetColumnVisibility(
   YearGrid,
   baseDetailsVisible
    ? Visibility.Visible
    : Visibility.Collapsed,
   "freiw. GKV/Pflege P1 mtl.",
   "Rente brutto p.a.",
   "Rente P1 brutto p.a.",
   "GKV/Pflege aus Rente p.a.");

  SetColumnVisibility(
   YearGrid,
   baseDetailsVisible && _settings.HouseholdPersonCount == 2
    ? Visibility.Visible
    : Visibility.Collapsed,
   "freiw. GKV/Pflege P2 mtl.",
   "Rente P2 brutto p.a.");

  SetColumnVisibility(
   StressYearGrid,
   stressDetailsVisible
    ? Visibility.Visible
    : Visibility.Collapsed,
   "freiw. GKV/Pflege P1 mtl.",
   "GKV/Pflege aus Rente p.a.");

  SetColumnVisibility(
   StressYearGrid,
   stressDetailsVisible && _settings.HouseholdPersonCount == 2
    ? Visibility.Visible
    : Visibility.Collapsed,
   "freiw. GKV/Pflege P2 mtl.");
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
  SetGroupWidth(BaseExpenseGroupColumn, YearGrid, 3, 7);
  SetGroupWidth(BasePensionGroupColumn, YearGrid, 10, 5);
  SetGroupWidth(BaseIncomeGroupColumn, YearGrid, 15, 4);
  SetGroupWidth(BaseTaxGroupColumn, YearGrid, 19, 1);
  SetGroupWidth(BaseWealthGroupColumn, YearGrid, 20, 1);
  SetGroupWidth(BaseStatusGroupColumn, YearGrid, 21, 1);
  SetGridWidth(YearGrid, BaseGroupHeaderGrid);

  SetGroupWidth(StressPlanGroupColumn, StressYearGrid, 0, 3);
  SetGroupWidth(StressExpenseGroupColumn, StressYearGrid, 3, 6);
  SetGroupWidth(StressPensionGroupColumn, StressYearGrid, 9, 2);
  SetGroupWidth(StressIncomeGroupColumn, StressYearGrid, 11, 3);
  SetGroupWidth(StressTaxGroupColumn, StressYearGrid, 14, 1);
  SetGroupWidth(StressWealthGroupColumn, StressYearGrid, 15, 2);
  SetGroupWidth(StressStatusGroupColumn, StressYearGrid, 17, 1);
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
  {
   DataGridColumn column = dataGrid.Columns[index];

   if (column.Visibility == Visibility.Visible)
    width += column.ActualWidth;
  }

  groupColumn.Width = new GridLength(width);
 }

 private static void SetGridWidth(DataGrid dataGrid, Grid groupHeaderGrid)
 {
  double width = 0d;

  foreach (DataGridColumn column in dataGrid.Columns)
  {
   if (column.Visibility == Visibility.Visible)
    width += column.ActualWidth;
  }

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

  return $"-10 % Lebenshaltungskosten: Basis {(minusBasis ? "Grün" : "Rot")}, Stress {(minusStress ? "Grün" : "Rot")}\n" +
         $"+10 % Lebenshaltungskosten: Basis {(plusBasis ? "Grün" : "Rot")}, Stress {(plusStress ? "Grün" : "Rot")}";
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
  {
   ChartEmptyText.Text = "Kein Vermögen vorhanden";
   ChartEmptyText.Visibility = Visibility.Visible;
   return;
  }

  ChartEmptyText.Text = "Keine Daten";
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

 private sealed class ResultsLayoutState
 {
  public Dictionary<string, Dictionary<string, double>> ColumnWidths { get; set; } =
   new(StringComparer.Ordinal);

  public Dictionary<string, bool> Panes { get; set; } =
   new(StringComparer.Ordinal);
 }
}
