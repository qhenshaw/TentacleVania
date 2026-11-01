namespace TentacleVania.UI.Fonts
{
    public static class LocaleFontRules
    {
        public static string RegionFor(string localeCode)
        {
            if (string.IsNullOrEmpty(localeCode))
                return null;
            var code = localeCode.ToLowerInvariant();
            if (code.StartsWith("ja"))
                return "jp";
            if (code.StartsWith("ko"))
                return "kr";
            if (!code.StartsWith("zh"))
                return null;
            return code.Contains("hant") || code.EndsWith("-tw") || code.EndsWith("-hk") || code.EndsWith("-mo") ? "tc" : "sc";
        }
    }
}
