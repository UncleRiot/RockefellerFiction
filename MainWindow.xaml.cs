using System.ComponentModel;
using System.Globalization;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RockefellerFiction;

public partial class MainWindow : Window
{
 private readonly HelpPopupController _help = new();
 private readonly Dictionary<string, FrameworkElement> _inputs = new();
 private StackPanel? _currentInputPanel;
 private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");
 private int _fieldNumber;
 private PlannerSettings _settings;
 private ProjectionResult? _baseResult;
 private ProjectionResult? _stressResult;
 private ResultsWindow? _resultsWindow;
 private StrategyAllocation _allocation;
 private bool _hasUnsavedChanges;
 private bool _isUpdatingStrategyAllocation;

 public MainWindow()
 {
  InitializeComponent();
  Background = (Brush)FindResource("BgBrush");
  Foreground = (Brush)FindResource("TextBrush");
  UiLayout.ApplyMainWindow(this);
  WindowBehavior.ApplyDarkTitleBar(this);

  SettingsImportResult saved = SettingsService.LoadWithAllocation();
  _settings = saved.Settings;
  _allocation = saved.Allocation;

  BuildInputForm();
  _hasUnsavedChanges = false;
  UpdateCalculationDocumentation();
 }

 private void BuildInputForm()
 {
  _fieldNumber = 0;

  AddSection("1. Startvermögen");
  AddMoney("StartCapital", "Verfügbares Startvermögen", _settings.StartCapital);

        AddSection("2. Planung");
  AddChoice("HouseholdPersonCount", "Haushalt",
   _settings.HouseholdPersonCount == 1 ? "1 Person" : "2 Personen",
   ["1 Person", "2 Personen"]);
  AddInt("Person1CurrentAge", "Aktuelles Alter Person 1", _settings.Person1Age);
  AddInt("PlanningYear", "Vorzeitiges Arbeitsende Person 1", _settings.PlanningYear);
  AddInt("Person2CurrentAge", "Aktuelles Alter Person 2", _settings.Person2Age);
  AddInt("Person2WorkEndYear", "Vorzeitiges Arbeitsende Person 2",
   _settings.Person2WorkEndYear > 0 ? _settings.Person2WorkEndYear : _settings.PlanningYear);
  AddMoney("Person2NetIncomeMonthly", "Nettoeinkommen Person 2 pro Monat", _settings.Person2NetIncomeMonthly);
  AddPercent("Person2NetIncomeIncreaseRate", "Nettoeinkommen-Steigerung Person 2 pro Jahr [Standardwert]", _settings.Person2NetIncomeIncreaseRate);
  AddInt("Person1RetirementAge", "Beginn gesetzliche Rente Person 1", _settings.Person1RetirementAge);
  AddInt("Person2RetirementAge", "Beginn gesetzliche Rente Person 2", _settings.Person2RetirementAge);
  AddInt("Person1EndAge", "Lebenserwartung Person 1 [Standardwert]", _settings.Person1EndAge);
  AddInt("Person2EndAge", "Lebenserwartung Person 2 [Standardwert]", _settings.Person2EndAge);

  AddSection("3. Steuern");
  AddMoney("CapitalGainsAllowance", "Sparer-Pauschbetrag Haushalt [Standardwert]", _settings.CapitalGainsAllowance);
  AddBool("JointTaxation", "Gemeinsame steuerliche Veranlagung [Standardwert]", _settings.JointTaxation);
  AddBool("ChurchTaxEnabled", "Kirchensteuer berücksichtigen", _settings.ChurchTaxEnabled);
  AddPercent("ChurchTaxRate", "Kirchensteuersatz", _settings.ChurchTaxRate);
  AddPercent("AdvanceLumpSumBaseRate", "Basiszins Vorabpauschale [Standardwert]", _settings.AdvanceLumpSumBaseRate);

  AddSection("4. Lebensstandard & Inflation");
  AddMoney("MonthlyLivingCosts", "Monatliche Ausgaben für das Leben", _settings.MonthlyLivingCosts);
  AddPercent("InflationRate", "Inflation pro Jahr [Standardwert]", _settings.InflationRate);
  AddPercent("PensionIncreaseRate", "Konservative Rentensteigerung pro Jahr [Standardwert]", _settings.PensionIncreaseRate);
  AddMoney("VoluntaryHealthInsuranceMinimumMonthlyIncome", "GKV/Pflege Mindest-Bemessungsgrundlage pro Monat [Standardwert]", _settings.VoluntaryHealthInsuranceMinimumMonthlyIncome);
  AddMoney("VoluntaryHealthInsuranceMaximumMonthlyIncome", "GKV/Pflege Beitragsbemessungsgrenze pro Monat [Standardwert]", _settings.VoluntaryHealthInsuranceMaximumMonthlyIncome);
  AddPercent("VoluntaryHealthInsuranceRate", "GKV Beitragssatz ohne Krankengeld [Standardwert]", _settings.VoluntaryHealthInsuranceRate);
  AddPercent("VoluntaryHealthInsuranceAdditionalRate", "GKV Zusatzbeitrag [Standardwert]", _settings.VoluntaryHealthInsuranceAdditionalRate);
  AddPercent("CareInsuranceChildlessRate", "Pflegeversicherung Beitragssatz", _settings.CareInsuranceChildlessRate);
  AddInt("HealthInsuranceBaseYear", "GKV/Pflege Basisjahr [Standardwert]", _settings.HealthInsuranceBaseYear);
  AddPercent("HealthInsuranceAssessmentIncreaseRate", "GKV/Pflege Bemessungsgrenzen Änderung p.a. [Standardwert]", _settings.HealthInsuranceAssessmentIncreaseRate);
  AddPercent("HealthInsuranceAdditionalRateAnnualChange", "GKV Zusatzbeitrag Änderung p.a. in Prozentpunkten [Standardwert]", _settings.HealthInsuranceAdditionalRateAnnualChange);
  AddPercent("CareInsuranceRateAnnualChange", "Pflegeversicherung Änderung p.a. in Prozentpunkten [Standardwert]", _settings.CareInsuranceRateAnnualChange);
  AddReadOnlyMoney("CalculatedHealthPerson1Monthly", "Berechnete GKV/Pflege Person 1 pro Monat", 0m);
  AddReadOnlyMoney("CalculatedHealthPerson2Monthly", "Berechnete GKV/Pflege Person 2 pro Monat", 0m);
  AddSection("5. Gesetzliche Rente");
  AddMoney("Person1PensionGrossMonthly", "Rente Person 1 – heute bereits erworben (brutto/Monat)", _settings.Person1PensionGrossMonthly);
        AddMoney("Person2PensionGrossMonthly", "Rente Person 2 – erwartet zum Arbeitsende (brutto/Monat)", _settings.Person2PensionGrossMonthly);
        AddBool("KvdrPerson1", "KVdR für Person 1 annehmen", _settings.KvdrPerson1);
  AddBool("KvdrPerson2", "KVdR für Person 2 annehmen", _settings.KvdrPerson2);

  AddSection("6. Sichere Reserve & Rücklagen");
  AddDecimal("ReserveYears", "Sichere Reserve in Jahresausgaben [Standardwert]", _settings.ReserveYears);
  AddBool("AutoRefillReserve", "Reserve automatisch wieder auffüllen [Standardwert]", _settings.AutoRefillReserve);
  AddBool("UseReserveOnNegativeStockYear", "Bei negativem Aktienjahr zuerst Reserve nutzen [Standardwert]", _settings.UseReserveOnNegativeStockYear);
  AddPercent("CashInterestRate", "Zins Tages-/Festgeld [Standardwert]", _settings.CashInterestRate);

  AddMoney("HouseTotalValue", "Hauswert inkl. Grundstück", _settings.HouseTotalValue);
  AddPercent("HouseBuildingShare", "Anteil Gebäude am Hauswert [Standardwert]", _settings.HouseBuildingShare);
  AddPercent("HouseReserveRate", "Jährliche Haus-Rücklage [Standardwert]", _settings.HouseReserveRate);
  AddMoney("CarReplacementValue", "Ersatzwert Auto", _settings.CarReplacementValue);
  AddInt("CarReplacementYears", "Auto-Ersatz nach Jahren [Standardwert]", _settings.CarReplacementYears);
  AddMoney("HealthReserveTarget", "Gesundheit / Zahnersatz Rücklage [Standardwert]", _settings.HealthReserveTarget);
  AddMoney("TravelReserveTarget", "Reisen / größere Wünsche Rücklage", _settings.TravelReserveTarget);
  AddMoney("OtherReserveTarget", "Sonstiges / Unvorhergesehenes Rücklage", _settings.OtherReserveTarget);

  AddSection("7. Bestehende Depots – optional");
  AddMoney("WorldEtfCurrentValue", "Wert des bereits vorhandenen Welt-ETF", _settings.WorldEtfCurrentValue);
  AddInt("WorldEtfStartYear", "Seit wann besteht dieser Welt-ETF?", _settings.WorldEtfStartYear);
  AddPercent("WorldEtfHistoricalReturn", "Bisherige durchschnittliche Rendite des Welt-ETF [Standardwert]", _settings.WorldEtfHistoricalReturn);
  AddPercent("WorldEtfReturn", "MSCI World / Welt-ETF Gesamtrendite [Standardwert]", _settings.WorldEtfReturn);
  AddPercent("WorldEtfDistribution", "MSCI World / Welt-ETF Ausschüttung", _settings.WorldEtfDistribution);
  AddMoney("DividendEtfCurrentValue", "Wert des bereits vorhandenen Dividenden-ETF", _settings.DividendEtfCurrentValue);
  AddInt("DividendEtfStartYear", "Seit wann besteht dieser Dividenden-ETF?", _settings.DividendEtfStartYear);
  AddPercent("DividendEtfHistoricalReturn", "Bisherige durchschnittliche Rendite des Dividenden-ETF [Standardwert]", _settings.DividendEtfHistoricalReturn);
  AddPercent("DividendEtfReturn", "Dividenden-ETF Gesamtrendite [Standardwert]", _settings.DividendEtfReturn);
  AddPercent("DividendEtfDistribution", "Dividenden-ETF Ausschüttung", _settings.DividendEtfDistribution);
  AddMoney("DividendStocksCurrentValue", "Wert der bereits vorhandenen Dividenden-Aktien", _settings.DividendStocksCurrentValue);
  AddInt("DividendStocksStartYear", "Seit wann bestehen diese Dividenden-Aktien?", _settings.DividendStocksStartYear);
  AddPercent("DividendStocksHistoricalReturn", "Bisherige durchschnittliche Rendite der Dividenden-Aktien [Standardwert]", _settings.DividendStocksHistoricalReturn);
  AddPercent("DividendStocksReturn", "Dividenden-Aktien Gesamtrendite [Standardwert]", _settings.DividendStocksReturn);
  AddPercent("DividendStocksDistribution", "Dividenden-Aktien Ausschüttung", _settings.DividendStocksDistribution);
  AddBool("DividendSurplusReinvest", "Nicht benötigte Dividenden wieder anlegen", _settings.DividendSurplusReinvest);

  AddSection("8. Strategie & Aufteilung");
  AddChoice("Strategy", "Strategie [Standardwert]", GetDisplayedStrategy(),
   ["Sicherheit", "Ausgewogen", "Wachstum", "Benutzerdefiniert"]);
  AddPercent("AllocCash", "Anteil Tages-/Festgeld [Standardwert]", _allocation.Cash);
  AddPercent("AllocWorld", "Anteil MSCI World / Welt-ETF [Standardwert]", _allocation.WorldEtf);
  AddPercent("AllocDividendEtf", "Anteil Dividenden-ETF [Standardwert]", _allocation.DividendEtf);
  AddPercent("AllocDividendStocks", "Anteil Dividenden-Aktien [Standardwert]", _allocation.DividendStocks);

  AddSection("9. Stressszenario");
  AddChoice("StressCrashPercent", "Crash-Stärke am Anfang [Standardwert]", FormatPercentChoice(_settings.StressCrashPercent),
   ["-15 %", "-25 %", "-40 %"]);
  AddBool("StressCrashAtStart", "Crash am Anfang simulieren [Standardwert]", _settings.StressCrashAtStart);
  AddBool("StressSecondCrashEnabled", "Späteren zweiten Crash simulieren [Standardwert]", _settings.StressSecondCrashEnabled);
  AddInt("StressSecondCrashYear", "Jahr des zweiten Crashs [Standardwert]", _settings.StressSecondCrashYear);
  AddChoice("StressSecondCrashPercent", "Stärke des zweiten Crashs [Standardwert]", FormatPercentChoice(_settings.StressSecondCrashPercent),
   ["-15 %", "-25 %", "-40 %"]);
  AddPercent("StressHealthInsuranceAssessmentAdditionalIncreaseRate", "Stress: zusätzl. Änderung GKV/Pflege Bemessungsgrenzen p.a. [Standardwert]", _settings.StressHealthInsuranceAssessmentAdditionalIncreaseRate);
  AddPercent("StressHealthInsuranceAdditionalRateAnnualChange", "Stress: zusätzl. GKV-Zusatzbeitrag p.a. in Prozentpunkten [Standardwert]", _settings.StressHealthInsuranceAdditionalRateAnnualChange);
  AddPercent("StressCareInsuranceRateAnnualChange", "Stress: zusätzl. Pflegebeitrag p.a. in Prozentpunkten [Standardwert]", _settings.StressCareInsuranceRateAnnualChange);

  AddSection("10. Haus optional");
  AddBool("HouseIncluded", "Hausverkauf in Planung berücksichtigen", _settings.HouseIncluded);
  AddInt("HouseSaleYear", "Haus-Verkaufsjahr", _settings.HouseSaleYear);
  AddMoney("HouseNetSaleProceeds", "Nettoerlös Hausverkauf", _settings.HouseNetSaleProceeds);

  UpdateHouseholdPersonVisibility();
  UpdateCalculatedHealthDisplays();
 }

