using CloudPad.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CloudPad
{
    [TestClass]
    public class Test_Timer
    {
        class UserQuery
        {
            private CancellationTokenSource cts;

            public UserQuery(CancellationTokenSource cts)
            {
                this.cts = cts;
            }

            public int tickCount;

            [TimerTrigger("*/1 * * * * *")]
            public void Tick()
            {
                Console.WriteLine(tickCount++ % 2 == 0 ? "tick" : "tock");

                if (2 < tickCount)
                {
                    cts.Cancel();
                }
            }
        }

        [TestMethod, Timeout(15 * 1000)]
        public async Task Test_Timer_Test()
        {
            var cts = new CancellationTokenSource();

            var query = new UserQuery(cts);

            using (var invoker = new JobHost(query, new string[0]))
            {
                await invoker.WaitAsync(cts.Token);
            }

            Assert.AreEqual(3, query.tickCount);
        }
    }
}
