namespace RockefellerFiction;

public static class ProjectionService
{
 public const int HouseMaintenanceBaseYear = 2026;
 public const decimal HouseMaintenanceRateUnder22 = 11.49m;
 public const decimal HouseMaintenanceRateFrom22 = 14.58m;
 public const decimal HouseMaintenanceRateFrom32 = 18.62m;

 public static int GetHouseAgeAtYear(PlannerSettings s, int year)
 {
  return Math.Max(0, s.HouseAge + year - DateTime.Today.Year);
 }

 public static decimal GetHouseMaintenanceRatePerSquareMeter(int houseAge)
 {
  if (houseAge >= 32)
   return HouseMaintenanceRateFrom32;

  if (houseAge >= 22)
   return HouseMaintenanceRateFrom22;

  return HouseMaintenanceRateUnder22;
 }

 public static decimal CalculateHouseMaintenanceExpense(PlannerSettings s, int year)
 {
  bool houseOwned = !s.HouseIncluded || year <= s.HouseSaleYear;

  if (!houseOwned || s.HouseLivingArea <= 0m)
   return 0m;

  int houseAge = GetHouseAgeAtYear(s, year);
  decimal ratePerSquareMeter = GetHouseMaintenanceRatePerSquareMeter(houseAge);
  int yearsFromBaseYear = Math.Max(0, year - HouseMaintenanceBaseYear);
  decimal inflationFactor = Pow(1m + s.InflationRate, yearsFromBaseYear);

  return s.HouseLivingArea * ratePerSquareMeter * inflationFactor;
 }

 public static StrategyAllocation GetInitialAllocation(
  PlannerSettings s,
  StrategyAllocation desiredAllocation)
 {
  if (s.StartCapital <= 0m)
   return desiredAllocation;

  decimal existingCash = Math.Max(0m, s.SecureInvestmentCurrentValue);
  decimal existingWorld = Math.Max(0m, s.WorldEtfCurrentValue);
  decimal existingDividendEtf = Math.Max(0m, s.DividendEtfCurrentValue);
  decimal existingDividendStocks = Math.Max(0m, s.DividendStocksCurrentValue);
  decimal existingInvestmentTotal =
   existingCash +
   existingWorld +
   existingDividendEtf +
   existingDividendStocks;

  if (existingInvestmentTotal >= s.StartCapital)
  {
   return new StrategyAllocation(
    existingCash / s.StartCapital,
    existingWorld / s.StartCapital,
    existingDividendEtf / s.StartCapital,
    existingDividendStocks / s.StartCapital);
  }

  decimal remainingCapital = s.StartCapital - existingInvestmentTotal;

  decimal targetCash = s.StartCapital * desiredAllocation.Cash;
  decimal targetWorld = s.StartCapital * desiredAllocation.WorldEtf;
  decimal targetDividendEtf = s.StartCapital * desiredAllocation.DividendEtf;
  decimal targetDividendStocks = s.StartCapital * desiredAllocation.DividendStocks;

  decimal cashGap = Math.Max(0m, targetCash - existingCash);
  decimal worldGap = Math.Max(0m, targetWorld - existingWorld);
  decimal dividendEtfGap = Math.Max(0m, targetDividendEtf - existingDividendEtf);
  decimal dividendStocksGap = Math.Max(0m, targetDividendStocks - existingDividendStocks);
  decimal totalGap =
   cashGap +
   worldGap +
   dividendEtfGap +
   dividendStocksGap;

  decimal cash;
  decimal world;
  decimal dividendEtf;
  decimal dividendStocks;

  if (totalGap <= 0m)
  {
   cash = existingCash + remainingCapital;
   world = existingWorld;
   dividendEtf = existingDividendEtf;
   dividendStocks = existingDividendStocks;
  }
  else
  {
   cash = existingCash + remainingCapital * cashGap / totalGap;
   world = existingWorld + remainingCapital * worldGap / totalGap;
   dividendEtf = existingDividendEtf + remainingCapital * dividendEtfGap / totalGap;
   dividendStocks = existingDividendStocks + remainingCapital * dividendStocksGap / totalGap;
  }

  return new StrategyAllocation(
   cash / s.StartCapital,
   world / s.StartCapital,
   dividendEtf / s.StartCapital,
   dividendStocks / s.StartCapital);
 }

