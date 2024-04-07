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
    /// Represents errors that occur when a document is password protected, but an incorrect password is specified
    /// or not specified at all.
    /// </summary>
    /// <example>
    /// <para>
    ///     If a PDF document is password protected, you need to specify the PDF password to be able to convert the
    ///     document. The following example shows how to open a password protected document.
    /// </para>
    /// <code language="cs" title="Convert password protected PDF">
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
    public class InvalidCredentialException : PdfException
    {
        /// <summary>
        /// Creates a new instance of <see cref="InvalidCredentialException"/>.
        /// </summary>
        /// <param name="message">Message to include in the exception.</param>
        public InvalidCredentialException(string message) : base(message)
        {
        }
    }
}
