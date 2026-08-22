using System.IO;
using System.Text;

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
  text.AppendLine("Die exakten Rechenzeilen werden direkt aus den aktuell im Projekt vorhandenen C#-Quelldateien gelesen.");
  text.AppendLine("Ändert sich eine Formel im Source, erscheint nach dem nächsten Öffnen dieser Lasche automatisch die geänderte Formel.");
  text.AppendLine();

  AppendCurrentValues(text, settings, allocation);
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

  text.AppendLine($"Planungsjahr = {settings.PlanningYear}");
  text.AppendLine($"Startvermögen = {settings.StartCapital:N2} €");
  text.AppendLine($"Monatliche Lebenshaltung = {settings.MonthlyLivingCosts:N2} €");
  text.AppendLine($"Inflation = {settings.InflationRate:P2}");
  text.AppendLine($"Rentensteigerung = {settings.PensionIncreaseRate:P2}");
  text.AppendLine();

  text.AppendLine("Anlageaufteilung:");
  text.AppendLine($"Tages-/Festgeld = {allocation.Cash:P2}");
  text.AppendLine($"Welt-ETF = {allocation.WorldEtf:P2}");
  text.AppendLine($"Dividenden-ETF = {allocation.DividendEtf:P2}");
  text.AppendLine($"Dividenden-Aktien = {allocation.DividendStocks:P2}");
  text.AppendLine();

  text.AppendLine("Renditen/Ausschüttungen:");
  text.AppendLine($"Tages-/Festgeld Zins = {settings.CashInterestRate:P2}");
  text.AppendLine($"Welt-ETF Gesamtrendite = {settings.WorldEtfReturn:P2}");
  text.AppendLine($"Welt-ETF Ausschüttung = {settings.WorldEtfDistribution:P2}");
  text.AppendLine($"Dividenden-ETF Gesamtrendite = {settings.DividendEtfReturn:P2}");
  text.AppendLine($"Dividenden-ETF Ausschüttung = {settings.DividendEtfDistribution:P2}");
  text.AppendLine($"Dividenden-Aktien Gesamtrendite = {settings.DividendStocksReturn:P2}");
  text.AppendLine($"Dividenden-Aktien Ausschüttung = {settings.DividendStocksDistribution:P2}");
  text.AppendLine();
 }

 private static void AppendHumanReadableCalculationFlow(StringBuilder text)
 {
  AppendTitle(text, "BERECHNUNGSABLAUF");

  AppendSection(
   text,
   "1. Planungszeitraum",
   "Das Programm ermittelt für beide Personen das rechnerische Endjahr aus Startalter und Planungsendalter. Verwendet wird das spätere der beiden Endjahre.",
   "ProjectionService.Calculate");

  AppendSection(
   text,
   "2. Inflation und Lebenshaltung",
   "Die monatlichen Lebenshaltungskosten werden auf ein Jahr hochgerechnet und anschließend für jedes Planungsjahr mit dem Inflationsfaktor fortgeschrieben.",
   "ProjectionService.Calculate");

  AppendSection(
   text,
   "3. Freiwillige GKV/Pflege vor Rentenbeginn",
   "Zinsen und Ausschüttungen werden als relevante Kapitalerträge zusammengeführt und 50/50 auf beide Personen verteilt. Pro Person wird die monatliche Bemessungsgrundlage zwischen Mindest-Bemessungsgrundlage und Beitragsbemessungsgrenze begrenzt. Darauf werden GKV-Satz, Zusatzbeitrag und Pflegeversicherung angewendet.",
   "ProjectionService.CalculateVoluntaryHealthAndCareAnnual");

  AppendSection(
   text,
   "4. Rente",
   "Vor dem jeweiligen Rentenalter wird keine gesetzliche Rente angesetzt. Ab Rentenbeginn wird die hinterlegte Monatsrente auf zwölf Monate hochgerechnet und mit der Rentensteigerung fortgeschrieben. Danach werden die im PensionService verwendeten Kranken-, Zusatz- und Pflegeanteile abgezogen.",
   "PensionService.CalculateAnnualPension");

  AppendSection(
   text,
   "5. Reserve und Rücklagen",
   "Der Reserve-Sollwert setzt sich aus der gewünschten sicheren Reserve in Jahresausgaben sowie den Haus-, Auto-, Gesundheits-, Reise- und sonstigen Rücklagen zusammen.",
   "ProjectionService.Calculate");

  AppendSection(
   text,
   "6. Anlageerträge und Ausschüttungen",
   "Für Tages-/Festgeld, Welt-ETF, Dividenden-ETF und Dividenden-Aktien werden die Erträge aus dem jeweiligen Bestand und den hinterlegten Sätzen berechnet. Ausschüttungen werden getrennt vom im Bestand verbleibenden Kurs-/Gesamtertrag behandelt.",
   "ProjectionService.Calculate");

  AppendSection(
   text,
   "7. Stressszenario",
   "Im Stresslauf werden die eingestellten Crash-Werte in den vorgesehenen Jahren auf die Renditen der Aktien-/ETF-Anteile angewendet. Optional kann ein zweiter Crash in einem späteren Jahr berücksichtigt werden.",
   "ProjectionService.Calculate");

  AppendSection(
   text,
   "8. Kapitalertragsteuer",
   "Zinsen, Dividenden von Einzelaktien, der steuerpflichtige Anteil von ETF-Ausschüttungen und realisierte Gewinne werden zusammengeführt. Danach wird der Sparer-Pauschbetrag abgezogen. Auf den Rest werden Kapitalertragsteuer und Solidaritätszuschlag berechnet.",
   "TaxService.CalculateCapitalTax");

  AppendSection(
   text,
   "9. Einmalige Einnahmen, Ausgaben und Hausverkauf",
   "Einmalige Zahlungsströme werden im jeweiligen Jahr berücksichtigt und vom Planungsjahr bis dahin mit der Inflation fortgeschrieben. Ein aktivierter Hausverkauf wird im Verkaufsjahr als Nettozufluss berücksichtigt.",
   "ProjectionService.Calculate / ProjectionService.SumCashFlows");

  AppendSection(
   text,
   "10. Finanzierung des Jahresbedarfs",
   "Der Jahresbedarf wird zuerst durch Nettorente und einmalige Einnahmen gedeckt, danach durch Ausschüttungen. Reicht das nicht, wird Tages-/Festgeld verwendet. Danach wird proportional aus den drei Aktien-/ETF-Bausteinen entnommen. Ein Rest wird als Finanzierungslücke ausgewiesen.",
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
   "Das Mindest-Startvermögen wird iterativ angenähert: zuerst wird eine ausreichend hohe Obergrenze gesucht, danach wird der Bereich wiederholt halbiert, bis die kleinste tragfähige Größenordnung gefunden ist.",
   "ProjectionService.EstimateMinimumStartCapital");

  AppendSection(
   text,
   "14. Strategieempfehlung",
   "Die Strategien werden in der Reihenfolge Wachstum, Ausgewogen und Sicherheit getestet. Zurückgegeben wird die erste Strategie, die Basis- und Stresslauf besteht. Besteht keine alle Prüfungen, wird Sicherheit verwendet.",
   "StrategyService.Recommend");

  AppendSection(
   text,
   "15. Handlungsempfehlung",
   "Die Handlungsempfehlung wertet Basis- und Stressstatus, Reservebedarf, zusätzlich benötigtes Startvermögen und einen hohen Anteil einzelner Dividenden-Aktien aus.",
   "RecommendationService.Build");
 }

 private static void AppendDynamicSourceDocumentation(StringBuilder text)
 {
  AppendTitle(text, "FORMELN DIREKT AUS DEM SOURCE");

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

   string[] lines = File.ReadAllLines(path);
   bool anyFormula = false;

   foreach (string rawLine in lines)
   {
    string line = rawLine.Trim();

    if (!IsCalculationLine(line))
     continue;

    text.AppendLine(line);
    anyFormula = true;
   }

   if (!anyFormula)
    text.AppendLine("Keine Rechenzeilen erkannt.");
  }
 }

 private static bool IsCalculationLine(string line)
 {
  if (string.IsNullOrWhiteSpace(line))
   return false;

  if (line.StartsWith("//", StringComparison.Ordinal))
   return false;

  if (line.StartsWith("private const decimal ", StringComparison.Ordinal))
   return true;

  if (line.StartsWith("decimal ", StringComparison.Ordinal))
   return true;

  if (line.StartsWith("bool ", StringComparison.Ordinal))
   return true;

  if (line.StartsWith("int ", StringComparison.Ordinal) &&
      line.Contains("=", StringComparison.Ordinal))
   return true;

  if (line.StartsWith("if ", StringComparison.Ordinal) ||
      line.StartsWith("else if ", StringComparison.Ordinal))
   return true;

  if (line.StartsWith("return ", StringComparison.Ordinal))
   return true;

  if (line.Contains("Math.", StringComparison.Ordinal))
   return true;

  if (line.Contains("Pow(", StringComparison.Ordinal))
   return true;

  if (line.Contains("+=", StringComparison.Ordinal) ||
      line.Contains("-=", StringComparison.Ordinal) ||
      line.Contains("*=", StringComparison.Ordinal) ||
      line.Contains("/=", StringComparison.Ordinal))
   return true;

  return false;
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
