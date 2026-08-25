namespace RockefellerFiction;

public static class PensionService
{
 private const decimal HealthInsuranceEmployeeShare = 0.073m;
 private const int RegularRetirementAge = 67;
 public const int MinimumInsuranceYearsForEarlyRetirement = 35;
 private const decimal EarlyRetirementReductionPerMonth = 0.003m;
 private const decimal MaximumEarlyRetirementReduction = 0.144m;
 private const decimal PensionIncomeExpenseAllowancePerPerson = 102m;
 private const decimal CurrentPensionValue2026 = 42.52m;
 private const decimal ProvisionalAverageAnnualEarnings2026 = 51944m;
 private const decimal PensionContributionAssessmentCeilingAnnual2026 = 101400m;

 public static int CalculateInsuranceYearsAtWorkEnd(
  int currentInsuranceYears,
  int workEndYear)
 {
  return
   Math.Max(0, currentInsuranceYears) +
   Math.Max(0, workEndYear - DateTime.Today.Year);
 }

 public static bool HasRequiredInsuranceYearsForEarlyRetirement(
  int currentInsuranceYears,
  int workEndYear,
  int retirementAge)
 {
  if (retirementAge >= RegularRetirementAge)
   return true;

  return
   CalculateInsuranceYearsAtWorkEnd(
    currentInsuranceYears,
    workEndYear) >=
   MinimumInsuranceYearsForEarlyRetirement;
 }

 public static HealthInsuranceProjectionParameters CalculateHealthInsuranceProjectionParameters(
  PlannerSettings s,
  int year,
  bool stress)
 {
  int yearsFromBaseYear = Math.Max(0, year - s.HealthInsuranceBaseYear);

  decimal assessmentIncreaseRate =
   s.HealthInsuranceAssessmentIncreaseRate +
   (stress ? s.StressHealthInsuranceAssessmentAdditionalIncreaseRate : 0m);

  decimal assessmentFactor = Pow(
   1m + assessmentIncreaseRate,
   yearsFromBaseYear);

  decimal additionalRateAnnualChange =
   s.HealthInsuranceAdditionalRateAnnualChange +
   (stress ? s.StressHealthInsuranceAdditionalRateAnnualChange : 0m);

  decimal careRateAnnualChange =
   s.CareInsuranceRateAnnualChange +
   (stress ? s.StressCareInsuranceRateAnnualChange : 0m);

  return new HealthInsuranceProjectionParameters(
   s.VoluntaryHealthInsuranceMinimumMonthlyIncome * assessmentFactor,
   s.VoluntaryHealthInsuranceMaximumMonthlyIncome * assessmentFactor,
   Math.Max(
    0m,
    s.VoluntaryHealthInsuranceAdditionalRate +
    (additionalRateAnnualChange * yearsFromBaseYear)),
   Math.Max(
    0m,
    s.CareInsuranceChildlessRate +
    (careRateAnnualChange * yearsFromBaseYear)));
 }

