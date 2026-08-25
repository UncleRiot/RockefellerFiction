using System.IO;
using System.Text;

namespace RockefellerFiction;

public static class CalculationDocumentationService
{

 public static string Build(
  PlannerSettings settings,
  StrategyAllocation allocation)
 {
  var text = new StringBuilder();

  AppendTitle(text, "BERECHNUNGSGRUNDLAGEN");

  text.AppendLine("Diese Ansicht wird bei jedem Öffnen neu aus den aktuell verwendeten Einstellungen und denselben Berechnungsmethoden erzeugt, die auch die App verwendet.");
  text.AppendLine("Statt C#-Sourcecode werden die wichtigsten Rechenwege in verständlicher Form mit den aktuell verwendeten Werten, Zwischenergebnissen und Formeln angezeigt.");
  text.AppendLine("Die Renten- und GKV/Pflege-Beispiele werden direkt über PensionService berechnet. Dadurch entsprechen ihre angezeigten Zwischenergebnisse der tatsächlich verwendeten Berechnungslogik.");
  text.AppendLine("Allgemeine Modellregeln werden zusätzlich mit der jeweils verantwortlichen Methode benannt, damit Abweichungen zwischen Beschreibung und Berechnung gezielt geprüft werden können.");
  text.AppendLine("Stand der externen Quellenprüfung: 25.08.2026.");
  text.AppendLine();

  AppendCurrentValues(text, settings, allocation);
  AppendHumanReadableCalculationFlow(text);
  AppendDynamicHumanReadableCalculations(text, settings, allocation);

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
  text.AppendLine($"Simulationsstartjahr (automatisch = Arbeitsende Person 1) = {settings.PlanningYear}");
  text.AppendLine($"Vorzeitiges Arbeitsende Person 1 = {(settings.Person1WorkEndYear > 0 ? settings.Person1WorkEndYear : settings.PlanningYear)}");
  text.AppendLine($"Heute bereits erworbene Bruttorente Person 1 = {settings.Person1PensionGrossMonthly:N2} € pro Monat");
  text.AppendLine($"Hochgerechnete Bruttorente Person 1 bei Beiträgen bis 67 = {settings.Person1ProjectedPensionGrossMonthlyAt67:N2} € pro Monat");
  text.AppendLine($"Entgeltpunkte Person 1 optional = {settings.Person1CurrentPensionPoints:N4}");
  text.AppendLine($"RV-pflichtiges Jahresbrutto Person 1 optional = {settings.Person1PensionableAnnualGross:N2} €");
  text.AppendLine($"RV-Brutto-Steigerung Person 1 optional = {settings.Person1PensionableAnnualGrossIncreaseRate:P2}");
  text.AppendLine($"Durchschnittsentgelt Rentenversicherung Steigerung = {settings.PensionAverageAnnualEarningsIncreaseRate:P2}");

  if (settings.HouseholdPersonCount == 2)
  {
   text.AppendLine($"Vorzeitiges Arbeitsende Person 2 = {(settings.Person2WorkEndYear > 0 ? settings.Person2WorkEndYear : settings.PlanningYear)}");
   text.AppendLine($"Nettoeinkommen Person 2 pro Monat = {settings.Person2NetIncomeMonthly:N2} €");
   text.AppendLine($"Nettoeinkommen-Steigerung Person 2 = {settings.Person2NetIncomeIncreaseRate:P2}");
   text.AppendLine($"Heute bereits erworbene Bruttorente Person 2 = {settings.Person2PensionGrossMonthly:N2} € pro Monat");
   text.AppendLine($"Hochgerechnete Bruttorente Person 2 bei Beiträgen bis 67 = {settings.Person2ProjectedPensionGrossMonthlyAt67:N2} € pro Monat");
   text.AppendLine($"Entgeltpunkte Person 2 optional = {settings.Person2CurrentPensionPoints:N4}");
   text.AppendLine($"RV-pflichtiges Jahresbrutto Person 2 optional = {settings.Person2PensionableAnnualGross:N2} €");
   text.AppendLine($"RV-Brutto-Steigerung Person 2 optional = {settings.Person2PensionableAnnualGrossIncreaseRate:P2}");
  }

  text.AppendLine($"Startvermögen = {settings.StartCapital:N2} €");
  text.AppendLine($"Monatliche Lebenshaltung = {settings.MonthlyLivingCosts:N2} €");
  text.AppendLine($"Inflation = {settings.InflationRate:P2}");
  text.AppendLine($"Rentensteigerung = {settings.PensionIncreaseRate:P2}");
  text.AppendLine("Rentenlogik: Wenn Entgeltpunkte eingetragen sind, wird die heutige Rentenanwartschaft daraus mit dem Rentenwert 2026 von 42,52 € je Entgeltpunkt berechnet. Wenn zusätzlich ein RV-pflichtiges Jahresbrutto eingetragen ist, werden zukünftige Entgeltpunkte bis zum früheren Zeitpunkt aus Arbeitsende, Rentenbeginn oder Alter 67 für jedes Beitragsjahr einzeln berechnet. Ausgangswerte 2026 sind das vorläufige Durchschnittsentgelt von 51.944 € und die Beitragsbemessungsgrenze von 101.400 € pro Jahr. Das eigene RV-Brutto wird mit der optionalen Brutto-Steigerung fortgeschrieben; Durchschnittsentgelt und Beitragsbemessungsgrenze werden mit der eingestellten Durchschnittsentgelt-Steigerung fortgeschrieben. Ohne Jahresbrutto wird wie bisher die DRV-Hochrechnung bis 67 anteilig nach Beitragsjahren verwendet. Ohne Entgeltpunkte bleibt die eingegebene heute bereits erworbene Rente die Ausgangsbasis. Danach werden ein möglicher Abschlag bei Rentenbeginn vor 67 und die eingestellte Rentensteigerung berücksichtigt.");
  text.AppendLine($"GKV/Pflege Basisjahr = {settings.HealthInsuranceBaseYear}");
  text.AppendLine($"GKV/Pflege Bemessungsgrenzen Änderung p.a. = {settings.HealthInsuranceAssessmentIncreaseRate:P2}");
  text.AppendLine($"GKV-Zusatzbeitrag Änderung p.a. in Prozentpunkten = {settings.HealthInsuranceAdditionalRateAnnualChange:P2}");
  text.AppendLine($"Pflegeversicherung Änderung p.a. in Prozentpunkten = {settings.CareInsuranceRateAnnualChange:P2}");
  text.AppendLine($"Stress: zusätzl. Änderung GKV/Pflege Bemessungsgrenzen p.a. = {settings.StressHealthInsuranceAssessmentAdditionalIncreaseRate:P2}");
  text.AppendLine($"Stress: zusätzl. GKV-Zusatzbeitrag p.a. in Prozentpunkten = {settings.StressHealthInsuranceAdditionalRateAnnualChange:P2}");
  text.AppendLine($"Stress: zusätzl. Pflegebeitrag p.a. in Prozentpunkten = {settings.StressCareInsuranceRateAnnualChange:P2}");
  text.AppendLine($"Basiszins Vorabpauschale = {settings.AdvanceLumpSumBaseRate:P2}");
  text.AppendLine();

  StrategyAllocation initialAllocation =
   ProjectionService.GetInitialAllocation(settings, allocation);

  text.AppendLine("Gewünschte Strategie-Aufteilung:");
  text.AppendLine($"Sichere Anlage = {allocation.Cash:P2}");
  text.AppendLine($"Welt-ETF = {allocation.WorldEtf:P2}");
  text.AppendLine($"Dividenden-ETF = {allocation.DividendEtf:P2}");
  text.AppendLine($"Dividenden-Aktien = {allocation.DividendStocks:P2}");
  text.AppendLine();
  text.AppendLine("Tatsächliche Startaufteilung unter Erhalt bestehender Anlagen:");
  text.AppendLine($"Sichere Anlage = {initialAllocation.Cash:P2}");
  text.AppendLine($"Welt-ETF = {initialAllocation.WorldEtf:P2}");
  text.AppendLine($"Dividenden-ETF = {initialAllocation.DividendEtf:P2}");
  text.AppendLine($"Dividenden-Aktien = {initialAllocation.DividendStocks:P2}");
  text.AppendLine();

  text.AppendLine("Renditen/Ausschüttungen:");
  text.AppendLine($"Sichere Anlage aktueller Stand = {settings.SecureInvestmentCurrentValue:N2} €");
  text.AppendLine($"Sichere Anlage Zins = {settings.CashInterestRate:P2}");
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
   "Vor dem jeweiligen geplanten Rentenalter wird keine gesetzliche Rente angesetzt. Wenn Entgeltpunkte eingetragen sind, bildet die App die heutige Rentenanwartschaft aus Entgeltpunkten × aktuellem Rentenwert 2026 (42,52 €). Die zusätzlich eingegebene heute bereits erworbene Monatsrente bleibt dann ein Referenzwert aus der Renteninformation und wird nicht als zweite gleichberechtigte Rentenbasis addiert. Ohne Entgeltpunkte bleibt die eingegebene heute bereits erworbene Monatsrente die Ausgangsbasis. Wenn ein RV-pflichtiges Jahresbrutto eingetragen ist, werden die zusätzlich bis zum Arbeitsende entstehenden Entgeltpunkte für jedes Beitragsjahr einzeln berechnet. Dabei werden Brutto, Durchschnittsentgelt und Beitragsbemessungsgrenze mit den jeweiligen Einstellungen fortgeschrieben. Ohne RV-Brutto wird die DRV-Hochrechnung bis 67 als Fallback verwendet und nur anteilig bis zum tatsächlichen Beitragsende berücksichtigt. Sind dabei Entgeltpunkte und die in derselben Renteninformation ausgewiesene bereits erworbene Monatsrente vorhanden, wird aus Monatsrente / Entgeltpunkten der in dieser Renteninformation enthaltene Rentenwert abgeleitet; nur der aus der DRV-Hochrechnung stammende zusätzliche Rentenanteil wird damit auf den aktuellen Rentenwert 2026 umgerechnet. Dadurch werden ältere Renteninformationen nicht mit zwei unterschiedlichen Rentenwert-Ständen vermischt. Das Beitragsende ist der früheste Zeitpunkt aus Arbeitsende, geplantem Rentenbeginn und Alter 67. Für einen geplanten Rentenbeginn vor 67 prüft die App zusätzlich, ob die bisher eingetragenen Versicherungsjahre zuzüglich der weiteren Jahre bis zum Arbeitsende mindestens 35 Jahre ergeben. Beginnt die Rente vor 67, werden 0,3 % je vorgezogenem Monat, höchstens 14,4 %, dauerhaft abgezogen. Die eingestellte Rentensteigerung bildet anschließend die angenommene Entwicklung des Rentenwerts bis zum jeweiligen Simulationsjahr ab; sie ist nicht identisch mit der Entwicklung des Durchschnittsentgelts, die für neu erworbene Entgeltpunkte verwendet wird. Für die Einkommensteuer verwendet die App den gesetzlich feststehenden Tarif 2026 als Basis. Die frühere automatische Kopplung der künftigen Tarifeckwerte und Soli-Freigrenzen an die allgemeine Inflation wurde entfernt. Stattdessen wird ausschließlich die separat eingestellte Steuertarif-/Grundfreibetrag-Steigerung verwendet. Der Standardwert 0 % bedeutet: keine nicht gesetzlich feststehende zukünftige Tarifanhebung wird unterstellt. Ein eigener Wert ist ausdrücklich eine Planungsannahme. KV/PV und die vereinfachte Rentensteuer werden anschließend wie im PensionService berechnet. Das Berechnungsprotokoll weist pro Jahr Rentenbrutto, KV/PV-Abzug, Besteuerungsanteil, festen steuerfreien Rentenbetrag, zu versteuerndes Renteneinkommen, Tarif-Fortschreibungsfaktor, projizierten Grundfreibetrag, Einkommensteuer, Solidaritätszuschlag und Kirchensteuer getrennt aus.",
   "PensionService.CalculatePensionProjectionDiagnostics / PensionService.CalculateAnnualPension / PensionService.CalculateProjectedIncomeTaxIncludingSurcharges");

