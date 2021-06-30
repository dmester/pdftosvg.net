// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PdfToSvg.Common
{
    internal class PageRange
    {
        public PageRange(int from, int to)
        {
            From = from;
            To = to;
        }

        public int From { get; }
        public int To { get; }

        public static bool TryParse(string input, out IList<PageRange> result)
        {
            result = new List<PageRange>();

            foreach (var part in input.Split(','))
            {
                var trimmed = part.Trim();
                if (trimmed.Length > 0)
                {
                    var match = Regex.Match(part.Trim(), "^(?:\\.\\.(\\d{1,4})|(\\d{1,4})(\\.\\.(\\d{1,4})?)?)$");
                    if (match.Success)
                    {
                        if (match.Groups[1].Success)
                        {
                            result.Add(new PageRange(-1, int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)));
                        }
                        else
                        {
                            var from = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

                            var to =
                                match.Groups[4].Success ? int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture) :
                                match.Groups[3].Success ? -1 :
                                from;

                            result.Add(new PageRange(from, to));
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return result.Count > 0;
        }

        public override string ToString()
        {
            var format =
                From < 0 ? "..{1}" :
                To < 0 ? "{0}.." :
                From == To ? "{0}" :
                "{0}..{1}";

            return string.Format(CultureInfo.InvariantCulture, format, From, To);
        }
    }
}
