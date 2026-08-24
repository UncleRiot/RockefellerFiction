using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace RockefellerFiction;

public partial class UpdateWindow : Window
{
 public UpdateWindow(UpdateInfo update)
 {
  InitializeComponent();

  WindowBehavior.ApplyDarkTitleBar(this);

  VersionText.Text =
   $"Installiert: {update.CurrentVersion}   |   Neu: {update.LatestVersion}";

  SetLink(
   ReleaseHyperlink,
   ReleaseUrlRun,
   update.ReleaseUrl,
   "Kein Release-Link verfügbar.");

  SetLink(
   DownloadHyperlink,
   DownloadUrlRun,
   update.DownloadUrl,
   "Für dieses Release wurde keine ZIP-Datei als Release-Asset gefunden.");

  ChangelogText.Text = update.Changelog;
 }

 private static void SetLink(
  System.Windows.Documents.Hyperlink hyperlink,
  System.Windows.Documents.Run run,
  string url,
  string fallbackText)
 {
  if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
  {
   hyperlink.NavigateUri = uri;
   hyperlink.IsEnabled = true;
   run.Text = url;
   return;
  }

  hyperlink.NavigateUri = null;
  hyperlink.IsEnabled = false;
  run.Text = fallbackText;
 }

 private void Link_RequestNavigate(
  object sender,
  RequestNavigateEventArgs e)
 {
  Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
  {
   UseShellExecute = true
  });

  e.Handled = true;
 }

 private void Close_Click(object sender, RoutedEventArgs e)
 {
  Close();
 }
}
