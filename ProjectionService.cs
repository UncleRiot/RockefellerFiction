namespace RockefellerFiction;

public static class ProjectionService
{
 public static ProjectionResult Calculate(PlannerSettings s, StrategyAllocation allocation, bool stress)
 {
  var result = new ProjectionResult();

  decimal cash = s.StartCapital * allocation.Cash;
  decimal world = s.StartCapital * allocation.WorldEtf;
  decimal divEtf = s.StartCapital * allocation.DividendEtf;
  decimal divStocks = s.StartCapital * allocation.DividendStocks;

  int finalYear = Math.Max(
   s.PlanningYear + Math.Max(0, s.Person1EndAge - s.Person1Age),
   s.PlanningYear + Math.Max(0, s.Person2EndAge - s.Person2Age));

  bool depleted = false;
  int? depletionYear = null;

  for (int year = s.PlanningYear; year <= finalYear; year++)
  {
   int offset = year - s.PlanningYear;
   int age1 = s.Person1Age + offset;
   int age2 = s.Person2Age + offset;

   decimal inflationFactor = Pow(1m + s.InflationRate, offset);
   decimal living = s.MonthlyLivingCosts * 12m * inflationFactor;
   decimal previousLiving = offset == 0
    ? living
    : s.MonthlyLivingCosts * 12m * Pow(1m + s.InflationRate, offset - 1);
   decimal livingCostIncrease = Math.Max(0m, living - previousLiving);
   decimal healthCare = 0m;
   decimal healthCarePerson1 = 0m;
   decimal healthCarePerson2 = 0m;
   decimal healthInsuranceRelevantCapitalIncome = 0m;
   decimal totalAnnualNeed = living;

   decimal houseReserveAnnual = s.HouseTotalValue * s.HouseBuildingShare * s.HouseReserveRate * inflationFactor;
   decimal carReserveAnnual = (s.CarReplacementValue / Math.Max(1, s.CarReplacementYears)) * inflationFactor;
   decimal healthTarget = s.HealthReserveTarget * inflationFactor;
   decimal travelTarget = s.TravelReserveTarget * inflationFactor;
   decimal otherTarget = s.OtherReserveTarget * inflationFactor;

   decimal reserveTarget = totalAnnualNeed * s.ReserveYears + houseReserveAnnual + carReserveAnnual +
                           healthTarget + travelTarget + otherTarget;

   decimal portfolioStart = cash + world + divEtf + divStocks;

   decimal cashReturn = cash * s.CashInterestRate;
   decimal wr = s.WorldEtfReturn;
   decimal er = s.DividendEtfReturn;
   decimal sr = s.DividendStocksReturn;

   if (stress && s.StressCrashAtStart && offset == 0)
   {
    wr += s.StressCrashPercent;
    er += s.StressCrashPercent;
    sr += s.StressCrashPercent;
   }

   if (stress && s.StressSecondCrashEnabled && year == s.StressSecondCrashYear)
   {
    wr += s.StressSecondCrashPercent;
    er += s.StressSecondCrashPercent;
    sr += s.StressSecondCrashPercent;
   }

   decimal worldGrossReturn = world * wr;
   decimal divEtfGrossReturn = divEtf * er;
   decimal divStocksGrossReturn = divStocks * sr;

   decimal worldDistribution = world * s.WorldEtfDistribution;
   decimal divEtfDistribution = divEtf * s.DividendEtfDistribution;
   decimal divStocksDistribution = divStocks * s.DividendStocksDistribution;
   decimal dividendsGross = worldDistribution + divEtfDistribution + divStocksDistribution;

   healthInsuranceRelevantCapitalIncome =
    Math.Max(0m, cashReturn) +
    Math.Max(0m, worldDistribution) +
    Math.Max(0m, divEtfDistribution) +
    Math.Max(0m, divStocksDistribution);

   decimal capitalIncomePerPersonMonthly = healthInsuranceRelevantCapitalIncome / 2m / 12m;

   if (age1 < s.Person1RetirementAge)
    healthCarePerson1 = CalculateVoluntaryHealthAndCareAnnual(s, capitalIncomePerPersonMonthly);

   if (age2 < s.Person2RetirementAge)
    healthCarePerson2 = CalculateVoluntaryHealthAndCareAnnual(s, capitalIncomePerPersonMonthly);

   healthCare = healthCarePerson1 + healthCarePerson2;
   totalAnnualNeed = living + healthCare;

   decimal taxes = TaxService.CalculateCapitalTax(
    cashReturn,
    divStocksDistribution,
    worldDistribution + divEtfDistribution,
    0m,
    s.CapitalGainsAllowance);

   decimal taxableCashWeight = Math.Max(0m, cashReturn);
   decimal taxableWorldWeight = Math.Max(0m, worldDistribution) * 0.70m;
   decimal taxableDivEtfWeight = Math.Max(0m, divEtfDistribution) * 0.70m;
   decimal taxableDivStocksWeight = Math.Max(0m, divStocksDistribution);
   decimal taxableWeightTotal =
    taxableCashWeight + taxableWorldWeight + taxableDivEtfWeight + taxableDivStocksWeight;

   decimal cashTax = taxableWeightTotal > 0m ? taxes * taxableCashWeight / taxableWeightTotal : 0m;
   decimal worldTax = taxableWeightTotal > 0m ? taxes * taxableWorldWeight / taxableWeightTotal : 0m;
   decimal divEtfTax = taxableWeightTotal > 0m ? taxes * taxableDivEtfWeight / taxableWeightTotal : 0m;
   decimal divStocksTax = taxableWeightTotal > 0m ? taxes * taxableDivStocksWeight / taxableWeightTotal : 0m;

   decimal cashNetIncome = Math.Max(0m, cashReturn - cashTax);
   decimal worldNetIncome = Math.Max(0m, worldDistribution - worldTax);
   decimal divEtfNetIncome = Math.Max(0m, divEtfDistribution - divEtfTax);
   decimal divStocksNetIncome = Math.Max(0m, divStocksDistribution - divStocksTax);

   var pension = PensionService.CalculateAnnualPension(s, year, age1, age2);

   decimal oneTimeIncome = SumCashFlows(s.OneTimeIncome, year, s.PlanningYear, s.InflationRate);
   if (s.HouseIncluded && s.HouseSaleYear == year)
    oneTimeIncome += s.HouseNetSaleProceeds;

   decimal oneTimeExpenses = SumCashFlows(s.OneTimeExpenses, year, s.PlanningYear, s.InflationRate);

   world = Math.Max(0m, world + worldGrossReturn - worldDistribution);
   divEtf = Math.Max(0m, divEtf + divEtfGrossReturn - divEtfDistribution);
   divStocks = Math.Max(0m, divStocks + divStocksGrossReturn - divStocksDistribution);
   cash = Math.Max(0m, cash + cashReturn);

   decimal requiredForYear = totalAnnualNeed + oneTimeExpenses + taxes;

   decimal fundingFromPension = Math.Min(Math.Max(0m, pension.Net), requiredForYear);
   decimal remainingAfterPension = Math.Max(0m, requiredForYear - fundingFromPension);

   decimal fundingFromOtherIncome = Math.Min(Math.Max(0m, oneTimeIncome), remainingAfterPension);
   decimal remainingAfterOtherIncome = Math.Max(0m, remainingAfterPension - fundingFromOtherIncome);

   decimal availableDividends = Math.Max(0m, dividendsGross);
   decimal fundingFromDividends = Math.Min(availableDividends, remainingAfterOtherIncome);
   decimal need = Math.Max(0m, remainingAfterOtherIncome - fundingFromDividends);

   if (s.DividendSurplusReinvest)
    cash += Math.Max(0m, availableDividends - fundingFromDividends);

   decimal withdrawn = 0m;

   if (need > 0m)
   {
    decimal fromCash = Math.Min(cash, need);
    cash -= fromCash;
    need -= fromCash;
    withdrawn += fromCash;
   }

   if (need > 0m)
   {
    decimal risky = world + divEtf + divStocks;
    if (risky > 0m)
    {
     decimal take = Math.Min(risky, need);
     decimal wShare = world / risky;
     decimal eShare = divEtf / risky;
     decimal sShare = divStocks / risky;

     world = Math.Max(0m, world - take * wShare);
     divEtf = Math.Max(0m, divEtf - take * eShare);
     divStocks = Math.Max(0m, divStocks - take * sShare);
     withdrawn += take;
     need -= take;
    }
   }

   decimal fundingGap = Math.Max(0m, need);

   bool stockYearNegative = wr < 0m || er < 0m || sr < 0m;
   if (s.AutoRefillReserve && cash < reserveTarget &&
       (!s.UseReserveOnNegativeStockYear || !stockYearNegative))
   {
    decimal missing = reserveTarget - cash;
    decimal risky = world + divEtf + divStocks;

    if (risky > 0m)
    {
     decimal refill = Math.Min(missing, risky);
     decimal wShare = world / risky;
     decimal eShare = divEtf / risky;
     decimal sShare = divStocks / risky;

     world -= refill * wShare;
     divEtf -= refill * eShare;
     divStocks -= refill * sShare;
     cash += refill;
    }
   }

   decimal portfolioEnd = cash + world + divEtf + divStocks;

   if (!depleted && portfolioEnd <= 0.01m && year < finalYear)
   {
    depleted = true;
    depletionYear = year;
   }

   string yearStatus = portfolioEnd <= 0.01m ? "Rot" : cash < reserveTarget ? "Gelb" : "Grün";

   result.Years.Add(new YearResult
   {
    Year = year,
    Age1 = age1,
    Age2 = age2,
    LivingCosts = living,
    LivingCostIncrease = livingCostIncrease,
    HealthCareCosts = healthCare,
    HealthCareCostsPerson1 = healthCarePerson1,
    HealthCareCostsPerson2 = healthCarePerson2,
    HealthCareCostsPerson1Monthly = healthCarePerson1 / 12m,
    HealthCareCostsPerson2Monthly = healthCarePerson2 / 12m,
    HealthInsuranceRelevantCapitalIncome = healthInsuranceRelevantCapitalIncome,
    TotalAnnualNeed = totalAnnualNeed,
    ReserveTarget = reserveTarget,
    ReserveActual = cash,
    PensionGross = pension.Gross,
    PensionNet = pension.Net,
    DividendsGross = dividendsGross,
    InterestGross = cashReturn,
    TaxesOnCapital = taxes,
    NetDividends = availableDividends,
    FundingFromPension = fundingFromPension,
    FundingFromDividends = fundingFromDividends,
    FundingFromOtherIncome = fundingFromOtherIncome,
    FundingFromCapital = withdrawn,
    FundingGap = fundingGap,
    WithdrawnFromCapital = withdrawn,
    TotalPortfolioStart = portfolioStart,
    TotalPortfolioEnd = portfolioEnd,
    CashEnd = cash,
    WorldEtfEnd = world,
    DividendEtfEnd = divEtf,
    DividendStocksEnd = divStocks,
    CashReturnContribution = cashReturn,
    WorldReturnContribution = worldGrossReturn,
    DividendEtfReturnContribution = divEtfGrossReturn,
    DividendStocksReturnContribution = divStocksGrossReturn,
    WorldPriceReturnContribution = worldGrossReturn - worldDistribution,
    WorldDistributionContribution = worldDistribution,
    DividendEtfPriceReturnContribution = divEtfGrossReturn - divEtfDistribution,
    DividendEtfDistributionContribution = divEtfDistribution,
    DividendStocksPriceReturnContribution = divStocksGrossReturn - divStocksDistribution,
    DividendStocksDistributionContribution = divStocksDistribution,
    CashNetIncome = cashNetIncome,
    WorldNetIncome = worldNetIncome,
    DividendEtfNetIncome = divEtfNetIncome,
    DividendStocksNetIncome = divStocksNetIncome,
    HouseReserveTarget = houseReserveAnnual,
    CarReserveTarget = carReserveAnnual,
    HealthReserveTarget = healthTarget,
    TravelReserveTarget = travelTarget,
    OtherReserveTarget = otherTarget,
    YearStatus = yearStatus
   });
  }

  result.FinalCapital = result.Years.LastOrDefault()?.TotalPortfolioEnd ?? 0m;
  result.ReachesPlanEnd = !depleted && result.FinalCapital >= 0m;
  result.DepletionYear = depletionYear;
  result.InitialRequiredCash = result.Years.FirstOrDefault()?.ReserveTarget ?? 0m;

  result.MinimumRequiredStartCapital = EstimateMinimumStartCapital(s, allocation, stress);
  result.RequiredAdditionalStartCapital = Math.Max(0m, result.MinimumRequiredStartCapital - s.StartCapital);

  return result;
 }

