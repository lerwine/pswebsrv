using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading;

namespace Erwine.Leonard.T.PSWebSrv.MimeEntities
{
    public static class Utility
    {
        public const char LBoundPrintableChar = ' ';

        public const char UBoundPrintableChar = '\x7f';

        private static readonly char[] _CRLF = new char[] { '\r', '\n' };

        public static ContentType GetContentTypeFromExtension(string extension)
        {
            if (String.IsNullOrEmpty(extension))
                return new ContentType(MediaTypeNames.Application.Octet);
            if (extension[0] != '.')
                extension = "." + extension;
            
            RegistryKey key = Registry.ClassesRoot.OpenSubKey(extension.ToLower(), false);
            if (key != null)
            {
                string s = (string)(key.GetValue("Content Type", ""));
                if (s.Length > 0)
                    return new ContentType(s);
                s = (string)(key.GetValue("PerceivedType", ""));
                if (String.Equals(s, "text", StringComparison.InvariantCultureIgnoreCase))
                    return new ContentType(MediaTypeNames.Text.Plain);
            }
            return new ContentType(MediaTypeNames.Application.Octet);
        }

        public static string GetExtensionFromContentType(ContentType contentType)
        {
            if (!String.IsNullOrEmpty(contentType.Name))
            {
                try
                {
                    string s = Path.GetExtension(contentType.Name);
                    if (!String.IsNullOrEmpty(s))
                        return s;
                }
                catch { }
            }

            if (String.IsNullOrEmpty(contentType.MediaType))
                return "";
            RegistryKey key = Registry.ClassesRoot.OpenSubKey("MIME\\Database\\Content Type\\" + contentType.MediaType.ToLower(), false);
            if (key != null)
                return (string)(key.GetValue("Extension", ""));
            return "";
        }

        public static int GetFieldSeparatorIndex(string source, int startIndex = 0)
        {
            if (String.IsNullOrEmpty(source))
                return -1;
            for (int i = startIndex; i < source.Length; i++)
            {
                char c = source[i];
                if (c < '!' || c > '~')
                    return -1;
                if (c == ':')
                    return (i == startIndex) ? -1 : i;
            }
            return -1;
        }

        public static int GetNextLineStart(string source, int startIndex = 0)
        {
            if (String.IsNullOrEmpty(source))
                throw new ArgumentNullException("source");
            if (String.IsNullOrEmpty(source) || startIndex >= source.Length)
                return -1;
            while (startIndex < source.Length && source[startIndex] != '\r' && source[startIndex] != '\n')
                startIndex++;
            if (startIndex < source.Length)
            {
                if (source[startIndex] == '\n')
                    startIndex++;
                else if (source[startIndex] == '\r')
                {
                    startIndex++;
                    if (startIndex < source.Length)
                    {
                        if (source[startIndex] == '\n')
                            startIndex++;
                    }
                    else
                        return -1;
                }
                if (startIndex < source.Length)
                    return startIndex;
            }

            return -1;
        }

        public static IEnumerable<string> SplitNewLines(this string source)
        {
            if (String.IsNullOrEmpty(source))
            {
                yield return source;
                yield break;
            }

            int startIndex = 0;
            int endIndex;
            
            while (startIndex < source.Length && (endIndex = source.IndexOfAny(_CRLF, startIndex)) >= 0)
            {
                yield return source.Substring(startIndex, endIndex - startIndex);
                startIndex = endIndex + 1;
                if (startIndex < source.Length && source[endIndex] == '\r' && source[startIndex] == '\n')
                    startIndex++;
            }
            if (startIndex == source.Length)
                yield return "";
            else if (startIndex == 0)
                yield return source;
            else
                yield return source.Substring(startIndex);
        }

        public static IEnumerable<string> GetUnfoldedFields(IEnumerable<string> source, out List<KeyValuePair<string, string>> fields)
        {
            List<KeyValuePair<string, List<string>>> d = new List<KeyValuePair<string, List<string>>>();
            List<string> current = null;
            IEnumerable<string> remaining = source.SelectMany(s => s.SplitNewLines()).SkipWhile(s =>
            {
                if (String.IsNullOrEmpty(s))
                    return false;
                if (Char.IsWhiteSpace(s[0]))
                {
                    if (current == null || s.Trim().Length == 0)
                        return false;
                    current.Add(s);
                    return true;
                }

                int index = GetFieldSeparatorIndex(s);
                if (index < 0)
                    return false;
                current = new List<string>();
                d.Add(new KeyValuePair<string, List<string>>(s.Substring(0, index), current));
                index++;
                if (index < s.Length)
                    current.Add(s.Substring(index));
                else
                    current.Add("");
                return true;
            });

            fields = new List<KeyValuePair<string, string>>();
            foreach (KeyValuePair<string, List<string>> item in d)
                fields.Add(new KeyValuePair<string, string>(item.Key, String.Join(" ", item.Value.Select(s => s.Trim()).ToArray())));
            
            return remaining;
        }
    }
}