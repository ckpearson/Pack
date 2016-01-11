using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Pack.Core.Palette
{
    public static class Palettes
    {
        public static Color[] Default => new[]
        {
            Color.FromArgb(86, 81, 117),
            Color.FromArgb(83, 138, 149),
            Color.FromArgb(103, 183, 158),
            Color.FromArgb(255, 183, 39),
            Color.FromArgb(228, 73, 28)
        };

        public static IEnumerable<string> GetPalettesForHelp()
        {
            yield return "Default: " + string.Join(";", Default.Select(c => string.Join(",", new[] {c.R, c.G, c.B})));
        }
    }
}