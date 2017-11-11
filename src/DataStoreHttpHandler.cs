using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Threading;

namespace Erwine.Leonard.T.PSWebSrv
{
    internal class DataStoreHttpHandler : PSHttpHandler
    {
        private string _dataStoreDirectory;

        internal DataStoreHttpHandler(string name, string dataStoreDirectory, string virtualPath, string pattern, string exclude)
            : base(name, virtualPath, pattern, exclude)
        {
            ValidateLocalPathAttribute.AssertPath(dataStoreDirectory, out dataStoreDirectory);
            _dataStoreDirectory = dataStoreDirectory;
        }
    }
}