namespace RockefellerFiction;

public static class StrategyService
{
 public static StrategyAllocation GetDefault(string strategy) =>
  strategy switch
  {
   "Sicherheit" => new(0.40m, 0.35m, 0.15m, 0.10m),
   "Wachstum" => new(0.15m, 0.60m, 0.15m, 0.10m),
   _ => new(0.25m, 0.50m, 0.15m, 0.10m)
  };

 public static string Recommend(PlannerSettings settings)
 {
  string[] candidates = ["Wachstum", "Ausgewogen", "Sicherheit"];

  foreach (string candidate in candidates)
  {
   StrategyAllocation allocation = GetDefault(candidate);

   ProjectionResult basisResult = ProjectionService.Calculate(settings, allocation, false);
   ProjectionResult stressResult = ProjectionService.Calculate(settings, allocation, true);

   if (basisResult.ReachesPlanEnd && stressResult.ReachesPlanEnd)
    return candidate;
  }

  return "Sicherheit";
 }
}
