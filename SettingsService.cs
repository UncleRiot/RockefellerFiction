using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace RockefellerFiction;

public sealed record SettingsImportResult(
 PlannerSettings Settings,
 StrategyAllocation Allocation);

public static class SettingsService
{
 private const int CurrentExportFormatVersion = 1;

 private static readonly JsonSerializerOptions JsonOptions = new()
 {
  WriteIndented = true,
  PropertyNameCaseInsensitive = true
 };

 private static readonly JsonSerializerOptions CompactJsonOptions = new()
 {
  WriteIndented = false,
  PropertyNameCaseInsensitive = true
 };

 public static PlannerSettings Load()
 {
  string path = FindSettingsPath();

  if (!File.Exists(path))
   return new PlannerSettings();

  try
  {
   string json = File.ReadAllText(path);
   return DeserializePlannerSettings(json);
  }
  catch
  {
   return new PlannerSettings();
  }
 }

 public static SettingsImportResult LoadWithAllocation()
 {
  string path = FindSettingsPath();

  if (!File.Exists(path))
  {
   var settings = new PlannerSettings();
   return new SettingsImportResult(
    settings,
    StrategyService.GetDefault(settings.Strategy));
  }

  try
  {
   string json = File.ReadAllText(path);
   return DeserializeSettingsImportResult(json);
  }
  catch
  {
   var settings = new PlannerSettings();
   return new SettingsImportResult(
    settings,
    StrategyService.GetDefault(settings.Strategy));
  }
 }

 public static void Save(PlannerSettings settings)
 {
  string path = FindSettingsPath();
  EnsureSettingsDirectory(path);

  string json = JsonSerializer.Serialize(settings, JsonOptions);
  File.WriteAllText(path, json);
 }

 public static void Save(
  PlannerSettings settings,
  StrategyAllocation allocation)
 {
  string path = FindSettingsPath();
  EnsureSettingsDirectory(path);

  var package = new SettingsExportPackage
  {
   FormatVersion = CurrentExportFormatVersion,
   Settings = settings,
   Allocation = allocation
  };

  string json = JsonSerializer.Serialize(package, JsonOptions);
  File.WriteAllText(path, json);
 }

 public static void Export(
  string path,
  PlannerSettings settings,
  StrategyAllocation allocation)
 {
  string extension = Path.GetExtension(path).ToLowerInvariant();

  switch (extension)
  {
   case ".json":
    ExportJson(path, settings, allocation);
    break;

   case ".csv":
    ExportCsv(path, settings, allocation);
    break;

   default:
    throw new InvalidOperationException("Unterstützte Exportformate sind .json und .csv.");
  }
 }

 public static SettingsImportResult Import(string path)
 {
  if (!File.Exists(path))
   throw new FileNotFoundException("Die Importdatei wurde nicht gefunden.", path);

  string extension = Path.GetExtension(path).ToLowerInvariant();

  return extension switch
  {
   ".json" => ImportJson(path),
   ".csv" => ImportCsv(path),
   _ => throw new InvalidOperationException("Unterstützte Importformate sind .json und .csv.")
  };
 }

 private static void ExportJson(
  string path,
  PlannerSettings settings,
  StrategyAllocation allocation)
 {
  var package = new SettingsExportPackage
  {
   FormatVersion = CurrentExportFormatVersion,
   Settings = settings,
   Allocation = allocation
  };

  string json = JsonSerializer.Serialize(package, JsonOptions);
  File.WriteAllText(path, json, Encoding.UTF8);
 }

 private static SettingsImportResult ImportJson(string path)
 {
  string json = File.ReadAllText(path, Encoding.UTF8);

  using JsonDocument document = JsonDocument.Parse(json);
  JsonElement root = document.RootElement;

  if (root.ValueKind == JsonValueKind.Object &&
      root.TryGetProperty("Settings", out _))
  {
   SettingsExportPackage? package =
    JsonSerializer.Deserialize<SettingsExportPackage>(json, JsonOptions);

   if (package?.Settings == null)
    throw new InvalidOperationException("Die JSON-Datei enthält keine gültigen Planungseinstellungen.");

   StrategyAllocation allocation =
    package.Allocation ?? StrategyService.GetDefault(package.Settings.Strategy);

   return new SettingsImportResult(package.Settings, allocation);
  }

  PlannerSettings legacySettings =
   JsonSerializer.Deserialize<PlannerSettings>(json, JsonOptions)
   ?? throw new InvalidOperationException("Die JSON-Datei enthält keine gültigen Planungseinstellungen.");

  return new SettingsImportResult(
   legacySettings,
   StrategyService.GetDefault(legacySettings.Strategy));
 }

