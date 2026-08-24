using System.Windows;

namespace RockefellerFiction;

public partial class App : Application
{
 protected override async void OnStartup(StartupEventArgs e)
 {
  base.OnStartup(e);

  UpdateInfo? update = await UpdateService.CheckForUpdateAsync();
  if (update == null)
   return;

  var window = new UpdateWindow(update);

  if (MainWindow != null && MainWindow.IsLoaded)
   window.Owner = MainWindow;

  window.ShowDialog();
 }
}
