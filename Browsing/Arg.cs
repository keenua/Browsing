using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Browsing
{
    /// <summary>
    /// Argument class. Stores name, value and some additional argument attributes
    /// </summary>
    public class Arg
    {
        /// <summary>Argument name</summary>
        public string name;
        /// <summary>Argument value</summary>
        public byte[] value;
        /// <summary>Additional attributes, i.e. "filename" for file arguments</summary>
        public NameValueCollection additional;
        /// <summary>Argument content type, i.e. "application/ocstream"</summary>
        public string contentType;
        /// <summary>Possible values for the argument. Mostly for the "select" nodes</summary>
        public NameValueCollection options;
        /// <summary>Argument encoding</summary>
        public Encoding enc = Encoding.GetEncoding("windows-1251");

        /// <summary>Argument</summary>
        /// <param name="name">Argument name</param>
        /// <param name="value">Argument value</param>
        public Arg(string name, byte[] value)
        {
            additional = null;
            contentType = "";
            options = new NameValueCollection();
            additional = new NameValueCollection();

            this.name = name;
            this.value = value;
        }

        /// <summary>Argument</summary>
        /// <param name="name">Argument name</param>
        /// <param name="value">Argument value</param>
        /// <param name="enc">Argument encoding</param>
        public Arg(string name, string value, Encoding enc)
            : this(name, enc.GetBytes(value))
        {
            this.enc = enc;
        }

        /// <summary>Argument</summary>
        /// <param name="name">Argument name</param>
        /// <param name="value">Argument value</param>
        /// <param name="additional">Additional attributes, i.e. "filename" for file arguments</param>
        public Arg(string name, byte[] value, NameValueCollection additional)
            : this(name, value)
        {
            this.additional = additional;
        }


        /// <summary>Argument</summary>
        /// <param name="name">Argument name</param>
        /// <param name="value">Argument value</param>
        /// <param name="additional">Additional attributes, i.e. "filename" for file arguments</param>
        /// <param name="enc">Argument encoding</param>
        public Arg(string name, string value, NameValueCollection additional, Encoding enc)
            : this(name, enc.GetBytes(value), additional)
        {
            this.enc = enc;
        }

        /// <summary>Argument</summary>
        /// <param name="name">Argument name</param>
        /// <param name="value">Argument value</param>
        /// <param name="additional">Additional attributes, i.e. "filename" for file arguments</param>
        /// <param name="contentType">Argument content type, i.e. "application/ocstream"</param>
        public Arg(string name, byte[] value, NameValueCollection additional, string contentType)
            : this(name, value, additional)
        {
            this.contentType = contentType;
        }

        /// <summary>Argument</summary>
        /// <param name="name">Argument name</param>
        /// <param name="value">Argument value</param>
        /// <param name="additional">Additional attributes, i.e. "filename" for file arguments</param>
        /// <param name="contentType">Argument content type, i.e. "application/ocstream"</param>
        /// <param name="enc">Argument encoding</param>
        public Arg(string name, string value, NameValueCollection additional, string contentType, Encoding enc)
            : this(name, enc.GetBytes(value), additional, contentType)
        {
            this.enc = enc;
        }

        /// <summary>
        /// Selects one of the possible values of the argument
        /// </summary>
        /// <param name="option">An option to select</param>
        public bool SelectOption(string option)
        {
            foreach (string o in options.Keys)
            {
                if (o == option)
                {
                    value = enc.GetBytes(options[o]);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sets the value of the argument
        /// </summary>
        /// <param name="value">Value to set</param>
        public void SetValue(string value)
        {
            this.value = enc.GetBytes(value);
        }

        /// <summary>
        /// Sets the value of the argument
        /// </summary>
        /// <param name="value">Value to set</param>
        public void SetValue(byte[] value)
        {
            this.value = value;
        }

        /// <summary>
        /// Prints argument content into a string
        /// </summary>
        public override string ToString()
        {
            string result = "";
            
            foreach (string s in ToStringLines()) 
            {
                result += s + "\r\n";
            }

            return result;
        }

        /// <summary>
        /// Prints argument content into a list of strings
        /// </summary>
        public List<string> ToStringLines()
        {
            List<string> result = new List<string>();

            result.Add("Name: " + name);
            result.Add("Value: \"" + enc.GetString(value) + "\"");
            result.Add("Encoding: " + enc.BodyName);
            if (contentType != "") result.Add("Contenty type: " + contentType);

            if (additional.Count != 0)
            {
                result.Add("Additional:");
                foreach (string k in additional.Keys) result.Add("\t" + k + " = \"" + additional[k] + "\"");
            }

            if (options.Count != 0)
            {
                result.Add("Options:");
                foreach (string k in options.Keys) result.Add("\t" + k + " = \"" + options[k] + "\"");
            }

            return result;
        }
    }

    /// <summary>
    /// Argument collection
    /// </summary>
    public class Args : List<Arg>
    {
        /// <summary>
        /// Arguments' encoding
        /// </summary>
        public Encoding enc = Encoding.GetEncoding("windows-1251");

        #region Add

        /// <summary>
        /// Add new argument
        /// </summary>
        /// <param name="name">Argument name</param>
        /// <param name="value">Argument value</param>
        public void Add(string name, byte[] value)
        {
            Add(new Arg(name, value));
        }

        /// <summary>
        /// Add new argument
        /// </summary>
        /// <param name="name">Argument name</param>
        /// <param name="value">Argument value</param>
        public void Add(string name, string value)
        {
            Add(new Arg(name, value, enc));
        }

        /// <summary>
        /// Add new argument
        /// </summary>
        /// <param name="name">Argument name</param>
        /// <param name="value">Argument value</param>
        /// <param name="additional">Additional attributes, i.e. "filename" for file arguments</param>
        public void Add(string name, byte[] value, NameValueCollection additional)
        {
            Add(new Arg(name, value, additional));
        }

        /// <summary>
        /// Add new argument
        /// </summary>
        /// <param name="name">Argument name</param>
        /// <param name="value">Argument value</param>
        /// <param name="additional">Additional attributes, i.e. "filename" for file arguments</param>
        public void Add(string name, string value, NameValueCollection additional)
        {
            Add(new Arg(name, value, additional, enc));
        }

        /// <summary>Argument</summary>
        /// <param name="name">Argument name</param>
        /// <param name="value">Argument value</param>
        /// <param name="additional">Additional attributes, i.e. "filename" for file arguments</param>
        /// <param name="contentType">Argument content type, i.e. "application/ocstream"</param>
        public void Add(string name, byte[] value, NameValueCollection additional, string contentType)
        {
            Add(new Arg(name, value, additional, contentType));
        }

        /// <summary>Argument</summary>
        /// <param name="name">Argument name</param>
        /// <param name="value">Argument value</param>
        /// <param name="additional">Additional attributes, i.e. "filename" for file arguments</param>
        /// <param name="contentType">Argument content type, i.e. "application/ocstream"</param>
        public void Add(string name, string value, NameValueCollection additional, string contentType)
        {
            Add(new Arg(name, value, additional, contentType, enc));
        }

        #endregion

        #region Get

        /// <summary>
        /// Get argument by name
        /// </summary>
        /// <param name="name">Argument name</param>
        public Arg this[string name]
        {
            get
            {
                foreach (Arg a in this) 
                    if (a.name == name) return a;
                return null;
            }
        }

        #endregion

        #region Remove

        /// <summary>
        /// Removes argument by name
        /// </summary>
        /// <param name="name">Argument name</param>
        public void Remove(string name)
        {
            for (int i = 0; i < Count; i++)
                if (this[i].name == name)
                {
                    RemoveAt(i);
                    return;
                }
        }

        /// <summary>
        /// Removes all arguments with a specified name
        /// </summary>
        /// <param name="name">Argument name</param>
        public void RemoveAll(string name)
        {
            while (this[name] != null) Remove(name);
        }

        #endregion

        #region Select

        public void SelectRadioValue(string name, string value)
        {
            RemoveAll(name);
            Add(name, value);
        }

        #endregion

        #region To string

        /// <summary>
        /// Prints collection content into a string
        /// </summary>
        public override string ToString()
        {
            string result = "";

            foreach (string s in ToStringLines())
            {
                result += s + "\r\n";
            }

            return result;
        }

        /// <summary>
        /// Prints collection content into a list of strings
        /// </summary>
        public List<string> ToStringLines()
        {
            List<string> result = new List<string>();

            result.Add("Encoding: " + enc.BodyName);
            result.Add("Arguments:");

            string prefix = "\t";

            foreach (Arg arg in this)
            {
                result.Add(prefix + "======ARG======");
                foreach (string s in arg.ToStringLines()) result.Add(prefix + s);
                result.Add("");
            }
            result.Add("===============");

            return result;
        }

        #endregion

        public NameValueCollection ToNameValueCollection()
        {
            var result = new NameValueCollection();

            foreach (var a in this)
            {
                result.Add(a.name, System.Web.HttpUtility.HtmlDecode(enc.GetString(a.value)));
            }

            return result;
        }
    }
}
