using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace CloudPad.Internal
{
    public interface ILINQPadScript
    {
        Task<ILINQPadScriptResult> RunAsync(string[] args);
    }

    public interface ILINQPadScriptResult
    {
        Task<string> GetResultAsync();
    }

    class LINQPadFile
    {
        // see LINQPad.ObjectModel.Query.Populate

        readonly Regex _validQueryHeader = new Regex("(?i)^\\s*<query");

        public XElement metadata_;
        public string source_;

        public bool Load(string fn)
        {
            return Parse(File.ReadAllText(fn));
        }

        public bool Parse(string data)
        {
            bool flag = !_validQueryHeader.IsMatch(data);
            bool result;
            if (flag)
            {
                result = false;
            }
            else
            {
                XElement xElement;
                int lineNumber;
                try
                {
                    XmlParserContext context = new XmlParserContext(null, null, null, XmlSpace.None);
                    Stream xmlFragment = new MemoryStream(Encoding.UTF8.GetBytes(data));
                    XmlTextReader xmlTextReader = new XmlTextReader(xmlFragment, XmlNodeType.Element, context);
                    xmlTextReader.MoveToContent();
                    XmlReader reader = xmlTextReader.ReadSubtree();
                    StringBuilder stringBuilder = new StringBuilder();
                    using (XmlWriter xmlWriter = XmlWriter.Create(stringBuilder))
                    {
                        xmlWriter.WriteNode(reader, true);
                    }
                    xElement = XElement.Parse(stringBuilder.ToString());
                    lineNumber = xmlTextReader.LineNumber;
                }
                catch (XmlException)
                {
                    result = false;
                    return result;
                }
                //bool flag2 = xElement.Attribute("Kind") != null;
                //if (flag2)
                //{
                //    try
                //    {
                //        this._language = (QueryLanguage)Enum.Parse(typeof(QueryLanguage), (string)xElement.Attribute("Kind"), true);
                //    }
                //    catch (ArgumentException)
                //    {
                //    }
                //}
                this.metadata_ = xElement;
                StringReader stringReader = new StringReader(data);
                for (int i = 0; i < lineNumber; i++)
                {
                    stringReader.ReadLine();
                }
                this.source_ = stringReader.ReadToEnd().Trim();
                result = true;
            }
            return result;
        }

        public void PrepareConnection()
        {
            foreach (var conn in metadata_.Elements("Connection"))
            {
                var password = (string)conn.Element("Password");

                if (string.IsNullOrEmpty(password))
                {
                    continue;
                }

                var password2 = LINQPad.Decrypt(password);

                if (string.IsNullOrEmpty(password2))
                {
                    throw new CloudPadException("connection password cannot be read by this user. it's likely that the file has been copied between different computers/users. you must fix the connection manually before you can deploy the script again.");
                }

                var driverData = conn.Element("DriverData");
                if (driverData == null)
                {
                    conn.Add(driverData = new XElement("DriverData"));
                }

                var extraCxOptions = driverData.Element("ExtraCxOptions");
                if (extraCxOptions == null)
                {
                    driverData.Add(extraCxOptions = new XElement("ExtraCxOptions"));
                }

                var cb = new SqlConnectionStringBuilder(extraCxOptions.Value);

                if (!cb.IntegratedSecurity)
                {
                    cb.Password = password2;
                }

                extraCxOptions.Value = cb.ToString();
            }
        }

        public void Save(string fn)
        {
            using (var outputStream = File.Create(fn))
            {
                Save(outputStream);
            }
        }

        public void Save(Stream outputStream)
        {
            var w = new StreamWriter(outputStream, Encoding.UTF8);
            var x = XmlWriter.Create(w, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true });
            metadata_.Save(x);
            x.Flush();
            w.WriteLine();
            w.WriteLine();
            w.WriteLine(source_);
            w.Flush();
        }
    }
}