 public static HealthInsurancePreview CalculateInitialVoluntaryHealthPreview(
  PlannerSettings s,
  StrategyAllocation allocation)
 {
  decimal cash = s.StartCapital * allocation.Cash;
  decimal world = s.StartCapital * allocation.WorldEtf;
  decimal divEtf = s.StartCapital * allocation.DividendEtf;
  decimal divStocks = s.StartCapital * allocation.DividendStocks;

  decimal interest = Math.Max(0m, cash * s.CashInterestRate);
  decimal worldDistribution = Math.Max(0m, world * s.WorldEtfDistribution);
  decimal divEtfDistribution = Math.Max(0m, divEtf * s.DividendEtfDistribution);
  decimal divStocksDistribution = Math.Max(0m, divStocks * s.DividendStocksDistribution);

  decimal relevantCapitalIncome =
   interest +
   worldDistribution +
   divEtfDistribution +
   divStocksDistribution;

  decimal monthlyCapitalIncomePerPerson = relevantCapitalIncome / 2m / 12m;

  decimal person1Monthly = s.Person1Age < s.Person1RetirementAge
   ? CalculateVoluntaryHealthAndCareAnnual(s, monthlyCapitalIncomePerPerson) / 12m
   : 0m;

  decimal person2Monthly = s.Person2Age < s.Person2RetirementAge
   ? CalculateVoluntaryHealthAndCareAnnual(s, monthlyCapitalIncomePerPerson) / 12m
   : 0m;

  return new HealthInsurancePreview(person1Monthly, person2Monthly);
 }

