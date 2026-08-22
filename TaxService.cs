namespace RockefellerFiction;

public static class TaxService
{
 private const decimal CapitalGainsTaxRate = 0.25m;
 private const decimal SolidarityRate = 0.055m;
 private const decimal EquityFundTaxableShare = 0.70m;

 public static decimal CalculateEquityFundAdvanceLumpSum(
  decimal valueAtStartOfYear,
  decimal valueAtEndOfYearBeforeSales,
  decimal distributions,
  decimal baseRate)
 {
  decimal startValue = Math.Max(0m, valueAtStartOfYear);
  decimal endValue = Math.Max(0m, valueAtEndOfYearBeforeSales);
  decimal positiveDistributions = Math.Max(0m, distributions);
  decimal normalizedBaseRate = Math.Max(0m, baseRate);

  decimal baseYield = startValue * normalizedBaseRate * 0.70m;
  decimal valueIncreaseWithDistributions =
   Math.Max(0m, endValue + positiveDistributions - startValue);
  decimal cappedBaseYield = Math.Min(baseYield, valueIncreaseWithDistributions);

  return Math.Max(0m, cappedBaseYield - positiveDistributions);
 }

 public static decimal CalculateCapitalTax(
  decimal interest,
  decimal stockDividends,
  decimal equityFundDistributions,
  decimal realizedStockGains,
  decimal realizedEquityFundGains,
  decimal allowance,
  bool churchTaxEnabled,
  decimal churchTaxRate)
 {
  return CalculateCapitalTax(
   interest,
   stockDividends,
   equityFundDistributions,
   realizedStockGains,
   realizedEquityFundGains,
   allowance,
   churchTaxEnabled,
   churchTaxRate,
   0m,
   0m,
   out _,
   out _);
 }

