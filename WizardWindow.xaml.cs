using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RockefellerFiction;

public partial class WizardWindow : Window
{
 private static readonly CultureInfo GermanCulture =
  CultureInfo.GetCultureInfo("de-DE");

 private readonly PlannerSettings _settings;
 private StrategyAllocation _allocation;
 private readonly List<WizardQuestion> _coreQuestions = [];
 private readonly List<WizardQuestion> _advancedQuestions = [];
 private readonly List<WizardQuestion> _expertQuestions = [];

 private WizardPhase _phase = WizardPhase.Intro;
 private int _questionIndex = -1;
 private FrameworkElement? _currentEditor;
 private bool _advancedEnabled;
 private bool _expertEnabled;
 private bool _returnToSummaryAfterEdit;

 public PlannerSettings Settings => _settings;
 public StrategyAllocation Allocation => _allocation;
 public bool StartCalculation { get; private set; }

 public WizardWindow(
  PlannerSettings settings,
  StrategyAllocation allocation)
 {
  InitializeComponent();

  Background = (Brush)FindResource("BgBrush");
  Foreground = (Brush)FindResource("TextBrush");
  WindowBehavior.ApplyDarkTitleBar(this);

  _settings = SettingsClone.Clone(settings);
  _allocation = allocation;

  if (_settings.Person1WorkEndYear <= 0)
   _settings.Person1WorkEndYear = _settings.PlanningYear;

  _settings.PlanningYear = _settings.Person1WorkEndYear;

  BuildQuestions();
  ShowIntro();
 }

