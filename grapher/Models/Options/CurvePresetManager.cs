using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace grapher.Models.Options
{
    public static class CurvePresetManager
    {
        public const string BackupFileName = "lut_original_backup.txt";
        public const string LutPointsFileName = "lut_points.txt";
        public const string PresetOriginal = "Original";
        public const string PresetCleanBump = "CleanBump";
        public const double TargetStartY = 1.0;
        public const double TargetEndY = 1.75;

        public static string ActivePreset { get; private set; } = PresetOriginal;

        public static string OriginalLutText { get; private set; } = string.Empty;

        public static string CleanBumpLutText { get; private set; } = string.Empty;

        public static double OriginalMinY { get; private set; } = 0.967887;

        public static double OriginalMaxY { get; private set; } = TargetEndY;

        public static void SetActivePreset(string preset)
        {
            ActivePreset = string.Equals(preset, PresetCleanBump, StringComparison.OrdinalIgnoreCase)
                ? PresetCleanBump
                : PresetOriginal;
        }

        public static void Initialize(string currentPointsBoxText)
        {
            string baseDir = Path.GetDirectoryName(typeof(CurvePresetManager).Assembly.Location);
            if (string.IsNullOrEmpty(baseDir))
            {
                baseDir = AppDomain.CurrentDomain.BaseDirectory;
            }
            string backupPath = Path.Combine(baseDir, BackupFileName);
            string lutPointsPath = Path.Combine(baseDir, LutPointsFileName);
            var utf8NoBom = new UTF8Encoding(false);

            string original = null;
            try
            {
                if (File.Exists(backupPath))
                {
                    string existingBackup = File.ReadAllText(backupPath).Trim();
                    if (!string.IsNullOrWhiteSpace(existingBackup) && HasDipBelowBaseline(existingBackup))
                    {
                        original = existingBackup;
                    }
                }

                if (string.IsNullOrWhiteSpace(original) && File.Exists(lutPointsPath))
                {
                    string diskPoints = File.ReadAllText(lutPointsPath).Trim();
                    if (!string.IsNullOrWhiteSpace(diskPoints) && HasDipBelowBaseline(diskPoints))
                    {
                        original = diskPoints;
                    }
                }

                if (string.IsNullOrWhiteSpace(original) && !string.IsNullOrWhiteSpace(currentPointsBoxText))
                {
                    string boxText = currentPointsBoxText.Trim();
                    if (HasDipBelowBaseline(boxText) || !File.Exists(backupPath))
                    {
                        original = boxText;
                    }
                }

                if (string.IsNullOrWhiteSpace(original) && File.Exists(backupPath))
                {
                    original = File.ReadAllText(backupPath).Trim();
                }

                if (!string.IsNullOrWhiteSpace(original))
                {
                    original = NormalizeOriginalLut(original);
                    File.WriteAllText(backupPath, original, utf8NoBom);
                }
            }
            catch
            {
                if (string.IsNullOrWhiteSpace(original))
                {
                    original = currentPointsBoxText ?? string.Empty;
                }
            }

            string normalizedOriginal = NormalizeOriginalLut(original ?? string.Empty);
            OriginalLutText = normalizedOriginal;
            var origPts = ParsePoints(OriginalLutText);
            if (origPts.Count >= 2)
            {
                OriginalMinY = origPts.Min(p => p.Y);
                OriginalMaxY = origPts.Max(p => p.Y);
            }

            CleanBumpLutText = BuildCleanBumpLut(OriginalLutText);

            string activeCheck = !string.IsNullOrWhiteSpace(currentPointsBoxText)
                ? currentPointsBoxText.Trim()
                : OriginalLutText;

            ActivePreset = (!string.IsNullOrWhiteSpace(activeCheck) && !HasDipBelowBaseline(activeCheck))
                ? PresetCleanBump
                : PresetOriginal;
        }

        private static bool HasDipBelowBaseline(string lutText)
        {
            var pts = ParsePoints(lutText);
            if (pts.Count < 5)
            {
                return false;
            }

            double y0 = pts[0].Y;
            double minY = pts.Min(p => p.Y);
            return minY < y0 - 0.005;
        }

        public static string NormalizeOriginalLut(string rawLut)
        {
            var pts = ParsePoints(rawLut);
            if (pts.Count < 2)
            {
                return rawLut ?? string.Empty;
            }

            int n = pts.Count;
            double srcStart = pts[0].Y;
            double srcEnd = pts[n - 1].Y;
            double srcSpan = srcEnd - srcStart;
            if (Math.Abs(srcSpan) < 1e-9)
            {
                return rawLut ?? string.Empty;
            }

            double scale = (TargetEndY - TargetStartY) / srcSpan;
            var sb = new StringBuilder(n * 22);
            for (int i = 0; i < n; i++)
            {
                double y;
                if (i == 0)
                {
                    y = TargetStartY;
                }
                else if (i == n - 1)
                {
                    y = TargetEndY;
                }
                else
                {
                    y = TargetStartY + (pts[i].Y - srcStart) * scale;
                    y = Math.Max(0.01, y);
                }

                sb.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "{0:0.#######},{1:0.########};",
                    pts[i].X,
                    y);
            }

            return sb.ToString();
        }

        public static string BuildCleanBumpLut(string originalLut)
        {
            var pts = ParsePoints(originalLut);
            if (pts.Count < 2)
            {
                return originalLut ?? string.Empty;
            }

            int n = pts.Count;
            const double yStart = TargetStartY;
            const double yEnd = TargetEndY;
            const double xMid = 60.0;
            const double halfSpan = 18.0; // smooth transition window [42..78]
            const double steepness = 3.2; // straight, decisive center ramp with smooth rounded knees
            double tanhNorm = Math.Tanh(steepness);

            var sb = new StringBuilder(n * 22);
            for (int i = 0; i < n; i++)
            {
                double x = pts[i].X;
                double y;
                if (x <= xMid - halfSpan)
                {
                    y = yStart;
                }
                else if (x >= xMid + halfSpan)
                {
                    y = yEnd;
                }
                else
                {
                    double u = (x - xMid) / halfSpan; // [-1..1]
                    double norm = Math.Tanh(steepness * u) / tanhNorm; // strictly [-1..1]
                    y = yStart + (yEnd - yStart) * 0.5 * (norm + 1.0);
                }

                sb.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "{0:0.#######},{1:0.########};",
                    x,
                    y);
            }

            return sb.ToString();
        }

        private static List<(double X, double Y)> ParsePoints(string lutText)
        {
            var result = new List<(double X, double Y)>();
            if (string.IsNullOrWhiteSpace(lutText))
            {
                return result;
            }

            var entries = lutText.Trim().Trim(';').Split(';');
            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry)) continue;
                var parts = entry.Trim().Split(',');
                if (parts.Length != 2) continue;

                if (double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                    double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                {
                    result.Add((x, y));
                }
            }

            return result;
        }
    }
}