 private void AddSection(string title)
 {
  var panel = new StackPanel();

  var scrollViewer = new ScrollViewer
  {
   VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
   HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
   Content = panel
  };

  var tabItem = new TabItem
  {
   Header = title,
   Content = scrollViewer
  };

  InputSubTabs.Items.Add(tabItem);
  _currentInputPanel = panel;

  if (InputSubTabs.SelectedIndex < 0)
   InputSubTabs.SelectedIndex = 0;
 }

 private void AddInt(string key, string label, int value) =>
  AddTextInput(key, label, value.ToString(CultureInfo.InvariantCulture), InputType.Integer);

 private void AddMoney(string key, string label, decimal value) =>
  AddTextInput(key, label, FormatMoneyValue(value), InputType.Money);

 private void AddReadOnlyMoney(string key, string label, decimal value)
 {
  var textBox = new TextBox
  {
   Text = FormatMoneyValue(value),
   Tag = InputType.Money,
   IsReadOnly = true,
   IsTabStop = false
  };

  _inputs[key] = textBox;
  if (_currentInputPanel == null)
   throw new InvalidOperationException("Keine Eingabe-Unterlasche aktiv.");

  _currentInputPanel.Children.Add(CreateRow(label, textBox));
 }

 private void AddDecimal(string key, string label, decimal value) =>
  AddTextInput(key, label, value.ToString("0.##", CultureInfo.InvariantCulture), InputType.Decimal);

