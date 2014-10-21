using Rotativa.Core;
using Rotativa.Core.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                PageMargins = new Margins(),
            };
        }

        private const string ContentType = "application/pdf";

        /// <summary>
        /// Options for called rotativa Driver
        /// </summary>
        public DriverOptions RotativaOptions { get; set; }

        /// <summary>
        /// This will be send to the browser as a name of the generated PDF file.
        /// </summary>
        public string FileName { get; set; }

        [Obsolete(@"Use BuildPdf(this.ControllerContext) method instead and use the resulting binary data to do what needed.")]
        public string SaveOnServerPath { get; set; }

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
        

        protected abstract string GetUrl(ControllerContext context);

        protected virtual byte[] CallTheDriver(ControllerContext context)
        {
            GetWkParams(context);
            var fileContent = WkhtmltopdfDriver.Convert(RotativaOptions);
            return fileContent;
        }

        protected HttpResponseBase PrepareResponse(HttpResponseBase response)
        {
            response.ContentType = ContentType;

            if (!String.IsNullOrEmpty(FileName))
                response.AddHeader("Content-Disposition", string.Format("attachment; filename=\"{0}\"", SanitizeFileName(FileName)));

            response.AddHeader("Content-Type", ContentType);

            return response;
        }

   
        #region Private 
        private void GetWkParams(ControllerContext context)
        {
            RotativaOptions.URL = GetUrl(context);

            HttpCookie authenticationCookie = null;
            if (context.HttpContext.Request.Cookies != null && context.HttpContext.Request.Cookies.AllKeys.Contains(FormsAuthentication.FormsCookieName))
            {
                authenticationCookie = context.HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName];
            }
            if (authenticationCookie != null)
            {
                RotativaOptions.Cookies.Add(authenticationCookie.Name, authenticationCookie.Value);
            }
        }

        private static string SanitizeFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidPathChars()) + new string(Path.GetInvalidFileNameChars()));
            string invalidCharsPattern = string.Format(@"[{0}]+", invalidChars);

            string result = Regex.Replace(name, invalidCharsPattern, "_");
            return result;
        }

        #endregion
    }
}