 private void BuildQuestions()
 {
  int currentYear = DateTime.Today.Year;

  AddChoice(
   _coreQuestions,
   "Grundkonfiguration",
   "Haushalt",
   "Für wie viele Personen soll die Planung gelten?",
   "Wähle „1 Person“, wenn nur deine eigene Planung berücksichtigt werden soll. Wähle „2 Personen“, wenn Einkommen, Rente und Lebensdauer einer zweiten Person mitgerechnet werden sollen.",
   () => _settings.HouseholdPersonCount == 1 ? "1 Person" : "2 Personen",
   value => _settings.HouseholdPersonCount = value == "1 Person" ? 1 : 2,
   ["1 Person", "2 Personen"]);

  AddMoney(
   _coreQuestions,
   "Grundkonfiguration",
   "Verfügbares Startvermögen",
   "Wie viel frei verfügbares Vermögen steht zu Beginn der Simulation zur Verfügung?",
   "Addiere Giro-/Tagesgeld, Festgeld und Depotvermögen, das für diese Planung tatsächlich verfügbar ist. Ein selbst bewohntes Haus gehört hier nicht hinein, solange ein Hausverkauf nicht separat berücksichtigt wird.",
   () => _settings.StartCapital,
   value => _settings.StartCapital = value,
   0m,
   1000000000m);

  AddInteger(
   _coreQuestions,
   "Grundkonfiguration",
   "Aktuelles Alter Person 1",
   "Wie alt ist Person 1 heute?",
   "Das ist das aktuelle vollendete Lebensalter.",
   () => _settings.Person1Age,
   value => _settings.Person1Age = value,
   18,
   110);

  AddInteger(
   _coreQuestions,
   "Grundkonfiguration",
   "Vorzeitiges Arbeitsende Person 1",
   "In welchem Kalenderjahr möchte Person 1 dauerhaft aufhören zu arbeiten?",
   "Das ist deine eigene Planungsentscheidung. Beispiel: Wenn Person 1 Ende 2034 aufhören soll, trage 2034 ein.",
   () => _settings.Person1WorkEndYear > 0 ? _settings.Person1WorkEndYear : _settings.PlanningYear,
   value =>
   {
    _settings.Person1WorkEndYear = value;
    _settings.PlanningYear = value;
   },
   currentYear,
   currentYear + 80);

  AddInteger(
   _coreQuestions,
   "Grundkonfiguration",
   "Aktuelles Alter Person 2",
   "Wie alt ist Person 2 heute?",
   "Das ist das aktuelle vollendete Lebensalter von Person 2.",
   () => _settings.Person2Age,
   value => _settings.Person2Age = value,
   18,
   110,
   isVisible: () => _settings.HouseholdPersonCount == 2);

  AddInteger(
   _coreQuestions,
   "Grundkonfiguration",
   "Vorzeitiges Arbeitsende Person 2",
   "In welchem Kalenderjahr möchte Person 2 dauerhaft aufhören zu arbeiten?",
   "Das ist die eigene Planungsentscheidung für Person 2. Bis zum Jahr vor diesem Arbeitsende kann das vorhandene Nettoeinkommen von Person 2 in der Simulation berücksichtigt werden.",
   () => _settings.Person2WorkEndYear > 0 ? _settings.Person2WorkEndYear : _settings.PlanningYear,
   value => _settings.Person2WorkEndYear = value,
   currentYear,
   currentYear + 80,
   isVisible: () => _settings.HouseholdPersonCount == 2);

  AddMoney(
   _coreQuestions,
   "Grundkonfiguration",
   "Nettoeinkommen Person 2 pro Monat",
   "Wie hoch ist das heutige monatliche Nettoeinkommen von Person 2?",
   "Nimm den regelmäßigen Netto-Auszahlungsbetrag aus der Gehaltsabrechnung. Dieses Einkommen wird nur bis zum geplanten Arbeitsende von Person 2 berücksichtigt.",
   () => _settings.Person2NetIncomeMonthly,
   value => _settings.Person2NetIncomeMonthly = value,
   0m,
   1000000m,
   isVisible: () =>
    _settings.HouseholdPersonCount == 2 &&
    _settings.Person2WorkEndYear > _settings.PlanningYear);

  AddMoney(
   _coreQuestions,
   "Grundkonfiguration",
   "Monatliche Ausgaben für das Leben",
   "Wie viel benötigt der Haushalt heute durchschnittlich pro Monat zum Leben?",
   "Nutze möglichst einen realistischen Durchschnitt aus Kontoauszügen oder Haushaltsbuch. Regelmäßige Lebenshaltung, Freizeit und laufende Kosten gehören hinein. Einmalige große Rücklagen werden in den erweiterten Fragen separat behandelt.",
   () => _settings.MonthlyLivingCosts,
   value => _settings.MonthlyLivingCosts = value,
   0m,
   1000000m);

  AddInteger(
   _coreQuestions,
   "Grundkonfiguration",
   "Geplanter Beginn gesetzliche Altersrente Person 1",
   "Ab welchem Alter soll Person 1 die gesetzliche Altersrente tatsächlich beziehen?",
   "Das ist eine Planungsentscheidung. Prüfe in deiner Renteninformation bzw. bei der Deutschen Rentenversicherung, ab welchem Alter ein Rentenbezug in deinem Fall möglich ist. Ein früherer Beginn kann Abschläge verursachen.",
   () => _settings.Person1RetirementAge,
   value => _settings.Person1RetirementAge = value,
   50,
   80,
   value =>
   {
    int workEndYear = _settings.Person1WorkEndYear > 0
     ? _settings.Person1WorkEndYear
     : _settings.PlanningYear;
    int workEndAge =
     _settings.Person1Age + Math.Max(0, workEndYear - currentYear);

    return value < workEndAge
     ? "Der Rentenbeginn darf im Wizard nicht vor dem geplanten Arbeitsende von Person 1 liegen."
     : null;
   });

  AddInteger(
   _coreQuestions,
   "Grundkonfiguration",
   "Geplanter Beginn gesetzliche Altersrente Person 2",
   "Ab welchem Alter soll Person 2 die gesetzliche Altersrente tatsächlich beziehen?",
   "Prüfe in der Renteninformation bzw. bei der Deutschen Rentenversicherung, ab welchem Alter ein Rentenbezug in diesem Fall möglich ist. Ein früherer Beginn kann Abschläge verursachen.",
   () => _settings.Person2RetirementAge,
   value => _settings.Person2RetirementAge = value,
   50,
   80,
   value =>
   {
    int workEndYear = _settings.Person2WorkEndYear > 0
     ? _settings.Person2WorkEndYear
     : _settings.PlanningYear;
    int workEndAge =
     _settings.Person2Age + Math.Max(0, workEndYear - currentYear);

    return value < workEndAge
     ? "Der Rentenbeginn darf im Wizard nicht vor dem geplanten Arbeitsende von Person 2 liegen."
     : null;
   },
   isVisible: () => _settings.HouseholdPersonCount == 2);

  AddMoney(
   _coreQuestions,
   "Grundkonfiguration",
   "Heute bereits erworbene Bruttorente Person 1",
   "Wie hoch ist die heute bereits erworbene gesetzliche Regelaltersrente von Person 1?",
   "Nimm aus der Renteninformation den Wert der künftigen Regelaltersrente, der gilt, wenn keine weiteren Beiträge mehr gezahlt würden. Nicht die Hochrechnung bei Weiterarbeit bis zur Regelaltersgrenze verwenden.",
   () => _settings.Person1PensionGrossMonthly,
   value => _settings.Person1PensionGrossMonthly = value,
   0m,
   100000m);

  AddMoney(
   _coreQuestions,
   "Grundkonfiguration",
   "Heute bereits erworbene Bruttorente Person 2",
   "Wie hoch ist die heute bereits erworbene gesetzliche Regelaltersrente von Person 2?",
   "Nimm aus der Renteninformation den Wert der künftigen Regelaltersrente ohne weitere Beitragszahlungen. Die Hochrechnung bei Weiterarbeit wird erst in den optionalen Präzisionsfragen abgefragt.",
   () => _settings.Person2PensionGrossMonthly,
   value => _settings.Person2PensionGrossMonthly = value,
   0m,
   100000m,
   isVisible: () => _settings.HouseholdPersonCount == 2);

  AddInteger(
   _advancedQuestions,
   "Lebenserwartung",
   "Lebenserwartung Person 1",
   "Bis zu welchem Alter soll die Planung für Person 1 reichen?",
   "Das ist eine vorsichtige Planungsannahme und kein vorhersehbarer Wert. Wenn du unsicher bist, kannst du den vorhandenen Standardwert beibehalten.",
   () => _settings.Person1EndAge,
   value => _settings.Person1EndAge = value,
   60,
   120,
   value => value < _settings.Person1RetirementAge
    ? "Die Lebenserwartung muss mindestens beim geplanten Rentenbeginn liegen."
    : null,
   optional: true,
   standardHint: () => $"Aktueller/Standardwert: {_settings.Person1EndAge}");

  AddInteger(
   _advancedQuestions,
   "Lebenserwartung",
   "Lebenserwartung Person 2",
   "Bis zu welchem Alter soll die Planung für Person 2 reichen?",
   "Das ist eine vorsichtige Planungsannahme. Wenn du unsicher bist, behalte den vorhandenen Wert.",
   () => _settings.Person2EndAge,
   value => _settings.Person2EndAge = value,
   60,
   120,
   value => value < _settings.Person2RetirementAge
    ? "Die Lebenserwartung muss mindestens beim geplanten Rentenbeginn liegen."
    : null,
   optional: true,
   standardHint: () => $"Aktueller/Standardwert: {_settings.Person2EndAge}",
   isVisible: () => _settings.HouseholdPersonCount == 2);

  AddMoney(
   _advancedQuestions,
   "Rente genauer",
   "DRV-Hochrechnung Person 1 bis 67",
   "Welche Bruttorente nennt die Renteninformation für Person 1 bei weiteren Beiträgen bis zur Regelaltersgrenze?",
   "In der Renteninformation steht zusätzlich zur bereits erworbenen Rente eine Hochrechnung, wenn bis zum Rentenbeginn Beiträge ungefähr wie im Durchschnitt der letzten Jahre weitergezahlt werden.",
   () => _settings.Person1ProjectedPensionGrossMonthlyAt67,
   value => _settings.Person1ProjectedPensionGrossMonthlyAt67 = value,
   0m,
   100000m,
   optional: true);

  AddMoney(
   _advancedQuestions,
   "Rente genauer",
   "DRV-Hochrechnung Person 2 bis 67",
   "Welche Bruttorente nennt die Renteninformation für Person 2 bei weiteren Beiträgen bis zur Regelaltersgrenze?",
   "Nimm den entsprechenden Hochrechnungswert aus der Renteninformation von Person 2.",
   () => _settings.Person2ProjectedPensionGrossMonthlyAt67,
   value => _settings.Person2ProjectedPensionGrossMonthlyAt67 = value,
   0m,
   100000m,
   optional: true,
   isVisible: () => _settings.HouseholdPersonCount == 2);

  AddDecimal(
   _advancedQuestions,
   "Rente genauer",
   "Aktuelle Entgeltpunkte Person 1",
   "Wie viele Entgeltpunkte hat Person 1 aktuell bereits erworben?",
   "Der Wert steht in einer Rentenauskunft, einem Rentenbescheid bzw. in detaillierten Unterlagen der Deutschen Rentenversicherung. Wenn du ihn nicht sicher findest, überspringe die Frage.",
   () => _settings.Person1CurrentPensionPoints,
   value => _settings.Person1CurrentPensionPoints = value,
   0m,
   1000m,
   optional: true);

  AddDecimal(
   _advancedQuestions,
   "Rente genauer",
   "Aktuelle Entgeltpunkte Person 2",
   "Wie viele Entgeltpunkte hat Person 2 aktuell bereits erworben?",
   "Der Wert steht in einer Rentenauskunft oder einem Rentenbescheid der Deutschen Rentenversicherung. Wenn unklar, überspringen.",
   () => _settings.Person2CurrentPensionPoints,
   value => _settings.Person2CurrentPensionPoints = value,
   0m,
   1000m,
   optional: true,
   isVisible: () => _settings.HouseholdPersonCount == 2);

  AddMoney(
   _advancedQuestions,
   "Rente genauer",
   "RV-pflichtiges Jahresbrutto Person 1",
   "Wie hoch ist das aktuelle rentenversicherungspflichtige Jahresbrutto von Person 1?",
   "Nutze die Gehaltsabrechnung oder Jahresmeldung zur Sozialversicherung. Gesucht ist das beitragspflichtige Brutto, nicht das Netto.",
   () => _settings.Person1PensionableAnnualGross,
   value => _settings.Person1PensionableAnnualGross = value,
   0m,
   1000000m,
   optional: true);

  AddMoney(
   _advancedQuestions,
   "Rente genauer",
   "RV-pflichtiges Jahresbrutto Person 2",
   "Wie hoch ist das aktuelle rentenversicherungspflichtige Jahresbrutto von Person 2?",
   "Nutze die Gehaltsabrechnung oder Jahresmeldung zur Sozialversicherung. Gesucht ist das beitragspflichtige Brutto.",
   () => _settings.Person2PensionableAnnualGross,
   value => _settings.Person2PensionableAnnualGross = value,
   0m,
   1000000m,
   optional: true,
   isVisible: () => _settings.HouseholdPersonCount == 2);

  AddPercent(
   _advancedQuestions,
   "Rente genauer",
   "RV-Brutto-Steigerung Person 1 p.a.",
   "Mit welcher jährlichen Steigerung des RV-pflichtigen Bruttos soll für Person 1 gerechnet werden?",
   "Das ist eine Planungsannahme. Wenn du keine eigene Annahme treffen möchtest, behalte 0 % bzw. den vorhandenen Wert.",
   () => _settings.Person1PensionableAnnualGrossIncreaseRate,
   value => _settings.Person1PensionableAnnualGrossIncreaseRate = value,
   -0.50m,
   0.50m,
   optional: true);

  AddPercent(
   _advancedQuestions,
   "Rente genauer",
   "RV-Brutto-Steigerung Person 2 p.a.",
   "Mit welcher jährlichen Steigerung des RV-pflichtigen Bruttos soll für Person 2 gerechnet werden?",
   "Das ist eine Planungsannahme. Wenn du unsicher bist, behalte den vorhandenen Wert.",
   () => _settings.Person2PensionableAnnualGrossIncreaseRate,
   value => _settings.Person2PensionableAnnualGrossIncreaseRate = value,
   -0.50m,
   0.50m,
   optional: true,
   isVisible: () => _settings.HouseholdPersonCount == 2);

  AddPercent(
   _expertQuestions,
   "Rente genauer",
   "Steigerung Durchschnittsentgelt Rentenversicherung p.a.",
   "Mit welcher jährlichen Entwicklung des rentenrechtlichen Durchschnittsentgelts soll gerechnet werden?",
   "Das ist eine Modellannahme für zukünftige Entgeltpunkte. Der vorhandene Standardwert kann normalerweise unverändert bleiben.",
   () => _settings.PensionAverageAnnualEarningsIncreaseRate,
   value => _settings.PensionAverageAnnualEarningsIncreaseRate = value,
   -0.10m,
   0.20m,
   optional: true,
   standardHint: () => $"Standardwert: {_settings.PensionAverageAnnualEarningsIncreaseRate:P1}");

  AddBool(
   _advancedQuestions,
   "Krankenversicherung",
   "KVdR für Person 1 annehmen",
   "Soll die App ab Rentenbeginn davon ausgehen, dass Person 1 die Voraussetzungen der Krankenversicherung der Rentner erfüllt?",
   "Die 9/10-Vorversicherungsregel und weitere Voraussetzungen prüft die App nicht. Wenn du unsicher bist, frage deine gesetzliche Krankenkasse.",
   () => _settings.KvdrPerson1,
   value => _settings.KvdrPerson1 = value,
   optional: true);

  AddBool(
   _advancedQuestions,
   "Krankenversicherung",
   "KVdR für Person 2 annehmen",
   "Soll die App ab Rentenbeginn davon ausgehen, dass Person 2 die KVdR-Voraussetzungen erfüllt?",
   "Wenn du die Voraussetzungen nicht sicher kennst, frage die gesetzliche Krankenkasse.",
   () => _settings.KvdrPerson2,
   value => _settings.KvdrPerson2 = value,
   optional: true,
   isVisible: () => _settings.HouseholdPersonCount == 2);

  AddPercent(
   _advancedQuestions,
   "Annahmen",
   "Inflation pro Jahr",
   "Mit welcher durchschnittlichen Inflation soll langfristig gerechnet werden?",
   "Das ist eine langfristige Modellannahme. Wenn du keine eigene Annahme hast, behalte den vorhandenen Standardwert.",
   () => _settings.InflationRate,
   value => _settings.InflationRate = value,
   -0.05m,
   0.20m,
   optional: true,
   standardHint: () => $"Aktueller/Standardwert: {_settings.InflationRate:P1}");

  AddPercent(
   _advancedQuestions,
   "Annahmen",
   "Rentensteigerung pro Jahr",
   "Mit welcher jährlichen Rentensteigerung soll gerechnet werden?",
   "Das ist eine langfristige Modellannahme. Künftige Rentenanpassungen stehen nicht fest.",
   () => _settings.PensionIncreaseRate,
   value => _settings.PensionIncreaseRate = value,
   -0.05m,
   0.20m,
   optional: true,
   standardHint: () => $"Aktueller/Standardwert: {_settings.PensionIncreaseRate:P1}");

  AddPercent(
   _advancedQuestions,
   "Krankenversicherung",
   "GKV Zusatzbeitrag",
   "Welcher Zusatzbeitrag der gesetzlichen Krankenkasse soll angesetzt werden?",
   "Den individuellen Zusatzbeitrag findest du auf der Website, in Schreiben oder in der Beitragsinformation deiner Krankenkasse.",
   () => _settings.VoluntaryHealthInsuranceAdditionalRate,
   value => _settings.VoluntaryHealthInsuranceAdditionalRate = value,
   0m,
   0.20m,
   optional: true);

  AddPercent(
   _advancedQuestions,
   "Krankenversicherung",
   "Pflegeversicherung Beitragssatz",
   "Welcher Pflegeversicherungs-Beitragssatz gilt für deine Planung?",
   "Der Satz hängt unter anderem von Kindern und deren Alter ab. Prüfe Beitragsbescheid, Krankenkasse oder die aktuellen offiziellen Angaben.",
   () => _settings.CareInsuranceChildlessRate,
   value => _settings.CareInsuranceChildlessRate = value,
   0m,
   0.20m,
   optional: true);

  AddBool(
   _advancedQuestions,
   "Steuern",
   "Gemeinsame steuerliche Veranlagung",
   "Soll bei zwei Personen eine gemeinsame steuerliche Veranlagung angenommen werden?",
   "Das ist eure steuerliche Situation bzw. Planungsannahme. Bei nur einer Person wird dieser Punkt nicht verwendet.",
   () => _settings.JointTaxation,
   value => _settings.JointTaxation = value,
   optional: true,
   isVisible: () => _settings.HouseholdPersonCount == 2);

  AddBool(
   _advancedQuestions,
   "Steuern",
   "Kirchensteuer berücksichtigen",
   "Soll Kirchensteuer in der Modellrechnung berücksichtigt werden?",
   "Wähle Ja, wenn Kirchensteuerpflicht besteht. Den konkreten Satz fragt der Wizard anschließend nur bei Ja ab.",
   () => _settings.ChurchTaxEnabled,
   value => _settings.ChurchTaxEnabled = value,
   optional: true);

  AddPercent(
   _advancedQuestions,
   "Steuern",
   "Kirchensteuersatz",
   "Welcher Kirchensteuersatz soll verwendet werden?",
   "Der Satz richtet sich nach dem Bundesland. Wenn keine Kirchensteuer berücksichtigt wird, wird dieses Feld nicht benötigt.",
   () => _settings.ChurchTaxRate,
   value => _settings.ChurchTaxRate = value,
   0m,
   1m,
   optional: true,
   isVisible: () => _settings.ChurchTaxEnabled);

  AddMoney(
   _advancedQuestions,
   "Steuern",
   "Sparer-Pauschbetrag Haushalt",
   "Welcher Sparer-Pauschbetrag soll für den Haushalt angesetzt werden?",
   "Wenn du keine besondere Konstellation modellieren möchtest, behalte den vorhandenen Standardwert.",
   () => _settings.CapitalGainsAllowance,
   value => _settings.CapitalGainsAllowance = value,
   0m,
   100000m,
   optional: true);

  AddDecimal(
   _expertQuestions,
   "Reserve",
   "Sichere Reserve in Jahresausgaben",
   "Wie viele Jahresausgaben sollen als sichere Reserve vorgesehen werden?",
   "Das ist eine persönliche Sicherheitsannahme. Wenn du unsicher bist, behalte den vorhandenen Standardwert.",
   () => _settings.ReserveYears,
   value => _settings.ReserveYears = value,
   0m,
   20m,
   optional: true,
   standardHint: () => $"Aktueller/Standardwert: {_settings.ReserveYears:0.##} Jahre");

  AddMoney(
   _advancedQuestions,
   "Rücklagen",
   "Hauswert inkl. Grundstück",
   "Welchen heutigen Gesamtwert hat ein vorhandenes Haus?",
   "Nutze eine aktuelle realistische Schätzung. Wenn kein Haus berücksichtigt werden soll, kannst du 0 eintragen oder die Frage überspringen.",
   () => _settings.HouseTotalValue,
   value => _settings.HouseTotalValue = value,
   0m,
   100000000m,
   optional: true);

  AddMoney(
   _advancedQuestions,
   "Rücklagen",
   "Ersatzwert Auto",
   "Welchen heutigen Betrag soll die Planung für einen späteren Autoersatz berücksichtigen?",
   "Nutze den Betrag, den ein vergleichbares Ersatzfahrzeug heute ungefähr kosten würde.",
   () => _settings.CarReplacementValue,
   value => _settings.CarReplacementValue = value,
   0m,
   1000000m,
   optional: true);

  AddMoney(
   _advancedQuestions,
   "Bestehende Depots",
   "Wert des vorhandenen Welt-ETF",
   "Wie hoch ist der heutige Wert eines bereits vorhandenen Welt-ETF?",
   "Den aktuellen Depotwert findest du im Broker-/Bankdepot. Nur bereits bestehende Bestände eintragen; die spätere Anlageaufteilung erfolgt separat.",
   () => _settings.WorldEtfCurrentValue,
   value => _settings.WorldEtfCurrentValue = value,
   0m,
   1000000000m,
   optional: true);

  AddMoney(
   _advancedQuestions,
   "Bestehende Depots",
   "Wert des vorhandenen Dividenden-ETF",
   "Wie hoch ist der heutige Wert eines bereits vorhandenen Dividenden-ETF?",
   "Den aktuellen Wert findest du im Depot. Wenn kein solcher ETF vorhanden ist, 0 beibehalten.",
   () => _settings.DividendEtfCurrentValue,
   value => _settings.DividendEtfCurrentValue = value,
   0m,
   1000000000m,
   optional: true);

  AddMoney(
   _advancedQuestions,
   "Bestehende Depots",
   "Wert vorhandener Dividenden-Aktien",
   "Wie hoch ist der heutige Wert bereits vorhandener Dividenden-Aktien?",
   "Nimm den aktuellen Depotwert der entsprechenden Einzelaktien. Wenn nicht vorhanden, 0 beibehalten.",
   () => _settings.DividendStocksCurrentValue,
   value => _settings.DividendStocksCurrentValue = value,
   0m,
   1000000000m,
   optional: true);

  AddPercent(
   _expertQuestions,
   "Rücklagen",
   "Anteil Gebäude am Hauswert",
   "Welcher Anteil des Hauswerts soll als Gebäudewert für die Haus-Rücklage verwendet werden?",
   "Wenn du keinen belastbaren Wert hast, behalte den vorhandenen Standardwert. Grundstück und Gebäude werden für diese Modellannahme getrennt betrachtet.",
   () => _settings.HouseBuildingShare,
   value => _settings.HouseBuildingShare = value,
   0m,
   1m,
   optional: true,
   standardHint: () => $"Aktueller/Standardwert: {_settings.HouseBuildingShare:P0}");

  AddPercent(
   _expertQuestions,
   "Rücklagen",
   "Jährliche Haus-Rücklage",
   "Welcher jährliche Prozentsatz des Gebäudewerts soll als Rücklagenziel angesetzt werden?",
   "Das ist eine Modellannahme für den sicheren Geldbedarf. Wenn du keine eigene Annahme hast, behalte den vorhandenen Standardwert.",
   () => _settings.HouseReserveRate,
   value => _settings.HouseReserveRate = value,
   0m,
   0.20m,
   optional: true,
   standardHint: () => $"Aktueller/Standardwert: {_settings.HouseReserveRate:P1}");

  AddInteger(
   _advancedQuestions,
   "Rücklagen",
   "Auto-Ersatz nach Jahren",
   "Nach wie vielen Jahren soll rechnerisch ein Autoersatz berücksichtigt werden?",
   "Das ist deine Planungsannahme. Wenn kein Auto relevant ist oder du unsicher bist, behalte den vorhandenen Wert.",
   () => _settings.CarReplacementYears,
   value => _settings.CarReplacementYears = value,
   1,
   100,
   optional: true,
   standardHint: () => $"Aktueller/Standardwert: {_settings.CarReplacementYears} Jahre");

  AddMoney(
   _advancedQuestions,
   "Rücklagen",
   "Gesundheit / Zahnersatz Rücklage",
   "Welcher heutige Zielbetrag soll für Gesundheit und Zahnersatz vorgehalten werden?",
   "Das ist eine persönliche Sicherheitsreserve. Wenn du keine eigene Zielgröße hast, behalte den vorhandenen Wert.",
   () => _settings.HealthReserveTarget,
   value => _settings.HealthReserveTarget = value,
   0m,
   10000000m,
   optional: true);

  AddMoney(
   _advancedQuestions,
   "Rücklagen",
   "Reisen / größere Wünsche Rücklage",
   "Welcher heutige Zielbetrag soll für Reisen oder größere Wünsche reserviert werden?",
   "Das ist eine persönliche Planungsgröße. 0 bedeutet, dass dafür keine eigene Rücklage vorgesehen wird.",
   () => _settings.TravelReserveTarget,
   value => _settings.TravelReserveTarget = value,
   0m,
   10000000m,
   optional: true);

  AddMoney(
   _advancedQuestions,
   "Rücklagen",
   "Sonstiges / Unvorhergesehenes Rücklage",
   "Welcher heutige Zielbetrag soll zusätzlich für Unvorhergesehenes reserviert werden?",
   "Das ist eine persönliche Sicherheitsreserve. Wenn keine separate Reserve gewünscht ist, 0 beibehalten.",
   () => _settings.OtherReserveTarget,
   value => _settings.OtherReserveTarget = value,
   0m,
   10000000m,
   optional: true);

  AddInteger(
   _advancedQuestions,
   "Bestehende Depots",
   "Startjahr des vorhandenen Welt-ETF",
   "Seit welchem Jahr besteht der bereits vorhandene Welt-ETF?",
   "Das Kauf-/Startjahr findest du in der Depot- oder Transaktionshistorie. Die App nutzt es nur zur Schätzung des steuerlichen Einstandswerts.",
   () => _settings.WorldEtfStartYear,
   value => _settings.WorldEtfStartYear = value,
   1900,
   currentYear,
   optional: true,
   isVisible: () => _settings.WorldEtfCurrentValue > 0m);

  AddPercent(
   _expertQuestions,
   "Bestehende Depots",
   "Bisherige Durchschnittsrendite Welt-ETF",
   "Welche durchschnittliche jährliche Rendite soll zur Schätzung des bisherigen Wertzuwachses verwendet werden?",
   "Wenn du sie nicht aus deiner Depotentwicklung ableiten kannst, behalte den vorhandenen Standardwert.",
   () => _settings.WorldEtfHistoricalReturn,
   value => _settings.WorldEtfHistoricalReturn = value,
   -0.99m,
   1m,
   optional: true,
   isVisible: () => _settings.WorldEtfCurrentValue > 0m);

  AddInteger(
   _advancedQuestions,
   "Bestehende Depots",
   "Startjahr des vorhandenen Dividenden-ETF",
   "Seit welchem Jahr besteht der bereits vorhandene Dividenden-ETF?",
   "Das Startjahr findest du in deiner Depot- oder Transaktionshistorie.",
   () => _settings.DividendEtfStartYear,
   value => _settings.DividendEtfStartYear = value,
   1900,
   currentYear,
   optional: true,
   isVisible: () => _settings.DividendEtfCurrentValue > 0m);

  AddPercent(
   _expertQuestions,
   "Bestehende Depots",
   "Bisherige Durchschnittsrendite Dividenden-ETF",
   "Welche durchschnittliche jährliche Rendite soll für den vorhandenen Dividenden-ETF angenommen werden?",
   "Wenn du keinen belastbaren Wert hast, behalte den vorhandenen Standardwert.",
   () => _settings.DividendEtfHistoricalReturn,
   value => _settings.DividendEtfHistoricalReturn = value,
   -0.99m,
   1m,
   optional: true,
   isVisible: () => _settings.DividendEtfCurrentValue > 0m);

  AddInteger(
   _advancedQuestions,
   "Bestehende Depots",
   "Startjahr der vorhandenen Dividenden-Aktien",
   "Seit welchem Jahr bestehen die bereits vorhandenen Dividenden-Aktien?",
   "Nutze die Depot- oder Transaktionshistorie. Bei vielen Einzelkäufen ist dies nur eine vereinfachte Modellangabe.",
   () => _settings.DividendStocksStartYear,
   value => _settings.DividendStocksStartYear = value,
   1900,
   currentYear,
   optional: true,
   isVisible: () => _settings.DividendStocksCurrentValue > 0m);

  AddPercent(
   _expertQuestions,
   "Bestehende Depots",
   "Bisherige Durchschnittsrendite Dividenden-Aktien",
   "Welche durchschnittliche jährliche Rendite soll für die vorhandenen Dividenden-Aktien angenommen werden?",
   "Wenn du keinen belastbaren Wert hast, behalte den vorhandenen Standardwert.",
   () => _settings.DividendStocksHistoricalReturn,
   value => _settings.DividendStocksHistoricalReturn = value,
   -0.99m,
   1m,
   optional: true,
   isVisible: () => _settings.DividendStocksCurrentValue > 0m);

  AddChoice(
   _advancedQuestions,
   "Strategie",
   "Anlagestrategie",
   "Welche vorhandene Standardstrategie soll verwendet werden?",
   "Wenn du keine individuelle Aufteilung festlegen möchtest, wähle eine der drei Standardstrategien. Die bestehende benutzerdefinierte Aufteilung wird vom Wizard nicht verändert, solange du hier überspringst.",
   () => GetStrategyForCurrentAllocation(),
   value =>
   {
    if (value is "Sicherheit" or "Ausgewogen" or "Wachstum")
    {
     _settings.Strategy = value;
     _allocation = StrategyService.GetDefault(value);
    }
   },
   ["Benutzerdefiniert (beibehalten)", "Sicherheit", "Ausgewogen", "Wachstum"],
   optional: true);

  AddChoice(
   _expertQuestions,
   "Stressszenario",
   "Crash-Stärke am Anfang",
   "Welche Kursbelastung soll das Stressszenario am Simulationsanfang verwenden?",
   "Das ist keine Prognose, sondern ein Belastungstest. Wenn du unsicher bist, behalte den vorhandenen Wert.",
   () => FormatStressCrash(_settings.StressCrashPercent),
   value => _settings.StressCrashPercent = ParseStressCrash(value),
   ["-15 %", "-25 %", "-40 %"],
   optional: true);
 }