 private void AddPercent(string key, string label, decimal value) =>
  AddTextInput(key, label, (value * 100m).ToString("0.##", CultureInfo.InvariantCulture), InputType.Percent);

 private void AddTextInput(string key, string label, string value, InputType type)
 {
  var textBox = new TextBox { Text = value, Tag = type };

  if (type == InputType.Money)
   textBox.TextChanged += MoneyTextBox_TextChanged;

  textBox.TextChanged += (_, _) => UpdateCalculatedHealthDisplays();
  textBox.TextChanged += (_, _) => _hasUnsavedChanges = true;
  if (key is "AllocCash" or "AllocWorld" or "AllocDividendEtf" or "AllocDividendStocks")
   textBox.TextChanged += (_, _) => UpdateDisplayedStrategyFromAllocation();

  _inputs[key] = textBox;
  if (_currentInputPanel == null)
   throw new InvalidOperationException("Keine Eingabe-Unterlasche aktiv.");

  _currentInputPanel.Children.Add(CreateRow(label, textBox));
 }

 private void AddBool(string key, string label, bool value)
 {
  var combo = new ComboBox();
  combo.Items.Add("Ja");
  combo.Items.Add("Nein");
  combo.SelectedItem = value ? "Ja" : "Nein";
  combo.SelectionChanged += (_, _) => UpdateCalculatedHealthDisplays();
  combo.SelectionChanged += (_, _) => _hasUnsavedChanges = true;
  _inputs[key] = combo;
  if (_currentInputPanel == null)
   throw new InvalidOperationException("Keine Eingabe-Unterlasche aktiv.");

  _currentInputPanel.Children.Add(CreateRow(label, combo));
 }

 private void AddChoice(string key, string label, string value, IEnumerable<string> choices)
 {
  var combo = new ComboBox();
  foreach (var item in choices) combo.Items.Add(item);
  combo.SelectedItem = value;
  combo.SelectionChanged += (_, _) => UpdateCalculatedHealthDisplays();
  combo.SelectionChanged += (_, _) => _hasUnsavedChanges = true;
  if (key == "HouseholdPersonCount")
  {
   combo.SelectionChanged += (_, _) => UpdateHouseholdPersonVisibility();
   combo.SelectionChanged += (_, _) => UpdateCapitalGainsAllowanceForHousehold();
  }
  if (key == "Strategy")
   combo.SelectionChanged += (_, _) => UpdateStrategyAllocation();
  _inputs[key] = combo;
  if (_currentInputPanel == null)
   throw new InvalidOperationException("Keine Eingabe-Unterlasche aktiv.");

  _currentInputPanel.Children.Add(CreateRow(label, combo));
 }

