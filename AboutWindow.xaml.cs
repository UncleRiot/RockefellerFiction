using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace RockefellerFiction;

public partial class AboutWindow : Window
{
 private const string GitHubRepositoryUrl = "https://github.com/UncleRiot/RockefellerFiction";
 private const string GitHubLatestReleaseApiUrl = "https://api.github.com/repos/UncleRiot/RockefellerFiction/releases/latest";
 private const string KoFiUrl = "https://ko-fi.com/uncleriot";

 private string? _updateUrl;

 public AboutWindow()
 {
  InitializeComponent();
  Background = (Brush)FindResource("BgBrush");
  Foreground = (Brush)FindResource("TextBrush");
  WindowBehavior.ApplyDarkTitleBar(this);

  VersionText.Text = GetApplicationVersionText();

  Loaded += AboutWindow_Loaded;
 }

 private async void AboutWindow_Loaded(object sender, RoutedEventArgs e)
 {
  await UpdateGitHubStatusAsync();
 }

 private static string GetApplicationVersionText()
 {
  Assembly assembly = Assembly.GetExecutingAssembly();

  string? informationalVersion =
   assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

  if (!string.IsNullOrWhiteSpace(informationalVersion))
  {
   int metadataIndex = informationalVersion.IndexOf('+');

   if (metadataIndex >= 0)
    informationalVersion = informationalVersion[..metadataIndex];

   return informationalVersion;
  }

  return assembly.GetName().Version?.ToString() ?? "unbekannt";
 }

 private async Task UpdateGitHubStatusAsync()
 {
  UpdateStatusText.Text = "Prüfe auf Updates...";
  UpdateStatusText.Foreground = (Brush)FindResource("MutedTextBrush");
  UpdateStatusText.Cursor = Cursors.Arrow;
  _updateUrl = null;

  try
  {
   using var client = new HttpClient
   {
    Timeout = TimeSpan.FromSeconds(10)
   };

   client.DefaultRequestHeaders.UserAgent.ParseAdd("RockefellerFiction");

   using HttpResponseMessage response =
    await client.GetAsync(GitHubLatestReleaseApiUrl);

   if (!response.IsSuccessStatusCode)
   {
    UpdateStatusText.Text = "Updateprüfung nicht verfügbar.";
    return;
   }

   string json = await response.Content.ReadAsStringAsync();

   using JsonDocument document = JsonDocument.Parse(json);

   JsonElement root = document.RootElement;

   string latestTag =
    root.TryGetProperty("tag_name", out JsonElement tagElement)
     ? tagElement.GetString() ?? string.Empty
     : string.Empty;

   string releaseUrl =
    root.TryGetProperty("html_url", out JsonElement urlElement)
     ? urlElement.GetString() ?? string.Empty
     : string.Empty;

   Version? currentVersion = ParseVersion(GetApplicationVersionText());
   Version? latestVersion = ParseVersion(latestTag);

   if (currentVersion == null || latestVersion == null)
   {
    UpdateStatusText.Text = "Updateprüfung nicht verfügbar.";
    return;
   }

   if (latestVersion > currentVersion)
   {
    UpdateStatusText.Text = $"Neue Version verfügbar: {latestTag}";
    UpdateStatusText.Foreground = (Brush)FindResource("AccentBrush");
    UpdateStatusText.Cursor = Cursors.Hand;
    _updateUrl = string.IsNullOrWhiteSpace(releaseUrl)
     ? GitHubRepositoryUrl + "/releases"
     : releaseUrl;
    return;
   }

   UpdateStatusText.Text = "Keine neue Version verfügbar.";
  }
  catch
  {
   UpdateStatusText.Text = "Updateprüfung nicht verfügbar.";
  }
 }

 private static Version? ParseVersion(string value)
 {
  if (string.IsNullOrWhiteSpace(value))
   return null;

  string normalized = value.Trim();

  if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
   normalized = normalized[1..];

  int metadataIndex = normalized.IndexOf('+');

  if (metadataIndex >= 0)
   normalized = normalized[..metadataIndex];

  int prereleaseIndex = normalized.IndexOf('-');

  if (prereleaseIndex >= 0)
   normalized = normalized[..prereleaseIndex];

  return Version.TryParse(normalized, out Version? version)
   ? version
   : null;
 }

 private void UpdateStatusText_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
 {
  if (string.IsNullOrWhiteSpace(_updateUrl))
   return;

  OpenUrl(_updateUrl);
 }

 private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
 {
  OpenUrl(e.Uri.AbsoluteUri);
  e.Handled = true;
 }

 private static void OpenUrl(string url)
 {
  Process.Start(new ProcessStartInfo
  {
   FileName = url,
   UseShellExecute = true
  });
 }

 private void KoFiImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
 {
  OpenUrl(KoFiUrl);
 }

 private void Ok_Click(object sender, RoutedEventArgs e)
 {
  Close();
 }
}
