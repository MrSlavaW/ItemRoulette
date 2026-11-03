using System;
using System.Collections.Generic;
using ItemRoulette.Configuration;
using RoR2;

namespace ItemRoulette.Core
{
    public enum ModLanguage
    {
        Auto = 0,
        English = 1,
        Russian = 2
    }

    public static class Localization
    {
        private static readonly Dictionary<string, string> _fallbackTokensRu = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _fallbackTokensEn = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Populates internal fallback token dictionary so strings never fail even if game engine language tables are missing.
        /// </summary>
        public static void SetFallbackTokens(string lang, Dictionary<string, string> tokens)
        {
            if (tokens == null) return;

            var target = (lang != null && lang.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
                ? _fallbackTokensRu
                : _fallbackTokensEn;

            lock (target)
            {
                foreach (var kvp in tokens)
                {
                    target[kvp.Key] = kvp.Value;
                }
            }
        }

        public static bool IsRussian
        {
            get
            {
                var setting = ModConfig.LanguageSetting != null ? ModConfig.LanguageSetting.Value : ModLanguage.Auto;
                if (setting == ModLanguage.Russian) return true;
                if (setting == ModLanguage.English) return false;

                // Auto mode: check RoR2.Language.currentLanguageName
                try
                {
                    string? lang = Language.currentLanguageName;
                    if (string.IsNullOrEmpty(lang)) return false;
                    return lang.StartsWith("ru", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Retrieves localized text by RoR2 Language token (e.g. "ITEMROULETTE_SPIN_BTN").
        /// Falls back to token itself if not found in language dictionaries.
        /// </summary>
        public static string Get(string token)
        {
            if (string.IsNullOrEmpty(token)) return string.Empty;

            try
            {
                if (Language.currentLanguage != null && Language.currentLanguage.TokenIsRegistered(token))
                {
                    return Language.GetString(token);
                }
            }
            catch { }

            // Fallback dictionary lookup
            var primaryFallback = IsRussian ? _fallbackTokensRu : _fallbackTokensEn;
            if (primaryFallback.TryGetValue(token, out string? val) && !string.IsNullOrEmpty(val))
            {
                return val;
            }

            if (_fallbackTokensEn.TryGetValue(token, out string? valEn) && !string.IsNullOrEmpty(valEn))
            {
                return valEn;
            }

            return token;
        }

        /// <summary>
        /// Retrieves localized text by token, with inline fallbacks if token is not registered.
        /// </summary>
        public static string Get(string token, string fallbackRu, string fallbackEn)
        {
            if (string.IsNullOrEmpty(token)) return IsRussian ? fallbackRu : fallbackEn;

            try
            {
                if (Language.currentLanguage != null && Language.currentLanguage.TokenIsRegistered(token))
                {
                    return Language.GetString(token);
                }
            }
            catch { }

            var primaryFallback = IsRussian ? _fallbackTokensRu : _fallbackTokensEn;
            if (primaryFallback.TryGetValue(token, out string? val) && !string.IsNullOrEmpty(val))
            {
                return val;
            }

            return IsRussian ? fallbackRu : fallbackEn;
        }

        /// <summary>
        /// Legacy bilingual Get overload for compatibility with existing inline calls.
        /// If first argument starts with ITEMROULETTE_ token, evaluates it through Language.GetString.
        /// Otherwise returns ru if IsRussian is true, else en.
        /// </summary>
        public static string Get(string ru, string en)
        {
            if (!string.IsNullOrEmpty(ru) && ru.StartsWith("ITEMROULETTE_"))
            {
                return Get(ru, ru, en);
            }
            return IsRussian ? ru : en;
        }

        /// <summary>
        /// Formats localized string by token using RoR2 game engine localization.
        /// </summary>
        public static string Format(string token, params object[] args)
        {
            if (string.IsNullOrEmpty(token)) return string.Empty;

            try
            {
                if (Language.currentLanguage != null && Language.currentLanguage.TokenIsRegistered(token))
                {
                    return Language.GetStringFormatted(token, args);
                }
            }
            catch { }

            string template = Get(token);
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }

        /// <summary>
        /// Retrieves fallback token value for a specific language ("ru" or "en").
        /// </summary>
        public static string GetForLang(string lang, string token)
        {
            if (string.IsNullOrEmpty(token)) return string.Empty;

            var target = (lang != null && lang.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
                ? _fallbackTokensRu
                : _fallbackTokensEn;

            lock (target)
            {
                if (target.TryGetValue(token, out string? val) && !string.IsNullOrEmpty(val))
                {
                    return val;
                }
            }

            return token;
        }

        /// <summary>
        /// Formats a token for a specific language ("ru" or "en").
        /// </summary>
        public static string FormatForLang(string lang, string token, params object[] args)
        {
            string template = GetForLang(lang, token);
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }

        /// <summary>
        /// Explicit token-based formatting using RoR2 game engine localization with safe fallback.
        /// </summary>
        public static string FormatToken(string token, params object[] args)
        {
            return Format(token, args);
        }

        /// <summary>
        /// Legacy bilingual Format overload.
        /// If first argument starts with ITEMROULETTE_ token, evaluates it via Language.GetStringFormatted.
        /// Otherwise evaluates template based on IsRussian and formats it.
        /// </summary>
        public static string Format(string ru, string en, params object[] args)
        {
            if (!string.IsNullOrEmpty(ru) && ru.StartsWith("ITEMROULETTE_"))
            {
                // The caller called Localization.Format(token, arg0, arg1, ...) where arg0 was a string.
                // C# overload resolution routed it here because 'en' is typed string.
                // Prepend 'en' back into the arguments array so no parameters are lost!
                object[] fullArgs = new object[args.Length + 1];
                fullArgs[0] = en;
                Array.Copy(args, 0, fullArgs, 1, args.Length);
                return Format(ru, fullArgs);
            }

            string template = IsRussian ? ru : en;
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }
    }
}