  AppendSection(
   text,
   "5. Reserve, Rücklagen und reale Haus-/Autoausgaben",
   "Die jährliche Haus-Instandhaltung wird als echte Ausgabe behandelt. Grundlage sind Wohnfläche und das im jeweiligen Simulationsjahr erreichte Alter der Immobilie. Verwendet werden die Richtwerte Stand 2026 nach § 28 Abs. 5a II. BV: unter 22 Jahren 11,49 €/m² p.a., ab 22 Jahren 14,58 €/m² p.a. und ab 32 Jahren 18,62 €/m² p.a. Das Gebäudealter läuft während der Simulation automatisch weiter. Die Richtwerte werden ab Basisjahr 2026 mit der eingestellten Inflation bis zum jeweiligen Simulationsjahr fortgeschrieben. Der Auto-Ersatz bleibt eine punktuelle echte Ausgabe im jeweiligen Ersatzjahr. Der sichere Reserve-Sollwert verwendet den wiederkehrenden Jahresbedarf aus Lebenshaltung, freiwilliger GKV/Pflege und Haus-Instandhaltung multipliziert mit den Reservejahren. Gesundheits-, Reise- und sonstige Zielrücklagen werden zusätzlich gehalten.",
   "ProjectionService.Calculate / ProjectionService.CalculateHouseMaintenanceExpense");