 private void ShowIntro()
 {
  _phase = WizardPhase.Intro;
  _questionIndex = -1;
  _returnToSummaryAfterEdit = false;

  PhaseText.Text = "Wizard";
  Progress.Maximum = 100;
  Progress.Value = 0;
  ProgressText.Text = "";
  QuestionTitle.Text = "Welchen Bereich möchtest du bearbeiten?";
  ExplanationText.Text =
   "Du kannst jedes Kapitel einzeln öffnen. Die Basics sind für eine erste sinnvolle Simulation gedacht. " +
   "Fortgeschrittene und Experteneinstellungen sind optional.";

  WherePanel.Visibility = Visibility.Collapsed;
  StandardHintText.Text = "";
  ErrorText.Text = "";

  var panel = new StackPanel();

  panel.Children.Add(CreateChapterButton(
   "1. Basics",
   "Die wichtigsten Angaben für eine brauchbare erste Simulation.",
   () =>
   {
    _phase = WizardPhase.Core;
    _questionIndex = FindNextVisibleIndex(_coreQuestions, -1);
    ShowCurrentQuestion();
   }));

  panel.Children.Add(CreateChapterButton(
   "2. Fortgeschritten",
   "Zusätzliche persönliche Angaben für eine genauere Modellierung.",
   () =>
   {
    _advancedEnabled = true;
    _phase = WizardPhase.Advanced;
    _questionIndex = FindNextVisibleIndex(_advancedQuestions, -1);
    ShowCurrentQuestion();
   }));

  panel.Children.Add(CreateChapterButton(
   "3. Experten",
   "Technische Modellannahmen und Belastungstests.",
   () =>
   {
    _expertEnabled = true;
    _phase = WizardPhase.Expert;
    _questionIndex = FindNextVisibleIndex(_expertQuestions, -1);
    ShowCurrentQuestion();
   }));

  panel.Children.Add(CreateInfoCard(
   "Hinweis",
   "Optionale Fragen kannst du mit „Weiß ich nicht“ überspringen. Dann bleibt der bereits vorhandene Wert bzw. Standardwert unverändert."));

  InputHost.Content = panel;

  BackButton.Visibility = Visibility.Collapsed;
  SkipButton.Visibility = Visibility.Collapsed;
  ApplyCalculateButton.Visibility = Visibility.Collapsed;
  NextButton.Visibility = Visibility.Collapsed;
 }

