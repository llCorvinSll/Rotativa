using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Rotativa.Core
{
    public class WkhtmltopdfDriver
    {
        /// <summary>
        /// Converts given HTML string to PDF.
        /// </summary>
        /// <param name="options">Driver options.</param>
        /// <param name="html">String containing HTML code that should be converted to PDF.</param>
        /// <returns>PDF as byte array.</returns>
        public static byte[] ConvertHtml(DriverOptions options, string html)
        {
            return Convert(options, html);
        }

        /// <summary>
        /// Converts given URL to PDF.
        /// </summary>
        /// <param name="options">Driver options.</param>
        /// <returns>PDF as byte array.</returns>
        public static byte[] Convert(DriverOptions options)
        {
            return Convert(options, null);
        }

        /// <summary>
        /// Converts given URL or HTML string to PDF.
        /// </summary>
        /// <param name="options">Driver options.</param>
        /// <param name="html">String containing HTML code that should be converted to PDF.</param>
        /// <returns>PDF as byte array.</returns>
        private static byte[] Convert(DriverOptions options, string html)
        {
            StringBuilder switches = new StringBuilder();

            // switches:
            //     "-q"  - silent output, only errors - no progress messages
            //     " -"  - switch output to stdout
            //     "- -" - switch input to stdin and output to stdout
            switches.AppendFormat("-q {0} -", options.ToString());

            // generate PDF from given HTML string, not from URL
            if (!string.IsNullOrEmpty(html))
            {
                switches.Append(" -");
                html = SpecialCharsEncode(html);
            }

            var proc = new Process
                           {
                               StartInfo = new ProcessStartInfo
                                               {
                                                   FileName = Path.Combine(options.WkhtmltopdfPath, "wkhtmltopdf.exe"),
                                                   Arguments = switches.ToString(),
                                                   UseShellExecute = false,
                                                   RedirectStandardOutput = true,
                                                   RedirectStandardError = true,
                                                   RedirectStandardInput = true,
                                                   WorkingDirectory = options.WkhtmltopdfPath,
                                                   CreateNoWindow = true
                                               }
                           };
            proc.Start();

            // generate PDF from given HTML string, not from URL
            if (!string.IsNullOrEmpty(html))
            {
                using (var sIn = proc.StandardInput)
                {
                    sIn.WriteLine(html);
                }
            }

            var ms = new MemoryStream();
            using (var sOut = proc.StandardOutput.BaseStream)
            {
                byte[] buffer = new byte[4096];
                int read;

                while ((read = sOut.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
            }

            string error = proc.StandardError.ReadToEnd();

            if (ms.Length == 0 && !options.MuteErrors)
            {
                throw new Exception(error);
            }

            proc.WaitForExit();

            return ms.ToArray();
        }

        /// <summary>
        /// Encode all special chars
        /// </summary>
        /// <param name="text">Html text</param>
        /// <returns>Html with special chars encoded</returns>
        private static string SpecialCharsEncode(string text)
        {
            var chars = text.ToCharArray();
            var result = new StringBuilder(text.Length + (int)(text.Length * 0.1));

            foreach (var c in chars)
            {
                var value = System.Convert.ToInt32(c);
                if (value > 127)
                    result.AppendFormat("&#{0};", value);
                else
                    result.Append(c);
            }

            return result.ToString();
        }
    }
}
