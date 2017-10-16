using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Web.Hosting;

namespace Erwine.Leonard.T.PSWebSrv
{
    public class WorkerRequest : SimpleWorkerRequest
    {
        private string _page = string.Empty;
        public WorkerRequest(string appVirtualDir, string appPhysicalDir, string page, string query, TextWriter output)
             : base(appVirtualDir, appPhysicalDir, page, query, output)
        {
            _page = page;
        }
        public WorkerRequest(string page, string query, TextWriter output)
             : base(page, query, output)
        {
            _page = page;
        }
        public override string GetFilePath()
        {
            return Path.Combine(base.GetFilePath(), _page);
        }
    }
}