 private Border CreateChapterButton(
  string title,
  string text,
  Action action)
 {
  var button = new Button
  {
   HorizontalAlignment = HorizontalAlignment.Stretch,
   Padding = new Thickness(0),
   Margin = new Thickness(0, 0, 0, 10),
   Style = (Style)FindResource("SecondaryButton")
  };

  var stack = new StackPanel
  {
   HorizontalAlignment = HorizontalAlignment.Center,
   Margin = new Thickness(0, 12, 0, 12)
  };

  stack.Children.Add(new TextBlock
  {
   Text = title,
   FontWeight = FontWeights.SemiBold,
   Foreground = (Brush)FindResource("AccentBrush"),
   TextAlignment = TextAlignment.Left
  });

  stack.Children.Add(new TextBlock
  {
   Text = text,
   Margin = new Thickness(0, 5, 0, 0),
   TextWrapping = TextWrapping.Wrap,
   Foreground = (Brush)FindResource("TextBrush"),
   TextAlignment = TextAlignment.Left
  });

  button.SizeChanged += (_, _) =>
  {
   stack.Width = Math.Max(0, button.ActualWidth - 40);
  };

  button.Content = stack;
  button.Click += (_, _) => action();

  return new Border
  {
   Child = button
  };
 }

 private Border CreateInfoCard(string title, string text)
 {
  var stack = new StackPanel();
  stack.Children.Add(new TextBlock
  {
   Text = title,
   FontWeight = FontWeights.SemiBold,
   Foreground = (Brush)FindResource("AccentBrush")
  });
  stack.Children.Add(new TextBlock
  {
   Text = text,
   Margin = new Thickness(0, 5, 0, 0),
   TextWrapping = TextWrapping.Wrap,
   Foreground = (Brush)FindResource("TextBrush")
  });

  return new Border
  {
   BorderBrush = (Brush)FindResource("MutedTextBrush"),
   BorderThickness = new Thickness(1),
   CornerRadius = new CornerRadius(6),
   Padding = new Thickness(12),
   Margin = new Thickness(0, 0, 0, 10),
   Child = stack
  };
 }

