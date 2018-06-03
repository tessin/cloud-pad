using CloudPad.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CloudPad
{
    [TestClass]
    public class PackagingTests
    {
        class UserQuery1
        {
            [TimerTrigger("*/5 * * * *")]
            void Test1()
            {
            }
        }

        [TestMethod]
        public async Task Packaging_Test1()
        {
            Console.WriteLine(Environment.CurrentDirectory);

            var scriptFileName = Path.GetFullPath(@"..\..\..\test_hello.linq");

            await Program.MainAsync(new UserQuery1(), new[] { "-" + Options.Script, scriptFileName, "-" + Options.Compile });
        }
    }
}
