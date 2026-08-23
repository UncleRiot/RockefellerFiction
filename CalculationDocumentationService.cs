using System.IO;
using System.Text;
using System.Text.Json;

namespace RockefellerFiction;

public static class CalculationDocumentationService
{
 private static readonly string[] CalculationSourceFiles =
 [
  "PensionService.cs",
  "ProjectionService.cs",
  "RecommendationService.cs",
  "StrategyService.cs",
  "TaxService.cs"
 ];

 public static string Build(
  PlannerSettings settings,
  StrategyAllocation allocation)
 {
  var text = new StringBuilder();

  AppendTitle(text, "BERECHNUNGSGRUNDLAGEN");

  text.AppendLine("Diese Ansicht wird bei jedem Öffnen neu erzeugt.");
  text.AppendLine("Die vollständigen aktuell verwendeten Einstellungen werden automatisch aus PlannerSettings und der aktuellen Strategieaufteilung ausgegeben.");
  text.AppendLine("Die Berechnungsquellen werden vollständig direkt aus den aktuell im Projekt vorhandenen C#-Quelldateien gelesen.");
  text.AppendLine("Ändert sich eine Einstellung, eine Formel oder eine Berechnungsmethode im Source, erscheint sie nach dem nächsten Öffnen dieser Lasche im automatischen Teil der Dokumentation.");
  text.AppendLine();

  AppendCurrentValues(text, settings, allocation);
  AppendCompleteSettingsSnapshot(text, settings, allocation);
  AppendHumanReadableCalculationFlow(text);
  AppendDynamicSourceDocumentation(text);

  return text.ToString();
 }