 public static AnnualPensionResult CalculateAnnualPension(
  PlannerSettings s, int year, int age1, int age2, bool stress)
 {
  decimal gross1 = 0m;
  decimal gross2 = 0m;

  int person1PensionStartYear =
   year - Math.Max(0, age1 - s.Person1RetirementAge);

  int person2PensionStartYear =
   year - Math.Max(0, age2 - s.Person2RetirementAge);

  int person1WorkEndYear =
   s.Person1WorkEndYear > 0
    ? s.Person1WorkEndYear
    : s.PlanningYear;

  decimal person1MonthlyAtRetirement =
   CalculateMonthlyPensionAtRetirementBeforePensionIncrease(
    s.Person1PensionGrossMonthly,
    s.Person1ProjectedPensionGrossMonthlyAt67,
    s.Person1CurrentPensionPoints,
    s.Person1PensionableAnnualGross,
    s.Person1PensionableAnnualGrossIncreaseRate,
    s.PensionAverageAnnualEarningsIncreaseRate,
    s.Person1Age,
    person1WorkEndYear,
    s.Person1RetirementAge);

  int person2WorkEndYear =
   s.Person2WorkEndYear > 0
    ? s.Person2WorkEndYear
    : s.PlanningYear;

  decimal person2MonthlyAtRetirement =
   CalculateMonthlyPensionAtRetirementBeforePensionIncrease(
    s.Person2PensionGrossMonthly,
    s.Person2ProjectedPensionGrossMonthlyAt67,
    s.Person2CurrentPensionPoints,
    s.Person2PensionableAnnualGross,
    s.Person2PensionableAnnualGrossIncreaseRate,
    s.PensionAverageAnnualEarningsIncreaseRate,
    s.Person2Age,
    person2WorkEndYear,
    s.Person2RetirementAge);

  if (age1 >= s.Person1RetirementAge && age1 <= s.Person1EndAge)
  {
   int yearsFromCurrentYear = Math.Max(0, year - DateTime.Today.Year);
   gross1 = person1MonthlyAtRetirement
    * CalculateEarlyRetirementFactor(s.Person1RetirementAge)
    * 12m
    * Pow(1m + s.PensionIncreaseRate, yearsFromCurrentYear);
  }

  if (s.HouseholdPersonCount == 2 &&
      age2 >= s.Person2RetirementAge &&
      age2 <= s.Person2EndAge)
  {
   int yearsFromCurrentYear = Math.Max(0, year - DateTime.Today.Year);
   gross2 = person2MonthlyAtRetirement
    * CalculateEarlyRetirementFactor(s.Person2RetirementAge)
    * 12m
    * Pow(1m + s.PensionIncreaseRate, yearsFromCurrentYear);
  }

  HealthInsuranceProjectionParameters healthInsuranceParameters =
   CalculateHealthInsuranceProjectionParameters(s, year, stress);

  decimal deductions1 = gross1 > 0m
   ? gross1 * (
    HealthInsuranceEmployeeShare +
    (healthInsuranceParameters.AdditionalRate / 2m) +
    healthInsuranceParameters.CareRate)
   : 0m;

  decimal deductions2 = gross2 > 0m
   ? gross2 * (
    HealthInsuranceEmployeeShare +
    (healthInsuranceParameters.AdditionalRate / 2m) +
    healthInsuranceParameters.CareRate)
   : 0m;

  PensionTaxableIncomeDiagnostics taxableIncome1 =
   CalculateTaxablePensionIncomeDiagnostics(
    gross1,
    deductions1,
    person1MonthlyAtRetirement,
    s.Person1RetirementAge,
    year,
    age1,
    person1PensionStartYear,
    s.PensionIncreaseRate);

  PensionTaxableIncomeDiagnostics taxableIncome2 =
   CalculateTaxablePensionIncomeDiagnostics(
    gross2,
    deductions2,
    person2MonthlyAtRetirement,
    s.Person2RetirementAge,
    year,
    age2,
    person2PensionStartYear,
    s.PensionIncreaseRate);

  bool person1Included = age1 <= s.Person1EndAge;
  bool person2Included =
   s.HouseholdPersonCount == 2 &&
   age2 <= s.Person2EndAge;

  ProjectedIncomeTaxDiagnostics incomeTax =
   CalculateProjectedIncomeTaxDiagnostics(
    s,
    year,
    taxableIncome1.TaxableIncome,
    taxableIncome2.TaxableIncome,
    person1Included,
    person2Included);

  decimal gross = gross1 + gross2;
  decimal deductions = deductions1 + deductions2;

  return new AnnualPensionResult(
   gross,
   Math.Max(0m, gross - deductions - incomeTax.Total),
   deductions,
   incomeTax.Total,
   gross1,
   gross2,
   taxableIncome1.TaxableIncome,
   taxableIncome2.TaxableIncome,
   deductions1,
   deductions2,
   taxableIncome1.TaxableShare,
   taxableIncome2.TaxableShare,
   taxableIncome1.FixedTaxFreePensionAmount,
   taxableIncome2.FixedTaxFreePensionAmount,
   taxableIncome1.PensionStartYear,
   taxableIncome2.PensionStartYear,
   incomeTax.IncomeTaxBeforeSurcharges,
   incomeTax.SolidaritySurcharge,
   incomeTax.ChurchTax,
   incomeTax.TariffFactor,
   incomeTax.ProjectedBasicAllowance);
 }

