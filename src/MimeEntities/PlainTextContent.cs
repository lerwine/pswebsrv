using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Mime;
using System.Threading;

namespace Erwine.Leonard.T.PSWebSrv.MimeEntities
{
    public class PlainTextContent : MimeEntity
    {
        public string Text { get; set; }

        public PlainTextContent(string text)
        {
            Text = text;
        }
    }
}