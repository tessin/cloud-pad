using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
