// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg
{
    /// <summary>
    /// Provides additional configuration options for opening a <see cref="PdfDocument"/>.
    /// </summary>
    /// <inheritdoc cref="OpenOptions.Password" path="example"/>
    /// <seealso cref="PdfDocument.Open(string, OpenOptions?, System.Threading.CancellationToken)"/>
    public class OpenOptions
    {
        /// <summary>
        /// The password used for opening the PDF. This can be either the user password or the owner password. If the
        /// owner password is specified, any usage restrictions are bypassed.
        /// </summary>
        /// <remarks>
        /// <para>
        ///     PDF documents have two types of passwords: a user password and an owner password. 
        ///     If the document is encrypted, you will need either the user password or the owner password to be able
        ///     to open and convert the document.
        /// </para>
        /// <para>
        ///     The owner might also have set usage restrictions on the document. You can find restrictions of the
        ///     current document in <see cref="DocumentPermissions"/>. PdfToSvg.NET will honor export restrictions.
        ///     If you are the author of the document you can bypass the usage restrictions by providing the owner
        ///     password.
        /// </para>
        /// </remarks>
        /// <example>
        /// <para>
        ///     If a PDF document is password protected, you need to specify the PDF password to be able to convert the
        ///     document. The following example shows how to open a password protected document.
        /// </para>
        /// <code language="cs" title="Convert password protected PDF">
        /// var openOptions = new OpenOptions
        /// {
        ///     Password = "password"
        /// };
        /// 
        /// using (var document = PdfDocument.Open("password-protected.pdf", openOptions))
        /// {
        ///     var pageNo = 1;
        /// 
        ///     foreach (var page in document.Pages)
        ///     {
        ///         page.SaveAsSvg($"output_{pageNo++}.svg");
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="PdfDocument.Open(string, OpenOptions?, System.Threading.CancellationToken)"/>
        public string? Password { get; set; }
    }
}
