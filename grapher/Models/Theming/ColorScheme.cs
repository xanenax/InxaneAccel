using System;
using System.Drawing;
using System.Xml.Serialization;
using grapher.Models.Theming.IO;

namespace grapher.Models.Theming
{
    public class ColorScheme
    {
        public string Name { get; set; }

        /// <summary>
        /// Default Chart Background Color.
        /// </summary>
        [XmlElement(Type = typeof(XmlColor))]
        public Color ChartBackground { get; set; }

        /// <summary>
        /// Default Chart Foreground Color.
        /// </summary>
        [XmlElement(Type = typeof(XmlColor))]
        public Color ChartForeground { get; set; }

        /// <summary>
        /// Used on things like sliders and primary chart lines.
        /// </summary>
        [XmlElement(Type = typeof(XmlColor))] public Color Primary { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color OnPrimary { get; set; } = Color.White;

        [XmlElement(Type = typeof(XmlColor))]
        // Alternative for <see cref="Primary"/>.
        public Color Secondary { get; set; }

        /// <summary>
        /// Mouse movement color which is shown on the charts.
        /// </summary>
        [XmlElement(Type = typeof(XmlColor))]
        public Color MouseMovement { get; set; }

        [XmlElement(Type = typeof(XmlColor))] public Color Field { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color OnField { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color OnFocusedField { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color EditedField { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color OnEditedField { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color ButtonFace { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color ButtonBorder { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color Control { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color ControlBorder { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color OnControl { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color DisabledControl { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color OnDisabledControl { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color Background { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color OnBackground { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color Surface { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color MenuBackground { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color MenuSelectedBorder { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color MenuSelected { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color CheckBoxBackground { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color CheckBoxBorder { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color CheckBoxHover { get; set; }
        [XmlElement(Type = typeof(XmlColor))] public Color CheckBoxChecked { get; set; }

        /// <summary>
        /// Will create gradients between <see cref="CheckBoxChecked"/> and <see cref="Secondary"/>,
        /// instead of just the solid colors for Checkbox checked states.
        /// </summary>
        public bool UseAccentGradientsForCheckboxes { get; set; }

        /// <summary>
        /// Will create gradients between <see cref="Primary"/> and <see cref="ButtonFace"/>,
        /// instead of just the solid colors for Buttons.
        /// </summary>
        public bool UseAccentGradientsForButtons { get; set; }

        public static readonly (string Name, Color Color)[] AppleAccents = new[]
        {
            ("Indigo", Color.FromArgb(99, 102, 241)),
            ("Apple Blue", Color.FromArgb(10, 132, 255)),
            ("Purple", Color.FromArgb(191, 90, 242)),
            ("Emerald", Color.FromArgb(48, 209, 88)),
            ("Orange", Color.FromArgb(255, 159, 10))
        };

        public static ColorScheme WithAccent(ColorScheme scheme, string accentName)
        {
            if (scheme == null)
            {
                return null;
            }

            var clone = scheme.Clone();
            if (string.IsNullOrWhiteSpace(accentName))
            {
                return clone;
            }

            Color? matched = null;
            foreach (var entry in AppleAccents)
            {
                if (string.Equals(entry.Name, accentName, StringComparison.OrdinalIgnoreCase))
                {
                    matched = entry.Color;
                    break;
                }
            }

            if (!matched.HasValue)
            {
                return clone;
            }

            Color accent = matched.Value;
            clone.Primary = accent;
            clone.MenuSelected = accent;
            clone.MenuSelectedBorder = accent;
            clone.CheckBoxChecked = accent;
            clone.OnPrimary = Color.White;

            bool isDark = clone.Background.GetBrightness() < 0.5f;
            if (isDark)
            {
                clone.EditedField = Blend(clone.Field, accent, 0.20f);
                clone.OnEditedField = Blend(accent, Color.White, 0.35f);
            }
            else
            {
                clone.EditedField = Blend(clone.Field, accent, 0.14f);
                clone.OnEditedField = Blend(accent, Color.Black, 0.20f);
            }

            return clone;
        }

        public ColorScheme Clone()
        {
            return (ColorScheme)MemberwiseClone();
        }

        public static Color Blend(Color baseColor, Color target, float amount)
        {
            int r = Math.Max(0, Math.Min(255, (int)Math.Round(baseColor.R + (target.R - baseColor.R) * amount)));
            int g = Math.Max(0, Math.Min(255, (int)Math.Round(baseColor.G + (target.G - baseColor.G) * amount)));
            int b = Math.Max(0, Math.Min(255, (int)Math.Round(baseColor.B + (target.B - baseColor.B) * amount)));
            return Color.FromArgb(r, g, b);
        }

        public static ColorScheme MacOSDarkTheme = new ColorScheme
        {
            Name = "macOS Dark",
            Primary = Color.FromArgb(99, 102, 241),
            OnPrimary = Color.White,
            Secondary = Color.FromArgb(48, 209, 88),
            MouseMovement = Color.FromArgb(255, 69, 58),
            ChartBackground = Color.FromArgb(18, 19, 23),
            ChartForeground = Color.FromArgb(243, 244, 246),
            Field = Color.FromArgb(14, 15, 19),
            OnFocusedField = Color.FromArgb(243, 244, 246),
            OnField = Color.FromArgb(142, 146, 159),
            EditedField = Color.FromArgb(24, 26, 54),
            OnEditedField = Color.FromArgb(165, 168, 255),
            ButtonFace = Color.FromArgb(34, 36, 44),
            ButtonBorder = Color.FromArgb(50, 53, 64),
            Control = Color.FromArgb(26, 27, 33),
            ControlBorder = Color.FromArgb(38, 40, 51),
            OnControl = Color.FromArgb(243, 244, 246),
            DisabledControl = Color.FromArgb(16, 18, 23),
            OnDisabledControl = Color.FromArgb(82, 85, 99),
            Background = Color.FromArgb(9, 10, 13),
            OnBackground = Color.FromArgb(243, 244, 246),
            Surface = Color.FromArgb(18, 19, 23),
            MenuBackground = Color.FromArgb(24, 25, 31),
            MenuSelectedBorder = Color.FromArgb(99, 102, 241),
            MenuSelected = Color.FromArgb(99, 102, 241),
            CheckBoxChecked = Color.FromArgb(99, 102, 241),
            CheckBoxBackground = Color.FromArgb(26, 27, 33),
            CheckBoxBorder = Color.FromArgb(54, 57, 68),
            CheckBoxHover = Color.FromArgb(34, 36, 44),
            UseAccentGradientsForCheckboxes = false,
            UseAccentGradientsForButtons = true
        };

        public static ColorScheme AppleDarkTheme = new ColorScheme
        {
            Name = "Apple Dark",
            Primary = Color.FromArgb(94, 92, 230),
            OnPrimary = Color.White,
            Secondary = Color.FromArgb(48, 209, 88),
            MouseMovement = Color.FromArgb(255, 69, 58),
            ChartBackground = Color.FromArgb(18, 19, 23),
            ChartForeground = Color.FromArgb(243, 244, 246),
            Field = Color.FromArgb(14, 15, 19),
            OnFocusedField = Color.FromArgb(243, 244, 246),
            OnField = Color.FromArgb(142, 146, 159),
            EditedField = Color.FromArgb(24, 24, 52),
            OnEditedField = Color.FromArgb(165, 164, 255),
            ButtonFace = Color.FromArgb(34, 36, 44),
            ButtonBorder = Color.FromArgb(50, 53, 64),
            Control = Color.FromArgb(26, 27, 33),
            ControlBorder = Color.FromArgb(38, 40, 51),
            OnControl = Color.FromArgb(243, 244, 246),
            DisabledControl = Color.FromArgb(16, 18, 23),
            OnDisabledControl = Color.FromArgb(82, 85, 99),
            Background = Color.FromArgb(9, 10, 13),
            OnBackground = Color.FromArgb(243, 244, 246),
            Surface = Color.FromArgb(18, 19, 23),
            MenuBackground = Color.FromArgb(24, 25, 31),
            MenuSelectedBorder = Color.FromArgb(94, 92, 230),
            MenuSelected = Color.FromArgb(94, 92, 230),
            CheckBoxChecked = Color.FromArgb(94, 92, 230),
            CheckBoxBackground = Color.FromArgb(26, 27, 33),
            CheckBoxBorder = Color.FromArgb(54, 57, 68),
            CheckBoxHover = Color.FromArgb(34, 36, 44),
            UseAccentGradientsForCheckboxes = false,
            UseAccentGradientsForButtons = true
        };

        public static ColorScheme AppleLightTheme = new ColorScheme
        {
            Name = "Apple Light",
            Primary = Color.FromArgb(94, 92, 230),
            OnPrimary = Color.White,
            Secondary = Color.FromArgb(52, 199, 89),
            MouseMovement = Color.FromArgb(255, 59, 48),
            ChartBackground = Color.FromArgb(255, 255, 255),
            ChartForeground = Color.FromArgb(28, 28, 30),
            Field = Color.FromArgb(242, 242, 247),
            OnFocusedField = Color.FromArgb(28, 28, 30),
            OnField = Color.FromArgb(110, 110, 115),
            EditedField = Color.FromArgb(234, 233, 252),
            OnEditedField = Color.FromArgb(72, 70, 200),
            ButtonFace = Color.FromArgb(229, 229, 234),
            ButtonBorder = Color.FromArgb(209, 209, 214),
            Control = Color.FromArgb(242, 242, 247),
            ControlBorder = Color.FromArgb(209, 209, 214),
            OnControl = Color.FromArgb(28, 28, 30),
            DisabledControl = Color.FromArgb(229, 229, 234),
            OnDisabledControl = Color.FromArgb(174, 174, 178),
            Background = Color.FromArgb(242, 242, 247),
            OnBackground = Color.FromArgb(28, 28, 30),
            Surface = Color.FromArgb(255, 255, 255),
            MenuBackground = Color.FromArgb(255, 255, 255),
            MenuSelectedBorder = Color.FromArgb(94, 92, 230),
            MenuSelected = Color.FromArgb(94, 92, 230),
            CheckBoxChecked = Color.FromArgb(94, 92, 230),
            CheckBoxBackground = Color.FromArgb(229, 229, 234),
            CheckBoxBorder = Color.FromArgb(199, 199, 204),
            CheckBoxHover = Color.FromArgb(209, 209, 214),
            UseAccentGradientsForCheckboxes = false,
            UseAccentGradientsForButtons = true
        };

        public static ColorScheme LightTheme = new ColorScheme
        {
            Name = "Light Theme",
            Primary = Color.Blue,
            Secondary = Color.Orange,
            ChartBackground = Color.White,
            ChartForeground = Color.Black,
            MouseMovement = Color.FromArgb(192, 0, 0),
            Field = Color.White,
            OnFocusedField = Color.FromArgb(16, 16, 16),
            OnField = Color.FromArgb(64, 64, 64),
            EditedField = Color.AntiqueWhite,
            OnEditedField = Color.DarkGray,
            ButtonFace = Color.FromArgb(225, 225, 225),
            ButtonBorder = Color.FromArgb(173, 173, 173),
            Control = Color.FromArgb(240, 240, 240),
            ControlBorder = Color.FromArgb(122, 122, 122),
            OnControl = Color.FromArgb(64, 64, 64),
            DisabledControl = Color.FromArgb(238, 238, 238),
            OnDisabledControl = Color.Gray,
            Background = Color.White,
            OnBackground = Color.FromArgb(64, 64, 64),
            Surface = Color.FromArgb(240, 240, 240),
            MenuBackground = Color.FromArgb(227, 227, 227),
            MenuSelectedBorder = Color.FromArgb(0, 120, 215),
            MenuSelected = Color.FromArgb(179, 215, 243),
            CheckBoxChecked = Color.FromArgb(83, 100, 190),
            CheckBoxBorder = Color.FromArgb(0, 120, 215),
            CheckBoxHover = Color.FromArgb(113, 166, 207),
            CheckBoxBackground = Color.FromArgb(196, 227, 250)
        };

        public static ColorScheme LightStreamerTheme = new ColorScheme
        {
            Name = "Light Streamer Theme",
            Primary = Color.Blue,
            Secondary = Color.Orange,
            ChartBackground = Color.Green,
            ChartForeground = Color.White,
            MouseMovement = Color.FromArgb(192, 0, 0),
            Field = Color.White,
            OnFocusedField = Color.FromArgb(16, 16, 16),
            OnField = Color.FromArgb(64, 64, 64),
            EditedField = Color.AntiqueWhite,
            OnEditedField = Color.DarkGray,
            ButtonFace = Color.FromArgb(225, 225, 225),
            ButtonBorder = Color.FromArgb(173, 173, 173),
            Control = Color.FromArgb(240, 240, 240),
            ControlBorder = Color.FromArgb(122, 122, 122),
            OnControl = Color.FromArgb(64, 64, 64),
            DisabledControl = Color.FromArgb(238, 238, 238),
            OnDisabledControl = Color.Gray,
            Background = Color.White,
            OnBackground = Color.FromArgb(64, 64, 64),
            Surface = Color.FromArgb(240, 240, 240),
            MenuBackground = Color.FromArgb(227, 227, 227),
            MenuSelectedBorder = Color.FromArgb(0, 120, 215),
            MenuSelected = Color.FromArgb(179, 215, 243),
            CheckBoxChecked = Color.FromArgb(83, 100, 190),
            CheckBoxBorder = Color.FromArgb(0, 120, 215),
            CheckBoxHover = Color.FromArgb(113, 166, 207),
            CheckBoxBackground = Color.FromArgb(196, 227, 250)
        };

        public static ColorScheme DarkTheme = new ColorScheme
        {
            Name = "Dark Theme",
            Primary = Color.FromArgb(147, 110, 227),
            Secondary = Color.FromArgb(223, 185, 136),
            MouseMovement = Color.FromArgb(237, 103, 103),
            ChartBackground = Color.FromArgb(57, 57, 57),
            ChartForeground = Color.FromArgb(234, 234, 234),
            Field = Color.FromArgb(81, 81, 81),
            OnFocusedField = Color.FromArgb(240, 240, 240),
            OnField = Color.FromArgb(184, 184, 184),
            EditedField = Color.FromArgb(116, 101, 31),
            OnEditedField = Color.FromArgb(232, 227, 222),
            ButtonFace = Color.FromArgb(100, 100, 100),
            ButtonBorder = Color.Gray,
            Control = Color.FromArgb(64, 64, 64),
            ControlBorder = Color.Gray,
            OnControl = Color.FromArgb(234, 234, 234),
            DisabledControl = Color.FromArgb(126, 126, 126),
            OnDisabledControl = Color.LightGray,
            Background = Color.FromArgb(57, 57, 57),
            OnBackground = Color.FromArgb(234, 234, 234),
            Surface = Color.FromArgb(48, 48, 48),
            MenuBackground = Color.FromArgb(48, 48, 48),
            MenuSelectedBorder = Color.FromArgb(64, 64, 64),
            MenuSelected = Color.FromArgb(77, 77, 77),
            CheckBoxChecked = Color.FromArgb(184, 184, 184),
            CheckBoxBackground = Color.FromArgb(87, 87, 87),
            CheckBoxBorder = Color.FromArgb(102, 102, 102),
            CheckBoxHover = Color.FromArgb(135, 135, 135)
        };

        public static ColorScheme AccentedDarkTheme = new ColorScheme
        {
            Name = "Accented Dark Theme",
            Primary = Color.FromArgb(147, 110, 227),
            Secondary = Color.FromArgb(223, 185, 136),
            MouseMovement = Color.FromArgb(237, 103, 103),
            ChartBackground = Color.FromArgb(57, 57, 57),
            ChartForeground = Color.FromArgb(234, 234, 234),
            Field = Color.FromArgb(81, 81, 81),
            OnFocusedField = Color.FromArgb(240, 240, 240),
            OnField = Color.FromArgb(184, 184, 184),
            EditedField = Color.FromArgb(116, 101, 31),
            OnEditedField = Color.FromArgb(232, 227, 222),
            ButtonFace = Color.FromArgb(147, 110, 227),
            ButtonBorder = Color.Gray,
            Control = Color.FromArgb(64, 64, 64),
            ControlBorder = Color.FromArgb(223, 185, 136),
            OnControl = Color.FromArgb(234, 234, 234),
            DisabledControl = Color.FromArgb(126, 126, 126),
            OnDisabledControl = Color.LightGray,
            Background = Color.FromArgb(57, 57, 57),
            OnBackground = Color.FromArgb(234, 234, 234),
            Surface = Color.FromArgb(48, 48, 48),
            MenuBackground = Color.FromArgb(48, 48, 48),
            MenuSelectedBorder = Color.FromArgb(64, 64, 64),
            MenuSelected = Color.FromArgb(77, 77, 77),
            CheckBoxChecked = Color.FromArgb(184, 184, 184),
            CheckBoxBackground = Color.FromArgb(87, 87, 87),
            CheckBoxBorder = Color.FromArgb(223, 185, 136),
            CheckBoxHover = Color.FromArgb(135, 135, 135),
            UseAccentGradientsForCheckboxes = true,
            UseAccentGradientsForButtons = true
        };

        public static ColorScheme DarkStreamerTheme = new ColorScheme
        {
            Name = "Dark Streamer Theme",
            Primary = Color.FromArgb(147, 110, 227),
            Secondary = Color.FromArgb(223, 185, 136),
            MouseMovement = Color.FromArgb(237, 103, 103),
            ChartBackground = Color.Green,
            ChartForeground = Color.White,
            Field = Color.FromArgb(81, 81, 81),
            OnFocusedField = Color.FromArgb(240, 240, 240),
            OnField = Color.FromArgb(184, 184, 184),
            EditedField = Color.FromArgb(116, 101, 31),
            OnEditedField = Color.FromArgb(232, 227, 222),
            ButtonFace = Color.FromArgb(100, 100, 100),
            ButtonBorder = Color.Gray,
            Control = Color.FromArgb(64, 64, 64),
            ControlBorder = Color.Gray,
            OnControl = Color.FromArgb(234, 234, 234),
            DisabledControl = Color.FromArgb(126, 126, 126),
            OnDisabledControl = Color.LightGray,
            Background = Color.FromArgb(57, 57, 57),
            OnBackground = Color.FromArgb(234, 234, 234),
            Surface = Color.FromArgb(48, 48, 48),
            MenuBackground = Color.FromArgb(48, 48, 48),
            MenuSelectedBorder = Color.FromArgb(64, 64, 64),
            MenuSelected = Color.FromArgb(77, 77, 77),
            CheckBoxChecked = Color.FromArgb(184, 184, 184),
            CheckBoxBackground = Color.FromArgb(87, 87, 87),
            CheckBoxBorder = Color.FromArgb(102, 102, 102),
            CheckBoxHover = Color.FromArgb(135, 135, 135)
        };
    }
}