 private void ShowCurrentQuestion()
 {
  List<WizardQuestion> list =
   _phase == WizardPhase.Core
    ? _coreQuestions
    : _phase == WizardPhase.Advanced
     ? _advancedQuestions
     : _expertQuestions;

  if (_questionIndex < 0 ||
      _questionIndex >= list.Count ||
      !IsQuestionVisible(list[_questionIndex]))
  {
   _questionIndex = FindNextVisibleIndex(list, -1);

   if (_questionIndex < 0)
   {
    ShowSummary();
    return;
   }
  }

  WizardQuestion question = list[_questionIndex];
  List<int> visibleIndexes = GetVisibleIndexes(list);
  int visiblePosition = visibleIndexes.IndexOf(_questionIndex) + 1;

  PhaseText.Text =
   _phase == WizardPhase.Core
    ? "Basics"
    : _phase == WizardPhase.Advanced
     ? "Fortgeschritten – optional"
     : "Experten – optional";

  Progress.Maximum = Math.Max(1, visibleIndexes.Count);
  Progress.Value = visiblePosition;
  ProgressText.Text = $"Frage {visiblePosition} von {visibleIndexes.Count}";

  QuestionTitle.Text = question.Title;
  ExplanationText.Text = question.Explanation;
  WhereText.Text = question.WhereToFind;
  WherePanel.Visibility = Visibility.Visible;

  StandardHintText.Text =
   question.StandardHint?.Invoke() ?? "";

  ErrorText.Text = "";
  _currentEditor = CreateEditor(question);
  InputHost.Content = _currentEditor;

  BackButton.Visibility = Visibility.Visible;
  SkipButton.Visibility =
   question.Optional
    ? Visibility.Visible
    : Visibility.Collapsed;
  ApplyCalculateButton.Visibility = Visibility.Collapsed;
  NextButton.Visibility = Visibility.Visible;
  NextButton.Content = _returnToSummaryAfterEdit ? "Speichern" : "Weiter";

  FocusEditor();
 }

