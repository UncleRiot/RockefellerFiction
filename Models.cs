namespace RockefellerFiction;

public sealed class PlannerSettings
{
 public int PlanningYear { get; set; } = 2027;
 public int Person1Age { get; set; } = 55;
 public int Person2Age { get; set; } = 50;
 public int Person1RetirementAge { get; set; } = 67;
 public int Person2RetirementAge { get; set; } = 67;
 public int Person1EndAge { get; set; } = 85;
 public int Person2EndAge { get; set; } = 90;

 public decimal MonthlyLivingCosts { get; set; } = 2500m;
 public decimal InflationRate { get; set; } = 0.025m;
 public decimal PensionIncreaseRate { get; set; } = 0.01m;
 public decimal VoluntaryHealthInsuranceMinimumMonthlyIncome { get; set; } = 1318.33m;
 public decimal VoluntaryHealthInsuranceMaximumMonthlyIncome { get; set; } = 5812.50m;
 public decimal VoluntaryHealthInsuranceRate { get; set; } = 0.14m;
 public decimal VoluntaryHealthInsuranceAdditionalRate { get; set; } = 0.029m;
 public decimal CareInsuranceChildlessRate { get; set; } = 0.042m;
 public decimal Person1PensionGrossMonthly { get; set; } = 1479m;
 public decimal Person2PensionGrossMonthly { get; set; } = 1120m;

 public decimal CapitalGainsAllowance { get; set; } = 2000m;
 public bool ChurchTaxEnabled { get; set; }
 public bool KvdrPerson1 { get; set; } = true;
 public bool KvdrPerson2 { get; set; } = true;

 public decimal ReserveYears { get; set; } = 2m;
 public bool AutoRefillReserve { get; set; } = true;
 public bool UseReserveOnNegativeStockYear { get; set; } = true;
 public decimal CashInterestRate { get; set; } = 0.005m;

 public decimal HouseTotalValue { get; set; } = 500000m;
 public decimal HouseBuildingShare { get; set; } = 0.70m;
 public decimal HouseReserveRate { get; set; } = 0.02m;
 public decimal CarReplacementValue { get; set; } = 15000m;
 public int CarReplacementYears { get; set; } = 15;
 public decimal HealthReserveTarget { get; set; } = 10000m;
 public decimal TravelReserveTarget { get; set; }
 public decimal OtherReserveTarget { get; set; }

 public decimal WorldEtfReturn { get; set; } = 0.05m;
 public decimal WorldEtfDistribution { get; set; } = 0.015m;
 public decimal DividendEtfReturn { get; set; } = 0.045m;
 public decimal DividendEtfDistribution { get; set; } = 0.025m;
 public decimal DividendStocksReturn { get; set; } = 0.04m;
 public decimal DividendStocksDistribution { get; set; } = 0.03m;

 public string Strategy { get; set; } = "Ausgewogen";
 public decimal StressCrashPercent { get; set; } = -0.25m;
 public bool StressCrashAtStart { get; set; } = true;
 public bool StressSecondCrashEnabled { get; set; }
 public int StressSecondCrashYear { get; set; } = 2045;
 public decimal StressSecondCrashPercent { get; set; } = -0.25m;

 public decimal StartCapital { get; set; }
 public bool HouseIncluded { get; set; }
 public int HouseSaleYear { get; set; }
 public decimal HouseNetSaleProceeds { get; set; }
 public bool DividendSurplusReinvest { get; set; }

 public List<OneTimeCashFlow> OneTimeIncome { get; set; } = [];
 public List<OneTimeCashFlow> OneTimeExpenses { get; set; } = [];
}

public sealed class OneTimeCashFlow
{
 public int Year { get; set; }
 public decimal AmountToday { get; set; }
 public string Description { get; set; } = "";
}

public sealed record StrategyAllocation(decimal Cash, decimal WorldEtf, decimal DividendEtf, decimal DividendStocks);

public sealed class YearResult
{
 public int Year { get; set; }
 public int Age1 { get; set; }
 public int Age2 { get; set; }
 public decimal LivingCosts { get; set; }
 public decimal LivingCostIncrease { get; set; }
 public decimal HealthCareCosts { get; set; }
 public decimal HealthCareCostsPerson1 { get; set; }
 public decimal HealthCareCostsPerson2 { get; set; }
 public decimal HealthCareCostsPerson1Monthly { get; set; }
 public decimal HealthCareCostsPerson2Monthly { get; set; }
 public decimal HealthInsuranceRelevantCapitalIncome { get; set; }
 public decimal TotalAnnualNeed { get; set; }
 public decimal ReserveTarget { get; set; }
 public decimal ReserveActual { get; set; }
 public decimal PensionGross { get; set; }
 public decimal PensionNet { get; set; }
 public decimal DividendsGross { get; set; }
 public decimal InterestGross { get; set; }
 public decimal TaxesOnCapital { get; set; }
 public decimal NetDividends { get; set; }
 public decimal FundingFromPension { get; set; }
 public decimal FundingFromDividends { get; set; }
 public decimal FundingFromOtherIncome { get; set; }
 public decimal FundingFromCapital { get; set; }
 public decimal FundingGap { get; set; }
 public decimal WithdrawnFromCapital { get; set; }
 public decimal TotalPortfolioStart { get; set; }
 public decimal TotalPortfolioEnd { get; set; }
 public decimal CashEnd { get; set; }
 public decimal WorldEtfEnd { get; set; }
 public decimal DividendEtfEnd { get; set; }
 public decimal DividendStocksEnd { get; set; }
 public decimal CashReturnContribution { get; set; }
 public decimal WorldReturnContribution { get; set; }
 public decimal DividendEtfReturnContribution { get; set; }
 public decimal DividendStocksReturnContribution { get; set; }
 public decimal WorldPriceReturnContribution { get; set; }
 public decimal WorldDistributionContribution { get; set; }
 public decimal DividendEtfPriceReturnContribution { get; set; }
 public decimal DividendEtfDistributionContribution { get; set; }
 public decimal DividendStocksPriceReturnContribution { get; set; }
 public decimal DividendStocksDistributionContribution { get; set; }
 public decimal CashNetIncome { get; set; }
 public decimal WorldNetIncome { get; set; }
 public decimal DividendEtfNetIncome { get; set; }
 public decimal DividendStocksNetIncome { get; set; }
 public decimal HouseReserveTarget { get; set; }
 public decimal CarReserveTarget { get; set; }
 public decimal HealthReserveTarget { get; set; }
 public decimal TravelReserveTarget { get; set; }
 public decimal OtherReserveTarget { get; set; }
 public string YearStatus { get; set; } = "Grün";
}

public sealed class ProjectionResult
{
 public List<YearResult> Years { get; set; } = [];
 public bool ReachesPlanEnd { get; set; }
 public int? DepletionYear { get; set; }
 public decimal FinalCapital { get; set; }
 public decimal RequiredAdditionalStartCapital { get; set; }
 public decimal MinimumRequiredStartCapital { get; set; }
 public decimal InitialRequiredCash { get; set; }
 public string OverallStatus => ReachesPlanEnd ? "Grün" : "Rot";
}

public sealed record HealthInsurancePreview(decimal Person1Monthly, decimal Person2Monthly);