 private FrameworkElement CreateRow(string label, FrameworkElement input)
 {
  const string standardValueSuffix = " [Standardwert]";
  bool isStandardValue = label.EndsWith(
   standardValueSuffix,
   StringComparison.Ordinal);

  string displayLabel = isStandardValue
   ? label[..^standardValueSuffix.Length]
   : label;

  _fieldNumber++;
  string numberedLabel = $"{_fieldNumber:00}. {displayLabel}";

  var grid = new Grid
  {
   Height = (double)FindResource("RowHeight"),
   Margin = new Thickness(0, 1, 0, 1)
  };

  grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength((double)FindResource("LabelWidth")) });
  grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength((double)FindResource("HelpColumnWidth")) });
  grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
  grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

  var labelText = new TextBlock
  {
   Text = numberedLabel,
   Tag = displayLabel,
   TextWrapping = TextWrapping.Wrap,
   VerticalAlignment = VerticalAlignment.Center,
   Margin = new Thickness(20, 0, 0, 0)
  };
  Grid.SetColumn(labelText, 0);
  grid.Children.Add(labelText);

  var helpButton = new Border
  {
   Width = (double)FindResource("HelpSize"),
   Height = (double)FindResource("HelpSize"),
   CornerRadius = new CornerRadius(20),
   Background = (Brush)FindResource("AccentBrush"),
   Cursor = Cursors.Hand,
   VerticalAlignment = VerticalAlignment.Center,
   HorizontalAlignment = HorizontalAlignment.Left,
   Child = new TextBlock
   {
    Text = "?",
    Foreground = Brushes.White,
    FontWeight = FontWeights.Bold,
    HorizontalAlignment = HorizontalAlignment.Center,
    VerticalAlignment = VerticalAlignment.Center
   }
  };

  int fieldNumber = _fieldNumber;
  helpButton.MouseEnter += (_, _) =>
   _help.ShowHover(helpButton, HintService.Get(fieldNumber));
  helpButton.MouseLeave += (_, _) => _help.ClearHover(helpButton);

  Grid.SetColumn(helpButton, 1);
  grid.Children.Add(helpButton);

  input.VerticalAlignment = VerticalAlignment.Center;
  Grid.SetColumn(input, 2);
  grid.Children.Add(input);

  if (isStandardValue)
  {
   var standardValueText = new TextBlock
   {
    Text = "[Standardwert]",
    Foreground = (Brush)FindResource("AccentBrush"),
    FontWeight = FontWeights.SemiBold,
    VerticalAlignment = VerticalAlignment.Center,
    Margin = new Thickness(10, 0, 0, 0)
   };

   Grid.SetColumn(standardValueText, 3);
   grid.Children.Add(standardValueText);
  }

  return grid;
 }

 private void UpdateHouseholdPersonVisibility()
 {
  if (!_inputs.TryGetValue("HouseholdPersonCount", out FrameworkElement? householdElement) ||
      householdElement is not ComboBox householdCombo)
   return;

  bool showPerson2 = string.Equals(
   householdCombo.SelectedItem?.ToString(),
   "2 Personen",
   StringComparison.Ordinal);

  string[] person2Keys =
  [
   "Person2CurrentAge",
   "Person2WorkEndYear",
   "Person2NetIncomeMonthly",
   "Person2NetIncomeIncreaseRate",
   "Person2RetirementAge",
   "Person2EndAge",
   "CalculatedHealthPerson2Monthly",
   "Person2PensionGrossMonthly",
   "KvdrPerson2",
   "JointTaxation"
  ];

  foreach (string key in person2Keys)
  {
   if (_inputs.TryGetValue(key, out FrameworkElement? input) &&
       input.Parent is Grid row)
    row.Visibility = showPerson2 ? Visibility.Visible : Visibility.Collapsed;
  }

 }


 private void RenumberVisibleFields()
 {
  int visibleFieldNumber = 0;

  foreach (object item in InputSubTabs.Items)
  {
   if (item is not TabItem tabItem ||
       tabItem.Content is not ScrollViewer scrollViewer ||
       scrollViewer.Content is not StackPanel panel)
    continue;

   foreach (UIElement child in panel.Children)
   {
    if (child is not Grid row || row.Visibility != Visibility.Visible)
     continue;

    TextBlock? labelText = row.Children
     .OfType<TextBlock>()
     .FirstOrDefault(x => x.Tag is string);

    if (labelText?.Tag is not string label)
     continue;

    visibleFieldNumber++;
    labelText.Text = $"{visibleFieldNumber:00}. {label}";
   }
  }
 }

 private int ReadHouseholdPersonCount()
 {
  return string.Equals(
   ReadChoice("HouseholdPersonCount"),
   "1 Person",
   StringComparison.Ordinal)
    ? 1
    : 2;
 }

 private void UpdateCapitalGainsAllowanceForHousehold()
 {
  if (!_inputs.TryGetValue("CapitalGainsAllowance", out FrameworkElement? allowanceElement) ||
      allowanceElement is not TextBox allowanceTextBox)
   return;

  decimal allowance = ReadHouseholdPersonCount() == 1
   ? 1000m
   : 2000m;

  allowanceTextBox.Text = FormatMoneyValue(allowance);
 }

 private void UpdateStrategyAllocation()
 {
  if (_isUpdatingStrategyAllocation ||
      !_inputs.TryGetValue("Strategy", out FrameworkElement? strategyElement) ||
      strategyElement is not ComboBox strategyCombo ||
      strategyCombo.SelectedItem is not string strategy ||
      strategy == "Benutzerdefiniert" ||
      !_inputs.TryGetValue("AllocCash", out FrameworkElement? cashElement) ||
      cashElement is not TextBox cashTextBox ||
      !_inputs.TryGetValue("AllocWorld", out FrameworkElement? worldElement) ||
      worldElement is not TextBox worldTextBox ||
      !_inputs.TryGetValue("AllocDividendEtf", out FrameworkElement? dividendEtfElement) ||
      dividendEtfElement is not TextBox dividendEtfTextBox ||
      !_inputs.TryGetValue("AllocDividendStocks", out FrameworkElement? dividendStocksElement) ||
      dividendStocksElement is not TextBox dividendStocksTextBox)
   return;

  StrategyAllocation allocation = StrategyService.GetDefault(strategy);

  _isUpdatingStrategyAllocation = true;
  try
  {
   cashTextBox.Text = (allocation.Cash * 100m).ToString("0.##", CultureInfo.InvariantCulture);
   worldTextBox.Text = (allocation.WorldEtf * 100m).ToString("0.##", CultureInfo.InvariantCulture);
   dividendEtfTextBox.Text = (allocation.DividendEtf * 100m).ToString("0.##", CultureInfo.InvariantCulture);
   dividendStocksTextBox.Text = (allocation.DividendStocks * 100m).ToString("0.##", CultureInfo.InvariantCulture);
  }
  finally
  {
   _isUpdatingStrategyAllocation = false;
  }
 }

 private void UpdateDisplayedStrategyFromAllocation()
 {
  if (_isUpdatingStrategyAllocation ||
      !_inputs.TryGetValue("Strategy", out FrameworkElement? strategyElement) ||
      strategyElement is not ComboBox strategyCombo ||
      !_inputs.TryGetValue("AllocCash", out FrameworkElement? cashElement) ||
      cashElement is not TextBox cashTextBox ||
      !_inputs.TryGetValue("AllocWorld", out FrameworkElement? worldElement) ||
      worldElement is not TextBox worldTextBox ||
      !_inputs.TryGetValue("AllocDividendEtf", out FrameworkElement? dividendEtfElement) ||
      dividendEtfElement is not TextBox dividendEtfTextBox ||
      !_inputs.TryGetValue("AllocDividendStocks", out FrameworkElement? dividendStocksElement) ||
      dividendStocksElement is not TextBox dividendStocksTextBox ||
      !decimal.TryParse(cashTextBox.Text, NumberStyles.Number, GermanCulture, out decimal cash) ||
      !decimal.TryParse(worldTextBox.Text, NumberStyles.Number, GermanCulture, out decimal world) ||
      !decimal.TryParse(dividendEtfTextBox.Text, NumberStyles.Number, GermanCulture, out decimal dividendEtf) ||
      !decimal.TryParse(dividendStocksTextBox.Text, NumberStyles.Number, GermanCulture, out decimal dividendStocks))
   return;

  var allocation = new StrategyAllocation(
   cash / 100m,
   world / 100m,
   dividendEtf / 100m,
   dividendStocks / 100m);

  string displayedStrategy = GetStrategyForAllocation(allocation);

  if (!string.Equals(
       strategyCombo.SelectedItem?.ToString(),
       displayedStrategy,
       StringComparison.Ordinal))
  {
   _isUpdatingStrategyAllocation = true;
   try
   {
    strategyCombo.SelectedItem = displayedStrategy;
   }
   finally
   {
    _isUpdatingStrategyAllocation = false;
   }
  }
 }

 private string GetDisplayedStrategy()
 {
  return GetStrategyForAllocation(_allocation);
 }

 private static string GetStrategyForAllocation(StrategyAllocation allocation)
 {
  foreach (string strategy in new[] { "Sicherheit", "Ausgewogen", "Wachstum" })
  {
   StrategyAllocation defaultAllocation = StrategyService.GetDefault(strategy);

   if (allocation.Cash == defaultAllocation.Cash &&
       allocation.WorldEtf == defaultAllocation.WorldEtf &&
       allocation.DividendEtf == defaultAllocation.DividendEtf &&
       allocation.DividendStocks == defaultAllocation.DividendStocks)
    return strategy;
  }

  return "Benutzerdefiniert";
 }

 private void UpdateCalculatedHealthDisplays()
 {
  if (!_inputs.TryGetValue("CalculatedHealthPerson1Monthly", out FrameworkElement? person1Element) ||
      !_inputs.TryGetValue("CalculatedHealthPerson2Monthly", out FrameworkElement? person2Element) ||
      person1Element is not TextBox person1TextBox ||
      person2Element is not TextBox person2TextBox)
   return;

  try
  {
   if (!TryReadHealthPreviewSettings(out PlannerSettings previewSettings, out StrategyAllocation previewAllocation))
   {
    person1TextBox.Text = "—";
    person2TextBox.Text = "—";
    return;
   }

   HealthInsurancePreview preview = ProjectionService.CalculateInitialVoluntaryHealthPreview(
    previewSettings,
    previewAllocation);

   person1TextBox.Text = FormatMoneyValue(preview.Person1Monthly);
   person2TextBox.Text = FormatMoneyValue(preview.Person2Monthly);
  }
  catch
  {
   person1TextBox.Text = "—";
   person2TextBox.Text = "—";
  }
 }

 private bool TryReadHealthPreviewSettings(
  out PlannerSettings previewSettings,
  out StrategyAllocation previewAllocation)
 {
  previewSettings = SettingsClone.Clone(_settings);

  try
  {
   previewSettings.HouseholdPersonCount = ReadHouseholdPersonCount();
   previewSettings.StartCapital = ReadDecimal("StartCapital", 0m, 1000000000m);
   previewSettings.Person1Age = ReadInt("Person1CurrentAge");
   previewSettings.PlanningYear = ReadInt("PlanningYear");
   previewSettings.Person1RetirementAge = ReadInt("Person1RetirementAge");

   if (previewSettings.HouseholdPersonCount == 2)
   {
    previewSettings.Person2Age = ReadInt("Person2CurrentAge");
    previewSettings.Person2WorkEndYear = ReadInt("Person2WorkEndYear");
    previewSettings.Person2NetIncomeMonthly = ReadDecimal("Person2NetIncomeMonthly", 0m, 1000000m);
    previewSettings.Person2NetIncomeIncreaseRate = ReadPercent("Person2NetIncomeIncreaseRate", -0.50m, 1m);
    previewSettings.Person2RetirementAge = ReadInt("Person2RetirementAge");
   }
   previewSettings.CashInterestRate = ReadPercent("CashInterestRate", -0.05m, 0.20m);
   previewSettings.WorldEtfDistribution = ReadPercent("WorldEtfDistribution", 0m, 0.50m);
   previewSettings.DividendEtfDistribution = ReadPercent("DividendEtfDistribution", 0m, 0.50m);
   previewSettings.DividendStocksDistribution = ReadPercent("DividendStocksDistribution", 0m, 0.50m);
   previewSettings.VoluntaryHealthInsuranceMinimumMonthlyIncome = ReadDecimal("VoluntaryHealthInsuranceMinimumMonthlyIncome", 0m, 100000m);
   previewSettings.VoluntaryHealthInsuranceMaximumMonthlyIncome = ReadDecimal("VoluntaryHealthInsuranceMaximumMonthlyIncome", 0m, 100000m);
   previewSettings.VoluntaryHealthInsuranceRate = ReadPercent("VoluntaryHealthInsuranceRate", 0m, 0.30m);
   previewSettings.VoluntaryHealthInsuranceAdditionalRate = ReadPercent("VoluntaryHealthInsuranceAdditionalRate", 0m, 0.20m);
   previewSettings.CareInsuranceChildlessRate = ReadPercent("CareInsuranceChildlessRate", 0m, 0.20m);
   previewSettings.HealthInsuranceBaseYear = ReadInt("HealthInsuranceBaseYear");
   previewSettings.HealthInsuranceAssessmentIncreaseRate = ReadPercent("HealthInsuranceAssessmentIncreaseRate", -0.50m, 1m);
   previewSettings.HealthInsuranceAdditionalRateAnnualChange = ReadPercent("HealthInsuranceAdditionalRateAnnualChange", -0.20m, 0.20m);
   previewSettings.CareInsuranceRateAnnualChange = ReadPercent("CareInsuranceRateAnnualChange", -0.20m, 0.20m);

   previewAllocation = new StrategyAllocation(
    ReadPercent("AllocCash", 0m, 1m),
    ReadPercent("AllocWorld", 0m, 1m),
    ReadPercent("AllocDividendEtf", 0m, 1m),
    ReadPercent("AllocDividendStocks", 0m, 1m));

   return true;
  }
  catch
  {
   previewAllocation = _allocation;
   return false;
  }
 }

 private void Calculate_Click(object sender, RoutedEventArgs e)
 {
  if (!TryReadSettings(out string error))
  {
   StatusText.Text = error;
   StatusText.Foreground = (Brush)FindResource("DangerBrush");
   return;
  }

  decimal sum = _allocation.Cash + _allocation.WorldEtf + _allocation.DividendEtf + _allocation.DividendStocks;
  if (Math.Abs(sum - 1m) > 0.0001m)
  {
   StatusText.Text = $"Warnung: Anlageaufteilung ergibt {sum:P1} statt 100 %.";
   StatusText.Foreground = (Brush)FindResource("WarningBrush");
   return;
  }

  _baseResult = ProjectionService.Calculate(_settings, _allocation, false);
  _stressResult = ProjectionService.Calculate(_settings, _allocation, true);

  if (_resultsWindow != null)
   _resultsWindow.UpdateResults(_settings, _allocation, _baseResult, _stressResult);

  CalculationLogService.Write(_settings, _allocation, _baseResult, _stressResult);
  UpdateCalculationLog();
  UpdateCalculationDocumentation();

  ResultsButton.IsEnabled = true;

  string recommended = StrategyService.Recommend(_settings);
  string reserveWarning = _baseResult.InitialRequiredCash > _settings.StartCapital * _allocation.Cash
   ? " Tages-/Festgeld ist kleiner als Reserve + Rücklagen."
   : "";

  StatusText.Inlines.Clear();

  StatusText.Inlines.Add(new System.Windows.Documents.Run($"Basis: {_baseResult.OverallStatus}")
  {
   Foreground = _baseResult.ReachesPlanEnd
    ? (Brush)FindResource("SuccessBrush")
    : (Brush)FindResource("DangerBrush")
  });

  StatusText.Inlines.Add(new System.Windows.Documents.Run(" | ")
  {
   Foreground = (Brush)FindResource("MutedTextBrush")
  });

  StatusText.Inlines.Add(new System.Windows.Documents.Run($"Stress: {_stressResult.OverallStatus}")
  {
   Foreground = _stressResult.ReachesPlanEnd
    ? (Brush)FindResource("SuccessBrush")
    : (Brush)FindResource("DangerBrush")
  });

  StatusText.Inlines.Add(new System.Windows.Documents.Run(" | ")
  {
   Foreground = (Brush)FindResource("MutedTextBrush")
  });

  StatusText.Inlines.Add(new System.Windows.Documents.Run($"Empfehlung: {recommended}.{reserveWarning}")
  {
   Foreground = (Brush)FindResource("WarningBrush")
  });
 }

 private void Save_Click(object sender, RoutedEventArgs e)
 {
  if (!TryReadSettings(out string error))
  {
   StatusText.Text = error;
   StatusText.Foreground = (Brush)FindResource("DangerBrush");
   return;
  }

  try
  {
   SettingsService.Save(_settings, _allocation);
   _hasUnsavedChanges = false;
   StatusText.Text = "Gespeichert: settings.json";
   StatusText.Foreground = (Brush)FindResource("SuccessBrush");
  }
  catch (Exception ex)
  {
   StatusText.Text = "Speicherfehler: " + ex.Message;
   StatusText.Foreground = (Brush)FindResource("DangerBrush");
  }
 }

 private void Export_Click(object sender, RoutedEventArgs e)
 {
  if (!TryReadSettings(out string error))
  {
   StatusText.Text = error;
   StatusText.Foreground = (Brush)FindResource("DangerBrush");
   return;
  }

  var dialog = new SaveFileDialog
  {
   Title = "Planung exportieren",
   Filter = "JSON-Datei (*.json)|*.json|CSV-Datei (*.csv)|*.csv",
   DefaultExt = ".json",
   AddExtension = true,
   FileName = "RockefellerFiction"
  };

  if (dialog.ShowDialog(this) != true)
   return;

  try
  {
   SettingsService.Export(dialog.FileName, _settings, _allocation);
   StatusText.Text = $"Exportiert: {dialog.FileName}";
   StatusText.Foreground = (Brush)FindResource("SuccessBrush");
  }
  catch (Exception ex)
  {
   StatusText.Text = "Exportfehler: " + ex.Message;
   StatusText.Foreground = (Brush)FindResource("DangerBrush");
  }
 }

 private void Import_Click(object sender, RoutedEventArgs e)
 {
  var dialog = new OpenFileDialog
  {
   Title = "Planung importieren",
   Filter = "Planungsdateien (*.json;*.csv)|*.json;*.csv|JSON-Datei (*.json)|*.json|CSV-Datei (*.csv)|*.csv",
   Multiselect = false
  };

  if (dialog.ShowDialog(this) != true)
   return;

  try
  {
   SettingsImportResult imported = SettingsService.Import(dialog.FileName);

   _settings = imported.Settings;
   _allocation = imported.Allocation;

   _baseResult = null;
   _stressResult = null;
   ResultsButton.IsEnabled = false;

   _inputs.Clear();
   InputSubTabs.Items.Clear();
   _currentInputPanel = null;
   BuildInputForm();
   _hasUnsavedChanges = true;
   UpdateCalculationDocumentation();

   StatusText.Text = $"Importiert: {dialog.FileName}";
   StatusText.Foreground = (Brush)FindResource("SuccessBrush");
  }
  catch (Exception ex)
  {
   StatusText.Text = "Importfehler: " + ex.Message;
   StatusText.Foreground = (Brush)FindResource("DangerBrush");
  }
 }

 private void About_Click(object sender, RoutedEventArgs e)
 {
  var window = new AboutWindow
  {
   Owner = this
  };

  window.ShowDialog();
 }

 private void Results_Click(object sender, RoutedEventArgs e)
 {
  if (_baseResult == null || _stressResult == null)
   return;

  if (_resultsWindow != null)
  {
   if (_resultsWindow.WindowState == WindowState.Minimized)
    _resultsWindow.WindowState = WindowState.Normal;

   _resultsWindow.Activate();
   return;
  }

  _resultsWindow = new ResultsWindow(
   _settings,
   _allocation,
   _baseResult,
   _stressResult)
  {
   Owner = this
  };

  _resultsWindow.Closed += (_, _) => _resultsWindow = null;
  _resultsWindow.Show();
 }

 private bool TryReadSettings(out string error)
 {
  error = "";

  try
  {
   _settings.HouseholdPersonCount = ReadHouseholdPersonCount();
   _settings.Person1Age = ReadInt("Person1CurrentAge");
   _settings.PlanningYear = ReadInt("PlanningYear");
   _settings.Person1RetirementAge = ReadInt("Person1RetirementAge");
   _settings.Person1EndAge = ReadInt("Person1EndAge");

   if (_settings.HouseholdPersonCount == 2)
   {
    _settings.Person2Age = ReadInt("Person2CurrentAge");
    _settings.Person2WorkEndYear = ReadInt("Person2WorkEndYear");
    _settings.Person2NetIncomeMonthly = ReadDecimal("Person2NetIncomeMonthly", 0m, 1000000m);
    _settings.Person2NetIncomeIncreaseRate = ReadPercent("Person2NetIncomeIncreaseRate", -0.50m, 1m);
    _settings.Person2RetirementAge = ReadInt("Person2RetirementAge");
    _settings.Person2EndAge = ReadInt("Person2EndAge");
   }

   _settings.MonthlyLivingCosts = ReadDecimal("MonthlyLivingCosts", 0m, 1000000m);
   _settings.InflationRate = ReadPercent("InflationRate", -0.05m, 0.20m);
   _settings.PensionIncreaseRate = ReadPercent("PensionIncreaseRate", -0.05m, 0.20m);
   _settings.VoluntaryHealthInsuranceMinimumMonthlyIncome = ReadDecimal("VoluntaryHealthInsuranceMinimumMonthlyIncome", 0m, 100000m);
   _settings.VoluntaryHealthInsuranceMaximumMonthlyIncome = ReadDecimal("VoluntaryHealthInsuranceMaximumMonthlyIncome", 0m, 100000m);
   _settings.VoluntaryHealthInsuranceRate = ReadPercent("VoluntaryHealthInsuranceRate", 0m, 0.30m);
   _settings.VoluntaryHealthInsuranceAdditionalRate = ReadPercent("VoluntaryHealthInsuranceAdditionalRate", 0m, 0.20m);
   _settings.CareInsuranceChildlessRate = ReadPercent("CareInsuranceChildlessRate", 0m, 0.20m);
   _settings.HealthInsuranceBaseYear = ReadInt("HealthInsuranceBaseYear");
   _settings.HealthInsuranceAssessmentIncreaseRate = ReadPercent("HealthInsuranceAssessmentIncreaseRate", -0.50m, 1m);
   _settings.HealthInsuranceAdditionalRateAnnualChange = ReadPercent("HealthInsuranceAdditionalRateAnnualChange", -0.20m, 0.20m);
   _settings.CareInsuranceRateAnnualChange = ReadPercent("CareInsuranceRateAnnualChange", -0.20m, 0.20m);

   _settings.Person1PensionGrossMonthly = ReadDecimal("Person1PensionGrossMonthly", 0m, 100000m);
   _settings.KvdrPerson1 = ReadBool("KvdrPerson1");

   if (_settings.HouseholdPersonCount == 2)
   {
    _settings.Person2PensionGrossMonthly = ReadDecimal("Person2PensionGrossMonthly", 0m, 100000m);
    _settings.KvdrPerson2 = ReadBool("KvdrPerson2");
   }

   _settings.StartCapital = ReadDecimal("StartCapital", 0m, 1000000000m);

   _settings.ReserveYears = ReadDecimal("ReserveYears", 0m, 20m);
   _settings.AutoRefillReserve = ReadBool("AutoRefillReserve");
   _settings.UseReserveOnNegativeStockYear = ReadBool("UseReserveOnNegativeStockYear");
   _settings.CashInterestRate = ReadPercent("CashInterestRate", -0.05m, 0.20m);

   _settings.HouseTotalValue = ReadDecimal("HouseTotalValue", 0m, 100000000m);
   _settings.HouseBuildingShare = ReadPercent("HouseBuildingShare", 0m, 1m);
   _settings.HouseReserveRate = ReadPercent("HouseReserveRate", 0m, 0.20m);
   _settings.CarReplacementValue = ReadDecimal("CarReplacementValue", 0m, 1000000m);
   _settings.CarReplacementYears = ReadInt("CarReplacementYears");
   _settings.HealthReserveTarget = ReadDecimal("HealthReserveTarget", 0m, 10000000m);
   _settings.TravelReserveTarget = ReadDecimal("TravelReserveTarget", 0m, 10000000m);
   _settings.OtherReserveTarget = ReadDecimal("OtherReserveTarget", 0m, 10000000m);

   _settings.WorldEtfCurrentValue = ReadDecimal("WorldEtfCurrentValue", 0m, 1000000000m);
   _settings.WorldEtfStartYear = ReadInt("WorldEtfStartYear");
   _settings.WorldEtfHistoricalReturn = ReadPercent("WorldEtfHistoricalReturn", -0.99m, 1m);
   _settings.WorldEtfReturn = ReadPercent("WorldEtfReturn", -1m, 1m);
   _settings.WorldEtfDistribution = ReadPercent("WorldEtfDistribution", 0m, 0.50m);
   _settings.DividendEtfCurrentValue = ReadDecimal("DividendEtfCurrentValue", 0m, 1000000000m);
   _settings.DividendEtfStartYear = ReadInt("DividendEtfStartYear");
   _settings.DividendEtfHistoricalReturn = ReadPercent("DividendEtfHistoricalReturn", -0.99m, 1m);
   _settings.DividendEtfReturn = ReadPercent("DividendEtfReturn", -1m, 1m);
   _settings.DividendEtfDistribution = ReadPercent("DividendEtfDistribution", 0m, 0.50m);
   _settings.DividendStocksCurrentValue = ReadDecimal("DividendStocksCurrentValue", 0m, 1000000000m);
   _settings.DividendStocksStartYear = ReadInt("DividendStocksStartYear");
   _settings.DividendStocksHistoricalReturn = ReadPercent("DividendStocksHistoricalReturn", -0.99m, 1m);
   _settings.DividendStocksReturn = ReadPercent("DividendStocksReturn", -1m, 1m);
   _settings.DividendStocksDistribution = ReadPercent("DividendStocksDistribution", 0m, 0.50m);

   _settings.DividendSurplusReinvest = ReadBool("DividendSurplusReinvest");
   _settings.Strategy = ReadChoice("Strategy");

   _allocation = new StrategyAllocation(
    ReadPercent("AllocCash", 0m, 1m),
    ReadPercent("AllocWorld", 0m, 1m),
    ReadPercent("AllocDividendEtf", 0m, 1m),
    ReadPercent("AllocDividendStocks", 0m, 1m));

   _settings.CapitalGainsAllowance = ReadDecimal("CapitalGainsAllowance", 0m, 100000m);
   _settings.JointTaxation =
    _settings.HouseholdPersonCount == 2 && ReadBool("JointTaxation");
   _settings.ChurchTaxEnabled = ReadBool("ChurchTaxEnabled");
   _settings.ChurchTaxRate = ReadPercent("ChurchTaxRate", 0m, 1m);
   _settings.AdvanceLumpSumBaseRate = ReadPercent("AdvanceLumpSumBaseRate", 0m, 1m);

   _settings.StressCrashPercent = ParsePercentChoice(ReadChoice("StressCrashPercent"));
   _settings.StressCrashAtStart = ReadBool("StressCrashAtStart");
   _settings.StressSecondCrashEnabled = ReadBool("StressSecondCrashEnabled");
   _settings.StressSecondCrashYear = ReadInt("StressSecondCrashYear");
   _settings.StressSecondCrashPercent = ParsePercentChoice(ReadChoice("StressSecondCrashPercent"));
   _settings.StressHealthInsuranceAssessmentAdditionalIncreaseRate = ReadPercent("StressHealthInsuranceAssessmentAdditionalIncreaseRate", 0m, 1m);
   _settings.StressHealthInsuranceAdditionalRateAnnualChange = ReadPercent("StressHealthInsuranceAdditionalRateAnnualChange", 0m, 0.20m);
   _settings.StressCareInsuranceRateAnnualChange = ReadPercent("StressCareInsuranceRateAnnualChange", 0m, 0.20m);

   _settings.HouseIncluded = ReadBool("HouseIncluded");
   _settings.HouseSaleYear = ReadInt("HouseSaleYear");
   _settings.HouseNetSaleProceeds = ReadDecimal("HouseNetSaleProceeds", 0m, 100000000m);

   int planningAgePerson1 =
    _settings.Person1Age + (_settings.PlanningYear - DateTime.Today.Year);

   if (_settings.Person1RetirementAge < planningAgePerson1)
    throw new InvalidOperationException("Der Beginn der gesetzlichen Rente darf nicht vor dem jeweiligen vorzeitigen Arbeitsende liegen.");

   if (_settings.HouseholdPersonCount == 2)
   {
    int workEndAgePerson2 =
     _settings.Person2Age + (_settings.Person2WorkEndYear - DateTime.Today.Year);

    if (_settings.Person2RetirementAge < workEndAgePerson2)
     throw new InvalidOperationException("Der Beginn der gesetzlichen Rente darf nicht vor dem jeweiligen vorzeitigen Arbeitsende liegen.");
   }

   if (_settings.Person1EndAge < _settings.Person1RetirementAge)
    throw new InvalidOperationException("Die Lebenserwartung muss nach dem Beginn der gesetzlichen Rente liegen.");

   if (_settings.HouseholdPersonCount == 2 &&
       _settings.Person2EndAge < _settings.Person2RetirementAge)
    throw new InvalidOperationException("Die Lebenserwartung muss nach dem Beginn der gesetzlichen Rente liegen.");

   if (_settings.CarReplacementYears <= 0)
    throw new InvalidOperationException("Auto-Ersatz nach Jahren muss größer als 0 sein.");

   if (_settings.VoluntaryHealthInsuranceMinimumMonthlyIncome > _settings.VoluntaryHealthInsuranceMaximumMonthlyIncome)
    throw new InvalidOperationException("Die GKV/Pflege Mindest-Bemessungsgrundlage darf nicht über der Beitragsbemessungsgrenze liegen.");

   if (_settings.HealthInsuranceBaseYear <= 0)
    throw new InvalidOperationException("Das GKV/Pflege Basisjahr muss größer als 0 sein.");

   int currentYear = DateTime.Today.Year;
   if (_settings.WorldEtfStartYear <= 0 || _settings.WorldEtfStartYear > currentYear)
    throw new InvalidOperationException("Das Startjahr des MSCI World / Welt-ETF muss im aktuellen Jahr oder davor liegen.");
   if (_settings.DividendEtfStartYear <= 0 || _settings.DividendEtfStartYear > currentYear)
    throw new InvalidOperationException("Das Startjahr des Dividenden-ETF muss im aktuellen Jahr oder davor liegen.");
   if (_settings.DividendStocksStartYear <= 0 || _settings.DividendStocksStartYear > currentYear)
    throw new InvalidOperationException("Das Startjahr der Dividenden-Aktien muss im aktuellen Jahr oder davor liegen.");

   return true;
  }
  catch (Exception ex)
  {
   error = "Eingabefehler: " + ex.Message;
   return false;
  }
 }

 private int ReadInt(string key)
 {
  if (_inputs[key] is not TextBox tb ||
      !int.TryParse(tb.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
   throw new InvalidOperationException($"Ungültige Ganzzahl bei „{key}“.");
  return value;
 }

 private decimal ReadDecimal(string key, decimal min, decimal max)
 {
  if (_inputs[key] is not TextBox tb)
   throw new InvalidOperationException($"Ungültige Zahl bei „{key}“.");

  decimal value;

  if (tb.Tag is InputType inputType && inputType == InputType.Money)
  {
   if (!decimal.TryParse(tb.Text, NumberStyles.Number, GermanCulture, out value))
    throw new InvalidOperationException($"Ungültige Zahl bei „{key}“.");
  }
  else
  {
   if (!decimal.TryParse(tb.Text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out value))
    throw new InvalidOperationException($"Ungültige Zahl bei „{key}“.");
  }

  if (value < min || value > max)
   throw new InvalidOperationException($"Wert bei „{key}“ liegt außerhalb des sinnvollen Bereichs.");

  return value;
 }

 private decimal ReadPercent(string key, decimal min, decimal max) =>
  ReadDecimal(key, min * 100m, max * 100m) / 100m;

 private bool ReadBool(string key) => ReadChoice(key) == "Ja";

 private string ReadChoice(string key)
 {
  if (_inputs[key] is not ComboBox cb || cb.SelectedItem is not string value)
   throw new InvalidOperationException($"Keine Auswahl bei „{key}“.");
  return value;
 }

 private static decimal ParsePercentChoice(string value) =>
  value switch
  {
   "-15 %" => -0.15m,
   "-40 %" => -0.40m,
   _ => -0.25m
  };

 private static string FormatPercentChoice(decimal value) =>
  value <= -0.39m ? "-40 %" :
  value <= -0.24m ? "-25 %" :
  "-15 %";

 private static string FormatMoneyValue(decimal value)
 {
  return value % 1m == 0m
   ? value.ToString("N0", GermanCulture)
   : value.ToString("N2", GermanCulture);
 }

 private void MoneyTextBox_TextChanged(object sender, TextChangedEventArgs e)
 {
  if (sender is not TextBox textBox)
   return;

  string original = textBox.Text;

  if (string.IsNullOrWhiteSpace(original))
   return;

  int caret = textBox.CaretIndex;
  int digitsBeforeCaret = original[..Math.Min(caret, original.Length)].Count(char.IsDigit);

  string cleaned = new string(original
   .Where(c => char.IsDigit(c) || c == ',')
   .ToArray());

  int firstComma = cleaned.IndexOf(',');
  if (firstComma >= 0)
  {
   string integerPart = cleaned[..firstComma];
   string decimalPart = new string(cleaned[(firstComma + 1)..].Where(char.IsDigit).Take(2).ToArray());
   cleaned = integerPart + "," + decimalPart;
  }

  string[] parts = cleaned.Split(',', 2);
  string digits = new string(parts[0].Where(char.IsDigit).ToArray());

  if (digits.Length == 0)
   digits = "0";

  if (!decimal.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out decimal integerValue))
   return;

  string formatted = integerValue.ToString("N0", GermanCulture);

  if (parts.Length == 2)
   formatted += "," + parts[1];

  if (formatted == original)
   return;

  textBox.TextChanged -= MoneyTextBox_TextChanged;
  textBox.Text = formatted;

  int newCaret = 0;
  int seenDigits = 0;

  while (newCaret < formatted.Length && seenDigits < digitsBeforeCaret)
  {
   if (char.IsDigit(formatted[newCaret]))
    seenDigits++;

   newCaret++;
  }

  textBox.CaretIndex = Math.Min(newCaret, formatted.Length);
  textBox.TextChanged += MoneyTextBox_TextChanged;
 }

 private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
 {
  if (MainTabs.SelectedItem is not TabItem selectedTab)
   return;

  string? header = selectedTab.Header?.ToString();

  if (string.Equals(header, "Berechnungsgrundlagen", StringComparison.Ordinal))
  {
   if (CalculationDocumentationText == null)
    return;

   TryReadSettings(out _);
   UpdateCalculationDocumentation();
   return;
  }

  if (string.Equals(header, "Protokoll", StringComparison.Ordinal))
   UpdateCalculationLog();
 }

 private void UpdateCalculationDocumentation()
 {
  if (CalculationDocumentationText == null)
   return;

  CalculationDocumentationText.Text =
   CalculationDocumentationService.Build(_settings, _allocation);
 }

 private void UpdateCalculationLog()
 {
  if (CalculationLogText == null)
   return;

  CalculationLogText.Text = CalculationLogService.Read();
 }

 private void CopyCalculationLog_Click(object sender, RoutedEventArgs e)
 {
  if (CalculationLogText == null ||
      string.IsNullOrEmpty(CalculationLogText.Text))
   return;

  Clipboard.SetText(CalculationLogText.Text);
 }

 private void Window_Closing(object? sender, CancelEventArgs e)
 {
  if (!_hasUnsavedChanges)
   return;

  var dialog = new SaveChangesWindow
  {
   Owner = this
  };

  dialog.ShowDialog();

  if (dialog.Choice == SaveChangesChoice.Cancel)
  {
   e.Cancel = true;
   return;
  }

  if (dialog.Choice == SaveChangesChoice.Discard)
   return;

  if (!TryReadSettings(out string error))
  {
   e.Cancel = true;
   StatusText.Text = error;
   StatusText.Foreground = (Brush)FindResource("DangerBrush");
   return;
  }

  try
  {
   SettingsService.Save(_settings, _allocation);
   _hasUnsavedChanges = false;
  }
  catch (Exception ex)
  {
   e.Cancel = true;
   StatusText.Text = "Speicherfehler: " + ex.Message;
   StatusText.Foreground = (Brush)FindResource("DangerBrush");
  }
 }

 private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
 {
  _help.ClosePinned();
 }

 private enum InputType
 {
  Integer,
  Decimal,
  Money,
  Percent
 }
}
