namespace FnfBot.Services;

public static class KeyNames
{
    private static readonly IReadOnlyDictionary<string, Keys> Aliases =
        new Dictionary<string, Keys>(StringComparer.OrdinalIgnoreCase)
        {
            ["LeftArrow"] = Keys.Left,
            ["RightArrow"] = Keys.Right,
            ["UpArrow"] = Keys.Up,
            ["DownArrow"] = Keys.Down,
            ["."] = Keys.OemPeriod,
            [">"] = Keys.OemPeriod,
            ["/"] = Keys.OemQuestion,
            ["?"] = Keys.OemQuestion,
            [","] = Keys.Oemcomma,
            ["<"] = Keys.Oemcomma,
            [";"] = Keys.OemSemicolon,
            [":"] = Keys.OemSemicolon,
            ["'"] = Keys.OemQuotes,
            ["\""] = Keys.OemQuotes,
            ["["] = Keys.OemOpenBrackets,
            ["{"] = Keys.OemOpenBrackets,
            ["]"] = Keys.OemCloseBrackets,
            ["}"] = Keys.OemCloseBrackets,
            ["\\"] = Keys.OemPipe,
            ["|"] = Keys.OemPipe,
            ["-"] = Keys.OemMinus,
            ["_"] = Keys.OemMinus,
            ["="] = Keys.Oemplus,
            ["+"] = Keys.Oemplus,
            ["`"] = Keys.Oemtilde,
            ["~"] = Keys.Oemtilde
        };

    public static bool TryParse(string? value, out Keys key)
    {
        key = Keys.None;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        if (!Aliases.TryGetValue(trimmed, out Keys parsed) &&
            !Enum.TryParse(trimmed, true, out parsed))
        {
            return false;
        }

        Keys keyCode = parsed & Keys.KeyCode;
        if (keyCode == Keys.None || parsed != keyCode || (int)keyCode > ushort.MaxValue)
        {
            return false;
        }

        key = keyCode;
        return true;
    }

    public static string ToDisplayName(Keys key)
    {
        return key switch
        {
            Keys.OemPeriod => ".",
            Keys.OemQuestion => "/",
            Keys.Oemcomma => ",",
            Keys.OemSemicolon => ";",
            Keys.OemQuotes => "'",
            Keys.OemOpenBrackets => "[",
            Keys.OemCloseBrackets => "]",
            Keys.OemPipe => "\\",
            Keys.OemMinus => "-",
            Keys.Oemplus => "=",
            Keys.Oemtilde => "`",
            Keys.Left => "Left Arrow",
            Keys.Right => "Right Arrow",
            Keys.Up => "Up Arrow",
            Keys.Down => "Down Arrow",
            _ => key.ToString()
        };
    }

    public static string ToSettingsName(Keys key)
    {
        return key switch
        {
            Keys.OemPeriod => ".",
            Keys.OemQuestion => "/",
            Keys.Oemcomma => ",",
            Keys.OemSemicolon => ";",
            Keys.OemQuotes => "'",
            Keys.OemOpenBrackets => "[",
            Keys.OemCloseBrackets => "]",
            Keys.OemPipe => "\\",
            Keys.OemMinus => "-",
            Keys.Oemplus => "=",
            Keys.Oemtilde => "`",
            _ => key.ToString()
        };
    }
}
