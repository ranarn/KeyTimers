using System.Windows;
using System.Windows.Input;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace KeyTimers.Views;

/// <summary>
/// Read-only TextBox that captures the next key press instead of accepting typed input.
/// Click to focus, then press any key to store its name.
/// </summary>
public sealed class KeyCaptureTextBox : WpfTextBox
{
    private string _savedText = "";

    public KeyCaptureTextBox()
    {
        IsReadOnly  = true;
        Cursor      = System.Windows.Input.Cursors.Hand;
        ToolTip     = "Click, then press a key to capture it";
        ContextMenu = null;
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        _savedText = Text;
        Text       = "Press a key…";
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        if (Text == "Press a key…") Text = _savedText;
        base.OnLostFocus(e);
    }

    protected override void OnPreviewKeyDown(WpfKeyEventArgs e)
    {
        // resolve the real key when e.g. Alt is held
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // ignore bare modifier and navigation keys
        if (key is Key.LeftShift or Key.RightShift or
                   Key.LeftCtrl  or Key.RightCtrl  or
                   Key.LeftAlt   or Key.RightAlt   or
                   Key.LWin      or Key.RWin        or
                   Key.Apps      or Key.Tab)
            return;

        var name = WpfKeyToName(key);
        if (name is null) return;

        Text = name;
        _savedText = name;
        GetBindingExpression(TextProperty)?.UpdateSource();
        e.Handled = true;
    }

    private static string? WpfKeyToName(Key key) => key switch
    {
        >= Key.A and <= Key.Z             => key.ToString(),
        >= Key.D0 and <= Key.D9           => ((char)('0' + (key - Key.D0))).ToString(),
        >= Key.NumPad0 and <= Key.NumPad9 => $"NUM{key - Key.NumPad0}",
        >= Key.F1 and <= Key.F12          => key.ToString(),
        Key.Space             => "SPACE",
        Key.Back              => "BACK",
        Key.Return            => "ENTER",
        Key.Escape            => "ESC",
        Key.Delete            => "DELETE",
        Key.Insert            => "INSERT",
        Key.Home              => "HOME",
        Key.End               => "END",
        Key.PageUp            => "PGUP",
        Key.PageDown          => "PGDN",
        Key.Left              => "LEFT",
        Key.Up                => "UP",
        Key.Right             => "RIGHT",
        Key.Down              => "DOWN",
        Key.Pause             => "PAUSE",
        Key.OemSemicolon      => ";",
        Key.OemPlus           => "=",
        Key.OemComma          => ",",
        Key.OemMinus          => "-",
        Key.OemPeriod         => ".",
        Key.OemQuestion       => "/",
        Key.OemTilde          => "`",
        Key.OemOpenBrackets   => "[",
        Key.OemPipe           => "\\",
        Key.OemCloseBrackets  => "]",
        Key.OemQuotes         => "'",
        _                     => null
    };
}
