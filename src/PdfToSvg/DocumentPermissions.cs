// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg
{
    /// <summary>
    /// Provides information about which operations to a <see cref="PdfDocument"/> that are allowed by the document author.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     For encrypted PDF documents, there are two passwords, a user password and an owner password. The user
    ///     password is needed to open the document, while the owner password can be used to get full access to the
    ///     document.
    /// </para>
    /// <para>
    ///     If <see cref="AllowExtractContent"/> is <c>false</c>, the document can only be converted by specifying the
    ///     owner password in <see cref="OpenOptions"/> when the document is opened.
    /// </para>
    /// <code language="cs" title="Converting a protected document">
    /// var openOptions = new OpenOptions
    /// {
    ///     Password = "my owner password"
    /// };
    /// 
    /// using (var doc = PdfDocument.Open("input.pdf", openOptions))
    /// {
    ///     var pageIndex = 0;
    ///
    ///     foreach (var page in doc.Pages)
    ///     {
    ///         page.SaveAsSvg($"output-{pageIndex++}.svg");
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public class DocumentPermissions
    {
        private readonly int permissions;
        private readonly int securityHandlerRevision;

        internal DocumentPermissions()
        {
            securityHandlerRevision = 3;
            permissions = -1; // All permissions
            HasOwnerPermission = true;
        }

        internal DocumentPermissions(int securityHandlerRevision, int permissions, bool hasOwnerPermission)
        {
            this.securityHandlerRevision = securityHandlerRevision;
            this.permissions = permissions;
            HasOwnerPermission = hasOwnerPermission;
        }

        /// <summary>
        /// Indicates whether the user has full permission to the document, by opening the document using the document owner password.
        /// </summary>
        public bool HasOwnerPermission { get; }


        // ISO 32000-2, Table 22

        /// <summary>
        /// Allows the user to modify the document.
        /// </summary>
        public bool AllowModifyContent => HasBit(4);

        /// <summary>
        /// Allows copying or extracting content from the document.
        /// </summary>
        public bool AllowExtractContent => HasBit(5);

        /// <summary>
        /// Allow the user to add or modify annotations.
        /// </summary>
        public bool AllowAnnotations => HasBit(6);

        /// <summary>
        /// Allow the user to fill in forms.
        /// </summary>
        public bool AllowFillForm => HasBit(9);

        /// <summary>
        /// Allow extracting content for accessibility purposes.
        /// </summary>
        public bool AllowExtractAccessibility => HasBit(10);

        /// <summary>
        /// Allow modifying the document e.g. by addding, rotating or removing pages.
        /// </summary>
        public bool AllowAssembleDocument => HasBit(11);

        /// <summary>
        /// Allows the user to print the document, possibly at a lower resolution.
        /// </summary>
        public bool AllowPrintLowQuality => HasBit(3) || HasBit(12);

        /// <summary>
        /// Allows the user to print the document at full resolution.
        /// </summary>
        public bool AllowPrintFullQuality => HasBit(securityHandlerRevision < 3 ? 3 : 12);

        private bool HasBit(int bit) => (permissions & (1 << (bit - 1))) != 0;
    }
}