 private FrameworkElement CreateEditor(WizardQuestion question)
 {
  if (question.Choices != null)
  {
   var combo = new ComboBox
   {
    MinWidth = 280,
    HorizontalAlignment = HorizontalAlignment.Left
   };

   foreach (string choice in question.Choices)
    combo.Items.Add(choice);

   combo.SelectedItem = question.GetValue();

   if (combo.SelectedIndex < 0 && combo.Items.Count > 0)
    combo.SelectedIndex = 0;

   return combo;
  }

  var textBox = new TextBox
  {
   Text = question.GetValue(),
   MinWidth = 280,
   MaxWidth = 420,
   HorizontalAlignment = HorizontalAlignment.Left,
   Padding = new Thickness(8, 6, 8, 6)
  };

  textBox.GotKeyboardFocus += (_, _) =>
  {
   if (Keyboard.FocusedElement == textBox)
    textBox.SelectAll();
  };

  return textBox;
 }

 private void FocusEditor()
 {
  Dispatcher.BeginInvoke(new Action(() =>
  {
   if (_currentEditor == null)
    return;

   _currentEditor.Focus();

   if (_currentEditor is TextBox textBox)
    textBox.SelectAll();
  }));
 }

 private void ShowPrecisionChoice()
 {
  _phase = WizardPhase.PrecisionChoice;
  _questionIndex = -1;
  _returnToSummaryAfterEdit = false;

  PhaseText.Text = "Basics abgeschlossen";
  Progress.Maximum = 100;
  Progress.Value = 100;
  ProgressText.Text = "Basics vollständig";
  QuestionTitle.Text = "Möchtest du die fortgeschrittenen Einstellungen bearbeiten?";
  ExplanationText.Text =
   "Die Basics sind jetzt erfasst. Du kannst direkt zur Zusammenfassung gehen " +
   "oder die optionalen fortgeschrittenen Einstellungen für eine genauere Modellierung bearbeiten.";

  WherePanel.Visibility = Visibility.Collapsed;
  StandardHintText.Text = "";
  ErrorText.Text = "";

  var panel = new StackPanel();

  var summaryButton = new Button
  {
   Content = "Basics reichen – zur Zusammenfassung",
   Padding = new Thickness(12, 9, 12, 9),
   Margin = new Thickness(0, 0, 0, 10),
   HorizontalAlignment = HorizontalAlignment.Stretch
  };
  summaryButton.Click += (_, _) =>
  {
   _advancedEnabled = false;
   _expertEnabled = false;
   ShowSummary();
  };

  var precisionButton = new Button
  {
   Content = "Fortgeschrittene Einstellungen",
   Padding = new Thickness(12, 9, 12, 9),
   HorizontalAlignment = HorizontalAlignment.Stretch
  };
  precisionButton.Click += (_, _) =>
  {
   _advancedEnabled = true;
   _phase = WizardPhase.Advanced;
   _questionIndex = FindNextVisibleIndex(_advancedQuestions, -1);
   ShowCurrentQuestion();
  };

  panel.Children.Add(summaryButton);
  panel.Children.Add(precisionButton);
  InputHost.Content = panel;

  BackButton.Visibility = Visibility.Visible;
  SkipButton.Visibility = Visibility.Collapsed;
  ApplyCalculateButton.Visibility = Visibility.Collapsed;
  NextButton.Visibility = Visibility.Collapsed;
 }

 private void ShowExpertChoice()
 {
  _phase = WizardPhase.ExpertChoice;
  _questionIndex = -1;
  _returnToSummaryAfterEdit = false;

  PhaseText.Text = "Fortgeschritten abgeschlossen";
  Progress.Maximum = 100;
  Progress.Value = 100;
  ProgressText.Text = "Fortgeschrittene Einstellungen vollständig";
  QuestionTitle.Text = "Möchtest du die Experteneinstellungen bearbeiten?";
  ExplanationText.Text =
   "Die fortgeschrittenen Einstellungen sind abgeschlossen. Du kannst direkt zur Zusammenfassung gehen " +
   "oder zusätzlich die optionalen Experteneinstellungen und Modellannahmen bearbeiten.";

  WherePanel.Visibility = Visibility.Collapsed;
  StandardHintText.Text = "";
  ErrorText.Text = "";

  var panel = new StackPanel();

  var summaryButton = new Button
  {
   Content = "Fortgeschritten reicht – zur Zusammenfassung",
   Padding = new Thickness(12, 9, 12, 9),
   Margin = new Thickness(0, 0, 0, 10),
   HorizontalAlignment = HorizontalAlignment.Stretch
  };
  summaryButton.Click += (_, _) =>
  {
   _expertEnabled = false;
   ShowSummary();
  };

  var expertButton = new Button
  {
   Content = "Experteneinstellungen",
   Padding = new Thickness(12, 9, 12, 9),
   HorizontalAlignment = HorizontalAlignment.Stretch
  };
  expertButton.Click += (_, _) =>
  {
   _expertEnabled = true;
   _phase = WizardPhase.Expert;
   _questionIndex = FindNextVisibleIndex(_expertQuestions, -1);
   ShowCurrentQuestion();
  };

  panel.Children.Add(summaryButton);
  panel.Children.Add(expertButton);
  InputHost.Content = panel;

  BackButton.Visibility = Visibility.Visible;
  SkipButton.Visibility = Visibility.Collapsed;
  ApplyCalculateButton.Visibility = Visibility.Collapsed;
  NextButton.Visibility = Visibility.Collapsed;
 }

 private void ShowSummary()
 {
  _phase = WizardPhase.Summary;
  _returnToSummaryAfterEdit = false;

  PhaseText.Text = "Zusammenfassung";
  Progress.Maximum = 100;
  Progress.Value = 100;
  ProgressText.Text = "Bereit zur Übernahme";
  QuestionTitle.Text = "Eingaben prüfen";
  ExplanationText.Text =
   "Die Werte werden erst in die Hauptkonfiguration übernommen, wenn du unten auf „Übernehmen“ oder „Übernehmen & berechnen“ klickst.";

  WherePanel.Visibility = Visibility.Collapsed;
  StandardHintText.Text = "";
  ErrorText.Text = "";

  var panel = new StackPanel();

  AppendSummaryQuestions(panel, _coreQuestions);

  if (_advancedEnabled)
   AppendSummaryQuestions(panel, _advancedQuestions);

  if (_expertEnabled)
   AppendSummaryQuestions(panel, _expertQuestions);

  InputHost.Content = panel;

  BackButton.Visibility = Visibility.Visible;
  SkipButton.Visibility = Visibility.Collapsed;
  ApplyCalculateButton.Visibility = Visibility.Visible;
  NextButton.Visibility = Visibility.Visible;
  NextButton.Content = "Übernehmen";
 }

 private void AppendSummaryQuestions(
  StackPanel panel,
  List<WizardQuestion> questions)
 {
  string? lastCategory = null;

  foreach (WizardQuestion question in questions.Where(IsQuestionVisible))
  {
   if (!string.Equals(lastCategory, question.Category, StringComparison.Ordinal))
   {
    panel.Children.Add(new TextBlock
    {
     Text = question.Category,
     FontWeight = FontWeights.SemiBold,
     Foreground = (Brush)FindResource("AccentBrush"),
     Margin = new Thickness(0, panel.Children.Count == 0 ? 0 : 14, 0, 6)
    });

    lastCategory = question.Category;
   }

   var row = new Grid
   {
    Margin = new Thickness(0, 0, 0, 5)
   };
   row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
   row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
   row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

   var label = new TextBlock
   {
    Text = question.Title,
    TextWrapping = TextWrapping.Wrap,
    Foreground = (Brush)FindResource("TextBrush"),
    VerticalAlignment = VerticalAlignment.Center
   };

   var value = new TextBlock
   {
    Text = question.GetValue(),
    Margin = new Thickness(12, 0, 12, 0),
    Foreground = (Brush)FindResource("TextBrush"),
    VerticalAlignment = VerticalAlignment.Center
   };

   var editButton = new Button
   {
    Content = "Ändern",
    Padding = new Thickness(8, 3, 8, 3),
    Style = (Style)FindResource("SecondaryButton"),
    Tag = question
   };
   editButton.Click += SummaryEdit_Click;

   Grid.SetColumn(label, 0);
   Grid.SetColumn(value, 1);
   Grid.SetColumn(editButton, 2);
   row.Children.Add(label);
   row.Children.Add(value);
   row.Children.Add(editButton);

   panel.Children.Add(row);
  }
 }