 public static decimal CalculateVoluntaryRetireeAdditionalHealthAndCareAnnual(
  PlannerSettings s,
  HealthInsuranceProjectionParameters healthInsuranceParameters,
  decimal annualPensionGross,
  decimal annualOtherIncome)
 {
  decimal pensionMonthly = Math.Max(0m, annualPensionGross) / 12m;
  decimal otherIncomeMonthly = Math.Max(0m, annualOtherIncome) / 12m;

  decimal totalMonthlyAssessment = Math.Clamp(
   pensionMonthly + otherIncomeMonthly,
   healthInsuranceParameters.MinimumMonthlyIncome,
   healthInsuranceParameters.MaximumMonthlyIncome);

  decimal pensionMonthlyAssessment = Math.Min(
   pensionMonthly,
   healthInsuranceParameters.MaximumMonthlyIncome);

  decimal additionalMonthlyAssessment = Math.Max(
   0m,
   totalMonthlyAssessment - pensionMonthlyAssessment);

  return additionalMonthlyAssessment * 12m * (
   s.VoluntaryHealthInsuranceRate +
   healthInsuranceParameters.AdditionalRate +
   healthInsuranceParameters.CareRate);
 }

 private static PensionTaxableIncomeDiagnostics CalculateTaxablePensionIncomeDiagnostics(
  decimal currentAnnualGross,
  decimal deductibleHealthAndCare,
  decimal pensionGrossMonthlyAtRetirement,
  int retirementAge,
  int currentYear,
  int currentAge,
  int pensionStartYear,
  decimal pensionIncreaseRate)
 {
  if (currentAnnualGross <= 0m || currentAge < retirementAge)
  {
   return new PensionTaxableIncomeDiagnostics(
    0m,
    0m,
    0m,
    Math.Max(0m, deductibleHealthAndCare),
    PensionIncomeExpenseAllowancePerPerson,
    pensionStartYear);
  }

  int yearsToPensionStart =
   Math.Max(0, pensionStartYear - DateTime.Today.Year);

  decimal initialAnnualGross =
   pensionGrossMonthlyAtRetirement *
   CalculateEarlyRetirementFactor(retirementAge) *
   12m *
   Pow(1m + pensionIncreaseRate, yearsToPensionStart);

  decimal taxableShare = GetPensionTaxableShare(pensionStartYear);
  decimal fixedTaxFreePensionAmount =
   initialAnnualGross * (1m - taxableShare);

  decimal taxableIncome = Math.Max(
   0m,
   currentAnnualGross -
   fixedTaxFreePensionAmount -
   Math.Max(0m, deductibleHealthAndCare) -
   PensionIncomeExpenseAllowancePerPerson);

  return new PensionTaxableIncomeDiagnostics(
   taxableIncome,
   taxableShare,
   fixedTaxFreePensionAmount,
   Math.Max(0m, deductibleHealthAndCare),
   PensionIncomeExpenseAllowancePerPerson,
   pensionStartYear);
 }

 private static decimal GetPensionTaxableShare(int pensionStartYear)
 {
  if (pensionStartYear <= 2005)
   return 0.50m;

  if (pensionStartYear <= 2020)
   return 0.52m + ((pensionStartYear - 2006) * 0.02m);

  if (pensionStartYear == 2021)
   return 0.81m;

  if (pensionStartYear == 2022)
   return 0.82m;

  return Math.Min(
   1m,
   0.825m + ((pensionStartYear - 2023) * 0.005m));
 }

 public static decimal CalculateProjectedIncomeTaxIncludingSurcharges(
  PlannerSettings s,
  int year,
  decimal taxableIncome1,
  decimal taxableIncome2,
  bool person1Included,
  bool person2Included) =>
  CalculateProjectedIncomeTaxDiagnostics(
   s,
   year,
   taxableIncome1,
   taxableIncome2,
   person1Included,
   person2Included).Total;