 public static bool ReachesPlanEnd(
  PlannerSettings settings,
  StrategyAllocation allocation,
  bool stress)
 {
  return CalculateCoreWithoutEstimate(settings, allocation, stress) > 0.01m;
 }

 private static decimal EstimateMinimumStartCapital(PlannerSettings s, StrategyAllocation allocation, bool stress)
 {
  decimal low = 0m;
  decimal high = Math.Max(100000m, s.StartCapital > 0m ? s.StartCapital : 100000m);

  for (int i = 0; i < 30; i++)
  {
   var copy = SettingsClone.Clone(s);
   copy.StartCapital = high;
   if (CalculateSimple(copy, allocation, stress))
    break;
   high *= 1.5m;
  }

  for (int i = 0; i < 45; i++)
  {
   decimal mid = (low + high) / 2m;
   var copy = SettingsClone.Clone(s);
   copy.StartCapital = mid;

   if (CalculateSimple(copy, allocation, stress))
    high = mid;
   else
    low = mid;
  }

  return high;
 }

 private static bool CalculateSimple(PlannerSettings s, StrategyAllocation allocation, bool stress)
 {
  var r = CalculateCoreWithoutEstimate(s, allocation, stress);
  return r > 0.01m;
 }

 private static decimal CalculateCoreWithoutEstimate(PlannerSettings s, StrategyAllocation allocation, bool stress)
 {
  decimal cash = s.StartCapital * allocation.Cash;
  decimal world = s.StartCapital * allocation.WorldEtf;
  decimal divEtf = s.StartCapital * allocation.DividendEtf;
  decimal divStocks = s.StartCapital * allocation.DividendStocks;

  int finalYear = Math.Max(
   s.PlanningYear + Math.Max(0, s.Person1EndAge - s.Person1Age),
   s.PlanningYear + Math.Max(0, s.Person2EndAge - s.Person2Age));

  for (int year = s.PlanningYear; year <= finalYear; year++)
  {
   int offset = year - s.PlanningYear;
   int age1 = s.Person1Age + offset;
   int age2 = s.Person2Age + offset;
   decimal factor = Pow(1m + s.InflationRate, offset);
   decimal living = s.MonthlyLivingCosts * 12m * factor;
   decimal healthCare = 0m;

   decimal wr = s.WorldEtfReturn;
   decimal er = s.DividendEtfReturn;
   decimal sr = s.DividendStocksReturn;

   if (stress && s.StressCrashAtStart && offset == 0)
   {
    wr += s.StressCrashPercent; er += s.StressCrashPercent; sr += s.StressCrashPercent;
   }
   if (stress && s.StressSecondCrashEnabled && year == s.StressSecondCrashYear)
   {
    wr += s.StressSecondCrashPercent; er += s.StressSecondCrashPercent; sr += s.StressSecondCrashPercent;
   }

   decimal interest = cash * s.CashInterestRate;
   decimal wd = world * s.WorldEtfDistribution;
   decimal ed = divEtf * s.DividendEtfDistribution;
   decimal sd = divStocks * s.DividendStocksDistribution;

   decimal healthInsuranceRelevantCapitalIncome =
    Math.Max(0m, interest) +
    Math.Max(0m, wd) +
    Math.Max(0m, ed) +
    Math.Max(0m, sd);

   decimal capitalIncomePerPersonMonthly = healthInsuranceRelevantCapitalIncome / 2m / 12m;

   if (age1 < s.Person1RetirementAge)
    healthCare += CalculateVoluntaryHealthAndCareAnnual(s, capitalIncomePerPersonMonthly);

   if (age2 < s.Person2RetirementAge)
    healthCare += CalculateVoluntaryHealthAndCareAnnual(s, capitalIncomePerPersonMonthly);

   decimal taxes = TaxService.CalculateCapitalTax(interest, sd, wd + ed, 0m, s.CapitalGainsAllowance);
   var pension = PensionService.CalculateAnnualPension(s, year, age1, age2);

   cash += interest;
   world = Math.Max(0m, world + world * wr - wd);
   divEtf = Math.Max(0m, divEtf + divEtf * er - ed);
   divStocks = Math.Max(0m, divStocks + divStocks * sr - sd);

   decimal need = living + healthCare + taxes - pension.Net - Math.Max(0m, wd + ed + sd);

   if (need > 0m)
   {
    decimal takeCash = Math.Min(cash, need);
    cash -= takeCash;
    need -= takeCash;
   }

   if (need > 0m)
   {
    decimal risky = world + divEtf + divStocks;
    if (risky <= 0m) return 0m;

    decimal take = Math.Min(risky, need);
    world -= take * (world / risky);
    divEtf -= take * (divEtf / risky);
    divStocks -= take * (divStocks / risky);
   }

   if (cash + world + divEtf + divStocks <= 0.01m && year < finalYear)
    return 0m;
  }

  return cash + world + divEtf + divStocks;
 }