 private void SummaryEdit_Click(object sender, RoutedEventArgs e)
 {
  if (sender is not Button button ||
      button.Tag is not WizardQuestion question)
   return;

  if (_coreQuestions.Contains(question))
  {
   _phase = WizardPhase.Core;
   _questionIndex = _coreQuestions.IndexOf(question);
  }
  else if (_advancedQuestions.Contains(question))
  {
   _advancedEnabled = true;
   _phase = WizardPhase.Advanced;
   _questionIndex = _advancedQuestions.IndexOf(question);
  }
  else
  {
   _expertEnabled = true;
   _phase = WizardPhase.Expert;
   _questionIndex = _expertQuestions.IndexOf(question);
  }

  _returnToSummaryAfterEdit = true;
  ShowCurrentQuestion();
 }

 private void Next_Click(object sender, RoutedEventArgs e)
 {
  ErrorText.Text = "";

  if (_phase == WizardPhase.Intro)
  {
   _phase = WizardPhase.Core;
   _questionIndex = FindNextVisibleIndex(_coreQuestions, -1);
   ShowCurrentQuestion();
   return;
  }

  if (_phase == WizardPhase.Summary)
  {
   Complete(false);
   return;
  }

  if (_phase is not (WizardPhase.Core or WizardPhase.Advanced or WizardPhase.Expert))
   return;

  List<WizardQuestion> list =
   _phase == WizardPhase.Core
    ? _coreQuestions
    : _phase == WizardPhase.Advanced
     ? _advancedQuestions
     : _expertQuestions;

  WizardQuestion question = list[_questionIndex];

  string? error = question.Apply(ReadEditorValue());
  if (!string.IsNullOrWhiteSpace(error))
  {
   ErrorText.Text = error;
   return;
  }

  if (_returnToSummaryAfterEdit)
  {
   ShowSummary();
   return;
  }

  int next = FindNextVisibleIndex(list, _questionIndex);

  if (next >= 0)
  {
   _questionIndex = next;
   ShowCurrentQuestion();
   return;
  }

  if (_phase == WizardPhase.Core)
  {
   ShowPrecisionChoice();
   return;
  }

  if (_phase == WizardPhase.Advanced)
  {
   ShowExpertChoice();
   return;
  }

  ShowSummary();
 }

 private void Back_Click(object sender, RoutedEventArgs e)
 {
  ErrorText.Text = "";

  if (_returnToSummaryAfterEdit)
  {
   ShowSummary();
   return;
  }

  if (_phase == WizardPhase.Core)
  {
   int previous = FindPreviousVisibleIndex(_coreQuestions, _questionIndex);

   if (previous >= 0)
   {
    _questionIndex = previous;
    ShowCurrentQuestion();
   }
   else
   {
    ShowIntro();
   }

   return;
  }

  if (_phase == WizardPhase.PrecisionChoice)
  {
   _phase = WizardPhase.Core;
   _questionIndex = FindPreviousVisibleIndex(_coreQuestions, _coreQuestions.Count);
   ShowCurrentQuestion();
   return;
  }

  if (_phase == WizardPhase.Advanced)
  {
   int previous = FindPreviousVisibleIndex(_advancedQuestions, _questionIndex);

   if (previous >= 0)
   {
    _questionIndex = previous;
    ShowCurrentQuestion();
   }
   else
   {
    ShowIntro();
   }

   return;
  }

  if (_phase == WizardPhase.ExpertChoice)
  {
   _phase = WizardPhase.Advanced;
   _questionIndex = FindPreviousVisibleIndex(
    _advancedQuestions,
    _advancedQuestions.Count);
   ShowCurrentQuestion();
   return;
  }

  if (_phase == WizardPhase.Expert)
  {
   int previous = FindPreviousVisibleIndex(_expertQuestions, _questionIndex);

   if (previous >= 0)
   {
    _questionIndex = previous;
    ShowCurrentQuestion();
   }
   else
   {
    ShowIntro();
   }

   return;
  }

  if (_phase == WizardPhase.Summary)
  {
   if (_expertEnabled)
   {
    _phase = WizardPhase.Expert;
    _questionIndex = FindPreviousVisibleIndex(
     _expertQuestions,
     _expertQuestions.Count);
    ShowCurrentQuestion();
   }
   else if (_advancedEnabled)
   {
    ShowExpertChoice();
   }
   else
   {
    ShowPrecisionChoice();
   }
  }
 }

 private void Skip_Click(object sender, RoutedEventArgs e)
 {
  if (_phase is not (WizardPhase.Core or WizardPhase.Advanced or WizardPhase.Expert))
   return;

  List<WizardQuestion> list =
   _phase == WizardPhase.Core
    ? _coreQuestions
    : _phase == WizardPhase.Advanced
     ? _advancedQuestions
     : _expertQuestions;

  if (_questionIndex < 0 ||
      _questionIndex >= list.Count ||
      !list[_questionIndex].Optional)
   return;

  if (_returnToSummaryAfterEdit)
  {
   ShowSummary();
   return;
  }

  int next = FindNextVisibleIndex(list, _questionIndex);

  if (next >= 0)
  {
   _questionIndex = next;
   ShowCurrentQuestion();
   return;
  }

  if (_phase == WizardPhase.Core)
   ShowPrecisionChoice();
  else if (_phase == WizardPhase.Advanced)
   ShowExpertChoice();
  else
   ShowSummary();
 }

 private void ApplyCalculate_Click(object sender, RoutedEventArgs e)
 {
  if (_phase != WizardPhase.Summary)
   return;

  Complete(true);
 }

 private void Complete(bool startCalculation)
 {
  string? validationError = ValidateFinalSettings();
  if (validationError != null)
  {
   ErrorText.Text = validationError;
   return;
  }

  StartCalculation = startCalculation;
  DialogResult = true;
  Close();
 }

 private string? ValidateFinalSettings()
 {
  int currentYear = DateTime.Today.Year;
  int workEndAgePerson1 =
   _settings.Person1Age +
   Math.Max(0, _settings.Person1WorkEndYear - currentYear);

  if (_settings.Person1RetirementAge < workEndAgePerson1)
   return "Der Rentenbeginn von Person 1 darf nicht vor dem geplanten Arbeitsende liegen.";

  if (_settings.Person1EndAge < _settings.Person1RetirementAge)
   return "Die Lebenserwartung von Person 1 muss nach dem Rentenbeginn liegen.";

  if (_settings.HouseholdPersonCount == 2)
  {
   int workEndAgePerson2 =
    _settings.Person2Age +
    Math.Max(0, _settings.Person2WorkEndYear - currentYear);

   if (_settings.Person2RetirementAge < workEndAgePerson2)
    return "Der Rentenbeginn von Person 2 darf nicht vor dem geplanten Arbeitsende liegen.";

   if (_settings.Person2EndAge < _settings.Person2RetirementAge)
    return "Die Lebenserwartung von Person 2 muss nach dem Rentenbeginn liegen.";
  }

  decimal existingDepotTotal =
   _settings.WorldEtfCurrentValue +
   _settings.DividendEtfCurrentValue +
   _settings.DividendStocksCurrentValue;

  if (existingDepotTotal > _settings.StartCapital)
   return "Die Summe der bestehenden Depotwerte darf das verfügbare Startvermögen nicht überschreiten.";

  return null;
 }

 private string ReadEditorValue()
 {
  return _currentEditor switch
  {
   TextBox textBox => textBox.Text,
   ComboBox comboBox => comboBox.SelectedItem?.ToString() ?? "",
   _ => ""
  };
 }

