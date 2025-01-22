using System;
using System.Collections.Generic;
using System.IO;
using Application = UnityEngine.Device.Application;

namespace GrumpyFoxGames
{
    public static class INIReader
    {
        private static readonly Dictionary<string, Dictionary<string, string>> _cache =
            new(StringComparer.OrdinalIgnoreCase);

        private static bool _isInitialized = false;
        private const string _fileName = "settings.ini";

        /// <summary>
        /// Gets a value from the .ini file for a specific section and key.
        /// If the file has not been initialized, it initializes automatically.
        /// </summary>
        /// <param name="section">The section to look in.</param>
        /// <param name="key">The key to retrieve the value for.</param>
        /// <returns>The value associated with the key in the section.</returns>
        public static string GetValue(string section, string key)
        {
#if UNITY_ANDROID
            return string.Empty;
#endif
            
            if (!_isInitialized)
            {
                if (string.IsNullOrEmpty(_fileName))
                {
                    throw new InvalidOperationException(
                        "INIReader must be initialized with a file name using SetFileName().");
                }

                Initialize();
            }

            if (_cache.TryGetValue(section, out var sectionDict) && sectionDict.TryGetValue(key, out var value))
            {
                return value;
            }

            throw new Exception($"Key '{key}' not found in section '{section}' of INI file.");
        }

        /// <summary>
        /// Initializes the INIReader by loading the specified .ini file.
        /// </summary>
        private static void Initialize()
        {
            var exePath = Directory.GetParent(Application.dataPath).ToString();
            var filePath = Path.Combine(exePath, _fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"INI file not found: {filePath}");
            }

            string currentSection = null;
            foreach (var line in File.ReadAllLines(filePath))
            {
                string trimmedLine = line.Trim();

                // Skip comments and empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";") ||
                    trimmedLine.StartsWith("#"))
                    continue;

                // Detect section headers
                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2).Trim();
                    if (!_cache.ContainsKey(currentSection))
                    {
                        _cache[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }

                    continue;
                }

                // Parse key-value pairs if in a valid section
                if (currentSection != null)
                {
                    string[] keyValue = trimmedLine.Split(new[] { '=' }, 2);
                    if (keyValue.Length == 2)
                    {
                        string key = keyValue[0].Trim();
                        string value = keyValue[1].Trim();

                        _cache[currentSection][key] = value;
                    }
                }
            }

            _isInitialized = true;
        }

        public static string GetINIPath()
        {
            var exePath = Directory.GetParent(Application.dataPath).ToString();
            return Path.Combine(exePath, GetINIFileName());
        }
        
        public static string GetINIFileName()
        {
            return "settings.ini";
        }
    }
}
