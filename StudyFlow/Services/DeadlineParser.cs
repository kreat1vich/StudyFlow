using System.Globalization;

namespace StudyFlow.Services;

public static class DeadlineParser
{
    private static readonly string[] Formats =
    {
        "dd.MM.yyyy",
        "dd.MM",
        "dd/MM/yyyy",
        "dd/MM",
        "yyyy-MM-dd"
    };

    public static bool TryParse(string? value, out DateTime date)
    {
        date = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var text = value.Trim();

        if (DateTime.TryParseExact(text, Formats, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var parsed))
        {
            date = parsed.Year == 1 ? parsed : parsed;

            if (text.Length == 5 && (text[2] == '.' || text[2] == '/'))
                date = new DateTime(DateTime.Today.Year, parsed.Month, parsed.Day);

            return true;
        }

        return DateTime.TryParse(text, CultureInfo.CurrentCulture,
            DateTimeStyles.AllowWhiteSpaces, out date);
    }
}
