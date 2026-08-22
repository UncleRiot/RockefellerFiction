namespace RockefellerFiction;

public static class TaxService
{
 private const decimal CapitalGainsTaxRate = 0.25m;
 private const decimal SolidarityRate = 0.055m;
 private const decimal EquityFundTaxableShare = 0.70m;

 public static decimal CalculateCapitalTax(
  decimal interest,
  decimal stockDividends,
  decimal equityFundDistributions,
  decimal realizedGains,
  decimal allowance)
 {
  decimal taxable =
   Math.Max(0m, interest) +
   Math.Max(0m, stockDividends) +
   Math.Max(0m, equityFundDistributions) * EquityFundTaxableShare +
   Math.Max(0m, realizedGains);

  taxable = Math.Max(0m, taxable - Math.Max(0m, allowance));
  decimal tax = taxable * CapitalGainsTaxRate;
  return tax + tax * SolidarityRate;
 }
}
