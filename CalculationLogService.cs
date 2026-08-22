using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace RockefellerFiction;

public static class CalculationLogService
{
 private static readonly CultureInfo GermanCulture =
  CultureInfo.GetCultureInfo("de-DE");

 public static string Write(
  PlannerSettings settings,
  StrategyAllocation allocation,
  ProjectionResult baseResult,
  ProjectionResult stressResult)
 {
  string path = Path.Combine(
   AppContext.BaseDirectory,
   "RockefellerFiction.log.br");

  var text = new StringBuilder();

  AppendHeader(text);
  AppendSettings(text, settings, allocation);
  AppendProjection(text, "BASIS", settings, baseResult);
  AppendProjection(text, "STRESS", settings, stressResult);

  using FileStream fileStream = File.Create(path);
  using var brotliStream = new BrotliStream(
   fileStream,
   CompressionLevel.Optimal);
  using var writer = new StreamWriter(
   brotliStream,
   new UTF8Encoding(true));

  writer.Write(text.ToString());
  return path;
 }

 public static string Read()
 {
  string path = Path.Combine(
   AppContext.BaseDirectory,
   "RockefellerFiction.log.br");

  if (!File.Exists(path))
   return "";

  using FileStream fileStream = File.OpenRead(path);
  using var brotliStream = new BrotliStream(
   fileStream,
   CompressionMode.Decompress);
  using var reader = new StreamReader(
   brotliStream,
   Encoding.UTF8,
   true);

  return reader.ReadToEnd();
 }

