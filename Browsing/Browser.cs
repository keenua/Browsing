using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Security;
using System.IO;
using System.Web;
using HtmlAgilityPack;
using System.Security.Cryptography.X509Certificates;

namespace Browsing
{
    /// <summary>
    /// Browser class. Has navigate and post functionality
    /// </summary>
    public class Browser : WebClient
    {
        public enum ContentType { UrlEncoded, UrlEncodedUTF8, TextHtmlUTF8 };
        public enum RedirectType { OnlyHost, All, None };

        #region Fields/constants/variables

        #region Static variables

        /// <summary>
        /// Pseudo-random number generator
        /// </summary>
        static Random rand = new Random((int)(DateTime.Now.Ticks % int.MaxValue));

        #endregion

        #region Constants

        /// <summary>
        /// This is how the boundary strings should start
        /// </summary>
        const string boundaryStart = "WebKitFormBoundary";
        /// <summary>
        /// The default encoding for this browser
        /// </summary>
        const string defaultEncoding = "windows-1251";
        /// <summary>
        /// Default user-agent header for this browser
        /// </summary>
        const string defaultUserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/14.0.835.163 Safari/535.1";

        #endregion

        #region Private variables

        /// <summary>
        /// This is the where the redirect url is stored. While not empty, the browser will navigate to it
        /// </summary>
        string _redirectUrl = "";

        bool _ignoreErrors = false;

        RedirectType _redirectType = RedirectType.All;

        #endregion

        #region Public variables/fields

        /// <summary>
        /// A container to store and read cookies from
        /// </summary>
        public CookieContainer CookieContainer { get; set; }

        public RedirectType Redirect
        {
            get { return _redirectType; }
            set { _redirectType = value; }
        }

        public bool IgnoreErrors
        {
            get
            {
                return _ignoreErrors;
            }
            set
            {
                _ignoreErrors = value;
            }
        }

        #endregion

        #endregion

        #region Methods

        #region Constructors

        public bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        
        /// <summary>
        /// Default construcotr
        /// </summary>
        public Browser()
            : this(new CookieContainer())
        {
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(AcceptAllCertifications);
        }

        /// <summary>
        /// Constructor by the cookie container
        /// </summary>
        /// <param name="c">A container to store and read cookies from</param>
        public Browser(CookieContainer c)
        {
            Encoding = Encoding.GetEncoding(defaultEncoding);
            this.CookieContainer = c;

            HtmlNode.ElementsFlags.Remove("form");
            HtmlNode.ElementsFlags.Remove("option");
            HtmlNode.ElementsFlags.Remove("select");
        }

        #endregion

        #region Overidden methods

        /// <summary>
        /// Overriden GetWebRequest method. Handles cookies and adds user-agent header
        /// </summary>
        /// <param name="address">The address of the web request</param>
        protected override WebRequest GetWebRequest(Uri address)
        {
            Headers.Set("User-Agent", defaultUserAgent);

            WebRequest request = base.GetWebRequest(address);
            if (request is HttpWebRequest)
            {
                HttpWebRequest req = request as HttpWebRequest;
                req.KeepAlive = true;
                req.CookieContainer = this.CookieContainer;
                req.AllowAutoRedirect = false;
            }

            return request;
        }

        /// <summary>
        /// Overriden GetWebResponse method. Handles redirects and adds cookies if needed
        /// </summary>
        /// <param name="request">WebRequest</param>
        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = null;

            try
            {
                response = base.GetWebResponse(request);
            }
            catch (Exception e)
            {
                if (e is WebException)
                {
                    if (IgnoreErrors)
                    {
                        WebException we = e as WebException;
                        if (we.Status == WebExceptionStatus.ProtocolError)
                        {
                            response = we.Response;
                        }
                    }
                    else throw e;
                }
            }

            if (response == null) return null;

            if (response is HttpWebResponse)
            {
                HandleRedirect(request, response);
                HandleCookies(request, response);
            }

            return response;
        }

        #endregion