  AppendSection(
   text,
   "6. Anlageerträge und Ausschüttungen",
   "Das Startvermögen und die unter „Vermögen“ eingetragenen Anlagewerte gelten zum Simulationsstart, der automatisch dem Arbeitsende von Person 1 entspricht. Bestehende Anlagebestände werden nicht verkauft oder reduziert. Die gewählte Strategie beschreibt die gewünschte Zielverteilung des gesamten Startvermögens. Der noch nicht zugeordnete Teil des Startvermögens wird proportional zu den noch offenen Zielabständen auf die untergewichteten Anlageklassen verteilt. Liegt ein bestehender Depotbestand bereits über seinem Zielwert, erhält diese Anlageklasse kein zusätzliches Kapital; die tatsächliche Startaufteilung darf deshalb von der Zielverteilung abweichen. Für neu zugeteilte Depotbeträge entspricht der steuerliche Einstandswert dem neu angelegten Betrag; für bestehende Depotbestände wird der Einstandswert aus Depotwert am Simulationsstart, Startjahr und bisheriger Durchschnittsrendite bis zum Simulationsstart geschätzt. Die Erträge werden aus den so gebildeten Beständen und den hinterlegten Sätzen berechnet. Ausschüttungen werden getrennt vom im Bestand verbleibenden Kurs-/Gesamtertrag behandelt.",
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
   "Der Jahresbedarf enthält Lebenshaltung, freiwillige GKV/Pflege, die jährliche Haus-Instandhaltung und in den fälligen Jahren den vollständigen inflationsangepassten Auto-Ersatz. Er wird zuerst durch Nettorente gedeckt. Solange Person 2 vor ihrem eigenen Arbeitsende noch arbeitet, wird anschließend ihr aktuelles monatliches Nettoeinkommen mit der eingetragenen jährlichen Steigerung berücksichtigt. Danach folgen einmalige Einnahmen und Ausschüttungen. Reicht das nicht, wird die sichere Anlage verwendet. Danach wird proportional aus den drei Aktien-/ETF-Bausteinen entnommen. Ein Rest wird als Finanzierungslücke ausgewiesen. Nicht benötigtes Nettoeinkommen von Person 2 wird der sicheren Anlage zugeschlagen.",
   "ProjectionService.Calculate");

