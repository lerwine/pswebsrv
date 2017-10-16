using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Web;

namespace Erwine.Leonard.T.PSWebSrv
{
    public class CustomAspHost : MarshalByRefObject
    {
        public void parse_code(string page, string query, ref StreamWriter sw)
        {
            WorkerRequest swr = new WorkerRequest(page, query, sw);
            HttpRuntime.ProcessRequest(swr);
        }
    }
}