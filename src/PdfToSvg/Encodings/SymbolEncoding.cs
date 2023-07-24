// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Encodings
{
    internal class SymbolEncoding : SingleByteEncoding
    {
        // Extracted from PDF spec 1.7 page 668-670.

        private static readonly string?[] glyphNames = new[]
        {
            null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null,
            "space", "exclam", "universal", "numbersign", "existential", "percent", "ampersand", "suchthat",
            "parenleft", "parenright", "asteriskmath", "plus", "comma", "minus", "period", "slash",
            "zero", "one", "two", "three", "four", "five", "six", "seven",
            "eight", "nine", "colon", "semicolon", "less", "equal", "greater", "question",
            "congruent", "Alpha", "Beta", "Chi", "Delta", "Epsilon", "Phi", "Gamma",
            "Eta", "Iota", "theta1", "Kappa", "Lambda", "Mu", "Nu", "Omicron",
            "Pi", "Theta", "Rho", "Sigma", "Tau", "Upsilon", "sigma1", "Omega",
            "Xi", "Psi", "Zeta", "bracketleft", "therefore", "bracketright", "perpendicular", "underscore",
            "radicalex", "alpha", "beta", "chi", "delta", "epsilon", "phi", "gamma",
            "eta", "iota", "phi1", "kappa", "lambda", "mu", "nu", "omicron",
            "pi", "theta", "rho", "sigma", "tau", "upsilon", "omega1", "omega",
            "xi", "psi", "zeta", "braceleft", "bar", "braceright", "similar", null,
            null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null,
            "Euro", "Upsilon1", "minute", "lessequal", "fraction", "infinity", "florin", "club",
            "diamond", "heart", "spade", "arrowboth", "arrowleft", "arrowup", "arrowright", "arrowdown",
            "degree", "plusminus", "second", "greaterequal", "multiply", "proportional", "partialdiff", "bullet",
            "divide", "notequal", "equivalence", "approxequal", "ellipsis", "arrowvertex", "arrowhorizex", "carriagereturn",
            "aleph", "Ifraktur", "Rfraktur", "weierstrass", "circlemultiply", "circleplus", "emptyset", "intersection",
            "union", "propersuperset", "reflexsuperset", "notsubset", "propersubset", "reflexsubset", "element", "notelement",
            "angle", "gradient", "registerserif", "copyrightserif", "trademarkserif", "product", "radical", "dotmath",
            "logicalnot", "logicaland", "logicalor", "arrowdblboth", "arrowdblleft", "arrowdblup", "arrowdblright", "arrowdbldown",
            "lozenge", "angleleft", "registersans", "copyrightsans", "trademarksans", "summation", "parenlefttp", "parenleftex",
            "parenleftbt", "bracketlefttp", "bracketleftex", "bracketleftbt", "bracelefttp", "braceleftmid", "braceleftbt", "braceex",
            null, "angleright", "integral", "integraltp", "integralex", "integralbt", "parenrighttp", "parenrightex",
            "parenrightbt", "bracketrighttp", "bracketrightex", "bracketrightbt", "bracerighttp", "bracerightmid", "bracerightbt", null,
        };

        private static readonly string?[] chars = GetUnicodeLookup(glyphNames);

        public SymbolEncoding() : base(chars, glyphNames) { }
    }
}