        #region Static methods

        /// <summary>
        /// Generates random letter or digit
        /// </summary>
        static char RandomChar()
        {
            int num = 0;

            while (!char.IsLetterOrDigit((char)num))
            {
                num = rand.Next(48, 122);
            }

            return (char)num;
        }

        /// <summary>
        /// Generates random string of letters and digits of specified length
        /// </summary>
        /// <param name="length">Length of the string</param>
        static string RandomString(int length)
        {
            string result = "";

            for (int i = 0; i < length; i++) result += RandomChar();

            return result;
        }

        /// <summary>
        /// Generates boundary, i.e. "----WebKitFormBoundaryRO5Sq9bfvDteWBtp"
        /// </summary>
        static string GenerateBoundary()
        {
            string result = "----";

            result += boundaryStart;

            result += RandomString(16);

            return result;
        }

        /// <summary>
        /// Converts html page string into HtmlDocument 
        /// </summary>
        /// <param name="page">Html page string</param>
        static HtmlDocument PageToDoc(string page)
        {
            HtmlDocument doc = new HtmlDocument();

            doc.LoadHtml(page);

            return doc;
        }

        /// <summary>
        /// Returns current timestamp in a js format 
        /// </summary>
        public static long CurrentTimestamp()
        {
            return (long)((DateTime.Now - new DateTime(1970, 01, 01)).TotalMilliseconds);
        }

        #endregion

        #region Private/protected methods

        #region Handlers

        /// <summary>
        /// Handles redirect response if needed
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="response">Response</param>
        protected void HandleRedirect(WebRequest request, WebResponse response)
        {
            if (Redirect != RedirectType.None)
            {


                HttpStatusCode code = (response as HttpWebResponse).StatusCode;

                // if redirect is needed
                if (code == HttpStatusCode.Found || code == HttpStatusCode.SeeOther || code == HttpStatusCode.MovedPermanently || code == HttpStatusCode.Moved)
                {
                    // Getting the location header and storing it in the "redirect"
                    _redirectUrl = (response as HttpWebResponse).Headers["Location"];

                    // If the url is not absolute
                    if (!_redirectUrl.StartsWith("http"))
                    {
                        HttpWebRequest webRequest = (request as HttpWebRequest);

                        string host = "";

                        if (Redirect == RedirectType.All)
                        {
                            // Getting the request uri full path 
                            host = webRequest.Headers["Host"] + webRequest.RequestUri.AbsolutePath;
                            // Now leaving only the "directory". I.e. it could have been "http://example.com/engine/post.php", so we leave only "http://example.com/engine"
                            host = host.Substring(0, host.LastIndexOf('/'));
                        }
                        else
                        {
                            host = webRequest.Headers["Host"];
                        }

                        // Have to make sure that the "/" symbol is between the "host" and "redirect" strings
                        if (!_redirectUrl.StartsWith("/") && !host.EndsWith("/")) _redirectUrl = "/" + _redirectUrl;

                        // Concatinating these two strings 
                        _redirectUrl = host + _redirectUrl;
                    }

                    // If the url is still not absolute, adding the "http://" at the beginning
                    if (!_redirectUrl.StartsWith("http"))
                    {
                        _redirectUrl = "http://" + _redirectUrl;
                    }
                }
                else _redirectUrl = "";
            }
        }

        /// <summary>
        /// Handles additional cookies
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="response">Response</param>
        protected void HandleCookies(WebRequest request, WebResponse response)
        {
            CookieCollection col = new CookieCollection();
            foreach (Cookie c in (response as HttpWebResponse).Cookies)
            {
                col.Add(new Cookie(c.Name, c.Value, c.Path, (request as HttpWebRequest).Headers["Host"]));
            }
            this.CookieContainer.Add(col);
        }

        #endregion

        #region Data

