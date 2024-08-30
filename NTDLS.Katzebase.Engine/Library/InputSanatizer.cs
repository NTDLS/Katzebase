namespace NTDLS.Katzebase.Engine.Library
{
    internal static class InputSanitizer
    {
        internal static string SanitizeUserInput(string? text)
        {
            if (double.TryParse(text, out var value))
            {
                return value.ToString();
            }
            return $"'{text}'";
        }
    }
}
