namespace RockefellerFiction;

public static class PensionService
{
 private const decimal HealthInsuranceEmployeeShare = 0.073m;
 private const decimal AdditionalContributionEmployeeShare = 0.0145m;
 private const decimal CareInsuranceChildless = 0.042m;

 public static (decimal Gross, decimal Net) CalculateAnnualPension(
  PlannerSettings s, int year, int age1, int age2)
 {
  decimal gross1 = 0m;
  decimal gross2 = 0m;

  if (age1 >= s.Person1RetirementAge)
  {
   int years = age1 - s.Person1RetirementAge;
   gross1 = s.Person1PensionGrossMonthly * 12m * Pow(1m + s.PensionIncreaseRate, years);
  }

  if (age2 >= s.Person2RetirementAge)
  {
   int years = age2 - s.Person2RetirementAge;
   gross2 = s.Person2PensionGrossMonthly * 12m * Pow(1m + s.PensionIncreaseRate, years);
  }

  decimal gross = gross1 + gross2;
  decimal deductions = 0m;

  if (gross1 > 0m)
   deductions += gross1 * (HealthInsuranceEmployeeShare + AdditionalContributionEmployeeShare + CareInsuranceChildless);
  if (gross2 > 0m)
   deductions += gross2 * (HealthInsuranceEmployeeShare + AdditionalContributionEmployeeShare + CareInsuranceChildless);

  return (gross, Math.Max(0m, gross - deductions));
 }

 private static decimal Pow(decimal value, int exponent)
 {
  decimal result = 1m;
  for (int i = 0; i < exponent; i++) result *= value;
  return result;
 }
}