        /// <summary>
        /// Adds argument data to the data stream
        /// </summary>
        /// <param name="arg">Argument</param>
        /// <param name="boundary">Boundary</param>
        /// <param name="stream">Reference to the data stream</param>
        protected void AddArg(Arg arg, string boundary, ref Stream stream)
        {
            string boundaryTemplate = "\r\n--{0}\r\nContent-Disposition: form-data; name=\"{1}\"{2}{3}\r\n\r\n";

            string add = "";

            if (arg.additional != null)
            {
                foreach (string key in arg.additional.Keys) add += "; " + key + "=\"" + arg.additional[key] + "\"";
            }

            string contentType = "";

            if (arg.contentType != "")
            {
                contentType = "\r\nContent-Type: " + arg.contentType;
            }

            string str = string.Format(boundaryTemplate, boundary, arg.name, add, contentType);
            byte[] headerBytes = Encoding.UTF8.GetBytes(str);
            stream.Write(headerBytes, 0, headerBytes.Length);

            stream.Write(arg.value, 0, arg.value.Length);
        }

        /// <summary>
        /// Generates the data byte array from the Args entity
        /// </summary>
        /// <param name="args">Arguments</param>
        /// <param name="withBoundary">Whether to use boundary</param>
        /// <param name="separator">String to separate the arguments. i.e "&" or ";"</param>
        /// <param name="uriEscape">Whether to escape characters in the request</param>
        protected byte[] GetData(Args args, bool withBoundary, string separator, bool uriEscape)
        {
            if (withBoundary)
            {
                Stream stream = new MemoryStream();

                string boundary = GenerateBoundary();

                Headers.Set("Content-Type", "multipart/form-data; boundary=" + boundary);

                foreach (Arg arg in args) AddArg(arg, boundary, ref stream);

                byte[] endBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

                stream.Write(endBytes, 0, endBytes.Length);

                stream.Position = 0;

                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                stream.Close();

                return data;
            }
            else
            {
                string data = "";

                foreach (Arg arg in args)
                {
                    string value = args.enc.GetString(arg.value);

                    if (uriEscape) value = UrlEncode(value);

                    data += arg.name + "=" + value + separator;
                }

                if (data != "") data = data.Substring(0, data.Length - 1);

                if (uriEscape) return Encoding.ASCII.GetBytes(data);

                return Encoding.GetBytes(data);
            }
        }

        /// <summary>
        /// Generates the data byte array from the Args entity
        /// </summary>
        /// <param name="args">Arguments</param>
        /// <param name="withBoundary">Whether to use boundary</param>
        /// <param name="uriEscape">Whether to escape characters in the request</param>
        protected byte[] GetData(Args args, bool withBoundary, bool uriEscape)
        {
            return GetData(args, withBoundary, ";", uriEscape);
        }

        /// <summary>
        /// Determines the image content type by its url's extension 
        /// </summary>
        /// <param name="url">Image url</param>
        protected string DetermineImageContentType(string url)
        {
            try
            {
                NameValueCollection types = new NameValueCollection();

                string extension = Path.GetExtension(url).ToLower();

                types.Add(".bmp", "image/bmp");
                types.Add(".gif", "image/gif");
                types.Add(".jpeg", "image/jpeg");
                types.Add(".jpg", "image/jpeg");
                types.Add(".png", "image/png");
                types.Add(".tif", "image/tiff");
                types.Add(".tiff", "image/tiff");

                return types[extension];
            }
            catch
            {
                return "image/jpeg";
            }
        }
        
        #endregion

        #endregion

        #region Public methods

        #region Navigate

