using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfMath
{
    // Atom representing base atom with accent above it.
    internal class AccentedAtom : Atom
    {
        public AccentedAtom(Atom baseAtom, string accentName)
        {
            this.BaseAtom = baseAtom;
            this.AccentAtom = WpfMath.SymbolAtom.GetAtom(accentName);

            if (this.AccentAtom.Type != TexAtomType.Accent)
                throw new ArgumentException("The specified symbol name is not an accent.", "accent");
        }

        public AccentedAtom(Atom baseAtom, TexFormula accent)
        {
            var rootSymbol = accent.RootAtom as WpfMath.SymbolAtom;
            if (rootSymbol == null)
                throw new ArgumentException("The formula for the accent is not a single symbol.", "accent");
            this.AccentAtom = (WpfMath.SymbolAtom)rootSymbol;

            if (this.AccentAtom.Type != TexAtomType.Accent)
                throw new ArgumentException("The specified symbol name is not an accent.", "accent");
        }

        // Atom over which accent symbol is placed.
        public Atom BaseAtom
        {
            get;
            private set;
        }

        // Atom representing accent symbol to place over base atom.
        public WpfMath.SymbolAtom AccentAtom
        {
            get;
            private set;
        }

        public override Box CreateBox(WpfMath.TexEnvironment environment)
        {
            var texFont = environment.TexFont;
            var style = environment.Style;

            // Create box for base atom.
            var baseBox = this.BaseAtom == null ? WpfMath.StrutBox.Empty : this.BaseAtom.CreateBox(environment.GetCrampedStyle());
            var skew = 0d;
            if (this.BaseAtom is CharSymbol)
                skew = texFont.GetSkew(((CharSymbol)this.BaseAtom).GetCharFont(texFont), style);

            // Find character of best scale for accent symbol.
            var accentChar = texFont.GetCharInfo(AccentAtom.Name, style);
            while (texFont.HasNextLarger(accentChar))
            {
                var nextLargerChar = texFont.GetNextLargerCharInfo(accentChar, style);
                if (nextLargerChar.Metrics.Width > baseBox.Width)
                    break;
                accentChar = nextLargerChar;
            }

            var resultBox = new WpfMath.VerticalBox();

            // Create and add box for accent symbol.
            Box accentBox;
            var accentItalicWidth = accentChar.Metrics.Italic;
            if (accentItalicWidth > WpfMath.TexUtilities.FloatPrecision)
            {
                accentBox = new WpfMath.HorizontalBox(new CharBox(environment, accentChar));
                accentBox.Add(new WpfMath.StrutBox(accentItalicWidth, 0, 0, 0));
            }
            else
            {
                accentBox = new CharBox(environment, accentChar);
            }
            resultBox.Add(accentBox);

            var delta = Math.Min(baseBox.Height, texFont.GetXHeight(style, accentChar.FontId));
            resultBox.Add(new WpfMath.StrutBox(0, -delta, 0, 0));

            // Centre and add box for base atom. Centre base box and accent box with respect to each other.
            var boxWidthsDiff = (baseBox.Width - accentBox.Width) / 2;
            accentBox.Shift = skew + Math.Max(boxWidthsDiff, 0);
            if (boxWidthsDiff < 0)
                baseBox = new WpfMath.HorizontalBox(baseBox, accentBox.Width, TexAlignment.Center);
            resultBox.Add(baseBox);

            // Adjust height and depth of result box.
            var depth = baseBox.Depth;
            var totalHeight = resultBox.Height + resultBox.Depth;
            resultBox.Depth = depth;
            resultBox.Height = totalHeight - depth;

            return resultBox;
        }
    }
}