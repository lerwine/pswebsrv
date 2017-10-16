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
    [OutputType(typeof(HttpListenerInfo))]
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
                UriBuilder uriBuilder = new UriBuilder();
                string scheme = (Scheme == null) ? "" : Scheme.Trim();
                uriBuilder.Scheme = (scheme.Length == 0) ? "http" : scheme;
                UriHostNameType hostNameType;
                if (MyInvocation.BoundParameters.ContainsKey("Host"))
                    uriBuilder.Host = ValidateHostNameAttribute.AssertHostName(Host, out hostNameType);
                else
                    uriBuilder.Host = ValidateHostNameAttribute.AssertHostName(IPAddress.Loopback, out hostNameType);

                if (MyInvocation.BoundParameters.ContainsKey("Port"))
                    uriBuilder.Port = Port;
                WriteObject(HttpListenerInfo.RegisterNew(this, ID, uriBuilder.Uri,
                    (hostNameType == UriHostNameType.Unknown) ? uriBuilder.Uri.HostNameType : hostNameType));
            }
            catch (ArgumentException e)
            {
                if (e.ParamName == null)
                    WriteError(new ErrorRecord(e, "NewHttpListener", ErrorCategory.InvalidArgument, MyInvocation.BoundParameters));
                else if (e.ParamName == "id")
                    WriteError(new ErrorRecord(e, "NewHttpListener", ErrorCategory.InvalidArgument, ID));
                else if (e.ParamName == "value")
                    WriteError(new ErrorRecord(e, "NewHttpListener", ErrorCategory.InvalidArgument, Host));
                else
                    WriteError(new ErrorRecord(e, "NewHttpListener", ErrorCategory.InvalidArgument, MyInvocation.BoundParameters));
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "NewHttpListener", ErrorCategory.OpenError, MyInvocation.BoundParameters));
                throw;
            }
        }
    }
}