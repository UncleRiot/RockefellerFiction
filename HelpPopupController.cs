using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace RockefellerFiction;

public sealed class HelpPopupController
{
 private Popup? _pinnedPopup;

 public void ShowHover(FrameworkElement anchor, string text)
 {
  if (_pinnedPopup != null) return;

  var toolTip = CreateToolTip(text);
  toolTip.PlacementTarget = anchor;
  anchor.ToolTip = toolTip;
  toolTip.IsOpen = true;
 }

 public void ClearHover(FrameworkElement anchor)
 {
  if (_pinnedPopup != null) return;

  if (anchor.ToolTip is ToolTip toolTip)
   toolTip.IsOpen = false;

  anchor.ToolTip = null;
 }

 public void TogglePinned(FrameworkElement anchor, string text)
 {
  ClosePinned();

  var border = new Border
  {
   Background = System.Windows.Media.Brushes.White,
   CornerRadius = new CornerRadius(6),
   Padding = new Thickness(12),
   MaxWidth = 360,
   Child = new TextBlock
   {
    Text = text,
    TextWrapping = TextWrapping.Wrap,
    Foreground = System.Windows.Media.Brushes.Black,
    FontSize = 13
   }
  };

  _pinnedPopup = new Popup
  {
   PlacementTarget = anchor,
   Placement = PlacementMode.Right,
   StaysOpen = true,
   AllowsTransparency = true,
   Child = border,
   HorizontalOffset = 8
  };

  _pinnedPopup.IsOpen = true;
 }

 public void ClosePinned()
 {
  if (_pinnedPopup == null) return;
  _pinnedPopup.IsOpen = false;
  _pinnedPopup = null;
 }

 private static ToolTip CreateToolTip(string text) =>
  new()
  {
   Background = System.Windows.Media.Brushes.White,
   Foreground = System.Windows.Media.Brushes.Black,
   Content = new TextBlock
   {
    Text = text,
    TextWrapping = TextWrapping.Wrap,
    MaxWidth = 360,
    Foreground = System.Windows.Media.Brushes.Black
   },
   Placement = PlacementMode.Right
  };
}
