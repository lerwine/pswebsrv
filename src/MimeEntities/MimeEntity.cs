using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Mime;
using System.Threading;

namespace Erwine.Leonard.T.PSWebSrv.MimeEntities
{
    public class MimeEntity
    {
        public static Dictionary<string, string> ReadMimeHeaders(TextReader source, out string extraText)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (source == null)
                throw new ArgumentNullException("source");
            string currentLine = source.ReadLine();
            int index;
            if (String.IsNullOrEmpty(currentLine) || (index = Utility.GetFieldSeparatorIndex(currentLine)) < 0)
            {
                extraText = currentLine;
                return result;
            }
            string name = currentLine.Substring(0, index);
            string value = currentLine.Substring(index + 1).Trim();
            while ((currentLine = source.ReadLine()) != null && currentLine.Length > 0)
            {
                if (Char.IsWhiteSpace(currentLine[0]))
                {
                    if ((currentLine = currentLine.Trim()).Length > 0)
                        value = (value.Length == 0) ? currentLine : value + " " + currentLine;
                }
                else
                {
                    if ((index = Utility.GetFieldSeparatorIndex(currentLine)) < 0)
                        break;
                    if (result.ContainsKey(name))
                        result[name] = value;
                    else
                        result.Add(name, value);
                    name = currentLine.Substring(0, index);
                    value = currentLine.Substring(index + 1).Trim();
                }
            }
            if (result.ContainsKey(name))
                result[name] = value;
            else
                result.Add(name, value);
            extraText = currentLine;
            return result;
        }

        public static MimeEntity Parse(TextReader source)
        {
            string firstContentLine;
            Dictionary<string, string> mimeHeaders = ReadMimeHeaders(source, out firstContentLine);

            ContentType ct = null;
            try
            {
                if (mimeHeaders.ContainsKey("Content-Type"))
                    ct = new ContentType(mimeHeaders["Content-Type"]);
            } catch { }
            
            if (ct == null)
            {
                string s = source.ReadToEnd();
                if (s == null)
                    s = firstContentLine;
                else if (firstContentLine != null)
                    s = firstContentLine + s;
                return new PlainTextContent(s);
            }
            
            if (ct.MediaType == "multipart/mixed" && !String.IsNullOrEmpty(ct.Boundary))
            {
                throw new NotImplementedException();
            }
            else
            {
                string enc = (mimeHeaders.ContainsKey("Content-Transfer-Encoding")) ? mimeHeaders["Content-Transfer-Encoding"] ?? "" : "";
                switch (enc)
                {
                    case "7bit":
                        break;
                    case "8bit":
                        break;
                    case "binary":
                        break;
                    case "quoted-printable":
                        break;
                    case "base64":
                        break;
                    default:
                        break;
                }
                throw new NotImplementedException();
            }
        }
    }
}
