using System.Text.Json;

namespace RockefellerFiction;

public static class SettingsClone
{
 public static PlannerSettings Clone(PlannerSettings source)
 {
  string json = JsonSerializer.Serialize(source);
  return JsonSerializer.Deserialize<PlannerSettings>(json) ?? new PlannerSettings();
 }
}