 private static void AppendCurrentValues(
  StringBuilder text,
  PlannerSettings settings,
  StrategyAllocation allocation)
 {
  AppendTitle(text, "AKTUELL VERWENDETE WERTE");

  text.AppendLine($"Haushalt = {settings.HouseholdPersonCount} {(settings.HouseholdPersonCount == 1 ? "Person" : "Personen")}");
  if (settings.HouseholdPersonCount == 2)
   text.AppendLine($"Gemeinsame steuerliche Veranlagung = {(settings.JointTaxation ? "Ja" : "Nein")}");
  text.AppendLine($"Simulationsstartjahr = {settings.PlanningYear}");
  text.AppendLine($"Vorzeitiges Arbeitsende Person 1 = {(settings.Person1WorkEndYear > 0 ? settings.Person1WorkEndYear : settings.PlanningYear)}");
  text.AppendLine($"Heute bereits erworbene Bruttorente Person 1 = {settings.Person1PensionGrossMonthly:N2} € pro Monat");
  text.AppendLine($"Hochgerechnete Bruttorente Person 1 bei Beiträgen bis 67 = {settings.Person1ProjectedPensionGrossMonthlyAt67:N2} € pro Monat");

  if (settings.HouseholdPersonCount == 2)
  {
   text.AppendLine($"Vorzeitiges Arbeitsende Person 2 = {(settings.Person2WorkEndYear > 0 ? settings.Person2WorkEndYear : settings.PlanningYear)}");
   text.AppendLine($"Nettoeinkommen Person 2 pro Monat = {settings.Person2NetIncomeMonthly:N2} €");
   text.AppendLine($"Nettoeinkommen-Steigerung Person 2 = {settings.Person2NetIncomeIncreaseRate:P2}");
   text.AppendLine($"Heute bereits erworbene Bruttorente Person 2 = {settings.Person2PensionGrossMonthly:N2} € pro Monat");
   text.AppendLine($"Hochgerechnete Bruttorente Person 2 bei Beiträgen bis 67 = {settings.Person2ProjectedPensionGrossMonthlyAt67:N2} € pro Monat");
  }

  text.AppendLine($"Startvermögen = {settings.StartCapital:N2} €");
  text.AppendLine($"Monatliche Lebenshaltung = {settings.MonthlyLivingCosts:N2} €");
  text.AppendLine($"Inflation = {settings.InflationRate:P2}");
  text.AppendLine($"Rentensteigerung = {settings.PensionIncreaseRate:P2}");
  text.AppendLine("Rente Person 1: Die zusätzliche Anwartschaft wird aus der Differenz zwischen heute bereits erworbener Rente und Hochrechnung bei Beiträgen bis 67 anteilig nach Beitragsjahren berechnet. Berücksichtigt werden Beitragsjahre bis zum früheren Zeitpunkt aus Arbeitsende, Rentenbeginn oder Alter 67. Danach werden ein möglicher Abschlag bei Rentenbeginn vor 67 und die eingestellte Rentensteigerung berücksichtigt.");
  if (settings.HouseholdPersonCount == 2)
   text.AppendLine("Rente Person 2: Die zusätzliche Anwartschaft wird aus der Differenz zwischen heute bereits erworbener Rente und Hochrechnung bei Beiträgen bis 67 anteilig nach Beitragsjahren berechnet. Berücksichtigt werden Beitragsjahre bis zum früheren Zeitpunkt aus Arbeitsende, Rentenbeginn oder Alter 67. Danach werden ein möglicher Abschlag bei Rentenbeginn vor 67 und die eingestellte Rentensteigerung berücksichtigt.");
  text.AppendLine($"GKV/Pflege Basisjahr = {settings.HealthInsuranceBaseYear}");
  text.AppendLine($"GKV/Pflege Bemessungsgrenzen Änderung p.a. = {settings.HealthInsuranceAssessmentIncreaseRate:P2}");
  text.AppendLine($"GKV-Zusatzbeitrag Änderung p.a. in Prozentpunkten = {settings.HealthInsuranceAdditionalRateAnnualChange:P2}");
  text.AppendLine($"Pflegeversicherung Änderung p.a. in Prozentpunkten = {settings.CareInsuranceRateAnnualChange:P2}");
  text.AppendLine($"Stress: zusätzl. Änderung GKV/Pflege Bemessungsgrenzen p.a. = {settings.StressHealthInsuranceAssessmentAdditionalIncreaseRate:P2}");
  text.AppendLine($"Stress: zusätzl. GKV-Zusatzbeitrag p.a. in Prozentpunkten = {settings.StressHealthInsuranceAdditionalRateAnnualChange:P2}");
  text.AppendLine($"Stress: zusätzl. Pflegebeitrag p.a. in Prozentpunkten = {settings.StressCareInsuranceRateAnnualChange:P2}");
  text.AppendLine($"Basiszins Vorabpauschale = {settings.AdvanceLumpSumBaseRate:P2}");
  text.AppendLine();

  text.AppendLine("Anlageaufteilung:");
  text.AppendLine($"Tages-/Festgeld = {allocation.Cash:P2}");
  text.AppendLine($"Welt-ETF = {allocation.WorldEtf:P2}");
  text.AppendLine($"Dividenden-ETF = {allocation.DividendEtf:P2}");
  text.AppendLine($"Dividenden-Aktien = {allocation.DividendStocks:P2}");
  text.AppendLine();

  text.AppendLine("Renditen/Ausschüttungen:");
  text.AppendLine($"Tages-/Festgeld Zins = {settings.CashInterestRate:P2}");
  text.AppendLine($"Welt-ETF aktueller Stand = {settings.WorldEtfCurrentValue:N2} €");
  text.AppendLine($"Welt-ETF besteht seit = {settings.WorldEtfStartYear}");
  text.AppendLine($"Welt-ETF bisherige Durchschnittsrendite = {settings.WorldEtfHistoricalReturn:P2}");
  text.AppendLine($"Welt-ETF Gesamtrendite = {settings.WorldEtfReturn:P2}");
  text.AppendLine($"Welt-ETF Ausschüttung = {settings.WorldEtfDistribution:P2}");
  text.AppendLine($"Dividenden-ETF aktueller Stand = {settings.DividendEtfCurrentValue:N2} €");
  text.AppendLine($"Dividenden-ETF besteht seit = {settings.DividendEtfStartYear}");
  text.AppendLine($"Dividenden-ETF bisherige Durchschnittsrendite = {settings.DividendEtfHistoricalReturn:P2}");
  text.AppendLine($"Dividenden-ETF Gesamtrendite = {settings.DividendEtfReturn:P2}");
  text.AppendLine($"Dividenden-ETF Ausschüttung = {settings.DividendEtfDistribution:P2}");
  text.AppendLine($"Dividenden-Aktien aktueller Stand = {settings.DividendStocksCurrentValue:N2} €");
  text.AppendLine($"Dividenden-Aktien bestehen seit = {settings.DividendStocksStartYear}");
  text.AppendLine($"Dividenden-Aktien bisherige Durchschnittsrendite = {settings.DividendStocksHistoricalReturn:P2}");
  text.AppendLine($"Dividenden-Aktien Gesamtrendite = {settings.DividendStocksReturn:P2}");
  text.AppendLine($"Dividenden-Aktien Ausschüttung = {settings.DividendStocksDistribution:P2}");
  text.AppendLine();
 }