 private static void ExportCsv(
  string path,
  PlannerSettings settings,
  StrategyAllocation allocation)
 {
  using var writer = new StreamWriter(path, false, new UTF8Encoding(true));

  WriteCsvRow(writer, "Section", "Key", "Value");
  WriteCsvRow(
   writer,
   "Meta",
   "FormatVersion",
   CurrentExportFormatVersion.ToString(CultureInfo.InvariantCulture));

  foreach (var property in typeof(PlannerSettings).GetProperties())
  {
   if (!property.CanRead)
    continue;

   object? value = property.GetValue(settings);
   string serializedValue =
    JsonSerializer.Serialize(value, property.PropertyType, CompactJsonOptions);

   WriteCsvRow(writer, "Settings", property.Name, serializedValue);
  }

  WriteCsvRow(
   writer,
   "Allocation",
   nameof(StrategyAllocation.Cash),
   JsonSerializer.Serialize(allocation.Cash, CompactJsonOptions));

  WriteCsvRow(
   writer,
   "Allocation",
   nameof(StrategyAllocation.WorldEtf),
   JsonSerializer.Serialize(allocation.WorldEtf, CompactJsonOptions));

  WriteCsvRow(
   writer,
   "Allocation",
   nameof(StrategyAllocation.DividendEtf),
   JsonSerializer.Serialize(allocation.DividendEtf, CompactJsonOptions));

  WriteCsvRow(
   writer,
   "Allocation",
   nameof(StrategyAllocation.DividendStocks),
   JsonSerializer.Serialize(allocation.DividendStocks, CompactJsonOptions));
 }

 private static SettingsImportResult ImportCsv(string path)
 {
  var settings = new PlannerSettings();

  decimal? cash = null;
  decimal? worldEtf = null;
  decimal? dividendEtf = null;
  decimal? dividendStocks = null;

  bool headerRead = false;

  foreach (string line in File.ReadLines(path, Encoding.UTF8))
  {
   if (string.IsNullOrWhiteSpace(line))
    continue;

   string[] columns = ParseCsvLine(line);

   if (!headerRead)
   {
    headerRead = true;
    continue;
   }

   if (columns.Length < 3)
    continue;

   string section = columns[0];
   string key = columns[1];
   string value = columns[2];

   if (section.Equals("Meta", StringComparison.OrdinalIgnoreCase))
    continue;

   if (section.Equals("Settings", StringComparison.OrdinalIgnoreCase))
   {
    var property = typeof(PlannerSettings)
     .GetProperties()
     .FirstOrDefault(x =>
      x.CanWrite &&
      x.Name.Equals(key, StringComparison.OrdinalIgnoreCase));

    if (property == null)
     continue;

    object? parsedValue =
     JsonSerializer.Deserialize(value, property.PropertyType, CompactJsonOptions);

    property.SetValue(settings, parsedValue);
    continue;
   }

   if (!section.Equals("Allocation", StringComparison.OrdinalIgnoreCase))
    continue;

   decimal parsedAllocation =
    JsonSerializer.Deserialize<decimal>(value, CompactJsonOptions);

   if (key.Equals(nameof(StrategyAllocation.Cash), StringComparison.OrdinalIgnoreCase))
    cash = parsedAllocation;
   else if (key.Equals(nameof(StrategyAllocation.WorldEtf), StringComparison.OrdinalIgnoreCase))
    worldEtf = parsedAllocation;
   else if (key.Equals(nameof(StrategyAllocation.DividendEtf), StringComparison.OrdinalIgnoreCase))
    dividendEtf = parsedAllocation;
   else if (key.Equals(nameof(StrategyAllocation.DividendStocks), StringComparison.OrdinalIgnoreCase))
    dividendStocks = parsedAllocation;
  }

  StrategyAllocation defaultAllocation =
   StrategyService.GetDefault(settings.Strategy);

  var allocation = new StrategyAllocation(
   cash ?? defaultAllocation.Cash,
   worldEtf ?? defaultAllocation.WorldEtf,
   dividendEtf ?? defaultAllocation.DividendEtf,
   dividendStocks ?? defaultAllocation.DividendStocks);

  return new SettingsImportResult(settings, allocation);
 }