 public static ProjectionResult Calculate(PlannerSettings s, StrategyAllocation allocation, bool stress)
 {
  var result = new ProjectionResult();

  StrategyAllocation initialAllocation = GetInitialAllocation(s, allocation);
  decimal cash = s.StartCapital * initialAllocation.Cash;
  decimal world = s.StartCapital * initialAllocation.WorldEtf;
  decimal divEtf = s.StartCapital * initialAllocation.DividendEtf;
  decimal divStocks = s.StartCapital * initialAllocation.DividendStocks;

  decimal worldNewInvestment = Math.Max(0m, world - s.WorldEtfCurrentValue);
  decimal divEtfNewInvestment = Math.Max(0m, divEtf - s.DividendEtfCurrentValue);
  decimal divStocksNewInvestment = Math.Max(0m, divStocks - s.DividendStocksCurrentValue);

  decimal worldCostBasis =
   EstimateInitialCostBasis(
    s.WorldEtfCurrentValue,
    s.WorldEtfCurrentValue,
    s.WorldEtfStartYear,
    s.WorldEtfHistoricalReturn,
    s.PlanningYear) +
   worldNewInvestment;
  decimal divEtfCostBasis =
   EstimateInitialCostBasis(
    s.DividendEtfCurrentValue,
    s.DividendEtfCurrentValue,
    s.DividendEtfStartYear,
    s.DividendEtfHistoricalReturn,
    s.PlanningYear) +
   divEtfNewInvestment;
  decimal divStocksCostBasis =
   EstimateInitialCostBasis(
    s.DividendStocksCurrentValue,
    s.DividendStocksCurrentValue,
    s.DividendStocksStartYear,
    s.DividendStocksHistoricalReturn,
    s.PlanningYear) +
   divStocksNewInvestment;

  decimal equityFundAdvanceLumpSumCarry = 0m;
  decimal pendingWorldAdvanceLumpSum = 0m;
  decimal pendingDividendEtfAdvanceLumpSum = 0m;
  decimal stockLossCarryForward = 0m;
  decimal otherLossCarryForward = 0m;

  int planningAgePerson1 = GetPlanningAge(s.Person1Age, s.PlanningYear);
  int planningAgePerson2 = GetPlanningAge(s.Person2Age, s.PlanningYear);

  int finalYear = s.HouseholdPersonCount == 1
   ? s.PlanningYear + Math.Max(0, s.Person1EndAge - planningAgePerson1)
   : Math.Max(
    s.PlanningYear + Math.Max(0, s.Person1EndAge - planningAgePerson1),
    s.PlanningYear + Math.Max(0, s.Person2EndAge - planningAgePerson2));

  bool depleted = false;
  int? depletionYear = null;

  for (int year = s.PlanningYear; year <= finalYear; year++)
  {
   int offset = year - s.PlanningYear;
   int age1 = planningAgePerson1 + offset;
   int age2 = planningAgePerson2 + offset;
   decimal openingStockLossCarryForward = stockLossCarryForward;
   decimal openingOtherLossCarryForward = otherLossCarryForward;

   HealthInsuranceProjectionParameters healthInsuranceParameters =
    PensionService.CalculateHealthInsuranceProjectionParameters(s, year, stress);

   decimal worldAdvanceLumpSumTaxableThisYear = pendingWorldAdvanceLumpSum;
   decimal dividendEtfAdvanceLumpSumTaxableThisYear = pendingDividendEtfAdvanceLumpSum;
   decimal equityFundAdvanceLumpSumTaxableThisYear =
    worldAdvanceLumpSumTaxableThisYear + dividendEtfAdvanceLumpSumTaxableThisYear;
   equityFundAdvanceLumpSumCarry += equityFundAdvanceLumpSumTaxableThisYear;
   pendingWorldAdvanceLumpSum = 0m;
   pendingDividendEtfAdvanceLumpSum = 0m;

   int yearsFromCurrentYear = Math.Max(0, year - DateTime.Today.Year);
   int previousYearOffsetFromCurrentYear = Math.Max(0, year - 1 - DateTime.Today.Year);
   decimal inflationFactor = Pow(1m + s.InflationRate, yearsFromCurrentYear);
   decimal living = s.MonthlyLivingCosts * 12m * inflationFactor;
   decimal previousLiving = s.MonthlyLivingCosts * 12m *
    Pow(1m + s.InflationRate, previousYearOffsetFromCurrentYear);
   decimal livingCostIncrease = Math.Max(0m, living - previousLiving);
   decimal healthCare = 0m;
   decimal healthCarePerson1 = 0m;
   decimal healthCarePerson2 = 0m;
   decimal healthInsuranceRelevantCapitalIncome = 0m;
   decimal totalAnnualNeed = living;

   decimal houseMaintenanceExpense =
    CalculateHouseMaintenanceExpense(s, year);

   int carReplacementIntervalYears = Math.Max(1, s.CarReplacementYears);
   int yearsSincePlanningStart = year - s.PlanningYear;
   bool carReplacementDue =
    yearsSincePlanningStart > 0 &&
    yearsSincePlanningStart % carReplacementIntervalYears == 0;
   decimal carReplacementExpense = carReplacementDue
    ? s.CarReplacementValue * inflationFactor
    : 0m;

   decimal healthTarget = s.HealthReserveTarget * inflationFactor;
   decimal travelTarget = s.TravelReserveTarget * inflationFactor;
   decimal otherTarget = s.OtherReserveTarget * inflationFactor;

   decimal portfolioStart = cash + world + divEtf + divStocks;

   decimal cashReturn = cash * s.CashInterestRate;
   decimal wr = s.WorldEtfReturn;
   decimal er = s.DividendEtfReturn;
   decimal sr = s.DividendStocksReturn;

   if (stress && s.StressCrashAtStart && offset == 0)
   {
    wr = s.StressCrashPercent;
    er = s.StressCrashPercent;
    sr = s.StressCrashPercent;
   }

   if (stress && s.StressSecondCrashEnabled && year == s.StressSecondCrashYear)
   {
    wr = s.StressSecondCrashPercent;
    er = s.StressSecondCrashPercent;
    sr = s.StressSecondCrashPercent;
   }

   decimal worldGrossReturn = world * wr;
   decimal divEtfGrossReturn = divEtf * er;
   decimal divStocksGrossReturn = divStocks * sr;

   decimal worldDistribution = world * s.WorldEtfDistribution;
   decimal divEtfDistribution = divEtf * s.DividendEtfDistribution;
   decimal divStocksDistribution = divStocks * s.DividendStocksDistribution;
   decimal dividendsGross = worldDistribution + divEtfDistribution + divStocksDistribution;

   decimal worldValueBeforeSales = Math.Max(0m, world + worldGrossReturn - worldDistribution);
   decimal divEtfValueBeforeSales = Math.Max(0m, divEtf + divEtfGrossReturn - divEtfDistribution);

   decimal currentYearWorldAdvanceLumpSum =
    TaxService.CalculateEquityFundAdvanceLumpSum(
     world,
     worldValueBeforeSales,
     worldDistribution,
     s.AdvanceLumpSumBaseRate);
   decimal currentYearDividendEtfAdvanceLumpSum =
    TaxService.CalculateEquityFundAdvanceLumpSum(
     divEtf,
     divEtfValueBeforeSales,
     divEtfDistribution,
     s.AdvanceLumpSumBaseRate);

   healthInsuranceRelevantCapitalIncome =
    Math.Max(0m, cashReturn) +
    Math.Max(0m, worldDistribution) +
    Math.Max(0m, divEtfDistribution) +
    Math.Max(0m, divStocksDistribution);

   decimal capitalIncomePerPersonAnnual =
    healthInsuranceRelevantCapitalIncome / Math.Max(1, s.HouseholdPersonCount);
   decimal capitalIncomePerPersonMonthly = capitalIncomePerPersonAnnual / 12m;

   var pension = PensionService.CalculateAnnualPension(s, year, age1, age2, stress);

   if (age1 < s.Person1RetirementAge)
   {
    healthCarePerson1 = CalculateVoluntaryHealthAndCareAnnual(
     s,
     healthInsuranceParameters,
     capitalIncomePerPersonMonthly);
   }
   else if (age1 <= s.Person1EndAge && !s.KvdrPerson1)
   {
    healthCarePerson1 = PensionService.CalculateVoluntaryRetireeAdditionalHealthAndCareAnnual(
     s,
     healthInsuranceParameters,
     pension.Person1Gross,
     capitalIncomePerPersonAnnual);
   }

   if (s.HouseholdPersonCount == 2 &&
       year >= GetPerson2WorkEndYear(s))
   {
    if (age2 < s.Person2RetirementAge)
    {
     healthCarePerson2 = CalculateVoluntaryHealthAndCareAnnual(
      s,
      healthInsuranceParameters,
      capitalIncomePerPersonMonthly);
    }
    else if (age2 <= s.Person2EndAge && !s.KvdrPerson2)
    {
     healthCarePerson2 = PensionService.CalculateVoluntaryRetireeAdditionalHealthAndCareAnnual(
      s,
      healthInsuranceParameters,
      pension.Person2Gross,
      capitalIncomePerPersonAnnual);
    }
   }

   healthCare = healthCarePerson1 + healthCarePerson2;
   decimal recurringAnnualNeed =
    living +
    healthCare +
    houseMaintenanceExpense;
   totalAnnualNeed =
    recurringAnnualNeed +
    carReplacementExpense;

   decimal reserveTarget =
    recurringAnnualNeed * s.ReserveYears +
    healthTarget +
    travelTarget +
    otherTarget;

   decimal realizedStockGains = 0m;
   decimal realizedEquityFundGains = 0m;

   decimal taxes = TaxService.CalculateCapitalTaxWithFavorableCheck(
    s,
    year,
    age1,
    age2,
    pension.TaxableIncome1,
    pension.TaxableIncome2,
    cashReturn,
    divStocksDistribution,
    worldDistribution + divEtfDistribution + equityFundAdvanceLumpSumTaxableThisYear,
    realizedStockGains,
    realizedEquityFundGains,
    GetEffectiveCapitalGainsAllowance(s, year),
    openingStockLossCarryForward,
    openingOtherLossCarryForward,
    out _,
    out _);

   decimal taxableCashWeight = Math.Max(0m, cashReturn);
   decimal taxableWorldWeight =
    (Math.Max(0m, worldDistribution) + worldAdvanceLumpSumTaxableThisYear) * 0.70m;
   decimal taxableDivEtfWeight =
    (Math.Max(0m, divEtfDistribution) + dividendEtfAdvanceLumpSumTaxableThisYear) * 0.70m;
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

   decimal person2NetEmploymentIncome = CalculatePerson2NetEmploymentIncome(s, year);

   decimal oneTimeIncome = SumCashFlows(s.OneTimeIncome, year, s.PlanningYear, s.InflationRate);
   if (s.HouseIncluded && s.HouseSaleYear == year)
    oneTimeIncome += s.HouseNetSaleProceeds;

   decimal oneTimeExpenses = SumCashFlows(s.OneTimeExpenses, year, s.PlanningYear, s.InflationRate);

   world = worldValueBeforeSales;
   divEtf = divEtfValueBeforeSales;
   divStocks = Math.Max(0m, divStocks + divStocksGrossReturn - divStocksDistribution);
   cash = Math.Max(0m, cash + cashReturn);

   if (world <= 0m)
    worldCostBasis = 0m;
   if (divEtf <= 0m)
    divEtfCostBasis = 0m;
   if (divStocks <= 0m)
    divStocksCostBasis = 0m;

   decimal requiredForYear = totalAnnualNeed + oneTimeExpenses + taxes;

   decimal fundingFromPension = Math.Min(Math.Max(0m, pension.Net), requiredForYear);
   decimal remainingAfterPension = Math.Max(0m, requiredForYear - fundingFromPension);

   decimal fundingFromPerson2Income =
    Math.Min(Math.Max(0m, person2NetEmploymentIncome), remainingAfterPension);
   decimal remainingAfterPerson2Income =
    Math.Max(0m, remainingAfterPension - fundingFromPerson2Income);

   decimal fundingFromOtherIncome = Math.Min(Math.Max(0m, oneTimeIncome), remainingAfterPerson2Income);
   decimal remainingAfterOtherIncome = Math.Max(0m, remainingAfterPerson2Income - fundingFromOtherIncome);

   cash += Math.Max(0m, pension.Net - fundingFromPension);
   cash += Math.Max(0m, person2NetEmploymentIncome - fundingFromPerson2Income);
   cash += Math.Max(0m, oneTimeIncome - fundingFromOtherIncome);

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
    decimal sold = SellRiskyAssets(
     ref world,
     ref worldCostBasis,
     ref divEtf,
     ref divEtfCostBasis,
     ref divStocks,
     ref divStocksCostBasis,
     ref equityFundAdvanceLumpSumCarry,
     need,
     out decimal realizedStockGain,
     out decimal realizedEquityFundGain);

    realizedStockGains += realizedStockGain;
    realizedEquityFundGains += realizedEquityFundGain;
    withdrawn += sold;
    need -= sold;
   }

   decimal fundingGap = Math.Max(0m, need);

   bool stockYearNegative = wr < 0m || er < 0m || sr < 0m;
   if (s.AutoRefillReserve && cash < reserveTarget &&
       (!s.UseReserveOnNegativeStockYear || !stockYearNegative))
   {
    decimal refill = SellRiskyAssets(
     ref world,
     ref worldCostBasis,
     ref divEtf,
     ref divEtfCostBasis,
     ref divStocks,
     ref divStocksCostBasis,
     ref equityFundAdvanceLumpSumCarry,
     reserveTarget - cash,
     out decimal realizedStockGain,
     out decimal realizedEquityFundGain);

    realizedStockGains += realizedStockGain;
    realizedEquityFundGains += realizedEquityFundGain;
    cash += refill;
   }

   taxes = SettleCapitalTaxAfterSales(
    s,
    year,
    age1,
    age2,
    pension.TaxableIncome1,
    pension.TaxableIncome2,
    cashReturn,
    divStocksDistribution,
    worldDistribution + divEtfDistribution + equityFundAdvanceLumpSumTaxableThisYear,
    ref cash,
    ref world,
    ref worldCostBasis,
    ref divEtf,
    ref divEtfCostBasis,
    ref divStocks,
    ref divStocksCostBasis,
    ref equityFundAdvanceLumpSumCarry,
    ref realizedStockGains,
    ref realizedEquityFundGains,
    ref withdrawn,
    taxes,
    openingStockLossCarryForward,
    openingOtherLossCarryForward,
    out decimal taxFundingGap);

   fundingGap += taxFundingGap;

   for (int pass = 0; pass < 32; pass++)
   {
    decimal realizedCapitalIncomeForHealthInsurance =
     Math.Max(0m, realizedStockGains) +
     Math.Max(0m, realizedEquityFundGains) * 0.70m;

    decimal finalCapitalIncomePerPersonAnnual =
     (healthInsuranceRelevantCapitalIncome + realizedCapitalIncomeForHealthInsurance) /
     Math.Max(1, s.HouseholdPersonCount);
    decimal finalCapitalIncomePerPersonMonthly =
     finalCapitalIncomePerPersonAnnual / 12m;

    decimal targetHealthCarePerson1 = 0m;
    decimal targetHealthCarePerson2 = 0m;

    if (age1 < s.Person1RetirementAge)
    {
     targetHealthCarePerson1 = CalculateVoluntaryHealthAndCareAnnual(
      s,
      healthInsuranceParameters,
      finalCapitalIncomePerPersonMonthly);
    }
    else if (age1 <= s.Person1EndAge && !s.KvdrPerson1)
    {
     targetHealthCarePerson1 = PensionService.CalculateVoluntaryRetireeAdditionalHealthAndCareAnnual(
      s,
      healthInsuranceParameters,
      pension.Person1Gross,
      finalCapitalIncomePerPersonAnnual);
    }

    if (s.HouseholdPersonCount == 2 &&
        year >= GetPerson2WorkEndYear(s))
    {
     if (age2 < s.Person2RetirementAge)
     {
      targetHealthCarePerson2 = CalculateVoluntaryHealthAndCareAnnual(
       s,
       healthInsuranceParameters,
       finalCapitalIncomePerPersonMonthly);
     }
     else if (age2 <= s.Person2EndAge && !s.KvdrPerson2)
     {
      targetHealthCarePerson2 = PensionService.CalculateVoluntaryRetireeAdditionalHealthAndCareAnnual(
       s,
       healthInsuranceParameters,
       pension.Person2Gross,
       finalCapitalIncomePerPersonAnnual);
     }
    }

    decimal additionalHealthCare =
     Math.Max(0m, targetHealthCarePerson1 - healthCarePerson1) +
     Math.Max(0m, targetHealthCarePerson2 - healthCarePerson2);

    healthCarePerson1 = Math.Max(healthCarePerson1, targetHealthCarePerson1);
    healthCarePerson2 = Math.Max(healthCarePerson2, targetHealthCarePerson2);
    healthCare = healthCarePerson1 + healthCarePerson2;
    recurringAnnualNeed =
     living +
     healthCare +
     houseMaintenanceExpense;
    totalAnnualNeed =
     recurringAnnualNeed +
     carReplacementExpense;
    reserveTarget =
     recurringAnnualNeed * s.ReserveYears +
     healthTarget +
     travelTarget +
     otherTarget;

    if (additionalHealthCare > 0.01m)
    {
     decimal remainingHealthCare = additionalHealthCare;

     decimal fromCash = Math.Min(cash, remainingHealthCare);
     cash -= fromCash;
     remainingHealthCare -= fromCash;
     withdrawn += fromCash;

     if (remainingHealthCare > 0.01m)
     {
      decimal sold = SellRiskyAssets(
       ref world,
       ref worldCostBasis,
       ref divEtf,
       ref divEtfCostBasis,
       ref divStocks,
       ref divStocksCostBasis,
       ref equityFundAdvanceLumpSumCarry,
       remainingHealthCare,
       out decimal realizedStockGain,
       out decimal realizedEquityFundGain);

      realizedStockGains += realizedStockGain;
      realizedEquityFundGains += realizedEquityFundGain;
      withdrawn += sold;
      remainingHealthCare -= sold;
     }

     fundingGap += Math.Max(0m, remainingHealthCare);
    }

    if (s.AutoRefillReserve && cash < reserveTarget &&
        (!s.UseReserveOnNegativeStockYear || !stockYearNegative))
    {
     decimal refill = SellRiskyAssets(
      ref world,
      ref worldCostBasis,
      ref divEtf,
      ref divEtfCostBasis,
      ref divStocks,
      ref divStocksCostBasis,
      ref equityFundAdvanceLumpSumCarry,
      reserveTarget - cash,
      out decimal realizedStockGain,
      out decimal realizedEquityFundGain);

     realizedStockGains += realizedStockGain;
     realizedEquityFundGains += realizedEquityFundGain;
     cash += refill;
    }

    if (fundingGap > 0.01m)
     break;

    decimal taxesBeforeHealthSettlement = taxes;
    taxes = SettleCapitalTaxAfterSales(
     s,
     year,
     age1,
     age2,
     pension.TaxableIncome1,
     pension.TaxableIncome2,
     cashReturn,
     divStocksDistribution,
     worldDistribution + divEtfDistribution + equityFundAdvanceLumpSumTaxableThisYear,
     ref cash,
     ref world,
     ref worldCostBasis,
     ref divEtf,
     ref divEtfCostBasis,
     ref divStocks,
     ref divStocksCostBasis,
     ref equityFundAdvanceLumpSumCarry,
     ref realizedStockGains,
     ref realizedEquityFundGains,
     ref withdrawn,
     taxes,
     openingStockLossCarryForward,
     openingOtherLossCarryForward,
     out decimal additionalTaxFundingGap);

    fundingGap += additionalTaxFundingGap;

    if (additionalHealthCare <= 0.01m &&
        Math.Abs(taxes - taxesBeforeHealthSettlement) <= 0.01m)
     break;
   }

   _ = TaxService.CalculateCapitalTaxWithFavorableCheck(
    s,
    year,
    age1,
    age2,
    pension.TaxableIncome1,
    pension.TaxableIncome2,
    cashReturn,
    divStocksDistribution,
    worldDistribution + divEtfDistribution + equityFundAdvanceLumpSumTaxableThisYear,
    realizedStockGains,
    realizedEquityFundGains,
    GetEffectiveCapitalGainsAllowance(s, year),
    openingStockLossCarryForward,
    openingOtherLossCarryForward,
    out stockLossCarryForward,
    out otherLossCarryForward);

   healthInsuranceRelevantCapitalIncome +=
    Math.Max(0m, realizedStockGains) +
    Math.Max(0m, realizedEquityFundGains) * 0.70m;
   requiredForYear = totalAnnualNeed + oneTimeExpenses + taxes;

   pendingWorldAdvanceLumpSum = worldValueBeforeSales > 0m
    ? currentYearWorldAdvanceLumpSum * Math.Min(1m, world / worldValueBeforeSales)
    : 0m;
   pendingDividendEtfAdvanceLumpSum = divEtfValueBeforeSales > 0m
    ? currentYearDividendEtfAdvanceLumpSum * Math.Min(1m, divEtf / divEtfValueBeforeSales)
    : 0m;

   decimal portfolioEnd = cash + world + divEtf + divStocks;

   if (!depleted && (fundingGap > 0.01m || (portfolioEnd <= 0.01m && year < finalYear)))
   {
    depleted = true;
    depletionYear = year;
   }

   string yearStatus =
    fundingGap > 0.01m || (portfolioEnd <= 0.01m && year < finalYear)
     ? "Rot"
     : year == finalYear
      ? "Grün"
      : cash < reserveTarget
       ? "Gelb"
       : "Grün";

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
    HealthInsuranceMinimumMonthlyIncomeApplied = healthInsuranceParameters.MinimumMonthlyIncome,
    HealthInsuranceMaximumMonthlyIncomeApplied = healthInsuranceParameters.MaximumMonthlyIncome,
    HealthInsuranceAdditionalRateApplied = healthInsuranceParameters.AdditionalRate,
    CareInsuranceRateApplied = healthInsuranceParameters.CareRate,
    TotalAnnualNeed = totalAnnualNeed,
    ReserveTarget = reserveTarget,
    ReserveActual = cash,
    PensionGross = pension.Gross,
    PensionPerson1Gross = pension.Person1Gross,
    PensionPerson2Gross = pension.Person2Gross,
    PensionNet = pension.Net,
    PensionHealthAndCareDeductions = pension.HealthAndCareDeductions,
    PensionHealthAndCareDeductionsPerson1 = pension.HealthAndCareDeductions1,
    PensionHealthAndCareDeductionsPerson2 = pension.HealthAndCareDeductions2,
    PensionTaxableIncomePerson1 = pension.TaxableIncome1,
    PensionTaxableIncomePerson2 = pension.TaxableIncome2,
    PensionTaxableSharePerson1 = pension.TaxableShare1,
    PensionTaxableSharePerson2 = pension.TaxableShare2,
    PensionFixedTaxFreeAmountPerson1 = pension.FixedTaxFreePensionAmount1,
    PensionFixedTaxFreeAmountPerson2 = pension.FixedTaxFreePensionAmount2,
    PensionStartYearPerson1 = pension.PensionStartYear1,
    PensionStartYearPerson2 = pension.PensionStartYear2,
    PensionIncomeTaxBeforeSurcharges = pension.IncomeTaxBeforeSurcharges,
    PensionSolidaritySurcharge = pension.SolidaritySurcharge,
    PensionChurchTax = pension.ChurchTax,
    PensionTaxTariffFactor = pension.TaxTariffFactor,
    PensionProjectedBasicAllowance = pension.ProjectedBasicAllowance,
    PensionIncomeTax = pension.IncomeTax,
    DividendsGross = dividendsGross,
    InterestGross = cashReturn,
    TaxesOnCapital = taxes,
    CapitalGainsAllowanceApplied = GetEffectiveCapitalGainsAllowance(s, year),
    AdvanceLumpSumTaxableThisYear = equityFundAdvanceLumpSumTaxableThisYear,
    AdvanceLumpSumCalculatedForNextYear = pendingWorldAdvanceLumpSum + pendingDividendEtfAdvanceLumpSum,
    RealizedStockGains = realizedStockGains,
    RealizedEquityFundGains = realizedEquityFundGains,
    StockLossCarryForward = stockLossCarryForward,
    OtherLossCarryForward = otherLossCarryForward,
    OneTimeIncome = oneTimeIncome,
    OneTimeExpenses = oneTimeExpenses,
    RequiredForYear = requiredForYear,
    NetDividends = availableDividends,
    FundingFromPension = fundingFromPension,
    FundingFromDividends = fundingFromDividends,
    Person2NetEmploymentIncome = person2NetEmploymentIncome,
    FundingFromPerson2Income = fundingFromPerson2Income,
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
    HouseMaintenanceExpense = houseMaintenanceExpense,
    CarReplacementExpense = carReplacementExpense,
    HouseReserveTarget = 0m,
    CarReserveTarget = 0m,
    HealthReserveTarget = healthTarget,
    TravelReserveTarget = travelTarget,
    OtherReserveTarget = otherTarget,
    YearStatus = yearStatus
   });
  }

  result.FinalCapital = result.Years.LastOrDefault()?.TotalPortfolioEnd ?? 0m;
  result.ReachesPlanEnd = !depleted;
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
  StrategyAllocation initialAllocation = GetInitialAllocation(s, allocation);
  decimal cash = s.StartCapital * initialAllocation.Cash;
  decimal world = s.StartCapital * initialAllocation.WorldEtf;
  decimal divEtf = s.StartCapital * initialAllocation.DividendEtf;
  decimal divStocks = s.StartCapital * initialAllocation.DividendStocks;

  decimal interest = Math.Max(0m, cash * s.CashInterestRate);
  decimal worldDistribution = Math.Max(0m, world * s.WorldEtfDistribution);
  decimal divEtfDistribution = Math.Max(0m, divEtf * s.DividendEtfDistribution);
  decimal divStocksDistribution = Math.Max(0m, divStocks * s.DividendStocksDistribution);

  decimal relevantCapitalIncome =
   interest +
   worldDistribution +
   divEtfDistribution +
   divStocksDistribution;

  decimal monthlyCapitalIncomePerPerson =
   relevantCapitalIncome / Math.Max(1, s.HouseholdPersonCount) / 12m;

  int planningAgePerson1 = GetPlanningAge(s.Person1Age, s.PlanningYear);
  int planningAgePerson2 = GetPlanningAge(s.Person2Age, s.PlanningYear);

  HealthInsuranceProjectionParameters healthInsuranceParameters =
   PensionService.CalculateHealthInsuranceProjectionParameters(
    s,
    s.PlanningYear,
    false);

  decimal person1Monthly = planningAgePerson1 < s.Person1RetirementAge
   ? CalculateVoluntaryHealthAndCareAnnual(
      s,
      healthInsuranceParameters,
      monthlyCapitalIncomePerPerson) / 12m
   : 0m;

  decimal person2Monthly =
   s.HouseholdPersonCount == 2 &&
   s.PlanningYear >= GetPerson2WorkEndYear(s) &&
   planningAgePerson2 < s.Person2RetirementAge
    ? CalculateVoluntaryHealthAndCareAnnual(
       s,
       healthInsuranceParameters,
       monthlyCapitalIncomePerPerson) / 12m
    : 0m;

  return new HealthInsurancePreview(person1Monthly, person2Monthly);
 }

 public static bool ReachesPlanEnd(
  PlannerSettings settings,
  StrategyAllocation allocation,
  bool stress)
 {
  return CalculateCoreWithoutEstimate(settings, allocation, stress) >= 0m;
 }

 private static decimal EstimateMinimumStartCapital(PlannerSettings s, StrategyAllocation allocation, bool stress)
 {
  decimal existingInvestmentTotal =
   s.SecureInvestmentCurrentValue +
   s.WorldEtfCurrentValue +
   s.DividendEtfCurrentValue +
   s.DividendStocksCurrentValue;

  decimal low = existingInvestmentTotal;
  decimal high = Math.Max(
   existingInvestmentTotal,
   Math.Max(100000m, s.StartCapital > 0m ? s.StartCapital : 100000m));

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
  return r >= 0m;
 }

 private static decimal CalculateCoreWithoutEstimate(PlannerSettings s, StrategyAllocation allocation, bool stress)
 {
  StrategyAllocation initialAllocation = GetInitialAllocation(s, allocation);
  decimal cash = s.StartCapital * initialAllocation.Cash;
  decimal world = s.StartCapital * initialAllocation.WorldEtf;
  decimal divEtf = s.StartCapital * initialAllocation.DividendEtf;
  decimal divStocks = s.StartCapital * initialAllocation.DividendStocks;

  decimal worldNewInvestment = Math.Max(0m, world - s.WorldEtfCurrentValue);
  decimal divEtfNewInvestment = Math.Max(0m, divEtf - s.DividendEtfCurrentValue);
  decimal divStocksNewInvestment = Math.Max(0m, divStocks - s.DividendStocksCurrentValue);

  decimal worldCostBasis =
   EstimateInitialCostBasis(
    s.WorldEtfCurrentValue,
    s.WorldEtfCurrentValue,
    s.WorldEtfStartYear,
    s.WorldEtfHistoricalReturn,
    s.PlanningYear) +
   worldNewInvestment;
  decimal divEtfCostBasis =
   EstimateInitialCostBasis(
    s.DividendEtfCurrentValue,
    s.DividendEtfCurrentValue,
    s.DividendEtfStartYear,
    s.DividendEtfHistoricalReturn,
    s.PlanningYear) +
   divEtfNewInvestment;
  decimal divStocksCostBasis =
   EstimateInitialCostBasis(
    s.DividendStocksCurrentValue,
    s.DividendStocksCurrentValue,
    s.DividendStocksStartYear,
    s.DividendStocksHistoricalReturn,
    s.PlanningYear) +
   divStocksNewInvestment;

  decimal equityFundAdvanceLumpSumCarry = 0m;
  decimal pendingWorldAdvanceLumpSum = 0m;
  decimal pendingDividendEtfAdvanceLumpSum = 0m;
  decimal stockLossCarryForward = 0m;
  decimal otherLossCarryForward = 0m;

  int planningAgePerson1 = GetPlanningAge(s.Person1Age, s.PlanningYear);
  int planningAgePerson2 = GetPlanningAge(s.Person2Age, s.PlanningYear);

  int finalYear = s.HouseholdPersonCount == 1
   ? s.PlanningYear + Math.Max(0, s.Person1EndAge - planningAgePerson1)
   : Math.Max(
    s.PlanningYear + Math.Max(0, s.Person1EndAge - planningAgePerson1),
    s.PlanningYear + Math.Max(0, s.Person2EndAge - planningAgePerson2));

  for (int year = s.PlanningYear; year <= finalYear; year++)
  {
   int offset = year - s.PlanningYear;
   int age1 = planningAgePerson1 + offset;
   int age2 = planningAgePerson2 + offset;
   decimal openingStockLossCarryForward = stockLossCarryForward;
   decimal openingOtherLossCarryForward = otherLossCarryForward;

   HealthInsuranceProjectionParameters healthInsuranceParameters =
    PensionService.CalculateHealthInsuranceProjectionParameters(s, year, stress);

   decimal worldAdvanceLumpSumTaxableThisYear = pendingWorldAdvanceLumpSum;
   decimal dividendEtfAdvanceLumpSumTaxableThisYear = pendingDividendEtfAdvanceLumpSum;
   decimal equityFundAdvanceLumpSumTaxableThisYear =
    worldAdvanceLumpSumTaxableThisYear + dividendEtfAdvanceLumpSumTaxableThisYear;
   equityFundAdvanceLumpSumCarry += equityFundAdvanceLumpSumTaxableThisYear;
   pendingWorldAdvanceLumpSum = 0m;
   pendingDividendEtfAdvanceLumpSum = 0m;

   int yearsFromCurrentYear = Math.Max(0, year - DateTime.Today.Year);
   decimal factor = Pow(1m + s.InflationRate, yearsFromCurrentYear);
   decimal living = s.MonthlyLivingCosts * 12m * factor;
   decimal healthCare = 0m;

   decimal houseMaintenanceExpense =
    CalculateHouseMaintenanceExpense(s, year);

   int carReplacementIntervalYears = Math.Max(1, s.CarReplacementYears);
   int yearsSincePlanningStart = year - s.PlanningYear;
   bool carReplacementDue =
    yearsSincePlanningStart > 0 &&
    yearsSincePlanningStart % carReplacementIntervalYears == 0;
   decimal carReplacementExpense = carReplacementDue
    ? s.CarReplacementValue * factor
    : 0m;

   decimal healthTarget = s.HealthReserveTarget * factor;
   decimal travelTarget = s.TravelReserveTarget * factor;
   decimal otherTarget = s.OtherReserveTarget * factor;

   decimal wr = s.WorldEtfReturn;
   decimal er = s.DividendEtfReturn;
   decimal sr = s.DividendStocksReturn;

   if (stress && s.StressCrashAtStart && offset == 0)
   {
    wr = s.StressCrashPercent;
    er = s.StressCrashPercent;
    sr = s.StressCrashPercent;
   }

   if (stress && s.StressSecondCrashEnabled && year == s.StressSecondCrashYear)
   {
    wr = s.StressSecondCrashPercent;
    er = s.StressSecondCrashPercent;
    sr = s.StressSecondCrashPercent;
   }

   decimal interest = cash * s.CashInterestRate;
   decimal wd = world * s.WorldEtfDistribution;
   decimal ed = divEtf * s.DividendEtfDistribution;
   decimal sd = divStocks * s.DividendStocksDistribution;

   decimal worldValueBeforeSales = Math.Max(0m, world + world * wr - wd);
   decimal divEtfValueBeforeSales = Math.Max(0m, divEtf + divEtf * er - ed);

   decimal currentYearWorldAdvanceLumpSum =
    TaxService.CalculateEquityFundAdvanceLumpSum(
     world,
     worldValueBeforeSales,
     wd,
     s.AdvanceLumpSumBaseRate);
   decimal currentYearDividendEtfAdvanceLumpSum =
    TaxService.CalculateEquityFundAdvanceLumpSum(
     divEtf,
     divEtfValueBeforeSales,
     ed,
     s.AdvanceLumpSumBaseRate);

   decimal healthInsuranceRelevantCapitalIncome =
    Math.Max(0m, interest) +
    Math.Max(0m, wd) +
    Math.Max(0m, ed) +
    Math.Max(0m, sd);

   decimal capitalIncomePerPersonAnnual =
    healthInsuranceRelevantCapitalIncome / Math.Max(1, s.HouseholdPersonCount);
   decimal capitalIncomePerPersonMonthly = capitalIncomePerPersonAnnual / 12m;

   var pension = PensionService.CalculateAnnualPension(s, year, age1, age2, stress);

   if (age1 < s.Person1RetirementAge)
   {
    healthCare += CalculateVoluntaryHealthAndCareAnnual(
     s,
     healthInsuranceParameters,
     capitalIncomePerPersonMonthly);
   }
   else if (age1 <= s.Person1EndAge && !s.KvdrPerson1)
   {
    healthCare += PensionService.CalculateVoluntaryRetireeAdditionalHealthAndCareAnnual(
     s,
     healthInsuranceParameters,
     pension.Person1Gross,
     capitalIncomePerPersonAnnual);
   }

   if (s.HouseholdPersonCount == 2 &&
       year >= GetPerson2WorkEndYear(s))
   {
    if (age2 < s.Person2RetirementAge)
    {
     healthCare += CalculateVoluntaryHealthAndCareAnnual(
      s,
      healthInsuranceParameters,
      capitalIncomePerPersonMonthly);
    }
    else if (age2 <= s.Person2EndAge && !s.KvdrPerson2)
    {
     healthCare += PensionService.CalculateVoluntaryRetireeAdditionalHealthAndCareAnnual(
      s,
      healthInsuranceParameters,
      pension.Person2Gross,
      capitalIncomePerPersonAnnual);
    }
   }

   decimal recurringAnnualNeed =
    living +
    healthCare +
    houseMaintenanceExpense;
   decimal totalAnnualNeed =
    recurringAnnualNeed +
    carReplacementExpense;
   decimal reserveTarget =
    recurringAnnualNeed * s.ReserveYears +
    healthTarget +
    travelTarget +
    otherTarget;

   decimal realizedStockGains = 0m;
   decimal realizedEquityFundGains = 0m;

   decimal taxes = TaxService.CalculateCapitalTaxWithFavorableCheck(
    s,
    year,
    age1,
    age2,
    pension.TaxableIncome1,
    pension.TaxableIncome2,
    interest,
    sd,
    wd + ed + equityFundAdvanceLumpSumTaxableThisYear,
    realizedStockGains,
    realizedEquityFundGains,
    GetEffectiveCapitalGainsAllowance(s, year),
    openingStockLossCarryForward,
    openingOtherLossCarryForward,
    out _,
    out _);
   decimal person2NetEmploymentIncome = CalculatePerson2NetEmploymentIncome(s, year);

   decimal oneTimeIncome = SumCashFlows(s.OneTimeIncome, year, s.PlanningYear, s.InflationRate);
   if (s.HouseIncluded && s.HouseSaleYear == year)
    oneTimeIncome += s.HouseNetSaleProceeds;

   decimal oneTimeExpenses = SumCashFlows(s.OneTimeExpenses, year, s.PlanningYear, s.InflationRate);

   cash = Math.Max(0m, cash + interest);
   world = worldValueBeforeSales;
   divEtf = divEtfValueBeforeSales;
   divStocks = Math.Max(0m, divStocks + divStocks * sr - sd);

   if (world <= 0m)
    worldCostBasis = 0m;
   if (divEtf <= 0m)
    divEtfCostBasis = 0m;
   if (divStocks <= 0m)
    divStocksCostBasis = 0m;

   decimal requiredForYear = totalAnnualNeed + oneTimeExpenses + taxes;

   decimal fundingFromPension = Math.Min(Math.Max(0m, pension.Net), requiredForYear);
   decimal remainingAfterPension = Math.Max(0m, requiredForYear - fundingFromPension);

   decimal fundingFromPerson2Income =
    Math.Min(Math.Max(0m, person2NetEmploymentIncome), remainingAfterPension);
   decimal remainingAfterPerson2Income =
    Math.Max(0m, remainingAfterPension - fundingFromPerson2Income);

   decimal fundingFromOtherIncome = Math.Min(Math.Max(0m, oneTimeIncome), remainingAfterPerson2Income);
   decimal remainingAfterOtherIncome = Math.Max(0m, remainingAfterPerson2Income - fundingFromOtherIncome);

   cash += Math.Max(0m, pension.Net - fundingFromPension);
   cash += Math.Max(0m, person2NetEmploymentIncome - fundingFromPerson2Income);
   cash += Math.Max(0m, oneTimeIncome - fundingFromOtherIncome);

   decimal availableDividends = Math.Max(0m, wd + ed + sd);
   decimal fundingFromDividends = Math.Min(availableDividends, remainingAfterOtherIncome);
   decimal need = Math.Max(0m, remainingAfterOtherIncome - fundingFromDividends);

   if (s.DividendSurplusReinvest)
    cash += Math.Max(0m, availableDividends - fundingFromDividends);

   if (need > 0m)
   {
    decimal takeCash = Math.Min(cash, need);
    cash -= takeCash;
    need -= takeCash;
   }

   if (need > 0m)
   {
    decimal sold = SellRiskyAssets(
     ref world,
     ref worldCostBasis,
     ref divEtf,
     ref divEtfCostBasis,
     ref divStocks,
     ref divStocksCostBasis,
     ref equityFundAdvanceLumpSumCarry,
     need,
     out decimal realizedStockGain,
     out decimal realizedEquityFundGain);

    realizedStockGains += realizedStockGain;
    realizedEquityFundGains += realizedEquityFundGain;
    need -= sold;
   }

   if (need > 0.01m)
    return -1m;

   bool stockYearNegative = wr < 0m || er < 0m || sr < 0m;
   if (s.AutoRefillReserve && cash < reserveTarget &&
       (!s.UseReserveOnNegativeStockYear || !stockYearNegative))
   {
    decimal refill = SellRiskyAssets(
     ref world,
     ref worldCostBasis,
     ref divEtf,
     ref divEtfCostBasis,
     ref divStocks,
     ref divStocksCostBasis,
     ref equityFundAdvanceLumpSumCarry,
     reserveTarget - cash,
     out decimal realizedStockGain,
     out decimal realizedEquityFundGain);

    realizedStockGains += realizedStockGain;
    realizedEquityFundGains += realizedEquityFundGain;
    cash += refill;
   }

   decimal taxWithdrawn = 0m;
   taxes = SettleCapitalTaxAfterSales(
    s,
    year,
    age1,
    age2,
    pension.TaxableIncome1,
    pension.TaxableIncome2,
    interest,
    sd,
    wd + ed + equityFundAdvanceLumpSumTaxableThisYear,
    ref cash,
    ref world,
    ref worldCostBasis,
    ref divEtf,
    ref divEtfCostBasis,
    ref divStocks,
    ref divStocksCostBasis,
    ref equityFundAdvanceLumpSumCarry,
    ref realizedStockGains,
    ref realizedEquityFundGains,
    ref taxWithdrawn,
    taxes,
    openingStockLossCarryForward,
    openingOtherLossCarryForward,
    out decimal taxFundingGap);

   if (taxFundingGap > 0.01m)
    return -1m;

   for (int pass = 0; pass < 32; pass++)
   {
    decimal realizedCapitalIncomeForHealthInsurance =
     Math.Max(0m, realizedStockGains) +
     Math.Max(0m, realizedEquityFundGains) * 0.70m;

    decimal finalCapitalIncomePerPersonAnnual =
     (healthInsuranceRelevantCapitalIncome + realizedCapitalIncomeForHealthInsurance) /
     Math.Max(1, s.HouseholdPersonCount);
    decimal finalCapitalIncomePerPersonMonthly =
     finalCapitalIncomePerPersonAnnual / 12m;

    decimal targetHealthCare = 0m;

    if (age1 < s.Person1RetirementAge)
    {
     targetHealthCare += CalculateVoluntaryHealthAndCareAnnual(
      s,
      healthInsuranceParameters,
      finalCapitalIncomePerPersonMonthly);
    }
    else if (age1 <= s.Person1EndAge && !s.KvdrPerson1)
    {
     targetHealthCare += PensionService.CalculateVoluntaryRetireeAdditionalHealthAndCareAnnual(
      s,
      healthInsuranceParameters,
      pension.Person1Gross,
      finalCapitalIncomePerPersonAnnual);
    }

    if (s.HouseholdPersonCount == 2 &&
        year >= GetPerson2WorkEndYear(s))
    {
     if (age2 < s.Person2RetirementAge)
     {
      targetHealthCare += CalculateVoluntaryHealthAndCareAnnual(
       s,
       healthInsuranceParameters,
       finalCapitalIncomePerPersonMonthly);
     }
     else if (age2 <= s.Person2EndAge && !s.KvdrPerson2)
     {
      targetHealthCare += PensionService.CalculateVoluntaryRetireeAdditionalHealthAndCareAnnual(
       s,
       healthInsuranceParameters,
       pension.Person2Gross,
       finalCapitalIncomePerPersonAnnual);
     }
    }

    decimal additionalHealthCare = Math.Max(0m, targetHealthCare - healthCare);

    healthCare = Math.Max(healthCare, targetHealthCare);
    recurringAnnualNeed =
     living +
     healthCare +
     houseMaintenanceExpense;
    totalAnnualNeed =
     recurringAnnualNeed +
     carReplacementExpense;
    reserveTarget =
     recurringAnnualNeed * s.ReserveYears +
     healthTarget +
     travelTarget +
     otherTarget;

    if (additionalHealthCare > 0.01m)
    {
     decimal remainingHealthCare = additionalHealthCare;

     decimal fromCash = Math.Min(cash, remainingHealthCare);
     cash -= fromCash;
     remainingHealthCare -= fromCash;

     if (remainingHealthCare > 0.01m)
     {
      decimal sold = SellRiskyAssets(
       ref world,
       ref worldCostBasis,
       ref divEtf,
       ref divEtfCostBasis,
       ref divStocks,
       ref divStocksCostBasis,
       ref equityFundAdvanceLumpSumCarry,
       remainingHealthCare,
       out decimal realizedStockGain,
       out decimal realizedEquityFundGain);

      realizedStockGains += realizedStockGain;
      realizedEquityFundGains += realizedEquityFundGain;
      remainingHealthCare -= sold;
     }

     if (remainingHealthCare > 0.01m)
      return -1m;
    }

    if (s.AutoRefillReserve && cash < reserveTarget &&
        (!s.UseReserveOnNegativeStockYear || !stockYearNegative))
    {
     decimal refill = SellRiskyAssets(
      ref world,
      ref worldCostBasis,
      ref divEtf,
      ref divEtfCostBasis,
      ref divStocks,
      ref divStocksCostBasis,
      ref equityFundAdvanceLumpSumCarry,
      reserveTarget - cash,
      out decimal realizedStockGain,
      out decimal realizedEquityFundGain);

     realizedStockGains += realizedStockGain;
     realizedEquityFundGains += realizedEquityFundGain;
     cash += refill;
    }

    decimal taxesBeforeHealthSettlement = taxes;
    taxes = SettleCapitalTaxAfterSales(
     s,
     year,
     age1,
     age2,
     pension.TaxableIncome1,
     pension.TaxableIncome2,
     interest,
     sd,
     wd + ed + equityFundAdvanceLumpSumTaxableThisYear,
     ref cash,
     ref world,
     ref worldCostBasis,
     ref divEtf,
     ref divEtfCostBasis,
     ref divStocks,
     ref divStocksCostBasis,
     ref equityFundAdvanceLumpSumCarry,
     ref realizedStockGains,
     ref realizedEquityFundGains,
     ref taxWithdrawn,
     taxes,
     openingStockLossCarryForward,
     openingOtherLossCarryForward,
     out decimal additionalTaxFundingGap);

    if (additionalTaxFundingGap > 0.01m)
     return -1m;

    if (additionalHealthCare <= 0.01m &&
        Math.Abs(taxes - taxesBeforeHealthSettlement) <= 0.01m)
     break;
   }

   _ = TaxService.CalculateCapitalTaxWithFavorableCheck(
    s,
    year,
    age1,
    age2,
    pension.TaxableIncome1,
    pension.TaxableIncome2,
    interest,
    sd,
    wd + ed + equityFundAdvanceLumpSumTaxableThisYear,
    realizedStockGains,
    realizedEquityFundGains,
    GetEffectiveCapitalGainsAllowance(s, year),
    openingStockLossCarryForward,
    openingOtherLossCarryForward,
    out stockLossCarryForward,
    out otherLossCarryForward);

   pendingWorldAdvanceLumpSum = worldValueBeforeSales > 0m
    ? currentYearWorldAdvanceLumpSum * Math.Min(1m, world / worldValueBeforeSales)
    : 0m;
   pendingDividendEtfAdvanceLumpSum = divEtfValueBeforeSales > 0m
    ? currentYearDividendEtfAdvanceLumpSum * Math.Min(1m, divEtf / divEtfValueBeforeSales)
    : 0m;

   if (cash + world + divEtf + divStocks <= 0.01m && year < finalYear)
    return -1m;
  }

  return cash + world + divEtf + divStocks;
 }

 private static decimal EstimateInitialCostBasis(
  decimal simulatedValue,
  decimal currentValue,
  int startYear,
  decimal historicalReturn,
  int valuationYear)
 {
  if (simulatedValue <= 0m || currentValue <= 0m)
   return Math.Max(0m, simulatedValue);

  int yearsHeld = Math.Max(0, valuationYear - startYear);
  if (yearsHeld == 0)
   return Math.Max(0m, simulatedValue);

  decimal growthFactor = Pow(1m + historicalReturn, yearsHeld);
  if (growthFactor <= 0m)
   return Math.Max(0m, simulatedValue);

  decimal estimatedCurrentCostBasis = currentValue / growthFactor;
  decimal costBasisRatio = estimatedCurrentCostBasis / currentValue;

  return Math.Max(0m, simulatedValue * costBasisRatio);
 }

 private static decimal SellRiskyAssets(
  ref decimal world,
  ref decimal worldCostBasis,
  ref decimal divEtf,
  ref decimal divEtfCostBasis,
  ref decimal divStocks,
  ref decimal divStocksCostBasis,
  ref decimal equityFundAdvanceLumpSumCarry,
  decimal requestedAmount,
  out decimal realizedStockGain,
  out decimal realizedEquityFundGain)
 {
  realizedStockGain = 0m;
  realizedEquityFundGain = 0m;

  decimal risky = world + divEtf + divStocks;
  if (requestedAmount <= 0m || risky <= 0m)
   return 0m;

  decimal sold = Math.Min(risky, requestedAmount);
  decimal worldSale = sold * world / risky;
  decimal divEtfSale = sold * divEtf / risky;
  decimal divStocksSale = sold * divStocks / risky;

  decimal realizedEquityFundGainBeforeAdvanceLumpSum =
   CalculateRealizedGainAndReduceCostBasis(
    ref worldCostBasis,
    world,
    worldSale) +
   CalculateRealizedGainAndReduceCostBasis(
    ref divEtfCostBasis,
    divEtf,
    divEtfSale);

  decimal soldFraction = Math.Min(1m, sold / risky);
  decimal removedAdvanceLumpSum = equityFundAdvanceLumpSumCarry * soldFraction;
  equityFundAdvanceLumpSumCarry =
   Math.Max(0m, equityFundAdvanceLumpSumCarry - removedAdvanceLumpSum);
  realizedEquityFundGain =
   realizedEquityFundGainBeforeAdvanceLumpSum - removedAdvanceLumpSum;

  realizedStockGain += CalculateRealizedGainAndReduceCostBasis(
   ref divStocksCostBasis,
   divStocks,
   divStocksSale);

  world = Math.Max(0m, world - worldSale);
  divEtf = Math.Max(0m, divEtf - divEtfSale);
  divStocks = Math.Max(0m, divStocks - divStocksSale);

  return sold;
 }

 private static decimal CalculateRealizedGainAndReduceCostBasis(
  ref decimal costBasis,
  decimal currentValue,
  decimal saleAmount)
 {
  if (saleAmount <= 0m || currentValue <= 0m)
   return 0m;

  decimal soldFraction = Math.Min(1m, saleAmount / currentValue);
  decimal removedCostBasis = costBasis * soldFraction;
  costBasis = Math.Max(0m, costBasis - removedCostBasis);

  return saleAmount - removedCostBasis;
 }

 private static decimal SettleCapitalTaxAfterSales(
  PlannerSettings s,
  int year,
  int age1,
  int age2,
  decimal regularTaxableIncome1,
  decimal regularTaxableIncome2,
  decimal interest,
  decimal stockDividends,
  decimal equityFundDistributions,
  ref decimal cash,
  ref decimal world,
  ref decimal worldCostBasis,
  ref decimal divEtf,
  ref decimal divEtfCostBasis,
  ref decimal divStocks,
  ref decimal divStocksCostBasis,
  ref decimal equityFundAdvanceLumpSumCarry,
  ref decimal realizedStockGains,
  ref decimal realizedEquityFundGains,
  ref decimal withdrawn,
  decimal baseTaxes,
  decimal openingStockLossCarryForward,
  decimal openingOtherLossCarryForward,
  out decimal fundingGap)
 {
  decimal paidTaxes = baseTaxes;
  decimal totalTaxes = baseTaxes;

  for (int pass = 0; pass < 32; pass++)
  {
   totalTaxes = TaxService.CalculateCapitalTaxWithFavorableCheck(
    s,
    year,
    age1,
    age2,
    regularTaxableIncome1,
    regularTaxableIncome2,
    interest,
    stockDividends,
    equityFundDistributions,
    realizedStockGains,
    realizedEquityFundGains,
    GetEffectiveCapitalGainsAllowance(s, year),
    openingStockLossCarryForward,
    openingOtherLossCarryForward,
    out _,
    out _);

   decimal additionalTax = Math.Max(0m, totalTaxes - paidTaxes);
   if (additionalTax <= 0.01m)
    break;

   decimal fromCash = Math.Min(cash, additionalTax);
   cash -= fromCash;
   withdrawn += fromCash;
   paidTaxes += fromCash;
   additionalTax -= fromCash;

   if (additionalTax <= 0.01m)
    continue;

   decimal sold = SellRiskyAssets(
    ref world,
    ref worldCostBasis,
    ref divEtf,
    ref divEtfCostBasis,
    ref divStocks,
    ref divStocksCostBasis,
    ref equityFundAdvanceLumpSumCarry,
    additionalTax,
    out decimal realizedStockGain,
    out decimal realizedEquityFundGain);

   realizedStockGains += realizedStockGain;
   realizedEquityFundGains += realizedEquityFundGain;
   withdrawn += sold;
   paidTaxes += sold;

   if (sold <= 0m)
    break;
  }

  totalTaxes = TaxService.CalculateCapitalTaxWithFavorableCheck(
   s,
   year,
   age1,
   age2,
   regularTaxableIncome1,
   regularTaxableIncome2,
   interest,
   stockDividends,
   equityFundDistributions,
   realizedStockGains,
   realizedEquityFundGains,
   GetEffectiveCapitalGainsAllowance(s, year),
   openingStockLossCarryForward,
   openingOtherLossCarryForward,
   out _,
   out _);

  fundingGap = Math.Max(0m, totalTaxes - paidTaxes);
  return totalTaxes;
 }

 private static decimal GetEffectiveCapitalGainsAllowance(
  PlannerSettings s,
  int year)
 {
  decimal householdAllowance = Math.Max(0m, s.CapitalGainsAllowance);

  if (s.HouseholdPersonCount != 2)
   return householdAllowance;

  int planningAgePerson1 = GetPlanningAge(s.Person1Age, s.PlanningYear);
  int planningAgePerson2 = GetPlanningAge(s.Person2Age, s.PlanningYear);
  int offset = year - s.PlanningYear;

  bool person1Included = planningAgePerson1 + offset <= s.Person1EndAge;
  bool person2Included = planningAgePerson2 + offset <= s.Person2EndAge;

  return person1Included && person2Included
   ? householdAllowance
   : householdAllowance / 2m;
 }

 private static decimal CalculateVoluntaryHealthAndCareAnnual(
  PlannerSettings s,
  HealthInsuranceProjectionParameters healthInsuranceParameters,
  decimal monthlyCapitalIncomePerPerson)
 {
  decimal contributionBaseMonthly = Math.Max(
   healthInsuranceParameters.MinimumMonthlyIncome,
   Math.Min(
    healthInsuranceParameters.MaximumMonthlyIncome,
    Math.Max(0m, monthlyCapitalIncomePerPerson)));

  decimal combinedRate =
   s.VoluntaryHealthInsuranceRate +
   healthInsuranceParameters.AdditionalRate +
   healthInsuranceParameters.CareRate;

  return contributionBaseMonthly * combinedRate * 12m;
 }

 private static int GetPerson2WorkEndYear(PlannerSettings s)
 {
  return s.Person2WorkEndYear > 0 ? s.Person2WorkEndYear : s.PlanningYear;
 }

 private static decimal CalculatePerson2NetEmploymentIncome(PlannerSettings s, int year)
 {
  if (s.HouseholdPersonCount != 2 ||
      year >= GetPerson2WorkEndYear(s) ||
      s.Person2NetIncomeMonthly <= 0m)
   return 0m;

  int yearsFromCurrentYear = Math.Max(0, year - DateTime.Today.Year);
  decimal incomeFactor = Pow(1m + s.Person2NetIncomeIncreaseRate, yearsFromCurrentYear);

  return s.Person2NetIncomeMonthly * 12m * incomeFactor;
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

 private static int GetPlanningAge(int currentAge, int planningYear)
 {
  return currentAge + (planningYear - DateTime.Today.Year);
 }

 private static decimal Pow(decimal value, int exponent)
 {
  decimal result = 1m;
  for (int i = 0; i < exponent; i++) result *= value;
  return result;
 }
}
