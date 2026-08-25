using System.IO;
using System.Text.Json;

namespace RockefellerFiction;

public static class HintService
{
 private static readonly string HintsFilePath = Path.Combine(
  AppContext.BaseDirectory,
  "hints.json");

 public static string Get(int fieldNumber)
 {
  try
  {
   if (!File.Exists(HintsFilePath))
    return $"Kein Hinweis für Feld {fieldNumber:00} hinterlegt.";

   string json = File.ReadAllText(HintsFilePath);
   Dictionary<string, string>? hints =
    JsonSerializer.Deserialize<Dictionary<string, string>>(json);

   if (hints == null)
    return $"Kein Hinweis für Feld {fieldNumber:00} hinterlegt.";

   string prefix = $"{fieldNumber:00} - ";

   foreach (KeyValuePair<string, string> entry in hints)
   {
    if (entry.Key.StartsWith(prefix, StringComparison.Ordinal))
     return entry.Value;
   }

   return $"Kein Hinweis für Feld {fieldNumber:00} hinterlegt.";
  }
  catch
  {
   return $"Hinweisdatei konnte für Feld {fieldNumber:00} nicht gelesen werden.";
  }
 }

 public static string Get(string key)
 {
  try
  {
   if (!File.Exists(HintsFilePath))
    return $"Kein Hinweis für „{key}“ hinterlegt.";

   string json = File.ReadAllText(HintsFilePath);
   Dictionary<string, string>? hints =
    JsonSerializer.Deserialize<Dictionary<string, string>>(json);

   if (hints == null ||
       !hints.TryGetValue(key, out string? hint) ||
       string.IsNullOrWhiteSpace(hint))
    return $"Kein Hinweis für „{key}“ hinterlegt.";

   return hint;
  }
  catch
  {
   return $"Hinweisdatei konnte für „{key}“ nicht gelesen werden.";
  }
 }



 public static string GetFieldLabel(int fieldNumber)
 {
  try
  {
   if (!File.Exists(HintsFilePath))
    return $"Feld {fieldNumber:00}";

   string json = File.ReadAllText(HintsFilePath);
   Dictionary<string, string>? hints =
    JsonSerializer.Deserialize<Dictionary<string, string>>(json);

   if (hints == null)
    return $"Feld {fieldNumber:00}";

   string prefix = $"{fieldNumber:00} - ";

   foreach (string key in hints.Keys)
   {
    if (key.StartsWith(prefix, StringComparison.Ordinal))
     return key[prefix.Length..];
   }

   return $"Feld {fieldNumber:00}";
  }
  catch
  {
   return $"Feld {fieldNumber:00}";
  }
 }
}
