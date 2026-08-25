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
 private readonly Dictionary<string, int> _fieldNumbersByKey = new();
 private readonly Dictionary<string, TabItem> _fieldTabsByKey = new();
 private StackPanel? _currentInputPanel;
 private TabItem? _currentInputTab;
 private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");
 private int _fieldNumber;
 private PlannerSettings _settings;
 private ProjectionResult? _baseResult;
 private ProjectionResult? _stressResult;
 private ResultsWindow? _resultsWindow;
 private StrategyAllocation _allocation;
 private bool _hasUnsavedChanges;
 private bool _isUpdatingStrategyAllocation;
 private TextBlock? _existingDepotsStartCapitalText;
 private TextBlock? _unassignedStartCapitalText;
 private TextBlock? _strategyStartCapitalText;
 private TextBlock? _houseMaintenanceAnnualText;
 private readonly Dictionary<string, TextBlock> _allocationPercentTexts = new();

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
  _fieldNumbersByKey.Clear();
  _fieldTabsByKey.Clear();

  if (_settings.Person1WorkEndYear <= 0)
   _settings.Person1WorkEndYear = _settings.PlanningYear;

  _settings.PlanningYear = _settings.Person1WorkEndYear;

  AddSection("1. Basisdaten & Planung");
  if (_currentInputPanel == null)
   throw new InvalidOperationException("Keine Eingabe-Unterlasche aktiv.");

  _currentInputPanel.Children.Add(new TextBlock
  {
   Text = "Hinweis: Person 1 muss die ältere Person sein, Person 2 die jüngere Person. Diese Zuordnung ist für die Berechnung zwingend erforderlich.",
   TextWrapping = TextWrapping.Wrap,
   Foreground = (Brush)FindResource("WarningBrush"),
   Margin = new Thickness(20, 10, 20, 10)
  });
  AddChoice("HouseholdPersonCount", "Wie viele Personen im Haushalt (max. 2)",
   _settings.HouseholdPersonCount == 1 ? "1 Person" : "2 Personen",
   ["1 Person", "2 Personen"]);
  AddInt("Person1CurrentAge", "Aktuelles Alter Person 1", _settings.Person1Age);
  AddInt("Person1WorkEndYear", "Vorzeitiges Arbeitsende Person 1",
   _settings.Person1WorkEndYear > 0 ? _settings.Person1WorkEndYear : _settings.PlanningYear);
  AddInt("Person2CurrentAge", "Aktuelles Alter Person 2", _settings.Person2Age);
  AddInt("Person2WorkEndYear", "Vorzeitiges Arbeitsende Person 2",
   _settings.Person2WorkEndYear > 0 ? _settings.Person2WorkEndYear : _settings.PlanningYear);
  AddMoney("Person2NetIncomeMonthly", "Nettoeinkommen Person 2 pro Monat", _settings.Person2NetIncomeMonthly);
  AddPercent("Person2NetIncomeIncreaseRate", "Nettoeinkommen-Steigerung Person 2 pro Jahr [Standardwert]", _settings.Person2NetIncomeIncreaseRate);
  AddInt("Person1RetirementAge", "Geplanter Beginn gesetzliche Altersrente Person 1", _settings.Person1RetirementAge);
  AddInt("Person2RetirementAge", "Geplanter Beginn gesetzliche Altersrente Person 2", _settings.Person2RetirementAge);
  AddInt("Person1EndAge", "Lebenserwartung Person 1 [Standardwert]", _settings.Person1EndAge);
  AddInt("Person2EndAge", "Lebenserwartung Person 2 [Standardwert]", _settings.Person2EndAge);
  AddMoney("MonthlyLivingCosts", "Monatliche Ausgaben für das Leben", _settings.MonthlyLivingCosts);
  AddPercent("InflationRate", "Inflation pro Jahr [Standardwert]", _settings.InflationRate);
  AddPercent("IncomeTaxTariffAnnualIncreaseRate", "Steuertarif / Grundfreibetrag Steigerung p.a. [Standardwert]", _settings.IncomeTaxTariffAnnualIncreaseRate);
  AddMoney("CapitalGainsAllowance", "Sparer-Pauschbetrag Haushalt [Standardwert]", _settings.CapitalGainsAllowance);
  AddBool("JointTaxation", "Gemeinsame steuerliche Veranlagung [Standardwert]", _settings.JointTaxation);
  AddBool("ChurchTaxEnabled", "Kirchensteuer berücksichtigen", _settings.ChurchTaxEnabled);
  AddPercent("ChurchTaxRate", "Kirchensteuersatz (optional)", _settings.ChurchTaxRate);

  AddSection("2. Gesetzliche Rente");
  AddMoney("Person1PensionGrossMonthly", "Rente Person 1 – heute bereits erworben (brutto/Monat)", _settings.Person1PensionGrossMonthly);
  AddMoney("Person2PensionGrossMonthly", "Rente Person 2 – heute bereits erworben (brutto/Monat)", _settings.Person2PensionGrossMonthly);
  AddMoney("Person1ProjectedPensionGrossMonthlyAt67", "Rente Person 1 – hochgerechnet mit 67 brutto/Monat (optional)", _settings.Person1ProjectedPensionGrossMonthlyAt67);
  AddMoney("Person2ProjectedPensionGrossMonthlyAt67", "Rente Person 2 – hochgerechnet mit 67 brutto/Monat (optional)", _settings.Person2ProjectedPensionGrossMonthlyAt67);
  AddInt("Person1CurrentInsuranceYears", "Bisherige Versicherungsjahre Person 1", _settings.Person1CurrentInsuranceYears);
  AddInt("Person2CurrentInsuranceYears", "Bisherige Versicherungsjahre Person 2", _settings.Person2CurrentInsuranceYears);
  AddDecimal("Person1CurrentPensionPoints", "Entgeltpunkte Person 1 (optional)", _settings.Person1CurrentPensionPoints);
  AddDecimal("Person2CurrentPensionPoints", "Entgeltpunkte Person 2 (optional)", _settings.Person2CurrentPensionPoints);
  AddMoney("Person1PensionableAnnualGross", "Brutto Jahresgehalt Person 1 (optional)", _settings.Person1PensionableAnnualGross);
  AddMoney("Person2PensionableAnnualGross", "Brutto Jahresgehalt Person 2 (optional)", _settings.Person2PensionableAnnualGross);
  AddPercent("Person1PensionableAnnualGrossIncreaseRate", "Brutto Jahresgehalt Person 1 Steigerung p.a. (optional)", _settings.Person1PensionableAnnualGrossIncreaseRate);
  AddPercent("Person2PensionableAnnualGrossIncreaseRate", "Brutto Jahresgehalt Person 2 Steigerung p.a. (optional)", _settings.Person2PensionableAnnualGrossIncreaseRate);
  AddPercent("PensionAverageAnnualEarningsIncreaseRate", "Entwicklung der durchschnittlichen Löhne p.a. (optional) [Standardwert]", _settings.PensionAverageAnnualEarningsIncreaseRate);
  AddPercent("PensionIncreaseRate", "Konservative Rentensteigerung pro Jahr [Standardwert]", _settings.PensionIncreaseRate);

  AddSection("3. Kranken- & Pflegeversicherung");
  AddBool("KvdrPerson1", "KVdR für Person 1 annehmen (optional) [Standardwert]", _settings.KvdrPerson1);
  AddBool("KvdrPerson2", "KVdR für Person 2 annehmen (optional) [Standardwert]", _settings.KvdrPerson2);
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

  AddSection("4. Rücklagen / Sonderausgaben");
  AddMoney("HouseTotalValue", "Hauswert inkl. Grundstück", _settings.HouseTotalValue);
  AddPercent("HouseBuildingShare", "Anteil Gebäude am Hauswert [Standardwert]", _settings.HouseBuildingShare);

  _houseMaintenanceAnnualText = new TextBlock
  {
   VerticalAlignment = VerticalAlignment.Center,
   HorizontalAlignment = HorizontalAlignment.Center,
   Margin = new Thickness(18, 0, 0, 0),
   FontWeight = FontWeights.SemiBold,
   Foreground = (Brush)FindResource("AccentBrush")
  };

  AddDecimal("HouseLivingArea", "Wohnfläche der Immobilie in m²", _settings.HouseLivingArea);
  AddTextInput(
   "HouseAge",
   "Alter der Immobilie in Jahren",
   _settings.HouseAge.ToString(CultureInfo.InvariantCulture),
   InputType.Integer,
   _houseMaintenanceAnnualText);
  AddMoney("CarReplacementValue", "Ersatzwert Auto", _settings.CarReplacementValue);
  AddInt("CarReplacementYears", "Auto-Ersatz nach Jahren [Standardwert]", _settings.CarReplacementYears);
  AddMoney("HealthReserveTarget", "Gesundheit / Zahnersatz Rücklage [Standardwert]", _settings.HealthReserveTarget);
  AddMoney("TravelReserveTarget", "Reisen / größere Wünsche Rücklage", _settings.TravelReserveTarget);
  AddMoney("OtherReserveTarget", "Sonstiges / Unvorhergesehenes Rücklage", _settings.OtherReserveTarget);

  AddSection("5. Vermögen");
  if (_currentInputPanel == null)
   throw new InvalidOperationException("Keine Eingabe-Unterlasche aktiv.");

  _unassignedStartCapitalText = new TextBlock
  {
   VerticalAlignment = VerticalAlignment.Center,
   Margin = new Thickness(10, 0, 0, 0)
  };
  AddMoney(
   "StartCapital",
   "Startvermögen bei vorzeitigem Arbeitsende Person 1",
   _settings.StartCapital,
   _unassignedStartCapitalText);

  _existingDepotsStartCapitalText = new TextBlock
  {
   TextWrapping = TextWrapping.Wrap,
   Foreground = (Brush)FindResource("MutedTextBrush"),
   Margin = new Thickness(20, 10, 20, 4)
  };
  _currentInputPanel.Children.Add(_existingDepotsStartCapitalText);

  _currentInputPanel.Children.Add(new TextBlock
  {
   Text = "Das Startvermögen entspricht 100 %. Alle darunter eingetragenen Anlagewerte sind Bestandteil dieses Gesamtvermögens und werden nicht zusätzlich hinzugerechnet.",
   TextWrapping = TextWrapping.Wrap,
   Foreground = (Brush)FindResource("WarningBrush"),
   Margin = new Thickness(20, 0, 20, 10)
  });

  AddAllocationMoney("SecureInvestmentCurrentValue", "Wert der bereits vorhandenen sicheren Anlage (optional)", _settings.SecureInvestmentCurrentValue);
  AddPercent("CashInterestRate", "Zins sichere Anlage [Standardwert]", _settings.CashInterestRate);
  AddAllocationMoney("WorldEtfCurrentValue", "Wert des bereits vorhandenen Welt-ETF (optional)", _settings.WorldEtfCurrentValue);
  AddInt("WorldEtfStartYear", "Seit wann besteht dieser Welt-ETF? (optional)", _settings.WorldEtfStartYear);
  AddPercent("WorldEtfHistoricalReturn", "Bisherige durchschnittliche Rendite des Welt-ETF (optional) [Standardwert]", _settings.WorldEtfHistoricalReturn);
  AddPercent("WorldEtfReturn", "MSCI World / Welt-ETF Gesamtrendite [Standardwert]", _settings.WorldEtfReturn);
  AddPercent("WorldEtfDistribution", "MSCI World / Welt-ETF Ausschüttung", _settings.WorldEtfDistribution);
  AddAllocationMoney("DividendEtfCurrentValue", "Wert des bereits vorhandenen Dividenden-ETF (optional)", _settings.DividendEtfCurrentValue);
  AddInt("DividendEtfStartYear", "Seit wann besteht dieser Dividenden-ETF? (optional)", _settings.DividendEtfStartYear);
  AddPercent("DividendEtfHistoricalReturn", "Bisherige durchschnittliche Rendite des Dividenden-ETF (optional) [Standardwert]", _settings.DividendEtfHistoricalReturn);
  AddPercent("DividendEtfReturn", "Dividenden-ETF Gesamtrendite [Standardwert]", _settings.DividendEtfReturn);
  AddPercent("DividendEtfDistribution", "Dividenden-ETF Ausschüttung", _settings.DividendEtfDistribution);
  AddAllocationMoney("DividendStocksCurrentValue", "Wert der bereits vorhandenen Dividenden-Aktien (optional)", _settings.DividendStocksCurrentValue);
  AddInt("DividendStocksStartYear", "Seit wann bestehen diese Dividenden-Aktien? (optional)", _settings.DividendStocksStartYear);
  AddPercent("DividendStocksHistoricalReturn", "Bisherige durchschnittliche Rendite der Dividenden-Aktien (optional) [Standardwert]", _settings.DividendStocksHistoricalReturn);
  AddPercent("DividendStocksReturn", "Dividenden-Aktien Gesamtrendite [Standardwert]", _settings.DividendStocksReturn);
  AddPercent("DividendStocksDistribution", "Dividenden-Aktien Ausschüttung", _settings.DividendStocksDistribution);
  AddBool("DividendSurplusReinvest", "Nicht benötigte Dividenden wieder anlegen", _settings.DividendSurplusReinvest);

  AddSection("6. Strategie & Aufteilung");
  if (_currentInputPanel == null)
   throw new InvalidOperationException("Keine Eingabe-Unterlasche aktiv.");

  _strategyStartCapitalText = new TextBlock
  {
   TextWrapping = TextWrapping.Wrap,
   Foreground = (Brush)FindResource("MutedTextBrush"),
   Margin = new Thickness(20, 10, 20, 4)
  };
  _currentInputPanel.Children.Add(_strategyStartCapitalText);

  _currentInputPanel.Children.Add(new TextBlock
  {
   Text = "Die Werte 77–80 werden aus dem Ist-Stand und der gewählten Strategie automatisch berechnet. Bestehende Anlagen aus Rubrik 5 werden nicht verkauft oder reduziert. Bei „Benutzerdefiniert (Ist-Stand)“ wird noch nicht zugeordnetes Startvermögen der sicheren Anlage zugerechnet. Die Standardstrategien verteilen nur noch nicht zugeordnetes Kapital in Richtung ihrer Zielaufteilung.",
   TextWrapping = TextWrapping.Wrap,
   Foreground = (Brush)FindResource("WarningBrush"),
   Margin = new Thickness(20, 0, 20, 10)
  });

  AddChoice("Strategy", "Strategie [Standardwert]", GetDisplayedStrategy(),
   ["Benutzerdefiniert (Ist-Stand)", "Sicherheit", "Ausgewogen", "Wachstum"]);
  AddDecimal("ReserveYears", "Sichere Reserve in Jahresausgaben [Standardwert]", _settings.ReserveYears);
  AddBool("AutoRefillReserve", "Reserve automatisch wieder auffüllen [Standardwert]", _settings.AutoRefillReserve);
  AddBool("UseReserveOnNegativeStockYear", "Bei negativem Aktienjahr zuerst Reserve nutzen [Standardwert]", _settings.UseReserveOnNegativeStockYear);
  AddOptionalAllocationMoney("AllocCash", "Sichere Anlage Zielbetrag (optional) [Standardwert]", _settings.StartCapital * _allocation.Cash);
  AddOptionalAllocationMoney("AllocWorld", "MSCI World / Welt-ETF Zielbetrag (optional) [Standardwert]", _settings.StartCapital * _allocation.WorldEtf);
  AddOptionalAllocationMoney("AllocDividendEtf", "Dividenden-ETF Zielbetrag (optional) [Standardwert]", _settings.StartCapital * _allocation.DividendEtf);
  AddOptionalAllocationMoney("AllocDividendStocks", "Dividenden-Aktien Zielbetrag (optional) [Standardwert]", _settings.StartCapital * _allocation.DividendStocks);
  AddPercent("AdvanceLumpSumBaseRate", "Basiszins Vorabpauschale Stand 2026 (optional) [Standardwert]", _settings.AdvanceLumpSumBaseRate);

  AddSection("7. Stressszenario");
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

  AddSection("8. Haus optional");
  AddBool("HouseIncluded", "Hausverkauf in Planung berücksichtigen", _settings.HouseIncluded);
  AddInt("HouseSaleYear", "Haus-Verkaufsjahr", _settings.HouseSaleYear);
  AddMoney("HouseNetSaleProceeds", "Nettoerlös Hausverkauf", _settings.HouseNetSaleProceeds);

  UpdateHouseholdPersonVisibility();
  UpdateStartCapitalReferenceDisplays();
  UpdateStrategyAllocation();
  UpdateAllocationPercentDisplays();
  UpdateUnassignedStartCapitalDisplay();
  UpdateCalculatedHealthDisplays();
  UpdateHouseMaintenanceAnnualDisplay();
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
  _currentInputTab = tabItem;

  if (InputSubTabs.SelectedIndex < 0)
   InputSubTabs.SelectedIndex = 0;
 }

 private void AddInt(string key, string label, int value) =>
  AddTextInput(key, label, value.ToString(CultureInfo.InvariantCulture), InputType.Integer);

 private void AddMoney(string key, string label, decimal value) =>
  AddTextInput(key, label, FormatMoneyValue(value), InputType.Money);

 private void AddMoney(string key, string label, decimal value, TextBlock valueInfo) =>
  AddTextInput(key, label, FormatMoneyValue(value), InputType.Money, valueInfo);

 private void AddAllocationMoney(string key, string label, decimal value)
 {
  var percentText = new TextBlock
  {
   VerticalAlignment = VerticalAlignment.Center,
   Margin = new Thickness(10, 0, 0, 0)
  };

  _allocationPercentTexts[key] = percentText;
  AddTextInput(key, label, FormatMoneyValue(value), InputType.Money, percentText);
 }

 private void AddOptionalAllocationMoney(string key, string label, decimal value)
 {
  var percentText = new TextBlock
  {
   VerticalAlignment = VerticalAlignment.Center,
   Margin = new Thickness(10, 0, 0, 0)
  };

  _allocationPercentTexts[key] = percentText;
  AddTextInput(key, label, FormatMoneyValue(value), InputType.OptionalMoney, percentText);
 }

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
  RegisterFieldNavigation(key);
 }

 private void AddDecimal(string key, string label, decimal value) =>
  AddTextInput(key, label, value.ToString("0.##", CultureInfo.InvariantCulture), InputType.Decimal);

 private void AddPercent(string key, string label, decimal value) =>
  AddTextInput(key, label, (value * 100m).ToString("0.##", CultureInfo.InvariantCulture), InputType.Percent);

 private void AddTextInput(
  string key,
  string label,
  string value,
  InputType type,
  TextBlock? valueInfo = null)
 {
  var textBox = new TextBox { Text = value, Tag = type };

  if (type is InputType.Money or InputType.OptionalMoney)
   textBox.TextChanged += MoneyTextBox_TextChanged;

  textBox.GotKeyboardFocus += TextBox_GotKeyboardFocus;
  textBox.LostKeyboardFocus += TextBox_LostKeyboardFocus;
  textBox.TextChanged += (_, _) => UpdateCalculatedHealthDisplays();
  textBox.TextChanged += (_, _) => _hasUnsavedChanges = true;

  if (key is "HouseLivingArea" or "HouseAge" or "InflationRate")
   textBox.TextChanged += (_, _) => UpdateHouseMaintenanceAnnualDisplay();

  if (key == "StartCapital")
  {
   textBox.TextChanged += (_, _) => UpdateStartCapitalReferenceDisplays();
   textBox.TextChanged += (_, _) => UpdateStrategyAllocation();
   textBox.TextChanged += (_, _) => UpdateAllocationPercentDisplays();
   textBox.TextChanged += (_, _) => UpdateUnassignedStartCapitalDisplay();
  }
  if (key is "AllocCash" or "AllocWorld" or "AllocDividendEtf" or "AllocDividendStocks")
  {
   textBox.TextChanged += (_, _) => UpdateAllocationPercentDisplays();
   textBox.TextChanged += (_, _) => UpdateDisplayedStrategyFromAllocation();
  }
  else if (key is "SecureInvestmentCurrentValue" or "WorldEtfCurrentValue" or "DividendEtfCurrentValue" or "DividendStocksCurrentValue")
  {
   textBox.TextChanged += (_, _) => UpdateStrategyAllocation();
   textBox.TextChanged += (_, _) => UpdateAllocationPercentDisplays();
   textBox.TextChanged += (_, _) => UpdateUnassignedStartCapitalDisplay();
  }

  _inputs[key] = textBox;
  if (_currentInputPanel == null)
   throw new InvalidOperationException("Keine Eingabe-Unterlasche aktiv.");

  _currentInputPanel.Children.Add(CreateRow(label, textBox, valueInfo));
  RegisterFieldNavigation(key);
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
  RegisterFieldNavigation(key);
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
  RegisterFieldNavigation(key);
 }

 private void RegisterFieldNavigation(string key)
 {
  _fieldNumbersByKey[key] = _fieldNumber;

  if (_currentInputTab != null)
   _fieldTabsByKey[key] = _currentInputTab;
 }

 private FrameworkElement CreateRow(
  string label,
  FrameworkElement input,
  TextBlock? valueInfo = null)
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

  if (valueInfo != null || isStandardValue)
  {
   var infoPanel = new StackPanel
   {
    Orientation = Orientation.Horizontal,
    VerticalAlignment = VerticalAlignment.Center
   };

   if (valueInfo != null)
    infoPanel.Children.Add(valueInfo);

   if (isStandardValue)
   {
    infoPanel.Children.Add(new TextBlock
    {
     Text = "[Standardwert]",
     Foreground = (Brush)FindResource("AccentBrush"),
     FontWeight = FontWeights.SemiBold,
     VerticalAlignment = VerticalAlignment.Center,
     Margin = new Thickness(10, 0, 0, 0)
    });
   }

   Grid.SetColumn(infoPanel, 3);
   grid.Children.Add(infoPanel);
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
   "Person2ProjectedPensionGrossMonthlyAt67",
   "Person2CurrentInsuranceYears",
   "Person2CurrentPensionPoints",
   "Person2PensionableAnnualGross",
   "Person2PensionableAnnualGrossIncreaseRate",
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

 private StrategyAllocation ReadDesiredAllocation(decimal startCapital)
 {
  string[] keys = ["AllocCash", "AllocWorld", "AllocDividendEtf", "AllocDividendStocks"];

  bool[] empty = keys
   .Select(key =>
    _inputs.TryGetValue(key, out FrameworkElement? element) &&
    element is TextBox textBox &&
    string.IsNullOrWhiteSpace(textBox.Text))
   .ToArray();

  if (empty.All(value => value))
  {
   string strategy = ReadChoice("Strategy");
   return GetDynamicStrategyAllocation(strategy);
  }

  if (empty.Any(value => value))
   throw new InvalidOperationException(
    "Entweder alle vier Zielbeträge ausfüllen oder alle vier Felder leer lassen.");

  decimal allocationCash = ReadDecimal("AllocCash", 0m, 1000000000m);
  decimal allocationWorld = ReadDecimal("AllocWorld", 0m, 1000000000m);
  decimal allocationDividendEtf = ReadDecimal("AllocDividendEtf", 0m, 1000000000m);
  decimal allocationDividendStocks = ReadDecimal("AllocDividendStocks", 0m, 1000000000m);
  decimal allocationTotal =
   allocationCash +
   allocationWorld +
   allocationDividendEtf +
   allocationDividendStocks;

  if (Math.Abs(allocationTotal - startCapital) > 0.01m)
   throw new InvalidOperationException(
    $"Die Summe der vier Zielbeträge muss genau dem Startvermögen von {FormatMoneyValue(startCapital)} € entsprechen.");

  if (startCapital <= 0m)
   return _allocation;

  return new StrategyAllocation(
   allocationCash / startCapital,
   allocationWorld / startCapital,
   allocationDividendEtf / startCapital,
   allocationDividendStocks / startCapital);
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

 private decimal GetCurrentStartCapital()
 {
  if (_inputs.TryGetValue("StartCapital", out FrameworkElement? startCapitalElement) &&
      startCapitalElement is TextBox startCapitalTextBox &&
      decimal.TryParse(startCapitalTextBox.Text, NumberStyles.Number, GermanCulture, out decimal startCapital))
   return startCapital;

  return _settings.StartCapital;
 }

 private void UpdateStartCapitalReferenceDisplays()
 {
  decimal startCapital = GetCurrentStartCapital();

  if (_existingDepotsStartCapitalText != null)
   _existingDepotsStartCapitalText.Text =
    $"Startvermögen: {FormatMoneyValue(startCapital)} € = 100 %";

  if (_strategyStartCapitalText != null)
   _strategyStartCapitalText.Text =
    $"Startvermögen: {FormatMoneyValue(startCapital)} € = 100 %";
 }

 private void UpdateUnassignedStartCapitalDisplay()
 {
  if (_unassignedStartCapitalText == null)
   return;

  decimal startCapital = GetCurrentStartCapital();

  if (startCapital <= 0m)
  {
   _unassignedStartCapitalText.Text = "Noch nicht zugeordnet: —";
   return;
  }

  decimal assigned = 0m;

  foreach (string key in new[]
  {
   "SecureInvestmentCurrentValue",
   "WorldEtfCurrentValue",
   "DividendEtfCurrentValue",
   "DividendStocksCurrentValue"
  })
  {
   if (_inputs.TryGetValue(key, out FrameworkElement? element) &&
       element is TextBox textBox &&
       decimal.TryParse(textBox.Text, NumberStyles.Number, GermanCulture, out decimal amount))
    assigned += Math.Max(0m, amount);
  }

  decimal difference = startCapital - assigned;
  decimal percent = Math.Abs(difference) / startCapital;

  _unassignedStartCapitalText.Text = difference >= 0m
   ? $"Noch nicht zugeordnet: {FormatMoneyValue(difference)} € ({percent:P1})"
   : $"Überzugeordnet: {FormatMoneyValue(Math.Abs(difference))} € ({percent:P1})";
 }

 private void UpdateAllocationPercentDisplays()
 {
  decimal startCapital = GetCurrentStartCapital();

  foreach (string key in new[]
  {
   "SecureInvestmentCurrentValue",
   "WorldEtfCurrentValue",
   "DividendEtfCurrentValue",
   "DividendStocksCurrentValue",
   "AllocCash",
   "AllocWorld",
   "AllocDividendEtf",
   "AllocDividendStocks"
  })
  {
   if (!_allocationPercentTexts.TryGetValue(key, out TextBlock? percentText) ||
       !_inputs.TryGetValue(key, out FrameworkElement? element) ||
       element is not TextBox textBox ||
       !decimal.TryParse(textBox.Text, NumberStyles.Number, GermanCulture, out decimal amount) ||
       startCapital <= 0m)
   {
    if (_allocationPercentTexts.TryGetValue(key, out TextBlock? unavailableText))
     unavailableText.Text = "(—)";

    continue;
   }

   percentText.Text = $"({amount / startCapital:P1})";
  }
 }

 private void UpdateStrategyAllocation()
 {
  if (_isUpdatingStrategyAllocation ||
      !_inputs.TryGetValue("Strategy", out FrameworkElement? strategyElement) ||
      strategyElement is not ComboBox strategyCombo ||
      strategyCombo.SelectedItem is not string strategy ||
      !_inputs.TryGetValue("AllocCash", out FrameworkElement? cashElement) ||
      cashElement is not TextBox cashTextBox ||
      !_inputs.TryGetValue("AllocWorld", out FrameworkElement? worldElement) ||
      worldElement is not TextBox worldTextBox ||
      !_inputs.TryGetValue("AllocDividendEtf", out FrameworkElement? dividendEtfElement) ||
      dividendEtfElement is not TextBox dividendEtfTextBox ||
      !_inputs.TryGetValue("AllocDividendStocks", out FrameworkElement? dividendStocksElement) ||
      dividendStocksElement is not TextBox dividendStocksTextBox)
   return;

  decimal startCapital = GetCurrentStartCapital();
  StrategyAllocation allocation = GetDynamicStrategyAllocation(strategy);

  _isUpdatingStrategyAllocation = true;
  try
  {
   cashTextBox.Text = FormatMoneyValue(startCapital * allocation.Cash);
   worldTextBox.Text = FormatMoneyValue(startCapital * allocation.WorldEtf);
   dividendEtfTextBox.Text = FormatMoneyValue(startCapital * allocation.DividendEtf);
   dividendStocksTextBox.Text = FormatMoneyValue(startCapital * allocation.DividendStocks);
  }
  finally
  {
   _isUpdatingStrategyAllocation = false;
  }

  UpdateAllocationPercentDisplays();
 }

 private StrategyAllocation GetDynamicStrategyAllocation(string strategy)
 {
  decimal startCapital = GetCurrentStartCapital();
  if (startCapital <= 0m)
   return new StrategyAllocation(0m, 0m, 0m, 0m);

  decimal existingCash = GetCurrentInvestmentAmount(
   "SecureInvestmentCurrentValue",
   _settings.SecureInvestmentCurrentValue);
  decimal existingWorld = GetCurrentInvestmentAmount(
   "WorldEtfCurrentValue",
   _settings.WorldEtfCurrentValue);
  decimal existingDividendEtf = GetCurrentInvestmentAmount(
   "DividendEtfCurrentValue",
   _settings.DividendEtfCurrentValue);
  decimal existingDividendStocks = GetCurrentInvestmentAmount(
   "DividendStocksCurrentValue",
   _settings.DividendStocksCurrentValue);

  if (strategy is "Benutzerdefiniert" or "Benutzerdefiniert (Ist-Stand)")
  {
   decimal existingTotal =
    existingCash +
    existingWorld +
    existingDividendEtf +
    existingDividendStocks;
   decimal unassignedCapital = Math.Max(0m, startCapital - existingTotal);

   return new StrategyAllocation(
    (existingCash + unassignedCapital) / startCapital,
    existingWorld / startCapital,
    existingDividendEtf / startCapital,
    existingDividendStocks / startCapital);
  }

  PlannerSettings previewSettings = SettingsClone.Clone(_settings);
  previewSettings.StartCapital = startCapital;
  previewSettings.SecureInvestmentCurrentValue = existingCash;
  previewSettings.WorldEtfCurrentValue = existingWorld;
  previewSettings.DividendEtfCurrentValue = existingDividendEtf;
  previewSettings.DividendStocksCurrentValue = existingDividendStocks;

  return ProjectionService.GetInitialAllocation(
   previewSettings,
   StrategyService.GetDefault(strategy));
 }

 private decimal GetCurrentInvestmentAmount(string key, decimal fallback)
 {
  if (_inputs.TryGetValue(key, out FrameworkElement? element) &&
      element is TextBox textBox &&
      decimal.TryParse(
       textBox.Text,
       NumberStyles.Number,
       GermanCulture,
       out decimal amount))
   return Math.Max(0m, amount);

  return Math.Max(0m, fallback);
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

  decimal startCapital = GetCurrentStartCapital();
  if (startCapital <= 0m)
   return;

  var allocation = new StrategyAllocation(
   cash / startCapital,
   world / startCapital,
   dividendEtf / startCapital,
   dividendStocks / startCapital);

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
  return _settings.Strategy is "Sicherheit" or "Ausgewogen" or "Wachstum"
   ? _settings.Strategy
   : "Benutzerdefiniert (Ist-Stand)";
 }

 private string GetStrategyForAllocation(StrategyAllocation allocation)
 {
  foreach (string strategy in new[]
  {
   "Benutzerdefiniert (Ist-Stand)",
   "Sicherheit",
   "Ausgewogen",
   "Wachstum"
  })
  {
   StrategyAllocation candidate = GetDynamicStrategyAllocation(strategy);

   if (Math.Abs(allocation.Cash - candidate.Cash) <= 0.000001m &&
       Math.Abs(allocation.WorldEtf - candidate.WorldEtf) <= 0.000001m &&
       Math.Abs(allocation.DividendEtf - candidate.DividendEtf) <= 0.000001m &&
       Math.Abs(allocation.DividendStocks - candidate.DividendStocks) <= 0.000001m)
    return strategy;
  }

  return "Benutzerdefiniert (Ist-Stand)";
 }


 private void UpdateHouseMaintenanceAnnualDisplay()
 {
  if (_houseMaintenanceAnnualText == null)
   return;

  if (!_inputs.TryGetValue("HouseLivingArea", out FrameworkElement? livingAreaElement) ||
      livingAreaElement is not TextBox livingAreaTextBox ||
      !_inputs.TryGetValue("HouseAge", out FrameworkElement? houseAgeElement) ||
      houseAgeElement is not TextBox houseAgeTextBox ||
      !decimal.TryParse(
       livingAreaTextBox.Text.Replace(',', '.'),
       NumberStyles.Number,
       CultureInfo.InvariantCulture,
       out decimal livingArea) ||
      !int.TryParse(
       houseAgeTextBox.Text,
       NumberStyles.Integer,
       CultureInfo.InvariantCulture,
       out int houseAge) ||
      livingArea < 0m ||
      houseAge < 0)
  {
   _houseMaintenanceAnnualText.Text = "{ — €/Jahr }";
   return;
  }

  PlannerSettings previewSettings = SettingsClone.Clone(_settings);
  previewSettings.HouseLivingArea = livingArea;
  previewSettings.HouseAge = houseAge;

  if (_inputs.TryGetValue("InflationRate", out FrameworkElement? inflationElement) &&
      inflationElement is TextBox inflationTextBox &&
      decimal.TryParse(
       inflationTextBox.Text.Replace(',', '.'),
       NumberStyles.Number,
       CultureInfo.InvariantCulture,
       out decimal inflationPercent))
   previewSettings.InflationRate = inflationPercent / 100m;

  decimal annualMaintenance =
   ProjectionService.CalculateHouseMaintenanceExpense(
    previewSettings,
    DateTime.Today.Year);

  _houseMaintenanceAnnualText.Text =
   $"{{ {FormatMoneyValue(annualMaintenance)} €/Jahr }}";
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
   previewSettings.Person1WorkEndYear = ReadInt("Person1WorkEndYear");
   previewSettings.PlanningYear = previewSettings.Person1WorkEndYear;
   previewSettings.Person1RetirementAge = ReadInt("Person1RetirementAge");

   if (previewSettings.HouseholdPersonCount == 2)
   {
    previewSettings.Person2Age = ReadInt("Person2CurrentAge");
    previewSettings.Person2WorkEndYear = ReadInt("Person2WorkEndYear");
    previewSettings.Person2NetIncomeMonthly = ReadDecimal("Person2NetIncomeMonthly", 0m, 1000000m);
    previewSettings.Person2NetIncomeIncreaseRate = ReadPercent("Person2NetIncomeIncreaseRate", -0.50m, 1m);
    previewSettings.Person2RetirementAge = ReadInt("Person2RetirementAge");
   }
   previewSettings.SecureInvestmentCurrentValue = ReadDecimal("SecureInvestmentCurrentValue", 0m, 1000000000m);
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

   previewAllocation = ReadDesiredAllocation(previewSettings.StartCapital);
   return true;
  }
  catch
  {
   previewAllocation = _allocation;
   return false;
  }
 }

 private void ShowInputError(
  string error,
  IEnumerable<string> fieldKeys)
 {
  StatusText.Inlines.Clear();

  StatusText.Inlines.Add(new System.Windows.Documents.Run(error)
  {
   Foreground = (Brush)FindResource("DangerBrush")
  });

  List<string> keys = fieldKeys
   .Where(key => _fieldNumbersByKey.ContainsKey(key))
   .Distinct(StringComparer.Ordinal)
   .OrderBy(key => _fieldNumbersByKey[key])
   .ToList();

  if (keys.Count == 0)
   return;

  StatusText.Inlines.Add(new System.Windows.Documents.Run(" | Ursächliche Felder: ")
  {
   Foreground = (Brush)FindResource("MutedTextBrush")
  });

  for (int i = 0; i < keys.Count; i++)
  {
   string key = keys[i];

   if (i > 0)
   {
    StatusText.Inlines.Add(new System.Windows.Documents.Run(", ")
    {
     Foreground = (Brush)FindResource("MutedTextBrush")
    });
   }

   var link = new System.Windows.Documents.Hyperlink(
    new System.Windows.Documents.Run($"{_fieldNumbersByKey[key]:00}"))
   {
    Foreground = (Brush)FindResource("AccentBrush"),
    Cursor = Cursors.Hand
   };

   link.Click += (_, _) => NavigateToInputField(key);
   StatusText.Inlines.Add(link);
  }
 }

 private void NavigateToInputField(string key)
 {
  if (_fieldTabsByKey.TryGetValue(key, out TabItem? tabItem))
  {
   MainTabs.SelectedIndex = 0;
   InputSubTabs.SelectedItem = tabItem;
  }

  if (!_inputs.TryGetValue(key, out FrameworkElement? input))
   return;

  input.BringIntoView();
  input.Focus();

  if (input is TextBox textBox)
   textBox.SelectAll();
 }

 private SortedDictionary<int, string> GetRawInputValuesForLog()
 {
  var values = new SortedDictionary<int, string>();

  foreach (KeyValuePair<string, int> field in _fieldNumbersByKey)
  {
   if (!_inputs.TryGetValue(field.Key, out FrameworkElement? input))
    continue;

   string value = input switch
   {
    TextBox textBox => textBox.Text,
    ComboBox comboBox => comboBox.SelectedItem?.ToString() ?? "",
    _ => ""
   };

   values[field.Value] = value;
  }

  return values;
 }

 private void WriteFailedCalculationLog(string error)
 {
  CalculationLogService.WriteFailure(
   error,
   GetRawInputValuesForLog());

  UpdateCalculationLog();
 }

 private void Calculate_Click(object sender, RoutedEventArgs e)
 {
  if (!TryReadSettings(
       out string error,
       out IReadOnlyList<string> errorFieldKeys))
  {
   ShowInputError(error, errorFieldKeys);
   WriteFailedCalculationLog(error);
   return;
  }

  decimal sum = _allocation.Cash + _allocation.WorldEtf + _allocation.DividendEtf + _allocation.DividendStocks;
  if (Math.Abs(sum - 1m) > 0.0001m)
  {
   string allocationError =
    $"Warnung: Anlageaufteilung ergibt {sum:P1} statt 100 %.";

   ShowInputError(
    allocationError,
    ["AllocCash", "AllocWorld", "AllocDividendEtf", "AllocDividendStocks"]);
   WriteFailedCalculationLog(allocationError);
   return;
  }

  try
  {
   _baseResult = ProjectionService.Calculate(_settings, _allocation, false);
   _stressResult = ProjectionService.Calculate(_settings, _allocation, true);

   if (_resultsWindow != null)
    _resultsWindow.UpdateResults(_settings, _allocation, _baseResult, _stressResult);

   CalculationLogService.Write(_settings, _allocation, _baseResult, _stressResult);
   UpdateCalculationLog();
   UpdateCalculationDocumentation();

   ResultsButton.IsEnabled = true;

   string recommended = StrategyService.Recommend(_settings);
   StrategyAllocation initialAllocation =
    ProjectionService.GetInitialAllocation(_settings, _allocation);
   decimal initialCash = _settings.StartCapital * initialAllocation.Cash;
   string reserveWarning = _baseResult.InitialRequiredCash > initialCash
    ? " Sichere Anlage ist kleiner als Reserve + Rücklagen."
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
  catch (Exception ex)
  {
   _baseResult = null;
   _stressResult = null;
   ResultsButton.IsEnabled = false;

   string calculationError =
    "Berechnungsfehler: " + ex.Message;

   ShowInputError(calculationError, []);
   WriteFailedCalculationLog(calculationError);
  }
 }
 private void Save_Click(object sender, RoutedEventArgs e)
 {
  if (!TryReadSettings(
       out string error,
       out IReadOnlyList<string> errorFieldKeys))
  {
   ShowInputError(error, errorFieldKeys);
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
  if (!TryReadSettings(
       out string error,
       out IReadOnlyList<string> errorFieldKeys))
  {
   ShowInputError(error, errorFieldKeys);
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

 private void Wizard_Click(object sender, RoutedEventArgs e)
 {
  var window = new WizardWindow(_settings, _allocation)
  {
   Owner = this
  };

  if (window.ShowDialog() != true)
   return;

  _settings = window.Settings;
  _allocation = window.Allocation;

  _baseResult = null;
  _stressResult = null;
  ResultsButton.IsEnabled = false;

  _inputs.Clear();
  InputSubTabs.Items.Clear();
  _currentInputPanel = null;
  BuildInputForm();

  _hasUnsavedChanges = true;
  UpdateCalculationDocumentation();

  StatusText.Text = "Wizard-Eingaben übernommen.";
  StatusText.Foreground = (Brush)FindResource("SuccessBrush");

  if (window.StartCalculation)
   Calculate_Click(this, new RoutedEventArgs());
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

 private bool TryReadSettings(
  out string error,
  out IReadOnlyList<string> errorFieldKeys)
 {
  error = "";
  errorFieldKeys = [];

  try
  {
   _settings.HouseholdPersonCount = ReadHouseholdPersonCount();
   _settings.Person1Age = ReadInt("Person1CurrentAge");
   _settings.Person1WorkEndYear = ReadInt("Person1WorkEndYear");
   _settings.PlanningYear = _settings.Person1WorkEndYear;
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
   _settings.IncomeTaxTariffAnnualIncreaseRate = ReadPercent("IncomeTaxTariffAnnualIncreaseRate", 0m, 0.20m);
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
   _settings.Person1ProjectedPensionGrossMonthlyAt67 = ReadDecimal("Person1ProjectedPensionGrossMonthlyAt67", 0m, 100000m);
   _settings.Person1CurrentInsuranceYears = ReadInt("Person1CurrentInsuranceYears");
   _settings.Person1CurrentPensionPoints = ReadDecimal("Person1CurrentPensionPoints", 0m, 1000m);
   _settings.Person1PensionableAnnualGross = ReadDecimal("Person1PensionableAnnualGross", 0m, 1000000m);
   _settings.Person1PensionableAnnualGrossIncreaseRate = ReadPercent("Person1PensionableAnnualGrossIncreaseRate", -0.50m, 0.50m);
   _settings.PensionAverageAnnualEarningsIncreaseRate = ReadPercent("PensionAverageAnnualEarningsIncreaseRate", -0.10m, 0.20m);
   _settings.KvdrPerson1 = ReadBool("KvdrPerson1");

   if (_settings.HouseholdPersonCount == 2)
   {
    _settings.Person2PensionGrossMonthly = ReadDecimal("Person2PensionGrossMonthly", 0m, 100000m);
    _settings.Person2ProjectedPensionGrossMonthlyAt67 = ReadDecimal("Person2ProjectedPensionGrossMonthlyAt67", 0m, 100000m);
    _settings.Person2CurrentInsuranceYears = ReadInt("Person2CurrentInsuranceYears");
    _settings.Person2CurrentPensionPoints = ReadDecimal("Person2CurrentPensionPoints", 0m, 1000m);
    _settings.Person2PensionableAnnualGross = ReadDecimal("Person2PensionableAnnualGross", 0m, 1000000m);
    _settings.Person2PensionableAnnualGrossIncreaseRate = ReadPercent("Person2PensionableAnnualGrossIncreaseRate", -0.50m, 0.50m);
    _settings.KvdrPerson2 = ReadBool("KvdrPerson2");
   }

   _settings.StartCapital = ReadDecimal("StartCapital", 0m, 1000000000m);

   _settings.ReserveYears = ReadDecimal("ReserveYears", 0m, 20m);
   _settings.AutoRefillReserve = ReadBool("AutoRefillReserve");
   _settings.UseReserveOnNegativeStockYear = ReadBool("UseReserveOnNegativeStockYear");

   _settings.HouseTotalValue = ReadDecimal("HouseTotalValue", 0m, 100000000m);
   _settings.HouseBuildingShare = ReadPercent("HouseBuildingShare", 0m, 1m);
   _settings.HouseLivingArea = ReadDecimal("HouseLivingArea", 0m, 10000m);
   _settings.HouseAge = ReadInt("HouseAge");
   _settings.CarReplacementValue = ReadDecimal("CarReplacementValue", 0m, 1000000m);
   _settings.CarReplacementYears = ReadInt("CarReplacementYears");
   _settings.HealthReserveTarget = ReadDecimal("HealthReserveTarget", 0m, 10000000m);
   _settings.TravelReserveTarget = ReadDecimal("TravelReserveTarget", 0m, 10000000m);
   _settings.OtherReserveTarget = ReadDecimal("OtherReserveTarget", 0m, 10000000m);

   _settings.SecureInvestmentCurrentValue = ReadDecimal("SecureInvestmentCurrentValue", 0m, 1000000000m);
   _settings.CashInterestRate = ReadPercent("CashInterestRate", -0.05m, 0.20m);

   _settings.WorldEtfCurrentValue = ReadDecimal("WorldEtfCurrentValue", 0m, 1000000000m);
   if (_settings.WorldEtfCurrentValue > 0m)
   {
    _settings.WorldEtfStartYear = ReadInt("WorldEtfStartYear");
    _settings.WorldEtfHistoricalReturn = ReadPercent("WorldEtfHistoricalReturn", -0.99m, 1m);
   }
   _settings.WorldEtfReturn = ReadPercent("WorldEtfReturn", -1m, 1m);
   _settings.WorldEtfDistribution = ReadPercent("WorldEtfDistribution", 0m, 0.50m);

   _settings.DividendEtfCurrentValue = ReadDecimal("DividendEtfCurrentValue", 0m, 1000000000m);
   if (_settings.DividendEtfCurrentValue > 0m)
   {
    _settings.DividendEtfStartYear = ReadInt("DividendEtfStartYear");
    _settings.DividendEtfHistoricalReturn = ReadPercent("DividendEtfHistoricalReturn", -0.99m, 1m);
   }
   _settings.DividendEtfReturn = ReadPercent("DividendEtfReturn", -1m, 1m);
   _settings.DividendEtfDistribution = ReadPercent("DividendEtfDistribution", 0m, 0.50m);

   _settings.DividendStocksCurrentValue = ReadDecimal("DividendStocksCurrentValue", 0m, 1000000000m);
   if (_settings.DividendStocksCurrentValue > 0m)
   {
    _settings.DividendStocksStartYear = ReadInt("DividendStocksStartYear");
    _settings.DividendStocksHistoricalReturn = ReadPercent("DividendStocksHistoricalReturn", -0.99m, 1m);
   }
   _settings.DividendStocksReturn = ReadPercent("DividendStocksReturn", -1m, 1m);
   _settings.DividendStocksDistribution = ReadPercent("DividendStocksDistribution", 0m, 0.50m);

   decimal existingInvestmentTotal =
    _settings.SecureInvestmentCurrentValue +
    _settings.WorldEtfCurrentValue +
    _settings.DividendEtfCurrentValue +
    _settings.DividendStocksCurrentValue;

   if (existingInvestmentTotal > _settings.StartCapital)
    throw new InputValidationException(
     "Die Summe der bestehenden Anlagewerte darf das verfügbare Startvermögen nicht überschreiten.",
     "StartCapital",
     "SecureInvestmentCurrentValue",
     "WorldEtfCurrentValue",
     "DividendEtfCurrentValue",
     "DividendStocksCurrentValue");

   _settings.DividendSurplusReinvest = ReadBool("DividendSurplusReinvest");
   _settings.Strategy = ReadChoice("Strategy");
   _allocation = ReadDesiredAllocation(_settings.StartCapital);

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

   int workEndAgePerson1 =
    _settings.Person1Age + (_settings.Person1WorkEndYear - DateTime.Today.Year);

   if (_settings.Person1RetirementAge < workEndAgePerson1)
    throw new InputValidationException(
     "Der Beginn der gesetzlichen Rente darf nicht vor dem jeweiligen vorzeitigen Arbeitsende liegen.",
     "Person1WorkEndYear",
     "Person1RetirementAge");

   if (_settings.HouseholdPersonCount == 2)
   {
    int workEndAgePerson2 =
     _settings.Person2Age + (_settings.Person2WorkEndYear - DateTime.Today.Year);

    if (_settings.Person2RetirementAge < workEndAgePerson2)
     throw new InputValidationException(
      "Der Beginn der gesetzlichen Rente darf nicht vor dem jeweiligen vorzeitigen Arbeitsende liegen.",
      "Person2WorkEndYear",
      "Person2RetirementAge");
   }

   if (_settings.Person1CurrentInsuranceYears < 0)
    throw new InputValidationException(
     "Die bisherigen Versicherungsjahre von Person 1 dürfen nicht negativ sein.",
     "Person1CurrentInsuranceYears");

   if (!PensionService.HasRequiredInsuranceYearsForEarlyRetirement(
        _settings.Person1CurrentInsuranceYears,
        _settings.Person1WorkEndYear,
        _settings.Person1RetirementAge))
   {
    int insuranceYearsAtWorkEnd =
     PensionService.CalculateInsuranceYearsAtWorkEnd(
      _settings.Person1CurrentInsuranceYears,
      _settings.Person1WorkEndYear);

    throw new InputValidationException(
     $"Person 1 erreicht bis zum Arbeitsende nur {insuranceYearsAtWorkEnd} Versicherungsjahre. " +
     $"Für einen Rentenbeginn vor 67 werden mindestens {PensionService.MinimumInsuranceYearsForEarlyRetirement} Versicherungsjahre benötigt.",
     "Person1WorkEndYear",
     "Person1RetirementAge",
     "Person1CurrentInsuranceYears");
   }

   if (_settings.Person1EndAge < _settings.Person1RetirementAge)
    throw new InputValidationException(
     "Die Lebenserwartung muss nach dem Beginn der gesetzlichen Rente liegen.",
     "Person1RetirementAge",
     "Person1EndAge");

   if (_settings.HouseholdPersonCount == 2 &&
       _settings.Person2CurrentInsuranceYears < 0)
    throw new InputValidationException(
     "Die bisherigen Versicherungsjahre von Person 2 dürfen nicht negativ sein.",
     "Person2CurrentInsuranceYears");

   if (_settings.HouseholdPersonCount == 2 &&
       !PensionService.HasRequiredInsuranceYearsForEarlyRetirement(
        _settings.Person2CurrentInsuranceYears,
        _settings.Person2WorkEndYear,
        _settings.Person2RetirementAge))
   {
    int insuranceYearsAtWorkEnd =
     PensionService.CalculateInsuranceYearsAtWorkEnd(
      _settings.Person2CurrentInsuranceYears,
      _settings.Person2WorkEndYear);

    throw new InputValidationException(
     $"Person 2 erreicht bis zum Arbeitsende nur {insuranceYearsAtWorkEnd} Versicherungsjahre. " +
     $"Für einen Rentenbeginn vor 67 werden mindestens {PensionService.MinimumInsuranceYearsForEarlyRetirement} Versicherungsjahre benötigt.",
     "Person2WorkEndYear",
     "Person2RetirementAge",
     "Person2CurrentInsuranceYears");
   }

   if (_settings.HouseholdPersonCount == 2 &&
       _settings.Person2EndAge < _settings.Person2RetirementAge)
    throw new InputValidationException(
     "Die Lebenserwartung muss nach dem Beginn der gesetzlichen Rente liegen.",
     "Person2RetirementAge",
     "Person2EndAge");

   if (_settings.HouseTotalValue > 0m &&
       _settings.HouseLivingArea <= 0m)
    throw new InputValidationException(
     "Die Wohnfläche der Immobilie muss größer als 0 m² sein, wenn ein Hauswert eingetragen ist.",
     "HouseTotalValue",
     "HouseLivingArea");

   if (_settings.HouseAge < 0)
    throw new InputValidationException(
     "Das Alter der Immobilie darf nicht negativ sein.",
     "HouseAge");

   if (_settings.CarReplacementYears <= 0)
    throw new InputValidationException(
     "Auto-Ersatz nach Jahren muss größer als 0 sein.",
     "CarReplacementYears");

   if (_settings.VoluntaryHealthInsuranceMinimumMonthlyIncome > _settings.VoluntaryHealthInsuranceMaximumMonthlyIncome)
    throw new InputValidationException(
     "Die GKV/Pflege Mindest-Bemessungsgrundlage darf nicht über der Beitragsbemessungsgrenze liegen.",
     "VoluntaryHealthInsuranceMinimumMonthlyIncome",
     "VoluntaryHealthInsuranceMaximumMonthlyIncome");

   if (_settings.HealthInsuranceBaseYear <= 0)
    throw new InputValidationException(
     "Das GKV/Pflege Basisjahr muss größer als 0 sein.",
     "HealthInsuranceBaseYear");

   int currentYear = DateTime.Today.Year;
   if (_settings.WorldEtfCurrentValue > 0m &&
       (_settings.WorldEtfStartYear <= 0 || _settings.WorldEtfStartYear > currentYear))
    throw new InputValidationException(
     "Das Startjahr des MSCI World / Welt-ETF muss im aktuellen Jahr oder davor liegen.",
     "WorldEtfCurrentValue",
     "WorldEtfStartYear");
   if (_settings.DividendEtfCurrentValue > 0m &&
       (_settings.DividendEtfStartYear <= 0 || _settings.DividendEtfStartYear > currentYear))
    throw new InputValidationException(
     "Das Startjahr des Dividenden-ETF muss im aktuellen Jahr oder davor liegen.",
     "DividendEtfCurrentValue",
     "DividendEtfStartYear");
   if (_settings.DividendStocksCurrentValue > 0m &&
       (_settings.DividendStocksStartYear <= 0 || _settings.DividendStocksStartYear > currentYear))
    throw new InputValidationException(
     "Das Startjahr der Dividenden-Aktien muss im aktuellen Jahr oder davor liegen.",
     "DividendStocksCurrentValue",
     "DividendStocksStartYear");

   return true;
  }
  catch (InputValidationException ex)
  {
   error = "Eingabefehler: " + ex.Message;
   errorFieldKeys = ex.FieldKeys;
   return false;
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
   throw new InputValidationException(
    $"Ungültige Ganzzahl bei „{key}“.",
    key);
  return value;
 }

 private decimal ReadDecimal(string key, decimal min, decimal max)
 {
  if (_inputs[key] is not TextBox tb)
   throw new InputValidationException(
    $"Ungültige Zahl bei „{key}“.",
    key);

  decimal value;

  if (tb.Tag is InputType inputType &&
      inputType is InputType.Money or InputType.OptionalMoney)
  {
   if (!decimal.TryParse(tb.Text, NumberStyles.Number, GermanCulture, out value))
    throw new InputValidationException(
     $"Ungültige Zahl bei „{key}“.",
     key);
  }
  else
  {
   if (!decimal.TryParse(tb.Text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out value))
    throw new InvalidOperationException($"Ungültige Zahl bei „{key}“.");
  }

  if (value < min || value > max)
   throw new InputValidationException(
    $"Wert bei „{key}“ liegt außerhalb des sinnvollen Bereichs.",
    key);

  return value;
 }

 private decimal ReadPercent(string key, decimal min, decimal max) =>
  ReadDecimal(key, min * 100m, max * 100m) / 100m;

 private bool ReadBool(string key) => ReadChoice(key) == "Ja";

 private string ReadChoice(string key)
 {
  if (_inputs[key] is not ComboBox cb || cb.SelectedItem is not string value)
   throw new InputValidationException(
    $"Keine Auswahl bei „{key}“.",
    key);
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

 private void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
 {
  if (sender is not TextBox textBox ||
      Mouse.LeftButton == MouseButtonState.Pressed ||
      Mouse.RightButton == MouseButtonState.Pressed)
   return;

  textBox.SelectAll();
 }

 private void TextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
 {
  if (sender is not TextBox textBox ||
      !string.IsNullOrWhiteSpace(textBox.Text) ||
      textBox.Tag is not InputType inputType ||
      inputType is InputType.Integer or InputType.OptionalMoney)
   return;

  textBox.Text = "0";
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

   TryReadSettings(out _, out _);
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

  if (!TryReadSettings(
       out string error,
       out IReadOnlyList<string> errorFieldKeys))
  {
   e.Cancel = true;
   ShowInputError(error, errorFieldKeys);
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

 private sealed class InputValidationException : InvalidOperationException
 {
  public IReadOnlyList<string> FieldKeys { get; }

  public InputValidationException(
   string message,
   params string[] fieldKeys)
   : base(message)
  {
   FieldKeys = fieldKeys;
  }
 }

 private enum InputType
 {
  Integer,
  Decimal,
  Money,
  OptionalMoney,
  Percent
 }
}