 private static void AppendHeader(StringBuilder text)
 {
  text.AppendLine("RockefellerFiction - Berechnungsprotokoll");
  text.AppendLine("========================================");
  text.AppendLine($"Erstellt: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
  text.AppendLine("Datei: RockefellerFiction.log.br");
  text.AppendLine("Hinweis: Dieses Protokoll dokumentiert die vom Programm verwendeten Eingaben,");
  text.AppendLine("Zwischenergebnisse und Resultate. Es ist kein Finanz-, Steuer- oder Rentenbescheid.");
  text.AppendLine();
 }

 private static void AppendSettings(
  StringBuilder text,
  PlannerSettings s,
  StrategyAllocation allocation)
 {
  AppendTitle(text, "EINGABEN");

  AppendValue(text, "Haushalt", $"{Math.Max(1, s.HouseholdPersonCount)} Person(en)");
  AppendValue(text, "Planungsjahr / Arbeitsende Person 1", s.PlanningYear);
  AppendValue(text, "Aktuelles Alter Person 1", s.Person1Age);
  AppendValue(text, "Rentenbeginn Person 1", s.Person1RetirementAge);
  AppendValue(text, "Lebenserwartung Person 1", s.Person1EndAge);

  if (s.HouseholdPersonCount == 2)
  {
   AppendValue(text, "Aktuelles Alter Person 2", s.Person2Age);
   AppendValue(text, "Arbeitsende Person 2", s.Person2WorkEndYear);
   AppendMoney(text, "Nettoeinkommen Person 2 pro Monat", s.Person2NetIncomeMonthly);
   AppendPercent(text, "Nettoeinkommen-Steigerung Person 2 p.a.", s.Person2NetIncomeIncreaseRate);
   AppendValue(text, "Rentenbeginn Person 2", s.Person2RetirementAge);
   AppendValue(text, "Lebenserwartung Person 2", s.Person2EndAge);
  }

  AppendMoney(text, "Startvermögen", s.StartCapital);
  AppendMoney(text, "Lebenshaltung pro Monat", s.MonthlyLivingCosts);
  AppendPercent(text, "Inflation p.a.", s.InflationRate);
  AppendValue(text, "Einkommensteuertarif Basisjahr", 2026);
  AppendValue(text, "Einkommensteuertarif Fortschreibung", "mit Inflation p.a.");
  AppendPercent(text, "Rentensteigerung p.a.", s.PensionIncreaseRate);

  AppendMoney(text, "Rente Person 1 brutto pro Monat", s.Person1PensionGrossMonthly);
  AppendValue(text, "KVdR Person 1", s.KvdrPerson1 ? "Ja" : "Nein");
  if (s.HouseholdPersonCount == 2)
  {
   AppendMoney(text, "Rente Person 2 brutto pro Monat", s.Person2PensionGrossMonthly);
   AppendValue(text, "KVdR Person 2", s.KvdrPerson2 ? "Ja" : "Nein");
  }

  AppendMoney(text, "GKV/Pflege Mindest-Bemessungsgrundlage pro Monat", s.VoluntaryHealthInsuranceMinimumMonthlyIncome);
  AppendMoney(text, "GKV/Pflege Beitragsbemessungsgrenze pro Monat", s.VoluntaryHealthInsuranceMaximumMonthlyIncome);
  AppendPercent(text, "GKV-Satz", s.VoluntaryHealthInsuranceRate);
  AppendPercent(text, "GKV-Zusatzbeitrag", s.VoluntaryHealthInsuranceAdditionalRate);
  AppendPercent(text, "Pflegeversicherung", s.CareInsuranceChildlessRate);
  AppendValue(text, "GKV/Pflege Basisjahr", s.HealthInsuranceBaseYear);
  AppendPercent(text, "GKV/Pflege Bemessungsgrenzen Änderung p.a.", s.HealthInsuranceAssessmentIncreaseRate);
  AppendPercent(text, "GKV-Zusatzbeitrag Änderung p.a. in Prozentpunkten", s.HealthInsuranceAdditionalRateAnnualChange);
  AppendPercent(text, "Pflegeversicherung Änderung p.a. in Prozentpunkten", s.CareInsuranceRateAnnualChange);
  AppendPercent(text, "Stress: zusätzl. Änderung GKV/Pflege Bemessungsgrenzen p.a.", s.StressHealthInsuranceAssessmentAdditionalIncreaseRate);
  AppendPercent(text, "Stress: zusätzl. GKV-Zusatzbeitrag p.a. in Prozentpunkten", s.StressHealthInsuranceAdditionalRateAnnualChange);
  AppendPercent(text, "Stress: zusätzl. Pflegebeitrag p.a. in Prozentpunkten", s.StressCareInsuranceRateAnnualChange);

  AppendValue(text, "Reserve in Jahresausgaben", s.ReserveYears.ToString("N2", GermanCulture));
  AppendValue(text, "Reserve automatisch auffüllen", s.AutoRefillReserve ? "Ja" : "Nein");
  AppendValue(text, "Reserve in negativen Aktienjahren schützen", s.UseReserveOnNegativeStockYear ? "Ja" : "Nein");

  AppendPercent(text, "Tages-/Festgeld Zins", s.CashInterestRate);
  AppendPercent(text, "Welt-ETF Gesamtrendite", s.WorldEtfReturn);
  AppendPercent(text, "Welt-ETF Ausschüttung", s.WorldEtfDistribution);
  AppendMoney(text, "Welt-ETF aktueller Stand", s.WorldEtfCurrentValue);
  AppendValue(text, "Welt-ETF besteht seit", s.WorldEtfStartYear);
  AppendPercent(text, "Welt-ETF historische Durchschnittsrendite", s.WorldEtfHistoricalReturn);

  AppendPercent(text, "Dividenden-ETF Gesamtrendite", s.DividendEtfReturn);
  AppendPercent(text, "Dividenden-ETF Ausschüttung", s.DividendEtfDistribution);
  AppendMoney(text, "Dividenden-ETF aktueller Stand", s.DividendEtfCurrentValue);
  AppendValue(text, "Dividenden-ETF besteht seit", s.DividendEtfStartYear);
  AppendPercent(text, "Dividenden-ETF historische Durchschnittsrendite", s.DividendEtfHistoricalReturn);

  AppendPercent(text, "Dividenden-Aktien Gesamtrendite", s.DividendStocksReturn);
  AppendPercent(text, "Dividenden-Aktien Ausschüttung", s.DividendStocksDistribution);
  AppendMoney(text, "Dividenden-Aktien aktueller Stand", s.DividendStocksCurrentValue);
  AppendValue(text, "Dividenden-Aktien bestehen seit", s.DividendStocksStartYear);
  AppendPercent(text, "Dividenden-Aktien historische Durchschnittsrendite", s.DividendStocksHistoricalReturn);
  AppendValue(text, "Überschüssige Ausschüttungen reinvestieren", s.DividendSurplusReinvest ? "Ja" : "Nein");

  AppendMoney(text, "Sparer-Pauschbetrag Haushalt", s.CapitalGainsAllowance);
  if (s.HouseholdPersonCount == 2)
   AppendValue(text, "Gemeinsame steuerliche Veranlagung", s.JointTaxation ? "Ja" : "Nein");
  AppendValue(text, "Kirchensteuer aktiv", s.ChurchTaxEnabled ? "Ja" : "Nein");
  AppendPercent(text, "Kirchensteuersatz", s.ChurchTaxRate);
  AppendPercent(text, "Basiszins Vorabpauschale", s.AdvanceLumpSumBaseRate);

  AppendValue(text, "Strategie", s.Strategy);
  AppendPercent(text, "Aufteilung Tages-/Festgeld", allocation.Cash);
  AppendPercent(text, "Aufteilung Welt-ETF", allocation.WorldEtf);
  AppendPercent(text, "Aufteilung Dividenden-ETF", allocation.DividendEtf);
  AppendPercent(text, "Aufteilung Dividenden-Aktien", allocation.DividendStocks);

  AppendValue(text, "Crash am Anfang", s.StressCrashAtStart ? "Ja" : "Nein");
  AppendPercent(text, "Crash-Stärke am Anfang", s.StressCrashPercent);
  AppendValue(text, "Zweiter Crash", s.StressSecondCrashEnabled ? "Ja" : "Nein");
  AppendValue(text, "Jahr zweiter Crash", s.StressSecondCrashYear);
  AppendPercent(text, "Stärke zweiter Crash", s.StressSecondCrashPercent);

  AppendValue(text, "Hausverkauf berücksichtigt", s.HouseIncluded ? "Ja" : "Nein");
  AppendValue(text, "Haus-Verkaufsjahr", s.HouseSaleYear);
  AppendMoney(text, "Nettoerlös Hausverkauf", s.HouseNetSaleProceeds);

  AppendMoney(text, "Hauswert", s.HouseTotalValue);
  AppendPercent(text, "Gebäudeanteil Haus", s.HouseBuildingShare);
  AppendPercent(text, "Haus-Instandhaltungsrate", s.HouseReserveRate);
  AppendMoney(text, "Auto-Ersatzwert", s.CarReplacementValue);
  AppendValue(text, "Auto-Ersatz nach Jahren", s.CarReplacementYears);
  AppendMoney(text, "Gesundheitsrücklage", s.HealthReserveTarget);
  AppendMoney(text, "Reiserücklage", s.TravelReserveTarget);
  AppendMoney(text, "Sonstige Rücklage", s.OtherReserveTarget);

  AppendCashFlows(text, "Einmalige Einnahmen", s.OneTimeIncome);
  AppendCashFlows(text, "Einmalige Ausgaben", s.OneTimeExpenses);
  text.AppendLine();
 }

 private static void AppendProjection(
  StringBuilder text,
  string title,
  PlannerSettings settings,
  ProjectionResult result)
 {
  AppendTitle(text, $"{title}-PROJEKTION");

  AppendValue(text, "Gesamtstatus", result.OverallStatus);
  AppendValue(text, "Planende erreicht", result.ReachesPlanEnd ? "Ja" : "Nein");
  AppendValue(text, "Erschöpfungsjahr", result.DepletionYear?.ToString(GermanCulture) ?? "-");
  AppendMoney(text, "Endvermögen", result.FinalCapital);
  AppendMoney(text, "Mindest-Startvermögen", result.MinimumRequiredStartCapital);
  AppendMoney(text, "Zusätzlich erforderliches Startvermögen", result.RequiredAdditionalStartCapital);
  AppendMoney(text, "Anfängliches Reserve-Soll", result.InitialRequiredCash);
  text.AppendLine();

  foreach (YearResult y in result.Years)
   AppendYear(text, settings, y);

  text.AppendLine();
 }

 private static void AppendYear(
  StringBuilder text,
  PlannerSettings settings,
  YearResult y)
 {
  text.AppendLine($"--- Jahr {y.Year} | Alter P1 {y.Age1}" +
   (settings.HouseholdPersonCount == 2 ? $" | Alter P2 {y.Age2}" : "") +
   $" | Status {y.YearStatus} ---");

  text.AppendLine("[Bedarf]");
  AppendMoney(text, "Lebenshaltung p.a.", y.LivingCosts);
  AppendMoney(text, "Inflationsanstieg ggü. Vorjahr", y.LivingCostIncrease);
  AppendMoney(text, "GKV/Pflege Person 1 p.a.", y.HealthCareCostsPerson1);
  if (settings.HouseholdPersonCount == 2)
   AppendMoney(text, "GKV/Pflege Person 2 p.a.", y.HealthCareCostsPerson2);
  AppendMoney(text, "GKV/Pflege gesamt p.a.", y.HealthCareCosts);
  AppendMoney(text, "GKV/Pflege Mindest-Bemessungsgrundlage angewendet mtl.", y.HealthInsuranceMinimumMonthlyIncomeApplied);
  AppendMoney(text, "GKV/Pflege Beitragsbemessungsgrenze angewendet mtl.", y.HealthInsuranceMaximumMonthlyIncomeApplied);
  AppendPercent(text, "GKV-Zusatzbeitrag angewendet", y.HealthInsuranceAdditionalRateApplied);
  AppendPercent(text, "Pflegeversicherung angewendet", y.CareInsuranceRateApplied);
  AppendMoney(text, "Gesamtbedarf p.a.", y.TotalAnnualNeed);
  AppendCheck(
   text,
   "Gesamtbedarf = Lebenshaltung + GKV/Pflege",
   y.TotalAnnualNeed,
   y.LivingCosts + y.HealthCareCosts);

  text.AppendLine("[Rente / Einkommen]");
  AppendMoney(text, "Rente brutto p.a.", y.PensionGross);
  AppendMoney(text, "Rentenabzüge GKV/Zusatz/Pflege p.a.", y.PensionHealthAndCareDeductions);
  AppendMoney(text, "Rentensteuer inkl. Zuschlagsteuern p.a.", y.PensionIncomeTax);
  AppendMoney(text, "Rente netto p.a.", y.PensionNet);
  AppendCheck(
   text,
   "Rente netto = brutto - GKV/Zusatz/Pflege - Rentensteuer",
   y.PensionNet,
   Math.Max(0m, y.PensionGross - y.PensionHealthAndCareDeductions - y.PensionIncomeTax));

  if (settings.HouseholdPersonCount == 2)
  {
   AppendMoney(text, "Nettoeinkommen Person 2 p.a.", y.Person2NetEmploymentIncome);
   AppendMoney(text, "Davon zur Bedarfsdeckung verwendet", y.FundingFromPerson2Income);
  }

  text.AppendLine("[Kapitalerträge / Steuern]");
  AppendMoney(text, "Zinsen brutto p.a.", y.InterestGross);
  AppendMoney(text, "Ausschüttungen brutto p.a.", y.DividendsGross);
  AppendMoney(text, "GKV-relevante Kapitalerträge p.a.", y.HealthInsuranceRelevantCapitalIncome);
  AppendMoney(text, "Vorabpauschale steuerlich in diesem Jahr angesetzt", y.AdvanceLumpSumTaxableThisYear);
  AppendMoney(text, "Vorabpauschale für Folgejahr berechnet", y.AdvanceLumpSumCalculatedForNextYear);
  AppendMoney(text, "Realisierter Gewinn/Verlust Einzelaktien", y.RealizedStockGains);
  AppendMoney(text, "Realisierter Gewinn/Verlust Aktien-ETFs", y.RealizedEquityFundGains);
  AppendMoney(text, "Verlustvortrag Aktien Ende Jahr", y.StockLossCarryForward);
  AppendMoney(text, "Sonstiger Verlustvortrag Kapital Ende Jahr", y.OtherLossCarryForward);
  AppendMoney(text, "Sparer-Pauschbetrag angewendet", y.CapitalGainsAllowanceApplied);
  AppendMoney(text, "Kapitalsteuern nach automatischer Günstigerprüfung p.a.", y.TaxesOnCapital);

  text.AppendLine("[Einmalige Zahlungsströme]");
  AppendMoney(text, "Einmalige Einnahmen inkl. Hausverkauf", y.OneTimeIncome);
  AppendMoney(text, "Einmalige Ausgaben", y.OneTimeExpenses);
  AppendMoney(text, "Für das Jahr zu finanzieren", y.RequiredForYear);

  text.AppendLine("[Finanzierung]");
  AppendMoney(text, "Aus Rente", y.FundingFromPension);
  if (settings.HouseholdPersonCount == 2)
   AppendMoney(text, "Aus Nettoeinkommen Person 2", y.FundingFromPerson2Income);
  AppendMoney(text, "Aus einmaligen Einnahmen", y.FundingFromOtherIncome);
  AppendMoney(text, "Aus Ausschüttungen", y.FundingFromDividends);
  AppendMoney(text, "Aus Vermögen / Verkäufen", y.FundingFromCapital);
  AppendMoney(text, "Finanzierungslücke", y.FundingGap);

  text.AppendLine("[Reserve / Rücklagen]");
  AppendMoney(text, "Reserve Soll", y.ReserveTarget);
  AppendMoney(text, "Reserve Ist / Tages-/Festgeld Ende", y.ReserveActual);
  AppendMoney(text, "Haus-Rücklage Soll", y.HouseReserveTarget);
  AppendMoney(text, "Auto-Rücklage Soll", y.CarReserveTarget);
  AppendMoney(text, "Gesundheitsrücklage Soll", y.HealthReserveTarget);
  AppendMoney(text, "Reiserücklage Soll", y.TravelReserveTarget);
  AppendMoney(text, "Sonstige Rücklage Soll", y.OtherReserveTarget);
  AppendCheck(
   text,
   "Reserve Soll = Gesamtbedarf x Reservejahre + Einzelrücklagen",
   y.ReserveTarget,
   y.TotalAnnualNeed * settings.ReserveYears +
   y.HouseReserveTarget +
   y.CarReserveTarget +
   y.HealthReserveTarget +
   y.TravelReserveTarget +
   y.OtherReserveTarget);

  text.AppendLine("[Vermögen]");
  AppendMoney(text, "Vermögen Jahresanfang", y.TotalPortfolioStart);
  AppendMoney(text, "Tages-/Festgeld Ende", y.CashEnd);
  AppendMoney(text, "Welt-ETF Ende", y.WorldEtfEnd);
  AppendMoney(text, "Dividenden-ETF Ende", y.DividendEtfEnd);
  AppendMoney(text, "Dividenden-Aktien Ende", y.DividendStocksEnd);
  AppendMoney(text, "Vermögen Jahresende", y.TotalPortfolioEnd);
  AppendCheck(
   text,
   "Vermögen Ende = Summe der vier Anlageklassen",
   y.TotalPortfolioEnd,
   y.CashEnd + y.WorldEtfEnd + y.DividendEtfEnd + y.DividendStocksEnd);

  text.AppendLine("[Ertragsbeiträge]");
  AppendMoney(text, "Tages-/Festgeld Ertrag", y.CashReturnContribution);
  AppendMoney(text, "Welt-ETF Gesamtertrag", y.WorldReturnContribution);
  AppendMoney(text, "Welt-ETF davon Kursanteil", y.WorldPriceReturnContribution);
  AppendMoney(text, "Welt-ETF davon Ausschüttung", y.WorldDistributionContribution);
  AppendMoney(text, "Dividenden-ETF Gesamtertrag", y.DividendEtfReturnContribution);
  AppendMoney(text, "Dividenden-ETF davon Kursanteil", y.DividendEtfPriceReturnContribution);
  AppendMoney(text, "Dividenden-ETF davon Ausschüttung", y.DividendEtfDistributionContribution);
  AppendMoney(text, "Dividenden-Aktien Gesamtertrag", y.DividendStocksReturnContribution);
  AppendMoney(text, "Dividenden-Aktien davon Kursanteil", y.DividendStocksPriceReturnContribution);
  AppendMoney(text, "Dividenden-Aktien davon Ausschüttung", y.DividendStocksDistributionContribution);

  text.AppendLine();
 }

 private static void AppendCashFlows(
  StringBuilder text,
  string title,
  IEnumerable<OneTimeCashFlow> flows)
 {
  text.AppendLine($"{title}:");

  if (!flows.Any())
  {
   text.AppendLine("  - keine");
   return;
  }

  foreach (OneTimeCashFlow flow in flows.OrderBy(x => x.Year))
  {
   text.AppendLine(
    $"  - {flow.Year}: {Money(flow.AmountToday)} | {flow.Description}");
  }
 }

 private static void AppendCheck(
  StringBuilder text,
  string label,
  decimal actual,
  decimal expected)
 {
  decimal difference = actual - expected;
  string status = Math.Abs(difference) <= 0.01m ? "OK" : "ABWEICHUNG";

  text.AppendLine(
   $"  Prüfschritt: {label} -> {status}; " +
   $"Ist {Money(actual)}, rechnerisch {Money(expected)}, Differenz {Money(difference)}");
 }

 private static void AppendTitle(StringBuilder text, string title)
 {
  text.AppendLine(title);
  text.AppendLine(new string('=', title.Length));
 }

 private static void AppendMoney(StringBuilder text, string label, decimal value) =>
  AppendValue(text, label, Money(value));

 private static void AppendPercent(StringBuilder text, string label, decimal value) =>
  AppendValue(text, label, value.ToString("P2", GermanCulture));

 private static void AppendValue(StringBuilder text, string label, object value) =>
  text.AppendLine($"{label} = {value}");

 private static string Money(decimal value) =>
  value.ToString("N2", GermanCulture) + " €";
}
