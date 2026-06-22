using FNFBot.Services;

namespace FNFBot.UI;

public sealed class KeyCaptureTextBox : TextBox
{
    private Keys _selectedKey;

    public KeyCaptureTextBox()
    {
        ReadOnly = true;
        ShortcutsEnabled = false;
        TextAlign = HorizontalAlignment.Center;
        Width = 105;
        Cursor = Cursors.Hand;
    }

    public Keys SelectedKey
    {
        get => _selectedKey;
        set
        {
            _selectedKey = value & Keys.KeyCode;
            Text = _selectedKey == Keys.None
                ? "(none)"
                : KeyNames.ToDisplayName(_selectedKey);
        }
    }

    public bool AllowEmpty { get; init; }

    protected override bool IsInputKey(Keys keyData)
    {
        return true;
    }

    protected override void OnKeyDown(KeyEventArgs eventArgs)
    {
        eventArgs.SuppressKeyPress = true;
        eventArgs.Handled = true;

        if (AllowEmpty && eventArgs.KeyCode is Keys.Back or Keys.Delete or Keys.Escape)
        {
            SelectedKey = Keys.None;
            return;
        }

        if (eventArgs.KeyCode is Keys.ShiftKey or Keys.ControlKey or Keys.Menu or
            Keys.LWin or Keys.RWin)
        {
            return;
        }

        SelectedKey = eventArgs.KeyCode;
    }

    protected override void OnMouseDown(MouseEventArgs eventArgs)
    {
        base.OnMouseDown(eventArgs);
        Focus();
        SelectAll();
    }
}
