using System.Text.RegularExpressions;

namespace STak.TakHub.Helpers
{
    public static partial class Strings
    {
        [GeneratedRegex(@"p{C}+")]
        private static partial Regex GetNonPrintableCharacterRegex();
        private static Regex NonPrintableCharacterRegex = GetNonPrintableCharacterRegex();


        public static string RemoveAllNonPrintableCharacters(string target)
        {
            return NonPrintableCharacterRegex.Replace(target, string.Empty);
        }
    }
}