 private static SettingsImportResult DeserializeSettingsImportResult(string json)
 {
  using JsonDocument document = JsonDocument.Parse(json);
  JsonElement root = document.RootElement;

  if (root.ValueKind == JsonValueKind.Object &&
      root.TryGetProperty("Settings", out _))
  {
   SettingsExportPackage? package =
    JsonSerializer.Deserialize<SettingsExportPackage>(json, JsonOptions);

   PlannerSettings settings = package?.Settings ?? new PlannerSettings();
   StrategyAllocation allocation =
    package?.Allocation ?? StrategyService.GetDefault(settings.Strategy);

   return new SettingsImportResult(settings, allocation);
  }

  PlannerSettings legacySettings =
   JsonSerializer.Deserialize<PlannerSettings>(json, JsonOptions)
   ?? new PlannerSettings();

  return new SettingsImportResult(
   legacySettings,
   StrategyService.GetDefault(legacySettings.Strategy));
 }

 private static PlannerSettings DeserializePlannerSettings(string json)
 {
  using JsonDocument document = JsonDocument.Parse(json);
  JsonElement root = document.RootElement;

  if (root.ValueKind == JsonValueKind.Object &&
      root.TryGetProperty("Settings", out JsonElement settingsElement))
  {
   return JsonSerializer.Deserialize<PlannerSettings>(
     settingsElement.GetRawText(),
     JsonOptions)
    ?? new PlannerSettings();
  }

  return JsonSerializer.Deserialize<PlannerSettings>(json, JsonOptions)
   ?? new PlannerSettings();
 }

 private static void WriteCsvRow(
  TextWriter writer,
  string section,
  string key,
  string value)
 {
  writer.Write(EscapeCsv(section));
  writer.Write(',');
  writer.Write(EscapeCsv(key));
  writer.Write(',');
  writer.WriteLine(EscapeCsv(value));
 }

 private static string EscapeCsv(string value)
 {
  if (!value.Contains(',') &&
      !value.Contains('"') &&
      !value.Contains('\r') &&
      !value.Contains('\n'))
   return value;

  return "\"" + value.Replace("\"", "\"\"") + "\"";
 }

 private static string[] ParseCsvLine(string line)
 {
  var values = new List<string>();
  var current = new StringBuilder();
  bool inQuotes = false;

  for (int i = 0; i < line.Length; i++)
  {
   char currentChar = line[i];

   if (currentChar == '"')
   {
    if (inQuotes &&
        i + 1 < line.Length &&
        line[i + 1] == '"')
    {
     current.Append('"');
     i++;
    }
    else
    {
     inQuotes = !inQuotes;
    }

    continue;
   }

   if (currentChar == ',' && !inQuotes)
   {
    values.Add(current.ToString());
    current.Clear();
    continue;
   }

   current.Append(currentChar);
  }

  values.Add(current.ToString());
  return values.ToArray();
 }

 private static string FindSettingsPath()
 {
  return Path.Combine(
   AppContext.BaseDirectory,
   "settings.json");
 }

 private static void EnsureSettingsDirectory(string path)
 {
  string? directory = Path.GetDirectoryName(path);

  if (!string.IsNullOrWhiteSpace(directory))
   Directory.CreateDirectory(directory);
 }

 private sealed class SettingsExportPackage
 {
  public int FormatVersion { get; set; } = CurrentExportFormatVersion;
  public PlannerSettings Settings { get; set; } = new();
  public StrategyAllocation? Allocation { get; set; }
 }
}
