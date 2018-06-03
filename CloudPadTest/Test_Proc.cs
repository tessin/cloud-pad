using CloudPad.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CloudPad
{
    [TestClass]
    public class Test_Proc
    {
        [TestMethod]
        public async Task Test_Proc_Test()
        {
            Console.WriteLine(Environment.CurrentDirectory);

            var linqPadScriptFileName = Path.GetFullPath(@"..\..\..\test_proc.linq");

            var traceWriter = new Mock<ITraceWriter>();

            using (var invoker = new Invoker())
            {
                await invoker.RunTimerTriggerAsync(linqPadScriptFileName, "Tick", traceWriter.Object);
                await invoker.RunTimerTriggerAsync(linqPadScriptFileName, "Tick", traceWriter.Object);
                await invoker.RunTimerTriggerAsync(linqPadScriptFileName, "Tick", traceWriter.Object);
            }
        }
    }
}