 private List<int> GetVisibleIndexes(List<WizardQuestion> questions)
 {
  var indexes = new List<int>();

  for (int i = 0; i < questions.Count; i++)
  {
   if (IsQuestionVisible(questions[i]))
    indexes.Add(i);
  }

  return indexes;
 }

 private static bool IsQuestionVisible(WizardQuestion question) =>
  question.IsVisible?.Invoke() ?? true;

 private static int FindNextVisibleIndex(
  List<WizardQuestion> questions,
  int afterIndex)
 {
  for (int i = afterIndex + 1; i < questions.Count; i++)
  {
   if (IsQuestionVisible(questions[i]))
    return i;
  }

  return -1;
 }

 private static int FindPreviousVisibleIndex(
  List<WizardQuestion> questions,
  int beforeIndex)
 {
  for (int i = Math.Min(beforeIndex - 1, questions.Count - 1); i >= 0; i--)
  {
   if (IsQuestionVisible(questions[i]))
    return i;
  }

  return -1;
 }

 private void Cancel_Click(object sender, RoutedEventArgs e)
 {
  DialogResult = false;
  Close();
 }

 private string GetStrategyForCurrentAllocation()
 {
  foreach (string strategy in new[] { "Sicherheit", "Ausgewogen", "Wachstum" })
  {
   StrategyAllocation candidate = StrategyService.GetDefault(strategy);

   if (_allocation.Cash == candidate.Cash &&
       _allocation.WorldEtf == candidate.WorldEtf &&
       _allocation.DividendEtf == candidate.DividendEtf &&
       _allocation.DividendStocks == candidate.DividendStocks)
    return strategy;
  }

  return "Benutzerdefiniert (beibehalten)";
 }

 private static string FormatStressCrash(decimal value) =>
  value <= -0.39m ? "-40 %" :
  value <= -0.24m ? "-25 %" :
  "-15 %";

 private static decimal ParseStressCrash(string value) =>
  value switch
  {
   "-15 %" => -0.15m,
   "-40 %" => -0.40m,
   _ => -0.25m
  };

 private void AddInteger(
  List<WizardQuestion> list,
  string category,
  string title,
  string explanation,
  string whereToFind,
  Func<int> getter,
  Action<int> setter,
  int min,
  int max,
  Func<int, string?>? validator = null,
  bool optional = false,
  Func<string>? standardHint = null,
  Func<bool>? isVisible = null)
 {
  list.Add(new WizardQuestion(
   category,
   title,
   explanation,
   whereToFind,
   optional,
   isVisible,
   () => getter().ToString(CultureInfo.InvariantCulture),
   value =>
   {
    if (!int.TryParse(
         value.Trim(),
         NumberStyles.Integer,
         CultureInfo.InvariantCulture,
         out int parsed))
     return "Bitte eine gültige Ganzzahl eingeben.";

    if (parsed < min || parsed > max)
     return $"Bitte einen Wert zwischen {min} und {max} eingeben.";

    string? error = validator?.Invoke(parsed);
    if (error != null)
     return error;

    setter(parsed);
    return null;
   },
   null,
   standardHint));
 }

 private void AddMoney(
  List<WizardQuestion> list,
  string category,
  string title,
  string explanation,
  string whereToFind,
  Func<decimal> getter,
  Action<decimal> setter,
  decimal min,
  decimal max,
  bool optional = false,
  Func<string>? standardHint = null,
  Func<bool>? isVisible = null)
 {
  AddDecimalInternal(
   list,
   category,
   title,
   explanation,
   whereToFind,
   getter,
   setter,
   min,
   max,
   optional,
   standardHint,
   isVisible,
   value => value.ToString("N2", GermanCulture),
   "Bitte einen gültigen Euro-Betrag eingeben.");
 }

 private void AddDecimal(
  List<WizardQuestion> list,
  string category,
  string title,
  string explanation,
  string whereToFind,
  Func<decimal> getter,
  Action<decimal> setter,
  decimal min,
  decimal max,
  bool optional = false,
  Func<string>? standardHint = null,
  Func<bool>? isVisible = null)
 {
  AddDecimalInternal(
   list,
   category,
   title,
   explanation,
   whereToFind,
   getter,
   setter,
   min,
   max,
   optional,
   standardHint,
   isVisible,
   value => value.ToString("0.####", GermanCulture),
   "Bitte eine gültige Zahl eingeben.");
 }

 private void AddPercent(
  List<WizardQuestion> list,
  string category,
  string title,
  string explanation,
  string whereToFind,
  Func<decimal> getter,
  Action<decimal> setter,
  decimal min,
  decimal max,
  bool optional = false,
  Func<string>? standardHint = null,
  Func<bool>? isVisible = null)
 {
  list.Add(new WizardQuestion(
   category,
   title,
   explanation,
   whereToFind,
   optional,
   isVisible,
   () => (getter() * 100m).ToString("0.##", GermanCulture),
   value =>
   {
    if (!TryParseGermanDecimal(value, out decimal percent))
     return "Bitte einen gültigen Prozentwert eingeben.";

    decimal parsed = percent / 100m;

    if (parsed < min || parsed > max)
     return $"Bitte einen Prozentwert zwischen {min:P0} und {max:P0} eingeben.";

    setter(parsed);
    return null;
   },
   null,
   standardHint));
 }

 private void AddDecimalInternal(
  List<WizardQuestion> list,
  string category,
  string title,
  string explanation,
  string whereToFind,
  Func<decimal> getter,
  Action<decimal> setter,
  decimal min,
  decimal max,
  bool optional,
  Func<string>? standardHint,
  Func<bool>? isVisible,
  Func<decimal, string> formatter,
  string parseError)
 {
  list.Add(new WizardQuestion(
   category,
   title,
   explanation,
   whereToFind,
   optional,
   isVisible,
   () => formatter(getter()),
   value =>
   {
    if (!TryParseGermanDecimal(value, out decimal parsed))
     return parseError;

    if (parsed < min || parsed > max)
     return "Der eingegebene Wert liegt außerhalb des von der App unterstützten Bereichs.";

    setter(parsed);
    return null;
   },
   null,
   standardHint));
 }

 private void AddChoice(
  List<WizardQuestion> list,
  string category,
  string title,
  string explanation,
  string whereToFind,
  Func<string> getter,
  Action<string> setter,
  string[] choices,
  bool optional = false,
  Func<string>? standardHint = null,
  Func<bool>? isVisible = null)
 {
  list.Add(new WizardQuestion(
   category,
   title,
   explanation,
   whereToFind,
   optional,
   isVisible,
   getter,
   value =>
   {
    if (!choices.Contains(value, StringComparer.Ordinal))
     return "Bitte eine der angebotenen Optionen auswählen.";

    setter(value);
    return null;
   },
   choices,
   standardHint));
 }

 private void AddBool(
  List<WizardQuestion> list,
  string category,
  string title,
  string explanation,
  string whereToFind,
  Func<bool> getter,
  Action<bool> setter,
  bool optional = false,
  Func<string>? standardHint = null,
  Func<bool>? isVisible = null)
 {
  AddChoice(
   list,
   category,
   title,
   explanation,
   whereToFind,
   () => getter() ? "Ja" : "Nein",
   value => setter(value == "Ja"),
   ["Ja", "Nein"],
   optional,
   standardHint,
   isVisible);
 }

 private static bool TryParseGermanDecimal(
  string value,
  out decimal result)
 {
  return decimal.TryParse(
   value.Trim(),
   NumberStyles.Number,
   GermanCulture,
   out result);
 }

 private sealed record WizardQuestion(
  string Category,
  string Title,
  string Explanation,
  string WhereToFind,
  bool Optional,
  Func<bool>? IsVisible,
  Func<string> GetValue,
  Func<string, string?> Apply,
  string[]? Choices,
  Func<string>? StandardHint);

 private enum WizardPhase
 {
  Intro,
  Core,
  PrecisionChoice,
  Advanced,
  ExpertChoice,
  Expert,
  Summary
 }
}
