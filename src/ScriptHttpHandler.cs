using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Threading;

namespace Erwine.Leonard.T.PSWebSrv
{
    internal class ScriptHttpHandler : PSHttpHandler
    {
        private ScriptBlock _scriptBlock;

        internal ScriptHttpHandler(string name, ScriptBlock scriptBlock, string virtualPath, string pattern, string exclude)
            : base(name, virtualPath, pattern, exclude)
        {
            _scriptBlock = ScriptBlock.Create(scriptBlock.ToString());
        }
    }
}