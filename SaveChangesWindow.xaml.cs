using System.Windows;
using System.Windows.Media;

namespace RockefellerFiction;

public enum SaveChangesChoice
{
 Cancel,
 Save,
 Discard
}

public partial class SaveChangesWindow : Window
{
 public SaveChangesChoice Choice { get; private set; } = SaveChangesChoice.Cancel;

 public SaveChangesWindow()
 {
  InitializeComponent();
  Background = (Brush)FindResource("BgBrush");
  Foreground = (Brush)FindResource("TextBrush");
  WindowBehavior.ApplyDarkTitleBar(this);
 }

 private void Save_Click(object sender, RoutedEventArgs e)
 {
  Choice = SaveChangesChoice.Save;
  DialogResult = true;
 }

 private void Discard_Click(object sender, RoutedEventArgs e)
 {
  Choice = SaveChangesChoice.Discard;
  DialogResult = true;
 }

 private void Cancel_Click(object sender, RoutedEventArgs e)
 {
  Choice = SaveChangesChoice.Cancel;
  DialogResult = false;
 }
}