 public static ProjectedIncomeTaxDiagnostics CalculateProjectedIncomeTaxDiagnostics(
  PlannerSettings s,
  int year,
  decimal taxableIncome1,
  decimal taxableIncome2,
  bool person1Included,
  bool person2Included)
 {
  decimal tariffAnnualIncreaseRate =
   Math.Max(0m, s.IncomeTaxTariffAnnualIncreaseRate);

  int yearsFrom2026 = Math.Max(0, year - 2026);
  decimal tariffFactor =
   Pow(1m + tariffAnnualIncreaseRate, yearsFrom2026);

  if (tariffFactor <= 0m)
   tariffFactor = 1m;

  decimal projectedBasicAllowance =
   12348m * tariffFactor;

  decimal incomeTax;
  decimal solidaritySurcharge;
  decimal churchTax;

  if (s.JointTaxation &&
      person1Included &&
      person2Included)
  {
   incomeTax = CalculateProjectedJointIncomeTaxUsing2026Tariff(
    Math.Max(0m, taxableIncome1) + Math.Max(0m, taxableIncome2),
    year,
    tariffAnnualIncreaseRate);

   solidaritySurcharge =
    CalculateProjectedSolidaritySurcharge(
     incomeTax,
     year,
     tariffAnnualIncreaseRate,
     true);

   churchTax =
    CalculateChurchTax(s, incomeTax);

   return new ProjectedIncomeTaxDiagnostics(
    incomeTax,
    solidaritySurcharge,
    churchTax,
    incomeTax + solidaritySurcharge + churchTax,
    tariffFactor,
    projectedBasicAllowance);
  }

  decimal incomeTax1 = person1Included
   ? CalculateProjectedIncomeTaxUsing2026Tariff(
      taxableIncome1,
      year,
      tariffAnnualIncreaseRate)
   : 0m;

  decimal incomeTax2 = person2Included
   ? CalculateProjectedIncomeTaxUsing2026Tariff(
      taxableIncome2,
      year,
      tariffAnnualIncreaseRate)
   : 0m;

  decimal solidaritySurcharge1 =
   CalculateProjectedSolidaritySurcharge(
    incomeTax1,
    year,
    tariffAnnualIncreaseRate,
    false);

  decimal solidaritySurcharge2 =
   CalculateProjectedSolidaritySurcharge(
    incomeTax2,
    year,
    tariffAnnualIncreaseRate,
    false);

  decimal churchTax1 =
   CalculateChurchTax(s, incomeTax1);

  decimal churchTax2 =
   CalculateChurchTax(s, incomeTax2);

  incomeTax = incomeTax1 + incomeTax2;
  solidaritySurcharge =
   solidaritySurcharge1 + solidaritySurcharge2;
  churchTax =
   churchTax1 + churchTax2;

  return new ProjectedIncomeTaxDiagnostics(
   incomeTax,
   solidaritySurcharge,
   churchTax,
   incomeTax + solidaritySurcharge + churchTax,
   tariffFactor,
   projectedBasicAllowance);
 }

 private static decimal CalculateProjectedSolidaritySurcharge(
  decimal incomeTax,
  int year,
  decimal tariffAnnualIncreaseRate,
  bool jointTaxation)
 {
  int yearsFrom2026 = Math.Max(0, year - 2026);
  decimal thresholdFactor = Pow(1m + tariffAnnualIncreaseRate, yearsFrom2026);

  if (thresholdFactor <= 0m)
   thresholdFactor = 1m;

  decimal threshold =
   (jointTaxation ? 40700m : 20350m) * thresholdFactor;

  if (incomeTax <= threshold)
   return 0m;

  decimal regularSurcharge = incomeTax * 0.055m;
  decimal transitionSurcharge = (incomeTax - threshold) * 0.119m;

  return Math.Max(0m, Math.Min(regularSurcharge, transitionSurcharge));
 }

 private static decimal CalculateChurchTax(
  PlannerSettings s,
  decimal incomeTax)
 {
  if (!s.ChurchTaxEnabled)
   return 0m;

  return Math.Max(0m, incomeTax) * Math.Max(0m, s.ChurchTaxRate);
 }

 private static decimal CalculateProjectedJointIncomeTaxUsing2026Tariff(
  decimal jointTaxableIncome,
  int year,
  decimal tariffAnnualIncreaseRate)
 {
  decimal halfIncome = Math.Floor(Math.Max(0m, jointTaxableIncome) / 2m);
  decimal taxPerPerson = CalculateProjectedIncomeTaxUsing2026Tariff(
   halfIncome,
   year,
   tariffAnnualIncreaseRate);

  return taxPerPerson * 2m;
 }

 private static decimal CalculateProjectedIncomeTaxUsing2026Tariff(
  decimal taxableIncome,
  int year,
  decimal tariffAnnualIncreaseRate)
 {
  int yearsFrom2026 = Math.Max(0, year - 2026);
  decimal tariffFactor = Pow(1m + tariffAnnualIncreaseRate, yearsFrom2026);

  if (tariffFactor <= 0m)
   tariffFactor = 1m;

  decimal taxableIncomeIn2026Euros =
   Math.Max(0m, taxableIncome) / tariffFactor;

  decimal taxIn2026Euros =
   CalculateIncomeTaxUsing2026Tariff(taxableIncomeIn2026Euros);

  return Math.Max(0m, Math.Floor(taxIn2026Euros * tariffFactor));
 }

