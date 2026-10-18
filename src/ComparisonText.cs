using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SimpleUpgradePreview
{
    // Pure text transformation: no game objects, item mutations, or save access.
    public static class ComparisonText
    {
        private static readonly Regex Tags = new Regex("<[^>]+>");
        private static readonly Regex Number = new Regex(@"^[+-]?\d+(?:[.,]\d+)?%?");

        private static Dictionary<string, string> ReadLines(string text, ISet<string> labels)
        {
            var result = new Dictionary<string, string>();
            foreach (string line in text.Split('\n'))
            {
                string clean = Tags.Replace(line, "").Trim();
                int colon = clean.IndexOf(':');
                if (colon < 0) continue;
                string label = clean.Substring(0, colon).Trim();
                if (!labels.Contains(label)) continue;
                string value = clean.Substring(colon + 1).Trim();
                // Duplicate labels can belong to appended recipes or effects: don't guess.
                if (result.ContainsKey(label)) result[label] = null;
                else result.Add(label, value);
            }
            return result;
        }

        public static string Apply(string displayed, string current, string upgraded, ISet<string> labels)
        {
            if (String.IsNullOrEmpty(displayed) || String.IsNullOrEmpty(current) ||
                String.IsNullOrEmpty(upgraded)) return displayed;
            var before = ReadLines(current, labels);
            var after = ReadLines(upgraded, labels);
            var displayedValues = ReadLines(displayed, labels);
            string[] lines = displayed.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string plain = Tags.Replace(lines[i], "").Trim();
                int colon = plain.IndexOf(':');
                if (colon < 0) continue;
                string label = plain.Substring(0, colon).Trim();
                string oldValue, newValue, displayedValue;
                if (!before.TryGetValue(label, out oldValue) || oldValue == null ||
                    !after.TryGetValue(label, out newValue) || newValue == null ||
                    !displayedValues.TryGetValue(label, out displayedValue) || displayedValue == null)
                    continue;
                Match oldNumber = Number.Match(oldValue);
                Match newNumber = Number.Match(newValue);
                if (!oldNumber.Success || !newNumber.Success || oldValue == newValue) continue;
                // Keep the target's skill-dependent range, but don't duplicate both ranges.
                string tail = newValue.Substring(newNumber.Length);
                lines[i] = label + ": <color=#BBBBBB>" + oldNumber.Value +
                    "</color> \u2192 <color=#FFD966>" + newNumber.Value + "</color>" + tail;
            }
            return String.Join("\n", lines);
        }
    }
}