 private static void AppendCompleteSettingsSnapshot(
  StringBuilder text,
  PlannerSettings settings,
  StrategyAllocation allocation)
 {
  AppendTitle(text, "VOLLSTÄNDIGE EINSTELLUNGEN – AUTOMATISCH");

  var jsonOptions = new JsonSerializerOptions
  {
   WriteIndented = true
  };

  text.AppendLine("PlannerSettings:");
  text.AppendLine(JsonSerializer.Serialize(settings, jsonOptions));
  text.AppendLine();

  text.AppendLine("StrategyAllocation:");
  text.AppendLine(JsonSerializer.Serialize(allocation, jsonOptions));
  text.AppendLine();
 }

 private static void AppendHumanReadableCalculationFlow(StringBuilder text)
 {
  AppendTitle(text, "BERECHNUNGSABLAUF");

  AppendSection(
   text,
   "1. Planungszeitraum",
   "Bei einem Ein-Personen-Haushalt richtet sich das Planungsende ausschließlich nach Person 1. Bei zwei Personen ermittelt das Programm für beide Personen das rechnerische Endjahr aus Startalter und Planungsendalter und verwendet das spätere der beiden Endjahre.",
   "ProjectionService.Calculate");

  AppendSection(
   text,
   "2. Inflation und Lebenshaltung",
   "Die monatlichen Lebenshaltungskosten sind heutige Euro-Beträge. Sie werden auf ein Jahr hochgerechnet und vom aktuellen Kalenderjahr bis zum jeweiligen Simulationsjahr mit der eingetragenen Inflation fortgeschrieben.",
   "ProjectionService.Calculate");

  AppendSection(
   text,
   "3. Freiwillige GKV/Pflege nach Arbeitsende und vor Rentenbeginn",
   "Zinsen, Ausschüttungen und bei Verkäufen realisierte positive Kursgewinne werden als relevante Kapitalerträge zusammengeführt. Bei den im Modell als Aktienfonds behandelten ETFs werden realisierte Gewinne mit dem bereits im Steuerteil verwendeten steuerpflichtigen Anteil von 70 % berücksichtigt. Bei einem Ein-Personen-Haushalt werden sie vollständig Person 1 zugerechnet; bei zwei Personen werden sie 50/50 auf beide Personen verteilt. Für Person 1 beginnt die Projektion mit ihrem Arbeitsende. Für Person 2 wird die freiwillige GKV/Pflege erst ab ihrem eigenen Arbeitsende und nur vor ihrem Rentenbeginn angesetzt; solange Person 2 noch arbeitet, wird dafür kein freiwilliger Beitrag auf Kapitalerträge berechnet. Mindest-Bemessungsgrundlage und Beitragsbemessungsgrenze werden ab dem eingetragenen GKV/Pflege-Basisjahr mit der eingetragenen relativen Jahresänderung fortgeschrieben. Zusatzbeitrag und Pflege-Beitragssatz werden ab dem Basisjahr um die eingetragene jährliche Änderung in Prozentpunkten verändert. Im Stressszenario werden die separat eingetragenen zusätzlichen Änderungen auf diese Annahmen aufgeschlagen. Es werden keine gesetzlichen Zukunftswerte automatisch unterstellt. Pro betroffener Person wird die monatliche Bemessungsgrundlage zwischen den für das jeweilige Jahr fortgeschriebenen Grenzen begrenzt. Darauf werden GKV-Satz, fortgeschriebener Zusatzbeitrag und fortgeschriebene Pflegeversicherung angewendet. Realisierte Verluste werden steuerlich nicht verworfen. Verluste aus Einzelaktien werden getrennt vorgetragen und ausschließlich mit Gewinnen aus Einzelaktien verrechnet. Sonstige Verluste aus den im Modell als Aktienfonds behandelten ETFs werden entsprechend der bestehenden 30-%-Teilfreistellung nur zu 70 % steuerlich berücksichtigt. Die Verlustverrechnung erfolgt vor dem Sparer-Pauschbetrag.",
   "ProjectionService.CalculateVoluntaryHealthAndCareAnnual / PensionService.CalculateHealthInsuranceProjectionParameters");

  AppendSection(
   text,
   "4. Rente und vereinfachte Rentensteuer",
   "Vor dem jeweiligen Rentenalter wird keine gesetzliche Rente angesetzt. Ausgangspunkt sind je Person die heute bereits erworbene Monatsrente und die Hochrechnung bei weiteren Beiträgen bis 67, jeweils ohne künftige Rentenanpassungen. Die Differenz wird gleichmäßig auf die verbleibenden Beitragsjahre bis 67 verteilt. Berücksichtigt werden nur Beitragsjahre bis zum früheren Zeitpunkt aus Arbeitsende, Rentenbeginn oder Alter 67. Beginnt die Rente vor 67, wird die so ermittelte Monatsrente um 0,3 % je vorgezogenem Monat, höchstens um 14,4 %, dauerhaft vermindert. Anspruchsvoraussetzungen oder abschlagsfreie Sonderregelungen werden nicht automatisch geprüft. Die eingestellte Rentensteigerung wird ab dem aktuellen Kalenderjahr bis zum jeweiligen Simulationsjahr auf die ermittelte Rente angewendet. Bei aktivierter KVdR werden der im PensionService hinterlegte Krankenversicherungsanteil sowie der für das jeweilige Jahr fortgeschriebene Zusatzbeitrag und Pflege-Beitragssatz von der gesetzlichen Rente abgezogen. Bei deaktivierter KVdR wird freiwillige GKV/Pflege angenommen: Zusätzlich zur Belastung auf die gesetzliche Rente werden die im Modell erfassten sonstigen beitragspflichtigen Einnahmen bis zur Beitragsbemessungsgrenze berücksichtigt; bei niedrigen Gesamteinnahmen greift die Mindest-Bemessungsgrundlage. Der auf die gesetzliche Rente entfallende Krankenversicherungsanteil bleibt wegen des modellierten Rentenversicherungszuschusses auf derselben Nettobelastung wie bei KVdR; zusätzliche freiwillige Beiträge werden als GKV/Pflege-Bedarf der jeweiligen Person ausgewiesen. Die Fortschreibung verwendet dieselben GKV/Pflege-Annahmen wie die freiwillige Versicherung und berücksichtigt im Stressszenario die dort eingetragenen zusätzlichen Änderungen. Zusätzlich wird eine vereinfachte Einkommensteuer auf die Renteneinkünfte berechnet. Bei einem Ein-Personen-Haushalt wird Person 2 vollständig ignoriert und der Einkommensteuertarif ohne Splitting verwendet. Bei zwei Personen steuert die Einstellung zur gemeinsamen steuerlichen Veranlagung die Berechnung: Bei aktivierter gemeinsamer Veranlagung wird der Splittingtarif verwendet, andernfalls werden die tariflichen Steuern beider Personen getrennt berechnet. Der steuerpflichtige Rentenanteil richtet sich nach dem jeweiligen Rentenbeginn; der daraus ermittelte steuerfreie Rentenbetrag wird für die weitere Simulation festgehalten. Pro rentenbeziehender Person wird der im PensionService hinterlegte Werbungskosten-Pauschbetrag berücksichtigt; die dort berechneten Kranken- und Pflegebeiträge mindern ebenfalls die für diese Modellrechnung angesetzten steuerpflichtigen Renteneinkünfte. Die tarifliche Einkommensteuer einschließlich aktivierter Kirchensteuer und Solidaritätszuschlag verwendet den im PensionService hinterlegten Einkommensteuertarif 2026 als gesetzliche Basis. Für spätere Simulationsjahre wird keine zukünftige Steuerformel erfunden: Stattdessen wird der 2026er Tarif modellintern mit der eingetragenen Inflation fortgeschrieben. Technisch wird das nominale zu versteuernde Einkommen zunächst auf 2026-Euro zurückgerechnet, mit dem 2026er Tarif besteuert und der Steuerbetrag anschließend mit demselben Inflationsfaktor wieder auf das Simulationsjahr hochgerechnet. Damit wird im Modell eine vollständige Inflationsanpassung des Tarifs unterstellt; tatsächliche zukünftige Gesetzesänderungen können davon abweichen. Das Nettoeinkommen von Person 2 aus laufender Beschäftigungsphase wird bewusst nicht in die Rentensteuer zurückgerechnet; die Steuer während einer parallelen Beschäftigungsphase von Person 2 ist deshalb eine Näherung.",
   "PensionService.CalculateAnnualPension / PensionService.CalculateJointPensionIncomeTax / PensionService.CalculateProjectedIncomeTaxIncludingSurcharges");

  AppendSection(
   text,
   "5. Reserve und Rücklagen",
   "Der Reserve-Sollwert setzt sich aus der gewünschten sicheren Reserve in Jahresausgaben sowie den Haus-, Auto-, Gesundheits-, Reise- und sonstigen Rücklagen zusammen. Lebenshaltung, heutiger Hauswert, heutiger Auto-Ersatzwert sowie die heutigen Zielbeträge für Gesundheit/Zahnersatz, Reisen/größere Wünsche und Sonstiges/Unvorhergesehenes werden vom aktuellen Kalenderjahr bis zum jeweiligen Simulationsjahr mit der eingetragenen Inflation fortgeschrieben.",
   "ProjectionService.Calculate");

  AppendSection(
   text,
   "6. Anlageerträge und Ausschüttungen",
   "Das Startvermögen und die unter „Bestehende Depots“ eingetragenen Depotwerte gelten zum gewählten Simulationsstartjahr. Diese bestehenden Bestände werden unverändert als Anfangsbestände übernommen. Nur der nach Abzug von Welt-ETF, Dividenden-ETF und Dividenden-Aktien verbleibende Teil des Startvermögens wird gemäß der eingestellten Strategie auf Tages-/Festgeld, Welt-ETF, Dividenden-ETF und Dividenden-Aktien verteilt. Für neu zugeteilte Depotbeträge entspricht der steuerliche Einstandswert dem neu angelegten Betrag; für bestehende Depotbestände wird der Einstandswert aus Depotwert am Simulationsstart, Startjahr und bisheriger Durchschnittsrendite bis zum Simulationsstart geschätzt. Die Erträge werden aus den so gebildeten Beständen und den hinterlegten Sätzen berechnet. Ausschüttungen werden getrennt vom im Bestand verbleibenden Kurs-/Gesamtertrag behandelt.",
   "ProjectionService.Calculate");

  AppendSection(
   text,
   "7. Stressszenario",
   "Im Stresslauf ersetzt der eingestellte Crash-Wert im jeweiligen Crash-Jahr die normale Gesamtrendite der Aktien-/ETF-Anteile. Ein eingestellter Crash von -25 % bedeutet damit für dieses Jahr eine Gesamtrendite von -25 % und wird nicht mit der normalen Jahresrendite verrechnet. Optional kann ein zweiter Crash in einem späteren Jahr berücksichtigt werden.",
   "ProjectionService.Calculate");

  AppendSection(
   text,
   "8. Kapitalertragsteuer",
   "Zinsen, Dividenden von Einzelaktien, der steuerpflichtige Anteil von ETF-Ausschüttungen, Vorabpauschalen und geschätzte realisierte Veräußerungsgewinne werden zusammengeführt. Die Vorabpauschale wird nach § 18 InvStG aus dem ETF-Wert zu Jahresbeginn, 70 % des eingetragenen Basiszinses, der Wertentwicklung und den Ausschüttungen begrenzt berechnet. Sie wird im Folgejahr als Kapitalertrag berücksichtigt. Ab Simulationsbeginn angesetzte Vorabpauschalen werden intern fortgeschrieben und bei späteren ETF-Verkäufen anteilig vom geschätzten Veräußerungsgewinn abgezogen. Für die Schätzung des bereits im Depot enthaltenen Kursgewinns werden aktueller Depotstand, Startjahr und bisherige Durchschnittsrendite verwendet. Bei späteren proportionalen Verkäufen wird der geschätzte Einstandswert anteilig fortgeschrieben. ETF-Veräußerungsgewinne werden wie die ETF-Ausschüttungen mit dem im TaxService hinterlegten steuerpflichtigen Anteil berücksichtigt. Danach wird der eingetragene Sparer-Pauschbetrag abgezogen. Die Haushaltsauswahl setzt dafür beim Wechsel automatisch 1.000 € für eine Person bzw. 2.000 € für zwei Personen; der anschließend manuell eingetragene Haushaltswert bleibt editierbar. Bei einem Zwei-Personen-Haushalt wird nach dem modellierten Lebensende der ersten Person ab dem folgenden Simulationsjahr nur noch die Hälfte dieses Haushaltswerts verwendet. Die Einstellung zur gemeinsamen steuerlichen Veranlagung steuert separat die tarifliche Renten-Einkommensteuer: Bei Ja wird solange beide Personen im Modell leben der Splittingtarif verwendet, bei Nein werden die tariflichen Steuern beider Personen getrennt berechnet. Die individuelle Eigentumszuordnung von Kapitalerträgen wird nicht separat modelliert. Zusätzlich wird für jedes Simulationsjahr automatisch eine Günstigerprüfung nach § 32d Abs. 6 EStG durchgeführt: Die Belastung aus dem gesonderten Kapitalertragsteuertarif wird mit der zusätzlichen tariflichen Einkommensteuer einschließlich Zuschlagsteuern verglichen; verwendet wird nur die niedrigere Belastung. Bei getrennter Veranlagung von zwei lebenden Personen werden die steuerpflichtigen Kapitaleinkünfte mangels individueller Depotzuordnung im bestehenden Modell hälftig zugeordnet. Ohne Kirchensteuer werden auf den Rest 25 % Kapitalertragsteuer und 5,5 % Solidaritätszuschlag auf die Kapitalertragsteuer berechnet. Ist Kirchensteuer aktiviert und ein Satz größer 0 % eingetragen, wird die Einkommensteuer auf Kapitalerträge nach § 32d Abs. 1 EStG mit dem eingetragenen Kirchensteuersatz berechnet; anschließend werden Kirchensteuer und Solidaritätszuschlag hinzugerechnet. Die Einstandswert-Ermittlung ist eine Näherung aus den eingegebenen Durchschnittswerten und keine depotgenaue FIFO-Steuerabrechnung.",
   "TaxService.CalculateCapitalTax / TaxService.CalculateCapitalTaxWithFavorableCheck / ProjectionService.EstimateInitialCostBasis / ProjectionService.SellRiskyAssets");

  AppendSection(
   text,
   "9. Einmalige Einnahmen, Ausgaben und Hausverkauf",
   "Einmalige Zahlungsströme werden im jeweiligen Jahr berücksichtigt und vom Planungsjahr bis dahin mit der Inflation fortgeschrieben. Ein aktivierter Hausverkauf wird im Verkaufsjahr als Nettozufluss berücksichtigt.",
   "ProjectionService.Calculate / ProjectionService.SumCashFlows");

  AppendSection(
   text,
   "10. Finanzierung des Jahresbedarfs",
   "Der Jahresbedarf wird zuerst durch Nettorente gedeckt. Solange Person 2 vor ihrem eigenen Arbeitsende noch arbeitet, wird anschließend ihr aktuelles monatliches Nettoeinkommen mit der eingetragenen jährlichen Steigerung berücksichtigt. Danach folgen einmalige Einnahmen und Ausschüttungen. Reicht das nicht, wird Tages-/Festgeld verwendet. Danach wird proportional aus den drei Aktien-/ETF-Bausteinen entnommen. Ein Rest wird als Finanzierungslücke ausgewiesen. Nicht benötigtes Nettoeinkommen von Person 2 wird dem sicheren Geldanteil zugeschlagen.",
   "ProjectionService.Calculate");

  AppendSection(
   text,
   "11. Reserve wieder auffüllen",
   "Ist die automatische Auffüllung aktiv und Tages-/Festgeld liegt unter dem Soll, wird Geld proportional aus den Aktien-/ETF-Bausteinen in den sicheren Anteil verschoben. Bei aktivierter Schutzregel wird das in negativen Aktienjahren nicht erzwungen.",
   "ProjectionService.Calculate");

  AppendSection(
   text,
   "12. Ampelstatus",
   "Rot bedeutet, dass das Portfolio praktisch aufgebraucht ist. Gelb bedeutet, dass die sichere Reserve unter dem Soll liegt. Grün bedeutet, dass weder Rot noch Gelb zutrifft. Der Gesamtstatus ist grün, wenn das Vermögen bis zum Planungsende reicht.",
   "ProjectionService.Calculate / ProjectionResult.OverallStatus");

  AppendSection(
   text,
   "13. Mindest-Startvermögen",
   "Das Mindest-Startvermögen wird iterativ angenähert. Da das Startvermögen die bestehenden Depotbestände enthält, kann der geprüfte Gesamtwert nicht unter deren Summe fallen. Zuerst wird eine ausreichend hohe Obergrenze gesucht, danach wird der Bereich wiederholt halbiert, bis die kleinste tragfähige Größenordnung gefunden ist.",
   "ProjectionService.EstimateMinimumStartCapital");

  AppendSection(
   text,
   "14. Strategieempfehlung",
   "Die Default-Strategien werden anhand der mit den aktuellen Renditeannahmen gewichteten erwarteten Portfoliorendite absteigend geprüft. Zurückgegeben wird die renditestärkste Strategie, die Basis- und Stresslauf besteht. Besteht keine Strategie beide Prüfungen, wird Sicherheit als konservativer Fallback verwendet.",
   "StrategyService.Recommend");

  AppendSection(
   text,
   "15. Handlungsempfehlung",
   "Die Handlungsempfehlung wertet Basis- und Stressstatus, Reservebedarf, zusätzlich benötigtes Startvermögen und einen hohen Anteil einzelner Dividenden-Aktien aus.",
   "RecommendationService.Build");
 }

