// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg
{
    /// <summary>
    /// Represents errors that occurs when an attempt is made to perform an operation not allowed by the document author.
    /// </summary>
    /// <example>
    /// <para>
    ///     If the author has restricted extraction of content from the PDF document, you can specify the PDF owner
    ///     password to override the restrictions.
    /// </para>
    /// <code language="cs" title="Convert PDF with usage restrictions">
    /// var openOptions = new OpenOptions();
    ///             
    /// do
    /// {
    ///     try
    ///     {
    ///         using (var document = PdfDocument.Open("maybe-password-protected.pdf", openOptions))
    ///         {
    ///             var pageNo = 1;
    /// 
    ///             foreach (var page in document.Pages)
    ///             {
    ///                 page.SaveAsSvg($"output_{pageNo++}.svg");
    ///             }
    ///         }
    /// 
    ///         Console.WriteLine("Success!");
    ///         break;
    ///     }
    ///     catch (Exception ex) when (ex is InvalidCredentialException || ex is PermissionException)
    ///     {
    ///         Console.WriteLine(string.IsNullOrEmpty(openOptions.Password)
    ///             ? "A password is required to convert the document."
    ///             : "The password is incorrect. Try again.");
    ///         Console.WriteLine();
    ///         Console.WriteLine("Enter password:");
    ///         openOptions.Password = Console.ReadLine();
    /// 
    ///         Console.WriteLine();
    ///     }
    /// }
    /// while (!string.IsNullOrEmpty(openOptions.Password));
    /// </code>
    /// </example>
    public class PermissionException : PdfException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="PermissionException"/>.
        /// </summary>
        /// <param name="message">Error message.</param>
        public PermissionException(string message) : base(message)
        {
        }
    }
}
