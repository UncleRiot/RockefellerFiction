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
 private StrategyAllocation _allocation;
 private bool _hasUnsavedChanges;

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

  AddSection("Startvermögen");
  AddMoney("StartCapital", "Verfügbares Startvermögen", _settings.StartCapital,
   "Trage hier das gesamte Geld ein, das dir zu Beginn der Planung tatsächlich zum Anlegen und für spätere Ausgaben zur Verfügung steht. Beispiel: Beginnt deine Planung 2027 und du hast dann 1.250.000 € frei verfügbares Vermögen, trägst du 1.250.000 € ein. Der Wert des Hauses gehört hier nicht hinein, solange ein Hausverkauf nicht separat berücksichtigt wird.");

  AddSection("Planung");
  AddInt("PlanningYear", "Planungsjahr", _settings.PlanningYear,
   "Trage das Kalenderjahr ein, ab dem die Vermögensplanung starten soll. Beispiel: Wenn ab 2027 Ausgaben, GKV/Pflege, Kapitalerträge und spätere Renten berücksichtigt werden sollen, trägst du 2027 ein. Das Planungsjahr ist nicht automatisch das Jahr des Rentenbeginns.");
  AddInt("Person1Age", "Alter Person 1 bei vorzeitigem Arbeitsende", _settings.Person1Age,
   "Trage das Alter ein, in dem Person 1 aufgrund des vorhandenen Vermögens dauerhaft aufhört zu arbeiten. Gemeint ist ausdrücklich das vorzeitige Arbeitsende vor Beginn der gesetzlichen Rente. Beispiel: Person 1 beendet die Erwerbstätigkeit mit 52 Jahren und bezieht die gesetzliche Rente erst später; dann trägst du hier 52 ein.");
  AddInt("Person2Age", "Alter Person 2 bei vorzeitigem Arbeitsende", _settings.Person2Age,
   "Trage das Alter ein, in dem Person 2 aufgrund des vorhandenen Vermögens dauerhaft aufhört zu arbeiten. Gemeint ist ausdrücklich das vorzeitige Arbeitsende vor Beginn der gesetzlichen Rente. Beispiel: Person 2 beendet die Erwerbstätigkeit mit 47 Jahren und bezieht die gesetzliche Rente erst später; dann trägst du hier 47 ein.");
  AddInt("Person1RetirementAge", "Beginn gesetzliche Rente Person 1", _settings.Person1RetirementAge,
   "Trage das Alter ein, ab dem Person 1 die gesetzliche Altersrente tatsächlich beziehen soll. Dieses Alter kann deutlich nach dem vorzeitigen Arbeitsende liegen.");
  AddInt("Person2RetirementAge", "Beginn gesetzliche Rente Person 2", _settings.Person2RetirementAge,
   "Trage das Alter ein, ab dem Person 2 die gesetzliche Altersrente tatsächlich beziehen soll. Dieses Alter kann deutlich nach dem vorzeitigen Arbeitsende liegen.");
  AddInt("Person1EndAge", "Lebenserwartung Person 1", _settings.Person1EndAge,
   "Trage das Alter ein, bis zu dem die Planung für Person 1 reichen soll. Das Programm verwendet diesen Wert als angenommenes Lebensalter für die langfristige Vermögensplanung.");
  AddInt("Person2EndAge", "Lebenserwartung Person 2", _settings.Person2EndAge,
   "Trage das Alter ein, bis zu dem die Planung für Person 2 reichen soll. Das Programm verwendet diesen Wert als angenommenes Lebensalter für die langfristige Vermögensplanung.");

  AddSection("Lebensstandard & Inflation");
  AddMoney("MonthlyLivingCosts", "Monatliche Ausgaben für das Leben", _settings.MonthlyLivingCosts,
   "Haus, Essen, Auto, Freizeit und private Versicherungen. Kranken- und Pflegeversicherung werden separat berücksichtigt.");
  AddPercent("InflationRate", "Inflation pro Jahr", _settings.InflationRate,
   "Jährliche Preissteigerung. Aus heutigen Ausgaben werden automatisch zukünftige Ausgaben berechnet.");
  AddPercent("PensionIncreaseRate", "Konservative Rentensteigerung pro Jahr", _settings.PensionIncreaseRate,
   "Jährliche angenommene Anpassung der gesetzlichen Rente. Default bewusst konservativ.");
  AddMoney("VoluntaryHealthInsuranceMinimumMonthlyIncome", "GKV/Pflege Mindest-Bemessungsgrundlage pro Monat", _settings.VoluntaryHealthInsuranceMinimumMonthlyIncome,
   "Untergrenze, auf deren Basis die freiwillige gesetzliche Kranken- und Pflegeversicherung mindestens berechnet wird.");
  AddMoney("VoluntaryHealthInsuranceMaximumMonthlyIncome", "GKV/Pflege Beitragsbemessungsgrenze pro Monat", _settings.VoluntaryHealthInsuranceMaximumMonthlyIncome,
   "Obergrenze der monatlichen Einnahmen, die für die freiwillige Kranken- und Pflegeversicherung berücksichtigt werden.");
  AddPercent("VoluntaryHealthInsuranceRate", "GKV Beitragssatz ohne Krankengeld", _settings.VoluntaryHealthInsuranceRate,
   "Ermäßigter Beitragssatz der freiwilligen gesetzlichen Krankenversicherung ohne Krankengeldanspruch.");
  AddPercent("VoluntaryHealthInsuranceAdditionalRate", "GKV Zusatzbeitrag", _settings.VoluntaryHealthInsuranceAdditionalRate,
   "Zusatzbeitrag der Krankenkasse. Default ist der aktuell verwendete Planungswert.");
  AddPercent("CareInsuranceChildlessRate", "Pflegeversicherung kinderlos", _settings.CareInsuranceChildlessRate,
   "Gesamter Beitragssatz der Pflegeversicherung für Kinderlose.");
  AddReadOnlyMoney("CalculatedHealthPerson1Monthly", "Berechnete GKV/Pflege Person 1 pro Monat", 0m,
   "Automatisch berechneter Monatsbeitrag aus den aktuell eingestellten Kapitalerträgen. Die Kapitalerträge werden 50/50 auf beide Personen verteilt.");
  AddReadOnlyMoney("CalculatedHealthPerson2Monthly", "Berechnete GKV/Pflege Person 2 pro Monat", 0m,
   "Automatisch berechneter Monatsbeitrag aus den aktuell eingestellten Kapitalerträgen. Die Kapitalerträge werden 50/50 auf beide Personen verteilt.");
  AddSection("Gesetzliche Rente");
  AddMoney("Person1PensionGrossMonthly", "Bereits erworbene Rente Person 1 pro Monat", _settings.Person1PensionGrossMonthly,
   "Bruttorente laut Renteninformation für den Fall, dass keine weiteren Beiträge mehr gezahlt werden.");
  AddMoney("Person2PensionGrossMonthly", "Bereits erworbene Rente Person 2 pro Monat", _settings.Person2PensionGrossMonthly,
   "Bruttorente laut Renteninformation für den Fall, dass keine weiteren Beiträge mehr gezahlt werden.");
  AddBool("KvdrPerson1", "KVdR für Person 1 annehmen", _settings.KvdrPerson1,
   "Wenn aktiv, wird für die Rentenphase die Krankenversicherung der Rentner angenommen.");
  AddBool("KvdrPerson2", "KVdR für Person 2 annehmen", _settings.KvdrPerson2,
   "Wenn aktiv, wird für die Rentenphase die Krankenversicherung der Rentner angenommen.");

  AddSection("Sichere Reserve & Rücklagen");
  AddDecimal("ReserveYears", "Sichere Reserve in Jahresausgaben", _settings.ReserveYears,
   "Wie viele Jahre eures Lebensbedarfs sicher in Tages-/Festgeld liegen sollen. Default: 2 Jahre.");
  AddBool("AutoRefillReserve", "Reserve automatisch wieder auffüllen", _settings.AutoRefillReserve,
   "Wenn aktiv, wird eine verbrauchte Reserve später wieder bis zum Zielwert aufgefüllt.");
  AddBool("UseReserveOnNegativeStockYear", "Bei negativem Aktienjahr zuerst Reserve nutzen", _settings.UseReserveOnNegativeStockYear,
   "Wenn Aktien/ETFs im Jahr negativ laufen, werden laufende Ausgaben zuerst aus der sicheren Reserve bezahlt.");
  AddPercent("CashInterestRate", "Zins Tages-/Festgeld", _settings.CashInterestRate,
   "Nominaler jährlicher Zinssatz für den sicheren Geldanteil.");

  AddMoney("HouseTotalValue", "Hauswert inkl. Grundstück", _settings.HouseTotalValue,
   "Gesamter heutiger Immobilienwert. Das Haus selbst zählt standardmäßig nicht als verfügbares Anlagevermögen.");
  AddPercent("HouseBuildingShare", "Anteil Gebäude am Hauswert", _settings.HouseBuildingShare,
   "Geschätzter Anteil des Gebäudes am Gesamtwert, ohne Grundstück. Default: 70 %.");
  AddPercent("HouseReserveRate", "Jährliche Haus-Rücklage", _settings.HouseReserveRate,
   "Jährlicher Rücklage-Satz auf den geschätzten Gebäudewert für Instandhaltung.");
  AddMoney("CarReplacementValue", "Ersatzwert Auto", _settings.CarReplacementValue,
   "Heutiger Betrag, den ein vergleichbares Ersatzfahrzeug kosten würde.");
  AddInt("CarReplacementYears", "Auto-Ersatz nach Jahren", _settings.CarReplacementYears,
   "Zeitraum, über den der Ersatzwert als Rücklage angespart wird.");
  AddMoney("HealthReserveTarget", "Gesundheit / Zahnersatz Rücklage", _settings.HealthReserveTarget,
   "Zielbetrag für einen separaten Gesundheit-/Zahnersatz-Topf.");
  AddMoney("TravelReserveTarget", "Reisen / größere Wünsche Rücklage", _settings.TravelReserveTarget,
   "Optionaler Zielbetrag. Default 0 €.");
  AddMoney("OtherReserveTarget", "Sonstiges / Unvorhergesehenes Rücklage", _settings.OtherReserveTarget,
   "Optionaler zusätzlicher Puffer. Default 0 €.");

  AddSection("Anlagen – konservative Defaultwerte");
  AddPercent("WorldEtfReturn", "MSCI World / Welt-ETF Gesamtrendite", _settings.WorldEtfReturn,
   "Fester durchschnittlicher nominaler Jahreswert für Kursentwicklung plus Ausschüttungen.");
  AddPercent("WorldEtfDistribution", "MSCI World / Welt-ETF Ausschüttung", _settings.WorldEtfDistribution,
   "Davon angenommener Anteil, der als Ausschüttung ausgezahlt wird.");
  AddPercent("DividendEtfReturn", "Dividenden-ETF Gesamtrendite", _settings.DividendEtfReturn,
   "Fester durchschnittlicher nominaler Jahreswert.");
  AddPercent("DividendEtfDistribution", "Dividenden-ETF Ausschüttung", _settings.DividendEtfDistribution,
   "Angenommene jährliche Ausschüttungsrendite.");
  AddPercent("DividendStocksReturn", "Dividenden-Aktien Gesamtrendite", _settings.DividendStocksReturn,
   "Fester durchschnittlicher nominaler Jahreswert.");
  AddPercent("DividendStocksDistribution", "Dividenden-Aktien Ausschüttung", _settings.DividendStocksDistribution,
   "Angenommene jährliche Dividendenrendite.");
  AddBool("DividendSurplusReinvest", "Nicht benötigte Dividenden wieder anlegen", _settings.DividendSurplusReinvest,
   "Default aus: Ausschüttungen stehen vollständig für euren Lebensstandard zur Verfügung.");

  AddSection("Strategie & Aufteilung");
  AddChoice("Strategy", "Strategie", _settings.Strategy,
   ["Sicherheit", "Ausgewogen", "Wachstum"],
   "Das Programm kann eine Strategie empfehlen. Du kannst sie jederzeit manuell ändern.");
  AddPercent("AllocCash", "Anteil Tages-/Festgeld", _allocation.Cash,
   "Anteil des Startvermögens im sicheren Geldbereich. Reserve und Rücklagen liegen darin.");
  AddPercent("AllocWorld", "Anteil MSCI World / Welt-ETF", _allocation.WorldEtf,
   "Anteil für breit gestreutes langfristiges Aktienwachstum.");
  AddPercent("AllocDividendEtf", "Anteil Dividenden-ETF", _allocation.DividendEtf,
   "Anteil für breit gestreute dividendenorientierte ETFs.");
  AddPercent("AllocDividendStocks", "Anteil Dividenden-Aktien", _allocation.DividendStocks,
   "Anteil für einzelne Dividenden-Aktien.");

  AddSection("Steuern");
  AddMoney("CapitalGainsAllowance", "Sparer-Pauschbetrag gemeinsam", _settings.CapitalGainsAllowance,
   "Gemeinsamer jährlicher Freibetrag für Kapitalerträge. Default: 2.000 €.");
  AddBool("ChurchTaxEnabled", "Kirchensteuer berücksichtigen", _settings.ChurchTaxEnabled,
   "Bei euch standardmäßig aus.");

  AddSection("Stressszenario");
  AddChoice("StressCrashPercent", "Crash-Stärke am Anfang", FormatPercentChoice(_settings.StressCrashPercent),
   ["-15 %", "-25 %", "-40 %"],
   "Zusätzlicher Kursrückgang im ersten Planungsjahr auf die Aktien-/ETF-Anteile.");
  AddBool("StressCrashAtStart", "Crash am Anfang simulieren", _settings.StressCrashAtStart,
   "Wenn aktiv, wird der gewählte Crash direkt im ersten Planungsjahr simuliert.");
  AddBool("StressSecondCrashEnabled", "Späteren zweiten Crash simulieren", _settings.StressSecondCrashEnabled,
   "Optional kann später im Ruhestand noch ein zweiter Crash simuliert werden.");
  AddInt("StressSecondCrashYear", "Jahr des zweiten Crashs", _settings.StressSecondCrashYear,
   "Kalenderjahr, in dem der zweite Crash eintreten soll.");
  AddChoice("StressSecondCrashPercent", "Stärke des zweiten Crashs", FormatPercentChoice(_settings.StressSecondCrashPercent),
   ["-15 %", "-25 %", "-40 %"],
   "Zusätzlicher Kursrückgang im gewählten Jahr.");

  AddSection("Haus optional");
  AddBool("HouseIncluded", "Hausverkauf in Planung berücksichtigen", _settings.HouseIncluded,
   "Standard aus. Wenn aktiv, kann ein späterer Netto-Verkaufserlös als Einnahme berücksichtigt werden.");
  AddInt("HouseSaleYear", "Haus-Verkaufsjahr", _settings.HouseSaleYear,
   "Nur relevant, wenn Hausverkauf berücksichtigt wird.");
  AddMoney("HouseNetSaleProceeds", "Nettoerlös Hausverkauf", _settings.HouseNetSaleProceeds,
   "Betrag, der nach Verkauf tatsächlich als verfügbares Kapital zufließt.");

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

 private void AddInt(string key, string label, int value, string help) =>
  AddTextInput(key, label, value.ToString(CultureInfo.InvariantCulture), help, InputType.Integer);

 private void AddMoney(string key, string label, decimal value, string help) =>
  AddTextInput(key, label, FormatMoneyValue(value), help, InputType.Money);

 private void AddReadOnlyMoney(string key, string label, decimal value, string help)
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

  _currentInputPanel.Children.Add(CreateRow(label, help, textBox));
 }

 private void AddDecimal(string key, string label, decimal value, string help) =>
  AddTextInput(key, label, value.ToString("0.##", CultureInfo.InvariantCulture), help, InputType.Decimal);

 private void AddPercent(string key, string label, decimal value, string help) =>
  AddTextInput(key, label, (value * 100m).ToString("0.##", CultureInfo.InvariantCulture), help, InputType.Percent);

 private void AddTextInput(string key, string label, string value, string help, InputType type)
 {
  var textBox = new TextBox { Text = value, Tag = type };

  if (type == InputType.Money)
   textBox.TextChanged += MoneyTextBox_TextChanged;

  textBox.TextChanged += (_, _) => UpdateCalculatedHealthDisplays();
  textBox.TextChanged += (_, _) => _hasUnsavedChanges = true;

  _inputs[key] = textBox;
  if (_currentInputPanel == null)
   throw new InvalidOperationException("Keine Eingabe-Unterlasche aktiv.");

  _currentInputPanel.Children.Add(CreateRow(label, help, textBox));
 }

 private void AddBool(string key, string label, bool value, string help)
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

  _currentInputPanel.Children.Add(CreateRow(label, help, combo));
 }

 private void AddChoice(string key, string label, string value, IEnumerable<string> choices, string help)
 {
  var combo = new ComboBox();
  foreach (var item in choices) combo.Items.Add(item);
  combo.SelectedItem = value;
  combo.SelectionChanged += (_, _) => UpdateCalculatedHealthDisplays();
  combo.SelectionChanged += (_, _) => _hasUnsavedChanges = true;
  _inputs[key] = combo;
  if (_currentInputPanel == null)
   throw new InvalidOperationException("Keine Eingabe-Unterlasche aktiv.");

  _currentInputPanel.Children.Add(CreateRow(label, help, combo));
 }

 private FrameworkElement CreateRow(string label, string helpText, FrameworkElement input)
 {
  _fieldNumber++;
  string numberedLabel = $"{_fieldNumber:00}. {label}";

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
   TextWrapping = TextWrapping.Wrap,
   VerticalAlignment = VerticalAlignment.Center
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

  helpButton.MouseEnter += (_, _) => _help.ShowHover(helpButton, helpText);
  helpButton.MouseLeave += (_, _) => _help.ClearHover(helpButton);
  helpButton.MouseLeftButtonDown += (_, e) =>
  {
   e.Handled = true;
   _help.TogglePinned(helpButton, helpText);
  };

  Grid.SetColumn(helpButton, 1);
  grid.Children.Add(helpButton);

  input.VerticalAlignment = VerticalAlignment.Center;
  Grid.SetColumn(input, 2);
  grid.Children.Add(input);

  return grid;
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
   previewSettings.StartCapital = ReadDecimal("StartCapital", 0m, 1000000000m);
   previewSettings.Person1Age = ReadInt("Person1Age");
   previewSettings.Person2Age = ReadInt("Person2Age");
   previewSettings.Person1RetirementAge = ReadInt("Person1RetirementAge");
   previewSettings.Person2RetirementAge = ReadInt("Person2RetirementAge");
   previewSettings.CashInterestRate = ReadPercent("CashInterestRate", -0.05m, 0.20m);
   previewSettings.WorldEtfDistribution = ReadPercent("WorldEtfDistribution", 0m, 0.50m);
   previewSettings.DividendEtfDistribution = ReadPercent("DividendEtfDistribution", 0m, 0.50m);
   previewSettings.DividendStocksDistribution = ReadPercent("DividendStocksDistribution", 0m, 0.50m);
   previewSettings.VoluntaryHealthInsuranceMinimumMonthlyIncome = ReadDecimal("VoluntaryHealthInsuranceMinimumMonthlyIncome", 0m, 100000m);
   previewSettings.VoluntaryHealthInsuranceMaximumMonthlyIncome = ReadDecimal("VoluntaryHealthInsuranceMaximumMonthlyIncome", 0m, 100000m);
   previewSettings.VoluntaryHealthInsuranceRate = ReadPercent("VoluntaryHealthInsuranceRate", 0m, 0.30m);
   previewSettings.VoluntaryHealthInsuranceAdditionalRate = ReadPercent("VoluntaryHealthInsuranceAdditionalRate", 0m, 0.20m);
   previewSettings.CareInsuranceChildlessRate = ReadPercent("CareInsuranceChildlessRate", 0m, 0.20m);

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
  UpdateCalculationDocumentation();

  ResultsButton.IsEnabled = true;

  string recommended = StrategyService.Recommend(_settings);
  string reserveWarning = _baseResult.InitialRequiredCash > _settings.StartCapital * _allocation.Cash
   ? " Tages-/Festgeld ist kleiner als Reserve + Rücklagen."
   : "";

  StatusText.Text =
   $"Basis: {_baseResult.OverallStatus} | Stress: {_stressResult.OverallStatus} | Empfehlung: {recommended}.{reserveWarning}";

  StatusText.Foreground =
   (_baseResult.ReachesPlanEnd && _stressResult.ReachesPlanEnd)
    ? (Brush)FindResource("SuccessBrush")
    : (Brush)FindResource("WarningBrush");
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

 private void Results_Click(object sender, RoutedEventArgs e)
 {
  if (_baseResult == null || _stressResult == null) return;

  var window = new ResultsWindow(_settings, _allocation, _baseResult, _stressResult)
  {
   Owner = this
  };
  window.ShowDialog();
 }

 private bool TryReadSettings(out string error)
 {
  error = "";

  try
  {
   _settings.PlanningYear = ReadInt("PlanningYear");
   _settings.Person1Age = ReadInt("Person1Age");
   _settings.Person2Age = ReadInt("Person2Age");
   _settings.Person1RetirementAge = ReadInt("Person1RetirementAge");
   _settings.Person2RetirementAge = ReadInt("Person2RetirementAge");
   _settings.Person1EndAge = ReadInt("Person1EndAge");
   _settings.Person2EndAge = ReadInt("Person2EndAge");

   _settings.MonthlyLivingCosts = ReadDecimal("MonthlyLivingCosts", 0m, 1000000m);
   _settings.InflationRate = ReadPercent("InflationRate", -0.05m, 0.20m);
   _settings.PensionIncreaseRate = ReadPercent("PensionIncreaseRate", -0.05m, 0.20m);
   _settings.VoluntaryHealthInsuranceMinimumMonthlyIncome = ReadDecimal("VoluntaryHealthInsuranceMinimumMonthlyIncome", 0m, 100000m);
   _settings.VoluntaryHealthInsuranceMaximumMonthlyIncome = ReadDecimal("VoluntaryHealthInsuranceMaximumMonthlyIncome", 0m, 100000m);
   _settings.VoluntaryHealthInsuranceRate = ReadPercent("VoluntaryHealthInsuranceRate", 0m, 0.30m);
   _settings.VoluntaryHealthInsuranceAdditionalRate = ReadPercent("VoluntaryHealthInsuranceAdditionalRate", 0m, 0.20m);
   _settings.CareInsuranceChildlessRate = ReadPercent("CareInsuranceChildlessRate", 0m, 0.20m);

   _settings.Person1PensionGrossMonthly = ReadDecimal("Person1PensionGrossMonthly", 0m, 100000m);
   _settings.Person2PensionGrossMonthly = ReadDecimal("Person2PensionGrossMonthly", 0m, 100000m);

   _settings.KvdrPerson1 = ReadBool("KvdrPerson1");
   _settings.KvdrPerson2 = ReadBool("KvdrPerson2");

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

   _settings.WorldEtfReturn = ReadPercent("WorldEtfReturn", -1m, 1m);
   _settings.WorldEtfDistribution = ReadPercent("WorldEtfDistribution", 0m, 0.50m);
   _settings.DividendEtfReturn = ReadPercent("DividendEtfReturn", -1m, 1m);
   _settings.DividendEtfDistribution = ReadPercent("DividendEtfDistribution", 0m, 0.50m);
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
   _settings.ChurchTaxEnabled = ReadBool("ChurchTaxEnabled");

   _settings.StressCrashPercent = ParsePercentChoice(ReadChoice("StressCrashPercent"));
   _settings.StressCrashAtStart = ReadBool("StressCrashAtStart");
   _settings.StressSecondCrashEnabled = ReadBool("StressSecondCrashEnabled");
   _settings.StressSecondCrashYear = ReadInt("StressSecondCrashYear");
   _settings.StressSecondCrashPercent = ParsePercentChoice(ReadChoice("StressSecondCrashPercent"));

   _settings.HouseIncluded = ReadBool("HouseIncluded");
   _settings.HouseSaleYear = ReadInt("HouseSaleYear");
   _settings.HouseNetSaleProceeds = ReadDecimal("HouseNetSaleProceeds", 0m, 100000000m);

   if (_settings.Person1RetirementAge < _settings.Person1Age ||
       _settings.Person2RetirementAge < _settings.Person2Age)
    throw new InvalidOperationException("Rentenalter darf nicht unter dem aktuellen Alter liegen.");

   if (_settings.Person1EndAge < _settings.Person1RetirementAge ||
       _settings.Person2EndAge < _settings.Person2RetirementAge)
    throw new InvalidOperationException("Planungsende muss nach dem Rentenbeginn liegen.");

   if (_settings.CarReplacementYears <= 0)
    throw new InvalidOperationException("Auto-Ersatz nach Jahren muss größer als 0 sein.");

   if (_settings.VoluntaryHealthInsuranceMinimumMonthlyIncome > _settings.VoluntaryHealthInsuranceMaximumMonthlyIncome)
    throw new InvalidOperationException("Die GKV/Pflege Mindest-Bemessungsgrundlage darf nicht über der Beitragsbemessungsgrenze liegen.");

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
  if (CalculationDocumentationText == null ||
      MainTabs.SelectedItem is not TabItem selectedTab ||
      !string.Equals(selectedTab.Header?.ToString(), "Berechnungsgrundlagen", StringComparison.Ordinal))
   return;

  TryReadSettings(out _);
  UpdateCalculationDocumentation();
 }

 private void UpdateCalculationDocumentation()
 {
  if (CalculationDocumentationText == null)
   return;

  CalculationDocumentationText.Text =
   CalculationDocumentationService.Build(_settings, _allocation);
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