  AppendSection(
   text,
   "11. Reserve wieder auffüllen",
   "Ist die automatische Auffüllung aktiv und Sichere Anlage liegt unter dem Soll, wird Geld proportional aus den Aktien-/ETF-Bausteinen in den sicheren Anteil verschoben. Bei aktivierter Schutzregel wird das in negativen Aktienjahren nicht erzwungen.",
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

 private static void AppendDynamicHumanReadableCalculations(
  StringBuilder text,
  PlannerSettings settings,
  StrategyAllocation allocation)
 {
  AppendTitle(text, "DYNAMISCHE RECHENWEGE – MENSCHLICH LESBAR");

  text.AppendLine("Die folgenden Beispiele werden aus den aktuell eingegebenen Werten erzeugt.");
  text.AppendLine("Sie zeigen die tatsächlich verwendeten Rechengrößen, ohne C#-Sourcecode anzuzeigen.");
  text.AppendLine();

  AppendPensionCalculation(text, settings, 1);

  if (settings.HouseholdPersonCount == 2)
   AppendPensionCalculation(text, settings, 2);

  AppendHealthInsuranceCalculation(text, settings, allocation);
  AppendInflationCalculation(text, settings);
  AppendInitialPortfolioCalculation(text, settings, allocation);
  AppendReserveCalculation(text, settings);
  AppendTaxCalculationExplanation(text, settings);
 }

 private static void AppendPensionCalculation(
  StringBuilder text,
  PlannerSettings settings,
  int person)
 {
  bool person1 = person == 1;

  decimal currentGrossMonthly = person1
   ? settings.Person1PensionGrossMonthly
   : settings.Person2PensionGrossMonthly;
  decimal projectedGrossMonthlyAt67 = person1
   ? settings.Person1ProjectedPensionGrossMonthlyAt67
   : settings.Person2ProjectedPensionGrossMonthlyAt67;
  decimal currentPensionPoints = person1
   ? settings.Person1CurrentPensionPoints
   : settings.Person2CurrentPensionPoints;
  decimal pensionableAnnualGross = person1
   ? settings.Person1PensionableAnnualGross
   : settings.Person2PensionableAnnualGross;
  decimal pensionableAnnualGrossIncreaseRate = person1
   ? settings.Person1PensionableAnnualGrossIncreaseRate
   : settings.Person2PensionableAnnualGrossIncreaseRate;
  int currentAge = person1
   ? settings.Person1Age
   : settings.Person2Age;
  int workEndYear = person1
   ? (settings.Person1WorkEndYear > 0 ? settings.Person1WorkEndYear : settings.PlanningYear)
   : (settings.Person2WorkEndYear > 0 ? settings.Person2WorkEndYear : settings.PlanningYear);
  int retirementAge = person1
   ? settings.Person1RetirementAge
   : settings.Person2RetirementAge;

  PensionProjectionDiagnostics diagnostics =
   PensionService.CalculatePensionProjectionDiagnostics(
    currentGrossMonthly,
    projectedGrossMonthlyAt67,
    currentPensionPoints,
    pensionableAnnualGross,
    pensionableAnnualGrossIncreaseRate,
    settings.PensionAverageAnnualEarningsIncreaseRate,
    currentAge,
    workEndYear,
    retirementAge);

  text.AppendLine($"Gesetzliche Rente Person {person}");
  text.AppendLine(new string('-', $"Gesetzliche Rente Person {person}".Length));
  text.AppendLine($"Ausgangsbasis = {diagnostics.CurrentPensionSource}");
  text.AppendLine($"Zukünftige Anwartschaft = {diagnostics.FutureAccrualSource}");
  text.AppendLine();

  if (diagnostics.EnteredCurrentPensionPoints > 0m)
  {
   text.AppendLine(
    $"Heutige Rentenanwartschaft = {diagnostics.EnteredCurrentPensionPoints:N4} Entgeltpunkte × 42,52 € Rentenwert = {diagnostics.CurrentPensionMonthlyUsed:N2} € pro Monat.");
  }
  else
  {
   text.AppendLine(
    $"Heutige Rentenanwartschaft = eingegebener Wert {diagnostics.CurrentPensionMonthlyUsed:N2} € pro Monat.");
  }

  text.AppendLine(
   $"Arbeitsende-Alter rechnerisch = {diagnostics.WorkEndAge}; geplantes Rentenalter = {retirementAge}; Beitragsende-Alter = {diagnostics.ContributionEndAge}.");
  int currentInsuranceYears = person1
   ? settings.Person1CurrentInsuranceYears
   : settings.Person2CurrentInsuranceYears;
  int insuranceYearsAtWorkEnd =
   PensionService.CalculateInsuranceYearsAtWorkEnd(
    currentInsuranceYears,
    workEndYear);

  text.AppendLine(
   $"Versicherungsjahre: bisher {currentInsuranceYears}; bis Arbeitsende rechnerisch {insuranceYearsAtWorkEnd}; Mindestwert für Rentenbeginn vor 67 = {PensionService.MinimumInsuranceYearsForEarlyRetirement}.");
  text.AppendLine(
   $"Zusätzliche Beitragsjahre = {diagnostics.AdditionalContributionYears} von maximal {diagnostics.YearsToRegularRetirement} Jahren bis 67.");

  if (diagnostics.FutureAccrualSource == "RV-pflichtiges Jahresbrutto")
  {
   text.AppendLine(
    $"Erstes Beitragsjahr: beitragspflichtiges Brutto {diagnostics.PensionableAnnualGrossUsed:N2} € / Durchschnittsentgelt {diagnostics.AverageAnnualEarningsFirstYear:N2} € = {diagnostics.AnnualPensionPoints:N6} Entgeltpunkte.");
   text.AppendLine(
    $"Letztes berücksichtigtes Beitragsjahr: beitragspflichtiges Brutto {diagnostics.PensionableAnnualGrossLastYear:N2} € / Durchschnittsentgelt {diagnostics.AverageAnnualEarningsLastYear:N2} € = {diagnostics.AnnualPensionPointsLastYear:N6} Entgeltpunkte.");
   text.AppendLine(
    $"Zusätzliche Entgeltpunkte gesamt = {diagnostics.AdditionalPensionPoints:N6}.");
   text.AppendLine(
    $"Zusätzliche Monatsrente = {diagnostics.AdditionalPensionPoints:N6} × 42,52 € = {diagnostics.AdditionalPensionMonthly:N2} €.");
  }
  else if (diagnostics.FutureAccrualSource == "DRV-Hochrechnung bis 67")
  {
   text.AppendLine(
    $"DRV-Fallback: von {diagnostics.EnteredCurrentPensionMonthly:N2} € heute auf {diagnostics.EnteredProjectedPensionMonthlyAt67:N2} € bei Beiträgen bis 67.");
   text.AppendLine(
    $"Davon werden nur {diagnostics.AdditionalContributionYears} der {diagnostics.YearsToRegularRetirement} möglichen Beitragsjahre berücksichtigt.");
   text.AppendLine(
    $"Zusätzliche Monatsrente bis zum Beitragsende = {diagnostics.AdditionalPensionMonthly:N2} €.");
  }
  else
  {
   text.AppendLine("Es werden keine zusätzlichen Rentenanwartschaften bis zum Beitragsende angesetzt.");
  }

  text.AppendLine(
   $"Monatsrente vor Rentensteigerung und vor Abschlag = {diagnostics.MonthlyAtRetirementBeforePensionIncrease:N2} €.");
  text.AppendLine(
   $"Faktor wegen geplantem Rentenbeginn = {diagnostics.EarlyRetirementFactor:P2}.");
  text.AppendLine(
   $"Monatsrente nach Abschlagsfaktor, noch ohne spätere Rentensteigerungen = {diagnostics.MonthlyAtRetirementAfterEarlyRetirementFactor:N2} €.");
  text.AppendLine();
  AppendReferenceSources(text, "Rente");
 }

 private static void AppendHealthInsuranceCalculation(
  StringBuilder text,
  PlannerSettings settings,
  StrategyAllocation allocation)
 {
  AppendSectionTitle(text, "Freiwillige GKV/Pflege zum Simulationsstart");

  HealthInsuranceProjectionParameters parameters =
   PensionService.CalculateHealthInsuranceProjectionParameters(
    settings,
    settings.PlanningYear,
    false);

  HealthInsurancePreview preview =
   ProjectionService.CalculateInitialVoluntaryHealthPreview(
    settings,
    allocation);

  text.AppendLine(
   $"Mindest-Bemessungsgrundlage im Jahr {settings.PlanningYear} = {parameters.MinimumMonthlyIncome:N2} € pro Monat.");
  text.AppendLine(
   $"Beitragsbemessungsgrenze im Jahr {settings.PlanningYear} = {parameters.MaximumMonthlyIncome:N2} € pro Monat.");
  text.AppendLine(
   $"Zusatzbeitrag im Jahr {settings.PlanningYear} = {parameters.AdditionalRate:P2}; Pflegebeitrag = {parameters.CareRate:P2}.");
  text.AppendLine(
   $"Berechneter freiwilliger GKV/Pflege-Beitrag Person 1 zum Simulationsstart = {preview.Person1Monthly:N2} € pro Monat.");

  if (settings.HouseholdPersonCount == 2)
  {
   text.AppendLine(
    $"Berechneter freiwilliger GKV/Pflege-Beitrag Person 2 zum Simulationsstart = {preview.Person2Monthly:N2} € pro Monat.");
  }

  text.AppendLine(
   "Grundregel vor Rentenbeginn: monatliche Bemessungsgrundlage = Kapitalerträge pro Person, mindestens Mindest-Bemessungsgrundlage und höchstens Beitragsbemessungsgrenze.");
  text.AppendLine(
   $"Jahresbeitrag = monatliche Bemessungsgrundlage × ({settings.VoluntaryHealthInsuranceRate:P2} GKV + Zusatzbeitrag + Pflegebeitrag) × 12.");
  text.AppendLine();
  AppendReferenceSources(text, "GKV/Pflege");
 }

 private static void AppendInflationCalculation(
  StringBuilder text,
  PlannerSettings settings)
 {
  AppendSectionTitle(text, "Lebenshaltung und Inflation");

  int yearsToPlanning = Math.Max(0, settings.PlanningYear - DateTime.Today.Year);
  decimal inflationFactor = Pow(
   1m + settings.InflationRate,
   yearsToPlanning);
  decimal annualLivingToday = settings.MonthlyLivingCosts * 12m;
  decimal annualLivingAtPlanning = annualLivingToday * inflationFactor;

  text.AppendLine(
   $"Heutige Lebenshaltung pro Jahr = {settings.MonthlyLivingCosts:N2} € × 12 = {annualLivingToday:N2} €.");
  text.AppendLine(
   $"Inflationsfaktor bis {settings.PlanningYear} = (1 + {settings.InflationRate:P2})^{yearsToPlanning} = {inflationFactor:N6}.");
  text.AppendLine(
   $"Lebenshaltung im Simulationsstartjahr = {annualLivingToday:N2} € × {inflationFactor:N6} = {annualLivingAtPlanning:N2} €.");
  text.AppendLine();
  AppendReferenceSources(text, "Inflation");
 }

 private static void AppendInitialPortfolioCalculation(
  StringBuilder text,
  PlannerSettings settings,
  StrategyAllocation allocation)
 {
  AppendSectionTitle(text, "Startvermögen und Anlageaufteilung");

  decimal existingInvestmentTotal =
   settings.SecureInvestmentCurrentValue +
   settings.WorldEtfCurrentValue +
   settings.DividendEtfCurrentValue +
   settings.DividendStocksCurrentValue;

  decimal cashTarget = settings.StartCapital * allocation.Cash;
  decimal worldTarget = settings.StartCapital * allocation.WorldEtf;
  decimal dividendEtfTarget = settings.StartCapital * allocation.DividendEtf;
  decimal dividendStocksTarget = settings.StartCapital * allocation.DividendStocks;

  text.AppendLine(
   $"Startvermögen = {settings.StartCapital:N2} € = 100 % der Anlageaufteilung.");
  text.AppendLine(
   $"Bestehende Anlagen = {settings.SecureInvestmentCurrentValue:N2} € sichere Anlage + {settings.WorldEtfCurrentValue:N2} € Welt-ETF + {settings.DividendEtfCurrentValue:N2} € Dividenden-ETF + {settings.DividendStocksCurrentValue:N2} € Dividenden-Aktien = {existingInvestmentTotal:N2} €. Diese Werte sind bereits im Startvermögen enthalten.");
  text.AppendLine(
   $"Sichere Anlage Zielbetrag = {cashTarget:N2} € ({allocation.Cash:P2}); davon bereits vorhanden: {settings.SecureInvestmentCurrentValue:N2} €.");
  text.AppendLine(
   $"Welt-ETF Zielbetrag = {worldTarget:N2} € ({allocation.WorldEtf:P2}); davon bereits vorhanden: {settings.WorldEtfCurrentValue:N2} €.");
  text.AppendLine(
   $"Dividenden-ETF Zielbetrag = {dividendEtfTarget:N2} € ({allocation.DividendEtf:P2}); davon bereits vorhanden: {settings.DividendEtfCurrentValue:N2} €.");
  text.AppendLine(
   $"Dividenden-Aktien Zielbetrag = {dividendStocksTarget:N2} € ({allocation.DividendStocks:P2}); davon bereits vorhanden: {settings.DividendStocksCurrentValue:N2} €.");
  text.AppendLine();
  AppendReferenceSources(text, "Startvermögen und Anlageaufteilung");
 }

 private static void AppendReserveCalculation(
  StringBuilder text,
  PlannerSettings settings)
 {
  AppendSectionTitle(text, "Reserve und Rücklagen zum Simulationsstart");

  int yearsToPlanning = Math.Max(0, settings.PlanningYear - DateTime.Today.Year);
  decimal inflationFactor = Pow(
   1m + settings.InflationRate,
   yearsToPlanning);

  decimal living =
   settings.MonthlyLivingCosts * 12m * inflationFactor;
  int houseAgeAtPlanning =
   ProjectionService.GetHouseAgeAtYear(settings, settings.PlanningYear);
  decimal houseRatePerSquareMeter =
   ProjectionService.GetHouseMaintenanceRatePerSquareMeter(houseAgeAtPlanning);
  decimal houseMaintenanceExpense =
   ProjectionService.CalculateHouseMaintenanceExpense(settings, settings.PlanningYear);
  int houseMaintenanceInflationYears =
   Math.Max(0, settings.PlanningYear - ProjectionService.HouseMaintenanceBaseYear);
  decimal houseMaintenanceInflationFactor =
   Pow(1m + settings.InflationRate, houseMaintenanceInflationYears);
  int carReplacementIntervalYears = Math.Max(1, settings.CarReplacementYears);
  int firstCarReplacementYear =
   settings.PlanningYear +
   carReplacementIntervalYears;
  int yearsToFirstCarReplacement =
   Math.Max(0, firstCarReplacementYear - DateTime.Today.Year);
  decimal firstCarReplacementExpense =
   settings.CarReplacementValue *
   Pow(1m + settings.InflationRate, yearsToFirstCarReplacement);
  decimal healthReserve =
   settings.HealthReserveTarget * inflationFactor;
  decimal travelReserve =
   settings.TravelReserveTarget * inflationFactor;
  decimal otherReserve =
   settings.OtherReserveTarget * inflationFactor;

  text.AppendLine(
   $"Sichere Reserve = wiederkehrender Jahresbedarf × {settings.ReserveYears:N2} Reservejahre. Zum wiederkehrenden Jahresbedarf zählen Lebenshaltung, freiwillige GKV/Pflege und die jährliche Haus-Instandhaltung.");
  text.AppendLine(
   $"Haus-Instandhaltung im Simulationsstartjahr = {settings.HouseLivingArea:N2} m² × {houseRatePerSquareMeter:N2} €/m² p.a. bei Immobilienalter {houseAgeAtPlanning} Jahre × Inflationsfaktor {houseMaintenanceInflationFactor:N4} ab Basisjahr {ProjectionService.HouseMaintenanceBaseYear} = {houseMaintenanceExpense:N2} € echte Ausgabe.");
  text.AppendLine(
   $"Auto-Ersatz: erster Ersatz {firstCarReplacementYear} (= Simulationsstart {settings.PlanningYear} + {carReplacementIntervalYears} Jahre), danach jeweils wieder nach {carReplacementIntervalYears} Jahren. Inflationsangepasster erster Ersatzwert = {firstCarReplacementExpense:N2} €.");
  text.AppendLine(
   $"Gesundheit/Zahnersatz Zielwert zum Simulationsstart = {healthReserve:N2} €.");
  text.AppendLine(
   $"Reisen/größere Wünsche Zielwert zum Simulationsstart = {travelReserve:N2} €.");
  text.AppendLine(
   $"Sonstiges/Unvorhergesehenes Zielwert zum Simulationsstart = {otherReserve:N2} €.");
  text.AppendLine(
   "Der endgültige Reserve-Sollwert verwendet den wiederkehrenden Jahresbedarf einschließlich freiwilliger GKV/Pflege und Haus-Instandhaltung. Der punktuelle Auto-Ersatz wird im Fälligkeitsjahr als echte Ausgabe finanziert, aber nicht mit den Reservejahren multipliziert.");
  text.AppendLine();
  AppendReferenceSources(text, "Reserve und Rücklagen");
 }

 private static void AppendTaxCalculationExplanation(
  StringBuilder text,
  PlannerSettings settings)
 {
  AppendSectionTitle(text, "Kapitalertragsteuer und Vorabpauschale");

  text.AppendLine(
   "Aktienfonds: Für Ausschüttungen, Vorabpauschalen und realisierte ETF-Gewinne werden im Steuerteil 70 % als steuerpflichtiger Anteil angesetzt.");
  text.AppendLine(
   $"Sparer-Pauschbetrag Haushalt = {settings.CapitalGainsAllowance:N2} €; er wird nach der Verlustverrechnung abgezogen.");
  text.AppendLine(
   "Ohne Kirchensteuer: Kapitalertragsteuer = 25 % des verbleibenden steuerpflichtigen Betrags; darauf kommen 5,5 % Solidaritätszuschlag.");
  if (settings.ChurchTaxEnabled)
  {
   text.AppendLine(
    $"Kirchensteuer ist aktiviert; verwendeter Kirchensteuersatz = {settings.ChurchTaxRate:P2}. Die Kapitalertragsteuer wird nach der im TaxService verwendeten Kirchensteuerformel berechnet.");
  }
  else
  {
   text.AppendLine("Kirchensteuer ist deaktiviert.");
  }

  text.AppendLine(
   "Zusätzlich wird jedes Jahr eine Günstigerprüfung durchgeführt: Die pauschale Kapitalertragsteuer wird mit der zusätzlichen tariflichen Einkommensteuer verglichen; verwendet wird die niedrigere Belastung.");
  text.AppendLine(
   $"Vorabpauschale: Basisertrag = ETF-Wert zu Jahresbeginn × {settings.AdvanceLumpSumBaseRate:P2} Basiszins × 70 %. Dieser Wert wird auf den tatsächlichen Wertzuwachs einschließlich Ausschüttungen begrenzt; Ausschüttungen werden anschließend abgezogen.");
  text.AppendLine();
  AppendReferenceSources(text, "Kapitalertragsteuer");
 }

 private static void AppendSectionTitle(
  StringBuilder text,
  string title)
 {
  text.AppendLine(title);
  text.AppendLine(new string('-', title.Length));
 }

 private static decimal Pow(decimal value, int exponent)
 {
  decimal result = 1m;
  for (int i = 0; i < exponent; i++)
   result *= value;
  return result;
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
  text.AppendLine("Interne Berechnungsquelle: " + source);
  text.AppendLine();
  AppendReferenceSources(text, title);
 }

 private static void AppendReferenceSources(
  StringBuilder text,
  string topic)
 {
  text.AppendLine("Quellen / fachliche Grundlage:");

  switch (topic)
  {
   case "2. Inflation und Lebenshaltung":
   case "Inflation":
    AppendExternalSource(
     text,
     "Statistisches Bundesamt – Verbraucherpreisindex und Inflationsrate",
     "https://www.destatis.de/DE/Themen/Wirtschaft/Preise/Verbraucherpreisindex/_inhalt.html",
     "Stand der Seite: 12.08.2026; URL geprüft am 24.08.2026");
    AppendExternalSource(
     text,
     "Statistisches Bundesamt – Verbraucherpreisindex: Gesamtindex und 12 Abteilungen",
     "https://www.destatis.de/DE/Themen/Wirtschaft/Preise/Verbraucherpreisindex/Tabellen/Verbraucherpreise-12Kategorien.html",
     "Stand: 12.08.2026; URL geprüft am 24.08.2026");
    text.AppendLine("Hinweis: Die in der App verwendete zukünftige Inflationsrate ist eine vom Benutzer gesetzte Modellannahme, keine gesetzlich vorgegebene Prognose.");
    break;

   case "3. Freiwillige GKV/Pflege nach Arbeitsende und vor Rentenbeginn":
   case "GKV/Pflege":
    AppendExternalSource(
     text,
     "Bundesministerium für Gesundheit – Beiträge der gesetzlichen Krankenversicherung (GKV)",
     "https://www.bundesgesundheitsministerium.de/beitraege/seite",
     "Werte 2026; URL geprüft am 24.08.2026");
    AppendExternalSource(
     text,
     "Bundesministerium für Gesundheit – Finanzierung der Pflegeversicherung",
     "https://www.bundesgesundheitsministerium.de/themen/pflege/online-ratgeber-pflege/die-pflegeversicherung/finanzierung",
     "Werte 2026; URL geprüft am 24.08.2026");
    AppendExternalSource(
     text,
     "Gesetze im Internet – § 240 SGB V, beitragspflichtige Einnahmen freiwilliger Mitglieder",
     "https://www.gesetze-im-internet.de/sgb_5/__240.html",
     "geltende Fassung; URL geprüft am 24.08.2026");
    break;

   case "4. Rente und vereinfachte Rentensteuer":
   case "Rente":
    AppendExternalSource(
     text,
     "Deutsche Rentenversicherung – Entgeltpunkte",
     "https://www.deutsche-rentenversicherung.de/SharedDocs/Glossareintraege/DE/E/entgeltpunkte.html",
     "2026-Werte enthalten; URL geprüft am 24.08.2026");
    AppendExternalSource(
     text,
     "Deutsche Rentenversicherung – Altersrente für langjährig Versicherte",
     "https://www.deutsche-rentenversicherung.de/DRV/DE/Rente/Allgemeine-Informationen/Rentenarten-und-Leistungen/Altersrente-fuer-langjaehrig-Versicherte/Altersrente_fuer_langjaehrig_Versicherte.html?https=1",
     "URL geprüft am 24.08.2026");
    AppendExternalSource(
     text,
     "Bundesministerium der Finanzen – Rentenbesteuerung",
     "https://www.bundesfinanzministerium.de/Content/DE/Standardartikel/Themen/Steuern/Steuerliche_Themengebiete/Rentenbesteuerung/2021-04-28-Rentenbesteuerung-Eine-Frage-der-Gerechtigkeit.html",
     "Stand: 17.06.2026; URL geprüft am 24.08.2026");
    break;

   case "8. Kapitalertragsteuer":
   case "Kapitalertragsteuer":
    AppendExternalSource(
     text,
     "Gesetze im Internet – § 32d EStG, gesonderter Steuertarif und Günstigerprüfung",
     "https://www.gesetze-im-internet.de/estg/__32d.html",
     "geltende Fassung; URL geprüft am 24.08.2026");
    AppendExternalSource(
     text,
     "Gesetze im Internet – § 18 InvStG, Vorabpauschale",
     "https://www.gesetze-im-internet.de/invstg_2018/__18.html",
     "geltende Fassung; URL geprüft am 24.08.2026");
    AppendExternalSource(
     text,
     "Gesetze im Internet – § 20 InvStG, Teilfreistellung",
     "https://www.gesetze-im-internet.de/invstg_2018/__20.html",
     "geltende Fassung; URL geprüft am 24.08.2026");
    AppendExternalSource(
     text,
     "Bundesministerium der Finanzen – Basiszins zur Berechnung der Vorabpauschale 2026",
     "https://www.bundesfinanzministerium.de/Content/DE/Downloads/BMF_Schreiben/Steuerarten/Investmentsteuer/2026-01-13-basiszins-berechnung-vorabpauschale.pdf?__blob=publicationFile&v=1",
     "Datum: 13.01.2026; Basiszins 2026: 3,20 %; URL geprüft am 24.08.2026");
    break;

   case "5. Reserve und Rücklagen":
   case "5. Reserve, Rücklagen und reale Haus-/Autoausgaben":
   case "Reserve und Rücklagen":
    AppendModelSource(text, "ProjectionService.cs", "Reservehöhe, Auto-Ersatz und die Fortschreibung der Haus-Instandhaltung sind Modellregeln der App.");
    AppendExternalSource(
     text,
     "KSK-Immobilien – Instandhaltungsrücklage; Richtwerte nach § 28 Abs. 5a II. BV, Stand 2026",
     "https://www.ksk-immobilien.de/wissen-ratgeber/lexikon/instandhaltungsruecklage/",
     "2026: 11,49 / 14,58 / 18,62 € je m² und Jahr für <22 / ≥22 / ≥32 Jahre; URL geprüft am 25.08.2026");
    AppendExternalSource(
     text,
     "Wüstenrot – Instandhaltungsrücklage: Höhe und Berechnung",
     "https://www.wuestenrot.de/modernisieren/instandhaltungsruecklage",
     "Altersabhängige Richtwertmethodik quergeprüft; URL geprüft am 25.08.2026");
    AppendExternalSource(
     text,
     "Statistisches Bundesamt – Verbraucherpreisindex und Inflationsrate",
     "https://www.destatis.de/DE/Themen/Wirtschaft/Preise/Verbraucherpreisindex/_inhalt.html",
     "Stand der Seite: 12.08.2026; URL geprüft am 25.08.2026");
    break;

   case "1. Planungszeitraum":
    AppendModelSource(text, "ProjectionService.Calculate", "Planungsbeginn und Planungsende sind Modellregeln der App; dafür gibt es keine gesetzliche Berechnungsvorgabe.");
    break;

   case "6. Anlageerträge und Ausschüttungen":
   case "Startvermögen und Anlageaufteilung":
    AppendModelSource(text, "ProjectionService.Calculate", "Anlageaufteilung, Renditen und Ausschüttungsannahmen sind frei gewählte Modellannahmen der App.");
    break;

   case "7. Stressszenario":
    AppendModelSource(text, "ProjectionService.Calculate", "Crash-Stärke, Crash-Jahr und Ersatz der Jahresrendite sind bewusst definierte Stress-Modellannahmen der App.");
    break;

   case "9. Einmalige Einnahmen, Ausgaben und Hausverkauf":
    AppendModelSource(text, "ProjectionService.Calculate / ProjectionService.SumCashFlows", "Zeitpunkt und Inflationsfortschreibung einmaliger Zahlungen sind Modellregeln der App.");
    break;

   case "10. Finanzierung des Jahresbedarfs":
    AppendModelSource(text, "ProjectionService.Calculate", "Die Reihenfolge Rente → Arbeitseinkommen P2 → sonstige Einnahmen → Ausschüttungen → Cash → Verkäufe ist eine Modellentscheidung der App.");
    break;

   case "11. Reserve wieder auffüllen":
    AppendModelSource(text, "ProjectionService.Calculate", "Die automatische Reserveauffüllung und die Schutzregel für negative Aktienjahre sind Modellentscheidungen der App.");
    break;

   case "12. Ampelstatus":
    AppendModelSource(text, "ProjectionService.Calculate / ProjectionResult.OverallStatus", "Die Schwellen für Grün, Gelb und Rot sind programminterne Bewertungsregeln.");
    break;

   case "13. Mindest-Startvermögen":
    AppendModelSource(text, "ProjectionService.EstimateMinimumStartCapital", "Das Mindest-Startvermögen wird numerisch über die konkrete App-Simulation angenähert; hierfür gibt es keine gesetzliche Formel.");
    break;

   case "14. Strategieempfehlung":
    AppendModelSource(text, "StrategyService.Recommend", "Die Auswahl der renditestärksten bestandenen Default-Strategie ist eine programminterne Entscheidungsregel.");
    break;

   case "15. Handlungsempfehlung":
    AppendModelSource(text, "RecommendationService.Build", "Die ausgegebenen Hinweise und Prioritäten sind programminterne Entscheidungsregeln.");
    break;

   default:
    AppendModelSource(text, "RockefellerFiction", "Für diesen Abschnitt ist keine eigene externe gesetzliche Berechnungsgrundlage hinterlegt.");
    break;
  }

  text.AppendLine();
 }

 private static void AppendExternalSource(
  StringBuilder text,
  string name,
  string url,
  string stand)
 {
  text.AppendLine("- " + name);
  text.AppendLine("  URL: " + url);
  text.AppendLine("  Stand: " + stand);
 }

 private static void AppendModelSource(
  StringBuilder text,
  string source,
  string explanation)
 {
  text.AppendLine("- Modellannahme / interne Berechnungsregel");
  text.AppendLine("  Quelle: " + source);
  text.AppendLine("  URL: keine – lokale Programmlogik");
  text.AppendLine("  Stand: 24.08.2026");
  text.AppendLine("  Hinweis: " + explanation);
 }
}
