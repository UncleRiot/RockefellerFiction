namespace RockefellerFiction;

public static class RecommendationService
{
 public static string Build(
  PlannerSettings settings,
  StrategyAllocation allocation,
  ProjectionResult basis,
  ProjectionResult stress)
 {
  var parts = new List<string>();

  if (basis.ReachesPlanEnd && stress.ReachesPlanEnd)
   parts.Add("Basis- und Stressszenario reichen bis zum Planungsende.");
  else if (basis.ReachesPlanEnd)
   parts.Add("Das Basisszenario reicht, das gewählte Stressszenario aber nicht.");
  else
   parts.Add("Das Vermögen reicht bereits im Basisszenario nicht bis zum Planungsende.");

  string recommendedStrategy = StrategyService.Recommend(settings);
  StrategyAllocation recommendedAllocation = StrategyService.GetDefault(recommendedStrategy);
  bool recommendedBasisReachesPlanEnd =
   ProjectionService.ReachesPlanEnd(settings, recommendedAllocation, false);
  bool recommendedStressReachesPlanEnd =
   ProjectionService.ReachesPlanEnd(settings, recommendedAllocation, true);

  if (!string.Equals(settings.Strategy, recommendedStrategy, StringComparison.Ordinal))
  {
   if (recommendedBasisReachesPlanEnd && recommendedStressReachesPlanEnd)
    parts.Add($"Für die aktuellen Werte ist „{recommendedStrategy}“ die renditestärkste Default-Strategie, die Basis und Stress erfüllt.");
   else
    parts.Add("Keine Default-Strategie erfüllt Basis und Stress vollständig. Als konservativer Fallback wird „Sicherheit“ empfohlen.");
  }
  else
  {
   if (recommendedBasisReachesPlanEnd && recommendedStressReachesPlanEnd)
    parts.Add($"Die gewählte Strategie „{settings.Strategy}“ entspricht der aktuellen Empfehlung.");
   else
    parts.Add("Keine Default-Strategie erfüllt Basis und Stress vollständig. Die gewählte Strategie „Sicherheit“ entspricht dem konservativen Fallback.");
  }

  decimal availableCash = settings.StartCapital * allocation.Cash;
  decimal requiredCash = basis.InitialRequiredCash;

  if (settings.StartCapital > 0m && availableCash < requiredCash)
  {
   decimal missing = requiredCash - availableCash;
   decimal missingQuote = missing / settings.StartCapital;

   parts.Add(
    $"Reserve/Rücklagen: Im Tages-/Festgeld fehlen zu Beginn rund {missing:N0} € " +
    $"({missingQuote:P1} des Startvermögens). Kleinste sinnvolle Anpassung: " +
    $"Tages-/Festgeld um etwa {missingQuote:P1} erhöhen und denselben Anteil aus dem größten Aktienbaustein reduzieren.");
  }
  else
  {
   parts.Add("Reserve/Rücklagen sind mit dem gewählten Tages-/Festgeld-Anteil zu Beginn abgedeckt.");
  }

  if (!stress.ReachesPlanEnd)
  {
   parts.Add(
    $"Größter Hebel: Für das Stressszenario fehlen rechnerisch rund " +
    $"{stress.RequiredAdditionalStartCapital:N0} € Startvermögen.");
  }
  else if (!basis.ReachesPlanEnd)
  {
   parts.Add(
    $"Größter Hebel: Für das Basisszenario fehlen rechnerisch rund " +
    $"{basis.RequiredAdditionalStartCapital:N0} € Startvermögen.");
  }
  else
  {
   parts.Add("Größter Hebel: Aktuell ist keine zwingende Änderung nötig.");
  }

  if (allocation.DividendStocks > 0.25m)
   parts.Add("Hinweis: Der Anteil einzelner Dividenden-Aktien ist hoch und erhöht das Einzelwertrisiko.");

  return string.Join(Environment.NewLine, parts);
 }
}
