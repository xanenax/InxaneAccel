using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using grapher.Models.Serialized;
using grapher.Models.Theming.IO;

namespace grapher.Models.Theming
{
    public static class ColorSchemeManager
    {
        private static XmlSerializer _xmlSerializer;

        public static XmlSerializer XmlSerializer =>
            _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(ColorScheme)));

        public static ColorScheme FromXml(XDocument xml)
        {
            ColorScheme deserializedObject;
            using (var reader = xml.CreateReader())
            {
                deserializedObject = (ColorScheme)XmlSerializer.Deserialize(reader);
            }

            return deserializedObject;
        }

        public static XDocument ToXml(ColorScheme scheme)
        {
            var doc = new XDocument();
            using (var writer = doc.CreateWriter())
            {
                XmlSerializer.Serialize(writer, scheme);
            }

            return doc;
        }

        public static string NormalizeSchemeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) ||
                string.Equals(name, "Apple Dark", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "OLED Midnight", StringComparison.OrdinalIgnoreCase))
            {
                return ColorScheme.MacOSDarkTheme.Name;
            }

            return name;
        }

        /// <summary>
        /// Retrieve the selected <see cref="ColorScheme"/> according to the <paramref name="settings"/>.
        /// </summary>
        public static ColorScheme GetSelected(GUISettings settings, IEnumerable<ColorScheme> schemes)
        {
            if (schemes == null)
            {
                schemes = LoadSchemes();
            }

            var targetName = NormalizeSchemeName(settings?.CurrentColorScheme);
            var scheme = schemes.FirstOrDefault(s => string.Equals(s.Name, targetName, StringComparison.OrdinalIgnoreCase))
                         ?? ColorScheme.MacOSDarkTheme;
            var accentName = string.IsNullOrWhiteSpace(settings?.CurrentAccentColor)
                ? "Indigo"
                : settings.CurrentAccentColor;

            return ColorScheme.WithAccent(scheme, accentName);
        }

        public static IEnumerable<ColorScheme> LoadSchemes()
        {
            var operations = new ThemeFileOperations();
            return operations.LoadThemes();
        }

        public static ColorScheme FromName(string name, string accentName = null)
        {
            var operations = new ThemeFileOperations();
            var schemes = operations.LoadThemes();

            var targetName = NormalizeSchemeName(name);
            var scheme = schemes.FirstOrDefault(s => string.Equals(s.Name, targetName, StringComparison.OrdinalIgnoreCase))
                         ?? ColorScheme.MacOSDarkTheme;

            return string.IsNullOrEmpty(accentName)
                ? scheme.Clone()
                : ColorScheme.WithAccent(scheme, accentName);
        }
    }
}