 private static decimal CalculateVoluntaryHealthAndCareAnnual(
  PlannerSettings s,
  decimal monthlyCapitalIncomePerPerson)
 {
  decimal contributionBaseMonthly = Math.Max(
   s.VoluntaryHealthInsuranceMinimumMonthlyIncome,
   Math.Min(
    s.VoluntaryHealthInsuranceMaximumMonthlyIncome,
    Math.Max(0m, monthlyCapitalIncomePerPerson)));

  decimal combinedRate =
   s.VoluntaryHealthInsuranceRate +
   s.VoluntaryHealthInsuranceAdditionalRate +
   s.CareInsuranceChildlessRate;

  return contributionBaseMonthly * combinedRate * 12m;
 }

 private static decimal SumCashFlows(IEnumerable<OneTimeCashFlow> flows, int year, int planningYear, decimal inflation)
 {
  decimal sum = 0m;
  foreach (var flow in flows.Where(x => x.Year == year))
  {
   int offset = Math.Max(0, flow.Year - planningYear);
   sum += flow.AmountToday * Pow(1m + inflation, offset);
  }
  return sum;
 }

 private static decimal Pow(decimal value, int exponent)
 {
  decimal result = 1m;
  for (int i = 0; i < exponent; i++) result *= value;
  return result;
 }
}
