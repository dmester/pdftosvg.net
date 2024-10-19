// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PdfToSvg
{
    /// <summary>
    /// Represents an optional content group (OCG) in the PDF document. An optional content group can be seen as a
    /// layer, whose visibility can be controlled individually.
    /// </summary>
    /// <param name="name">Display name of the layer</param>
    /// <example>
    /// <para>
    ///     The following example lists all optional content groups that would show up in a PDF reader.
    /// </para>
    /// <code language="cs" title="List groups">
    /// using (var doc = PdfDocument.Open("input.pdf"))
    /// {
    ///     Console.WriteLine("Optional content groups in this document:");
    ///
    ///     foreach (var ocg in doc.OptionalContentGroups)
    ///     {
    ///         Console.WriteLine($" * {ocg.Name} (visible: {ocg.Visible})");
    ///     }
    /// }
    /// </code>
    /// <para>
    ///     The following example hides all optional content groups. Note that the document can provide additional
    ///     logics for when groups should be visible.
    /// </para>
    /// <code language="cs" title="Hide all groups">
    /// using (var doc = PdfDocument.Open("input.pdf"))
    /// {
    ///     foreach (var ocg in doc.OptionalContentGroups)
    ///     {
    ///         ocg.Visible = false;
    ///     }
    ///
    ///     doc.Pages[0].SaveAsSvg("output.svg");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="PdfDocument.OptionalContentGroups">PdfDocument.OptionalContentGroups Property</seealso>
    public sealed class OptionalContentGroup(string name)
    {
        /// <summary>
        /// Gets the name of this content group, as it would be displayed in a PDF reader.
        /// </summary>
        public string Name => name;

        /// <summary>
        /// Gets or sets a value indicating whether this group should be visible.
        /// </summary>
        /// <remarks>
        /// <note type="note">
        ///     The PDF document might provide additional logics controlling when a group should be visible.
        /// </note>
        /// </remarks>
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets a string representation of this layer.
        /// </summary>
        public override string ToString() => $"{name} (visible: {Visible})";
    }
}