 private static void AppendDynamicSourceDocumentation(StringBuilder text)
 {
  AppendTitle(text, "BERECHNUNGSQUELLEN – VOLLSTÄNDIG AUTOMATISCH");

  string? projectRoot = FindProjectRoot();

  if (projectRoot == null)
  {
   text.AppendLine("Projektordner mit RockefellerFiction.csproj wurde nicht gefunden.");
   return;
  }

  foreach (string fileName in CalculationSourceFiles)
  {
   text.AppendLine();
   text.AppendLine(new string('=', 90));
   text.AppendLine(fileName);
   text.AppendLine(new string('=', 90));

   string path = Path.Combine(projectRoot, fileName);

   if (!File.Exists(path))
   {
    text.AppendLine("Datei nicht gefunden.");
    continue;
   }

   text.AppendLine(File.ReadAllText(path));
  }
 }

 private static string? FindProjectRoot()
 {
  DirectoryInfo? directory =
   new DirectoryInfo(Directory.GetCurrentDirectory());

  for (int i = 0; i < 8 && directory != null; i++, directory = directory.Parent)
  {
   if (File.Exists(Path.Combine(directory.FullName, "RockefellerFiction.csproj")))
    return directory.FullName;
  }

  directory = new DirectoryInfo(AppContext.BaseDirectory);

  for (int i = 0; i < 8 && directory != null; i++, directory = directory.Parent)
  {
   if (File.Exists(Path.Combine(directory.FullName, "RockefellerFiction.csproj")))
    return directory.FullName;
  }

  return null;
 }

 private static void AppendTitle(StringBuilder text, string title)
 {
  text.AppendLine(title);
  text.AppendLine(new string('=', title.Length));
  text.AppendLine();
 }

 private static void AppendSection(
  StringBuilder text,
  string title,
  string explanation,
  string source)
 {
  text.AppendLine(title);
  text.AppendLine(new string('-', title.Length));
  text.AppendLine(explanation);
  text.AppendLine();
  text.AppendLine("Quelle: " + source);
  text.AppendLine();
 }
}
