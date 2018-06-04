using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CloudPad.Internal
{
    class PublishProfile
    {
        public Uri PublishUrl { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public void ReadFromFile(string path)
        {
            if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var publishData = JToken.Parse(File.ReadAllText(path));
                var publishProfile = publishData["publish-profile"];

                PublishUrl = new Uri("https://" + (string)publishProfile["publishUrl"]);
                UserName = (string)publishProfile["userName"];
                Password = (string)publishProfile["userPWD"];
                return;
            }

            if (path.EndsWith(".PublishSettings", StringComparison.OrdinalIgnoreCase))
            {
                var publishData = XElement.Load(path);
                var publishProfile = publishData.Elements("publishProfile").Where(el => (string)el.Attribute("publishMethod") == "MSDeploy").Single();

                PublishUrl = new Uri("https://" + (string)publishProfile.Attribute("publishUrl"));
                UserName = (string)publishProfile.Attribute("userName");
                Password = (string)publishProfile.Attribute("userPWD");
                return;
            }

            throw new NotSupportedException();
        }
    }
}