 private static decimal CalculateIncomeTaxUsing2026Tariff(decimal taxableIncome)
 {
  decimal x = Math.Floor(Math.Max(0m, taxableIncome));
  decimal tax;

  if (x <= 12348m)
  {
   tax = 0m;
  }
  else if (x <= 17799m)
  {
   decimal y = (x - 12348m) / 10000m;
   tax = (914.51m * y + 1400m) * y;
  }
  else if (x <= 69878m)
  {
   decimal z = (x - 17799m) / 10000m;
   tax = (173.10m * z + 2397m) * z + 1034.87m;
  }
  else if (x <= 277825m)
  {
   tax = 0.42m * x - 11135.63m;
  }
  else
  {
   tax = 0.45m * x - 19470.38m;
  }

  return Math.Max(0m, Math.Floor(tax));
 }

 public static PensionProjectionDiagnostics CalculatePensionProjectionDiagnostics(
  decimal currentGrossMonthly,
  decimal projectedGrossMonthlyAt67,
  decimal currentPensionPoints,
  decimal pensionableAnnualGross,
  decimal pensionableAnnualGrossIncreaseRate,
  decimal averageAnnualEarningsIncreaseRate,
  int currentAge,
  int workEndYear,
  int retirementAge)
 {
  decimal enteredCurrentPension = Math.Max(0m, currentGrossMonthly);
  decimal enteredProjectedPension = Math.Max(0m, projectedGrossMonthlyAt67);
  decimal pensionPoints = Math.Max(0m, currentPensionPoints);
  decimal annualGross = Math.Max(0m, pensionableAnnualGross);
  decimal grossIncreaseRate = Math.Max(-0.50m, pensionableAnnualGrossIncreaseRate);
  decimal averageEarningsIncreaseRate = Math.Max(-0.10m, averageAnnualEarningsIncreaseRate);

  string currentPensionSource =
   pensionPoints > 0m
    ? $"Entgeltpunkte × aktueller Rentenwert 2026 ({CurrentPensionValue2026:N2} €)"
    : "Heute bereits erworbene Rente";

  decimal currentPension = pensionPoints > 0m
   ? pensionPoints * CurrentPensionValue2026
   : enteredCurrentPension;

  int yearsToRegularRetirement =
   Math.Max(0, RegularRetirementAge - currentAge);

  int workEndAge =
   currentAge +
   Math.Max(0, workEndYear - DateTime.Today.Year);

  int contributionEndAge =
   Math.Min(
    RegularRetirementAge,
    Math.Min(retirementAge, workEndAge));

  int additionalContributionYears =
   Math.Clamp(
    contributionEndAge - currentAge,
    0,
    yearsToRegularRetirement);

  decimal pensionableGrossUsed = 0m;
  decimal pensionableGrossLastYear = 0m;
  decimal averageAnnualEarningsFirstYear = 0m;
  decimal averageAnnualEarningsLastYear = 0m;
  decimal annualPensionPoints = 0m;
  decimal annualPensionPointsLastYear = 0m;
  decimal additionalPensionPoints = 0m;
  decimal additionalPensionMonthly = 0m;
  string futureAccrualSource = "Keine zusätzlichen Anwartschaften";

  if (yearsToRegularRetirement > 0 &&
      additionalContributionYears > 0)
  {
   if (annualGross > 0m)
   {
    futureAccrualSource = "RV-pflichtiges Jahresbrutto";

    for (int contributionYearIndex = 0;
         contributionYearIndex < additionalContributionYears;
         contributionYearIndex++)
    {
     decimal grossForYear =
      annualGross *
      Pow(1m + grossIncreaseRate, contributionYearIndex);

     decimal averageEarningsForYear =
      ProvisionalAverageAnnualEarnings2026 *
      Pow(1m + averageEarningsIncreaseRate, contributionYearIndex);

     decimal assessmentCeilingForYear =
      PensionContributionAssessmentCeilingAnnual2026 *
      Pow(1m + averageEarningsIncreaseRate, contributionYearIndex);

     decimal pensionableGrossForYear =
      Math.Min(
       grossForYear,
       assessmentCeilingForYear);

     decimal pensionPointsForYear =
      averageEarningsForYear > 0m
       ? pensionableGrossForYear / averageEarningsForYear
       : 0m;

     if (contributionYearIndex == 0)
     {
      pensionableGrossUsed = pensionableGrossForYear;
      averageAnnualEarningsFirstYear = averageEarningsForYear;
      annualPensionPoints = pensionPointsForYear;
     }

     pensionableGrossLastYear = pensionableGrossForYear;
     averageAnnualEarningsLastYear = averageEarningsForYear;
     annualPensionPointsLastYear = pensionPointsForYear;
     additionalPensionPoints += pensionPointsForYear;
    }

    additionalPensionMonthly =
     additionalPensionPoints * CurrentPensionValue2026;
   }
   else
   {
    decimal projectedPensionAt67 =
     Math.Max(enteredCurrentPension, enteredProjectedPension);

    if (projectedPensionAt67 > enteredCurrentPension)
    {
     decimal projectedAdditionalPensionMonthly =
      (projectedPensionAt67 - enteredCurrentPension) /
      yearsToRegularRetirement *
      additionalContributionYears;

     if (pensionPoints > 0m &&
         enteredCurrentPension > 0m)
     {
      decimal pensionValueAtInformationDate =
       enteredCurrentPension / pensionPoints;

      if (pensionValueAtInformationDate > 0m)
      {
       projectedAdditionalPensionMonthly *=
        CurrentPensionValue2026 / pensionValueAtInformationDate;

       futureAccrualSource =
        "DRV-Hochrechnung bis 67, auf aktuellen Rentenwert 2026 umgerechnet";
      }
      else
      {
       futureAccrualSource = "DRV-Hochrechnung bis 67";
      }
     }
     else
     {
      futureAccrualSource = "DRV-Hochrechnung bis 67";
     }

     additionalPensionMonthly =
      projectedAdditionalPensionMonthly;
    }
   }
  }

  decimal monthlyAtRetirementBeforePensionIncrease =
   currentPension + additionalPensionMonthly;

  decimal earlyRetirementFactor =
   CalculateEarlyRetirementFactor(retirementAge);

  return new PensionProjectionDiagnostics(
   currentPensionSource,
   futureAccrualSource,
   enteredCurrentPension,
   enteredProjectedPension,
   pensionPoints,
   annualGross,
   grossIncreaseRate,
   averageEarningsIncreaseRate,
   currentPension,
   yearsToRegularRetirement,
   workEndAge,
   contributionEndAge,
   additionalContributionYears,
   pensionableGrossUsed,
   pensionableGrossLastYear,
   averageAnnualEarningsFirstYear,
   averageAnnualEarningsLastYear,
   annualPensionPoints,
   annualPensionPointsLastYear,
   additionalPensionPoints,
   additionalPensionMonthly,
   monthlyAtRetirementBeforePensionIncrease,
   earlyRetirementFactor,
   monthlyAtRetirementBeforePensionIncrease * earlyRetirementFactor);
 }

