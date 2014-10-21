using Rotativa.Core;
using Rotativa.Core.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;


namespace Rotativa.MVC
{
    public abstract class AsPdfResultBase : ActionResult
    {
        public AsPdfResultBase()
        {
            RotativaOptions = new DriverOptions
            {
            WkhtmltopdfPath = string.Empty,
            FormsAuthenticationCookieName = ".ASPXAUTH",
            PageMargins = new Margins(),
            };
        }

        private const string ContentType = "application/pdf";

        public DriverOptions RotativaOptions { get; set; }

        /// <summary>
        /// This will be send to the browser as a name of the generated PDF file.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Path to wkhtmltopdf binary.
        /// </summary>
        /// public string WkhtmltopdfPath { get; set; }

        /// <summary>
        /// Sets cookies.
        /// </summary>
        [OptionFlag("--cookie")]
        public Dictionary<string, string> Cookies { get; set; }


        [Obsolete(@"Use BuildPdf(this.ControllerContext) method instead and use the resulting binary data to do what needed.")]
        public string SaveOnServerPath { get; set; }

        protected abstract string GetUrl(ControllerContext context);

        private string GetWkParams(ControllerContext context)
        {
            var switches = string.Empty;

            HttpCookie authenticationCookie = null;
            if (context.HttpContext.Request.Cookies != null && context.HttpContext.Request.Cookies.AllKeys.Contains(FormsAuthentication.FormsCookieName))
            {
                authenticationCookie = context.HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName];
            }
            if (authenticationCookie != null)
            {
                var authCookieValue = authenticationCookie.Value;
                switches += " --cookie " +  RotativaOptions.FormsAuthenticationCookieName + " " + authCookieValue;
            }

            switches += " " + RotativaOptions.ToString();

            var url = GetUrl(context);
            switches += " " + url;

            return switches;
        }

        protected virtual byte[] CallTheDriver(ControllerContext context)
        {
            var switches = GetWkParams(context);
            var fileContent = WkhtmltopdfDriver.Convert(RotativaOptions.WkhtmltopdfPath, switches);
            return fileContent;
        }

        public byte[] BuildPdf(ControllerContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            if (RotativaOptions.WkhtmltopdfPath == string.Empty)
                RotativaOptions.WkhtmltopdfPath = HttpContext.Current.Server.MapPath("~/Rotativa");

            var fileContent = CallTheDriver(context);

            if (!string.IsNullOrEmpty(SaveOnServerPath))
            {
                File.WriteAllBytes(SaveOnServerPath, fileContent);
            }

            return fileContent;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            var fileContent = BuildPdf(context);

            var response = PrepareResponse(context.HttpContext.Response);

            response.OutputStream.Write(fileContent, 0, fileContent.Length);
        }

        private static string SanitizeFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidPathChars()) + new string(Path.GetInvalidFileNameChars()));
            string invalidCharsPattern = string.Format(@"[{0}]+", invalidChars);

            string result = Regex.Replace(name, invalidCharsPattern, "_");
            return result;
        }

        protected HttpResponseBase PrepareResponse(HttpResponseBase response)
        {
            response.ContentType = ContentType;

            if (!String.IsNullOrEmpty(FileName))
                response.AddHeader("Content-Disposition", string.Format("attachment; filename=\"{0}\"", SanitizeFileName(FileName)));

            response.AddHeader("Content-Type", ContentType);

            return response;
        }
    }
}