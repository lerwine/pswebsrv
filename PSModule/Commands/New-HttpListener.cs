using System;
using System.Linq;
using System.Management.Automation;
using System.Net;

namespace Erwine.Leonard.T.PSWebSrv.Commands
{
    /// <summary>
    /// Creates and registers a new <seealso cref="HttpListener" />.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "HttpListener")]
    //[OutputType(typeof(HttpListenerInfo))]
    public class New_HttpListener : PSCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "String which uniquely identifies the new HTTP Listener.")]
        [ValidateNotNullOrEmpty()]
        public string ID { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "The scheme to use.")]
        [ValidateNotNullOrEmpty()]
        public string Scheme { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "Host name or IP Address to listen on.")]
        [ValidateHostName()]
        [Alias("Host", "HostIP", "IP", "IPAddress")]
        public object HostName { get; set; }

        [Parameter(HelpMessage = "Port to listen on.")]
        [ValidateNotNullOrEmpty()]
        public int Port { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                throw new NotImplementedException();
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "New_HttpListener", ErrorCategory.OpenError, MyInvocation.BoundParameters));
                throw;
            }
        }
    }
}