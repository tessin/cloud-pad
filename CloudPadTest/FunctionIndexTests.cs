using CloudPad.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CloudPad
{
    [TestClass]
    public class FunctionIndexTests
    {
        class UserQuery
        {
            public void Test()
            {

            }

            public void Test(bool success)
            {

            }
        }

        [TestMethod]
        public void FunctionIndex_Test()
        {
            var index = new FunctionIndex(new UserQuery());

            index.Initialize();
        }
    }
}
