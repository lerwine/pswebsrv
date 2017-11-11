using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Threading;

namespace Erwine.Leonard.T.PSWebSrv
{
    internal class PathHttpHandler : PSHttpHandler
    {
        private string _sourceDirectory;

        internal PathHttpHandler(string name, string sourceDirectory, string virtualPath, string pattern, string exclude)
            : base(name, virtualPath, pattern, exclude)
        {
            ValidateLocalPathAttribute.AssertPath(sourceDirectory, out sourceDirectory);
            _sourceDirectory = sourceDirectory;
        }
    }
}