 public static decimal CalculateCapitalTax(
  decimal interest,
  decimal stockDividends,
  decimal equityFundDistributions,
  decimal realizedStockGains,
  decimal realizedEquityFundGains,
  decimal allowance,
  bool churchTaxEnabled,
  decimal churchTaxRate,
  decimal openingStockLossCarryForward,
  decimal openingOtherLossCarryForward,
  out decimal stockLossCarryForward,
  out decimal otherLossCarryForward)
 {
  decimal remainingStockGain = Math.Max(0m, realizedStockGains);
  decimal currentStockLoss = Math.Max(0m, -realizedStockGains);

  decimal remainingOtherIncome =
   Math.Max(0m, interest) +
   Math.Max(0m, stockDividends) +
   Math.Max(0m, equityFundDistributions) * EquityFundTaxableShare +
   Math.Max(0m, realizedEquityFundGains) * EquityFundTaxableShare;

  decimal currentOtherLoss =
   Math.Max(0m, -realizedEquityFundGains) * EquityFundTaxableShare;

  // 1. Aktienverluste des laufenden Jahres nur mit Aktienveräußerungsgewinnen.
  decimal currentStockOffset = Math.Min(remainingStockGain, currentStockLoss);
  remainingStockGain -= currentStockOffset;
  currentStockLoss -= currentStockOffset;

  // 2. Sonstige Verluste des laufenden Jahres mit positiven Kapitaleinkünften.
  decimal currentOtherOffset = Math.Min(remainingOtherIncome, currentOtherLoss);
  remainingOtherIncome -= currentOtherOffset;
  currentOtherLoss -= currentOtherOffset;

  decimal currentOtherAgainstStock = Math.Min(remainingStockGain, currentOtherLoss);
  remainingStockGain -= currentOtherAgainstStock;
  currentOtherLoss -= currentOtherAgainstStock;

  // 3. Aktien-Verlustvortrag nur mit den danach verbleibenden Aktiengewinnen.
  decimal availableStockLossCarry =
   Math.Max(0m, openingStockLossCarryForward) + currentStockLoss;
  decimal stockCarryOffset = Math.Min(remainingStockGain, availableStockLossCarry);
  remainingStockGain -= stockCarryOffset;
  availableStockLossCarry -= stockCarryOffset;

  // 4. Sonstiger Verlustvortrag mit den danach verbleibenden positiven Kapitaleinkünften.
  decimal availableOtherLossCarry =
   Math.Max(0m, openingOtherLossCarryForward) + currentOtherLoss;
  decimal otherCarryOffset = Math.Min(remainingOtherIncome, availableOtherLossCarry);
  remainingOtherIncome -= otherCarryOffset;
  availableOtherLossCarry -= otherCarryOffset;

  decimal otherCarryAgainstStock = Math.Min(remainingStockGain, availableOtherLossCarry);
  remainingStockGain -= otherCarryAgainstStock;
  availableOtherLossCarry -= otherCarryAgainstStock;

  stockLossCarryForward = availableStockLossCarry;
  otherLossCarryForward = availableOtherLossCarry;

  decimal taxable = remainingStockGain + remainingOtherIncome;

  // Sparer-Pauschbetrag erst nach Verlustverrechnung.
  taxable = Math.Max(0m, taxable - Math.Max(0m, allowance));

  decimal normalizedChurchTaxRate = Math.Max(0m, churchTaxRate);

  if (churchTaxEnabled && normalizedChurchTaxRate > 0m)
  {
   decimal incomeTax = taxable / (4m + normalizedChurchTaxRate);
   decimal churchTax = incomeTax * normalizedChurchTaxRate;
   decimal solidarity = incomeTax * SolidarityRate;
   return incomeTax + churchTax + solidarity;
  }

  decimal tax = taxable * CapitalGainsTaxRate;
  return tax + tax * SolidarityRate;
 }
 public static decimal CalculateCapitalTaxWithFavorableCheck(
  PlannerSettings s,
  int year,
  int age1,
  int age2,
  decimal regularTaxableIncome1,
  decimal regularTaxableIncome2,
  decimal interest,
  decimal stockDividends,
  decimal equityFundDistributions,
  decimal realizedStockGains,
  decimal realizedEquityFundGains,
  decimal allowance,
  decimal openingStockLossCarryForward,
  decimal openingOtherLossCarryForward,
  out decimal stockLossCarryForward,
  out decimal otherLossCarryForward)
 {
  decimal flatTax = CalculateCapitalTax(
   interest,
   stockDividends,
   equityFundDistributions,
   realizedStockGains,
   realizedEquityFundGains,
   allowance,
   s.ChurchTaxEnabled,
   s.ChurchTaxRate,
   openingStockLossCarryForward,
   openingOtherLossCarryForward,
   out stockLossCarryForward,
   out otherLossCarryForward);

  decimal remainingStockGain = Math.Max(0m, realizedStockGains);
  decimal currentStockLoss = Math.Max(0m, -realizedStockGains);

  decimal remainingOtherIncome =
   Math.Max(0m, interest) +
   Math.Max(0m, stockDividends) +
   Math.Max(0m, equityFundDistributions) * EquityFundTaxableShare +
   Math.Max(0m, realizedEquityFundGains) * EquityFundTaxableShare;

  decimal currentOtherLoss =
   Math.Max(0m, -realizedEquityFundGains) * EquityFundTaxableShare;

  decimal currentStockOffset = Math.Min(remainingStockGain, currentStockLoss);
  remainingStockGain -= currentStockOffset;
  currentStockLoss -= currentStockOffset;

  decimal currentOtherOffset = Math.Min(remainingOtherIncome, currentOtherLoss);
  remainingOtherIncome -= currentOtherOffset;
  currentOtherLoss -= currentOtherOffset;

  decimal currentOtherAgainstStock = Math.Min(remainingStockGain, currentOtherLoss);
  remainingStockGain -= currentOtherAgainstStock;
  currentOtherLoss -= currentOtherAgainstStock;

  decimal availableStockLossCarry =
   Math.Max(0m, openingStockLossCarryForward) + currentStockLoss;
  decimal stockCarryOffset = Math.Min(remainingStockGain, availableStockLossCarry);
  remainingStockGain -= stockCarryOffset;

  decimal availableOtherLossCarry =
   Math.Max(0m, openingOtherLossCarryForward) + currentOtherLoss;
  decimal otherCarryOffset = Math.Min(remainingOtherIncome, availableOtherLossCarry);
  remainingOtherIncome -= otherCarryOffset;
  availableOtherLossCarry -= otherCarryOffset;

  decimal otherCarryAgainstStock = Math.Min(remainingStockGain, availableOtherLossCarry);
  remainingStockGain -= otherCarryAgainstStock;

  decimal taxableCapitalIncome = Math.Max(
   0m,
   remainingStockGain +
   remainingOtherIncome -
   Math.Max(0m, allowance));

  if (taxableCapitalIncome <= 0m)
   return 0m;

  bool person1Included = age1 <= s.Person1EndAge;
  bool person2Included =
   s.HouseholdPersonCount == 2 &&
   age2 <= s.Person2EndAge;

  decimal regularTotalTax =
   PensionService.CalculateProjectedIncomeTaxIncludingSurcharges(
    s,
    year,
    regularTaxableIncome1,
    regularTaxableIncome2,
    person1Included,
    person2Included);

  decimal capitalIncome1 = 0m;
  decimal capitalIncome2 = 0m;

  if (s.JointTaxation &&
      person1Included &&
      person2Included)
  {
   capitalIncome1 = taxableCapitalIncome;
  }
  else if (person1Included && person2Included)
  {
   capitalIncome1 = taxableCapitalIncome / 2m;
   capitalIncome2 = taxableCapitalIncome - capitalIncome1;
  }
  else if (person2Included)
  {
   capitalIncome2 = taxableCapitalIncome;
  }
  else
  {
   capitalIncome1 = taxableCapitalIncome;
  }

  decimal combinedTariffTax =
   PensionService.CalculateProjectedIncomeTaxIncludingSurcharges(
    s,
    year,
    regularTaxableIncome1 + capitalIncome1,
    regularTaxableIncome2 + capitalIncome2,
    person1Included,
    person2Included);

  decimal favorableCapitalTax =
   Math.Max(0m, combinedTariffTax - regularTotalTax);

  return Math.Min(flatTax, favorableCapitalTax);
 }

}