 private static decimal CalculateMonthlyPensionAtRetirementBeforePensionIncrease(
  decimal currentGrossMonthly,
  decimal projectedGrossMonthlyAt67,
  decimal currentPensionPoints,
  decimal pensionableAnnualGross,
  decimal pensionableAnnualGrossIncreaseRate,
  decimal averageAnnualEarningsIncreaseRate,
  int currentAge,
  int workEndYear,
  int retirementAge)
 {
  return CalculatePensionProjectionDiagnostics(
   currentGrossMonthly,
   projectedGrossMonthlyAt67,
   currentPensionPoints,
   pensionableAnnualGross,
   pensionableAnnualGrossIncreaseRate,
   averageAnnualEarningsIncreaseRate,
   currentAge,
   workEndYear,
   retirementAge)
   .MonthlyAtRetirementBeforePensionIncrease;
 }

 private static decimal CalculateEarlyRetirementFactor(int retirementAge)
 {
  int monthsEarly = Math.Max(0, (RegularRetirementAge - retirementAge) * 12);
  decimal reduction = Math.Min(
   MaximumEarlyRetirementReduction,
   monthsEarly * EarlyRetirementReductionPerMonth);

  return 1m - reduction;
 }

 private static decimal Pow(decimal value, int exponent)
 {
  decimal result = 1m;
  for (int i = 0; i < exponent; i++) result *= value;
  return result;
 }
}