        /// <summary>
        /// Navigates to the specified url
        /// </summary>
        /// <param name="url">Url to navigate to</param>
        /// <param name="args">Arguments. The null value is allowed</param>
        public HtmlDocument Navigate(string url, NameValueCollection args)
        {
            try
            {
                if (args != null) QueryString = args;

                string page = DownloadString(url);

                while (_redirectUrl != "") page = DownloadString(_redirectUrl);

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(page);

                return doc;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Navigates to the specified url
        /// </summary>
        /// <param name="url">Url to navigate to</param>
        public HtmlDocument Navigate(string url)
        {
            return Navigate(url, null);
        }

        #endregion

        #region Post

        #region Raw

        /// <summary>
        /// Posts data to the specified url
        /// </summary>
        /// <param name="url">Url to post data to</param>
        /// <param name="data">Data to post</param>
        public string PostRaw(string url, byte[] data)
        {
            byte[] bytes = UploadData(url, "POST", data);

            string page = Encoding.GetString(bytes);

            while (_redirectUrl != "") page = DownloadString(_redirectUrl);

            return page;
        }

        /// <summary>
        /// Posts data to the specified url
        /// </summary>
        /// <param name="url">Url to post data to</param>
        /// <param name="args">Data in a string presentation</param>
        public string PostRaw(string url, string args)
        {
            return PostRaw(url, Encoding.GetBytes(args));
        }

        /// <summary>
        /// Posts data to the specified url
        /// </summary>
        /// <param name="url">Url to post data to</param>
        /// <param name="args">Arguments</param>
        /// <param name="withBoundary">Whether to use boundary as a separator</param>
        /// <param name="separator">String to separate the arguments. i.e "&" or ";"</param>
        /// <param name="uriEscape">Whether to escape characters in the request</param>
        public string PostRaw(string url, Args args, bool withBoundary, string separator, bool uriEscape)
        {
            byte[] data = GetData(args, withBoundary, separator, uriEscape);

            return PostRaw(url, data);
        }

        /// <summary>
        /// Posts data to the specified url
        /// </summary>
        /// <param name="url">Url to post data to</param>
        /// <param name="args">Arguments</param>
        /// <param name="withBoundary">Whether to use boundary as a separator</param>
        /// <param name="uriEscape">Whether to escape characters in the request</param>
        public string PostRaw(string url, Args args, bool withBoundary, bool uriEscape)
        {
            return PostRaw(url, args, withBoundary, ";", uriEscape);
        }

        /// <summary>
        /// Posts data to the specified url
        /// </summary>
        /// <param name="url">Url to post data to</param>
        /// <param name="args">Arguments</param>
        /// <param name="withBoundary">Whether to use boundary as a separator</param>
        /// <param name="files">Files to post (Key = argument name, Value = file path)</param>
        /// <param name="contentType">Files content type, i.e. "application/octet-stream", "image/jpeg", etc. When the value is "", image content type is automatically determined</param>
        public string PostRaw(string url, Args args, bool withBoundary, NameValueCollection files, string contentType)
        {
            if (files != null)
                foreach (string key in files.Keys)
                {
                    List<string> urls = new List<string>();

                    foreach (string u in files[key].Split(',')) urls.Add(u);

                    foreach (string fileUrl in urls)
                    {
                        string fileName = Path.GetFileName(fileUrl);

                        byte[] fileBytes = new byte[0];

                        if (fileUrl != "")
                        {
                            fileBytes = DownloadData(fileUrl);
                        }

                        NameValueCollection additional = new NameValueCollection();
                        additional.Add("filename", fileName);

                        if (contentType == "") contentType = DetermineImageContentType(fileUrl);

                        args.Add(key, fileBytes, additional, contentType);
                    }
                }

            return PostRaw(url, args, withBoundary, false);
        }

        /// <summary>
        /// Posts data to the specified url
        /// </summary>
        /// <param name="url">Url to post data to</param>
        /// <param name="args">Arguments</param>
        /// <param name="withBoundary">Whether to use boundary as a separator</param>
        /// <param name="files">Files to post (Key = argument name, Value = file path)</param>
        public string PostRaw(string url, Args args, bool withBoundary, NameValueCollection files)
        {
            return PostRaw(url, args, withBoundary, files, "");
        }

        #endregion

        #region Post

        /// <summary>
        /// Posts data to the specified url
        /// </summary>
        /// <param name="url">Url to post data to</param>
        /// <param name="data">Data to post</param>
        public HtmlDocument Post(string url, byte[] data)
        {
            string page = PostRaw(url, data);

            return PageToDoc(page);
        }

        /// <summary>
        /// Posts data to the specified url
        /// </summary>
        /// <param name="url">Url to post data to</param>
        /// <param name="args">Data in a string presentation</param>
        public HtmlDocument Post(string url, string args)
        {
            string page = PostRaw(url, args);

            return PageToDoc(page);
        }

        /// <summary>
        /// Posts data to the specified url
        /// </summary>
        /// <param name="url">Url to post data to</param>
        /// <param name="args">Arguments</param>
        /// <param name="withBoundary">Whether to use boundary as a separator</param>
        /// <param name="separator">String to separate the arguments. i.e "&" or ";"</param>
        /// <param name="uriEscape">Whether to escape characters in the request</param>
        public HtmlDocument Post(string url, Args args, bool withBoundary, string separator, bool uriEscape)
        {
            string page = PostRaw(url, args, withBoundary, separator, uriEscape);

            return PageToDoc(page);
        }

        /// <summary>
        /// Posts data to the specified url
        /// </summary>
        /// <param name="url">Url to post data to</param>
        /// <param name="args">Arguments</param>
        /// <param name="withBoundary">Whether to use boundary as a separator</param>
        /// <param name="uriEscape">Whether to escape characters in the request</param>
        public HtmlDocument Post(string url, Args args, bool withBoundary, bool uriEscape)
        {
            string page = PostRaw(url, args, withBoundary, uriEscape);

            return PageToDoc(page);
        }

        /// <summary>
        /// Posts data to the specified url
        /// </summary>
        /// <param name="url">Url to post data to</param>
        /// <param name="args">Arguments</param>
        /// <param name="withBoundary">Whether to use boundary as a separator</param>
        /// <param name="files">Files to post (Key = argument name, Value = file path)</param>
        /// <param name="contentType">Files content type, i.e. "application/octet-stream", "image/jpeg", etc. When the value is "", image content type is automatically determined</param>
        public HtmlDocument Post(string url, Args args, bool withBoundary, NameValueCollection files, string contentType)
        {
            string page = PostRaw(url, args, withBoundary, files, contentType);

            return PageToDoc(page);
        }

        /// <summary>
        /// Posts data to the specified url
        /// </summary>
        /// <param name="url">Url to post data to</param>
        /// <param name="args">Arguments</param>
        /// <param name="withBoundary">Whether to use boundary as a separator</param>
        /// <param name="files">Files to post (Key = argument name, Value = file path)</param>
        public HtmlDocument Post(string url, Args args, bool withBoundary, NameValueCollection files)
        {
            string page = PostRaw(url, args, withBoundary, files);

            return PageToDoc(page);
        }

        #endregion

        #endregion

        #region Args extraction

        public NameValueCollection ExtractOptions(HtmlNode input)
        {
            NameValueCollection options = new NameValueCollection();

            HtmlNodeCollection optionNodes = input.SelectNodes(".//option");

            if (optionNodes == null) return options;

            foreach (HtmlNode o in optionNodes)
            {
                string text = o.InnerText.Trim();
                string value = o.GetAttributeValue("value", "");

                options.Add(text, value);
            }

            return options;
        }

        public Args ExtractArgs(HtmlNode formNode, out string action)
        {
            action = "";
            if (formNode == null) return null;

            action = formNode.GetAttributeValue("action", "");

            Args args = new Args();
            args.enc = Encoding;

            List<HtmlNode> inputs = formNode.Descendants("input").ToList();
            inputs.AddRange(formNode.Descendants("select"));
            inputs.AddRange(formNode.Descendants("textarea"));
            inputs.AddRange(formNode.Descendants("button"));

            if (inputs == null) return null;

            foreach (HtmlNode i in inputs)
            {
                string name = i.GetAttributeValue("name", "");
                string value = i.GetAttributeValue("value", "");

                if (name != "") 
                {
                    Arg arg = new Arg(name, value, Encoding);

                    arg.options = ExtractOptions(i);

                    args.Add(arg);
                }
            }

            return args;
        }

        public Args ExtractArgs(HtmlNode formNode)
        {
            string action = "";

            return ExtractArgs(formNode, out action);
        }

        public Args ExtractArgs(HtmlDocument doc, string xpath, out string action)
        {
            HtmlNode formNode = doc.DocumentNode.SelectSingleNode(xpath);

            action = "";

            if (formNode == null) return null;

            return ExtractArgs(formNode, out action);
        }

        public Args ExtractArgs(HtmlDocument doc, string xpath)
        {
            string action = "";

            return ExtractArgs(doc, xpath, out action);
        }

        #endregion

        public void SetContentType(string type)
        {
            Headers.Set("Content-Type", type);
        }

        public void SetContentType(ContentType type)
        {
            switch (type)
            {
                case ContentType.UrlEncoded: SetContentType("application/x-www-form-urlencoded");
                    break;
                case ContentType.UrlEncodedUTF8: SetContentType("application/x-www-form-urlencoded; charset=UTF-8");
                    break;
                case ContentType.TextHtmlUTF8: SetContentType("text/html; charset=UTF-8");
                    break;
            }
        }

        public void MatchCookies(string sourceUrl, string destUrl)
        {
            Uri sourceUri = new Uri(sourceUrl);
            Uri destUri = new Uri(destUrl);

            string sourceCookieHeader = CookieContainer.GetCookieHeader(sourceUri);

            CookieContainer.SetCookies(destUri, sourceCookieHeader);
        }

        public static NameValueCollection HeaderToCookies(string header)
        {
            NameValueCollection result = new NameValueCollection();

            string[] cookies = header.Split(';');

            foreach (string c in cookies)
            {
                string[] nv = c.Trim().Split('=');

                if (nv.Length != 2) continue;

                string name = nv[0].Trim();
                string value = nv[1].Trim();

                if (name == "") continue;

                if (result[name] == null) result.Add(name, value);
            }

            return result;
        }

        public static string CookiesToHeader(NameValueCollection cookies)
        {
            string result = "";

            foreach (string k in cookies.Keys)
            {
                if (result != "") result += " ";

                result += k + "=" + cookies[k] + ";"; 
            }

            if (result.EndsWith(";")) result = result.Substring(0, result.Length - 1);

            return result;
        }

        public void DeleteCookie(string url, string name)
        {
            Uri uri = new Uri(url);

            string header = CookieContainer.GetCookieHeader(new Uri(url));

            NameValueCollection cookies = HeaderToCookies(header);

            cookies.Remove(name);

            header = CookiesToHeader(cookies);

            CookieContainer = new System.Net.CookieContainer();

            foreach (string k in cookies.Keys) CookieContainer.SetCookies(uri, k + "=" + cookies[k]);
        }

        public void AddCookie(string url, string name, string value)
        {
            Uri uri = new Uri(url);

            string header = CookieContainer.GetCookieHeader(new Uri(url));

            NameValueCollection cookies = HeaderToCookies(header);

            cookies.Add(name, value);

            header = CookiesToHeader(cookies);

            CookieContainer = new System.Net.CookieContainer();

            foreach (string k in cookies.Keys) CookieContainer.SetCookies(uri, k + "=" + cookies[k]);
        }

        public string UrlEncode(string str)
        {
            string result = HttpUtility.UrlEncode(str);

            for (int i = 0; i < result.Length - 2; i++)
            {
                if (result[i] == '%')
                {
                    string start = result.Substring(0, i + 1) + result.Substring(i + 1, 2).ToUpper();
                    string end = "";
                    if (i + 3 < result.Length) end = result.Substring(i + 3);

                    result = start + end;
                }
            }

            return result;
        }

        public string GetCookieValue(string uri, string name)
        {
            foreach (Cookie c in CookieContainer.GetCookies(new Uri(uri))) if (c.Name == name) return c.Value;

            return "";
        }

        #endregion

        #endregion
    }
}
