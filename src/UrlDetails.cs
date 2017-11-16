using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Erwine.Leonard.T.PSWebSrv
{
    public class UrlDetails
    {
        /*
            (
                (?<scheme>[^:\\/@?#]*)
                    :
                    (
                        [\\/]{2}
                        (
                            (
                                (?<username>[^:\\/@?#]*)
                                (:(?<password>[^:\\/@?#]*))?@
                            )?
                            (?<host>[^:\\/?#]*)(:(?<port>\d+)?
                        )?
                    )?
                |
                (?<host>//[^\\/?#])
                |
                (
                    (?<username>[^:\\/@?#]*)
                    (:(?<password>[^:\\/@?#]*))?@
                )?
                (?<host>[^:\\/?#]*)(:(?<port>\d+)?
            )?
            (?<path>[^?#]+)?(?<query>\?[^#]*)?(?<fragment>#.+)?
         */
        public void Parse(string uri)
        {
            int index1 = uri.IndexOfAny(new char[] { ':', '@', '/', '\\' });
            int index2, index3, index4, index5, index6;
            char c = uri[index1];
            if (c == ':')
            {
                index2 = uri.IndexOfAny(new char[] { '\\', '/', '@', ':' }, index1 + 1);
                c = uri[index2];
                if (c == '@')
                {
                    // :@
                    index3 = uri.IndexOfAny(new char[] { '\\', '/', ':' }, index1 + 1);
                    c = uri[index2];
                    if (c == ':')
                    {
                        // :@:
                        // text:text@text:
                        // user:pw@host:port/path
                    }
                    else
                    {
                        // :@/
                        // text:text@text/
                        // user:pw@host/path
                    }
                    return;
                }
                if (c == '\\' || c == '/')
                {
                    // :/
                    index3 = uri.IndexOfAny(new char[] { '\\', '/', ':', '@' }, index2 + 1);
                    c = uri[index3];
                    if (index3 == '\\' || index3 == '/')
                    {
                        // ://
                        index4 = uri.IndexOfAny(new char[] { '\\', '/', ':', '@' }, index3 + 1);
                        c = uri[index4];
                        if (c == ':')
                        {
                            // ://:
                            index5 = uri.IndexOfAny(new char[] { '\\', '/', ':', '@' }, index4 + 1);
                            c = uri[index5];
                            if (c == '/' || c == '\\')
                            {
                                // ://:/
                                // http://host:port/path
                            }
                            if (c == '@')
                            {
                                // ://:@
                                index6 = uri.IndexOfAny(new char[] { '\\', '/', ':', '@' }, index5 + 1);
                                c = uri[index6];
                                if (c == '/' || c == '\\')
                                {
                                    // ://:@/
                                    // http://user:pw@host/path
                                }
                                if (c == ':')
                                {
                                    // ://:@:
                                    // http://user:pw@host:port/path
                                }
                            }
                            
                            return;
                        }
                        if (c == '@')
                        {
                            // ://@
                            // http://user@host:port/path
                            // http://user@host/path
                        }
                        
                        if (c == '\\' || c == '/')
                        {
                            // :///
                            // http://host/path
                            // http:///path
                        }
                        return;
                    }

                    // :/?
                    // (:/?) host:port/path
                }
                // ::
                // urn:my
            }

            if (c == '@')
            {
                index2 = uri.IndexOfAny(new char[] { ':', '/', '\\' }, index1 + 1);
                c = uri[index2];
                if (c == ':')
                {
                    // user@host:port/path
                }
                if (c == '/')
                {
                    // user@host/path
                }

                // user@host
            }

            if ((c == '\\' || c == '/') && index1 == 0 && uri.IndexOfAny(new char[] { '/', '\\' }, 1) == 1)
            {
                // \\host\path
            }
            // path
        }

        public char PathSeparator = '/';
        public char AltPathSeparator = '\\';
        public char SchemeSeparator = ':';
        public char QuerySeparator = '?';
        public char FragmentSeparator = '#';
        private string _scheme;
        private string _userName;
        private string _password;
        private string _hostName;
        private int _port;
        private string[] _path;
        private Tuple<string, string> _query;
        private string _fragment;

        public UrlDetails(string url)
        {
            int index = url.IndexOfAny(new char[] { '#', '?' });
            if (index < 0)
                Initialize3(url, null, null);
            else
            {
                char c = url[index];
                if (c == '#')
                    Initialize3(url.Substring(0, index), null, url.Substring(index + 1));
                else
                {
                    string s = url.Substring(0, index);
                    index++;
                    int index2 = url.IndexOf('#', index);
                    if (index2 < 0)
                        Initialize3(s, url.Substring(index), null);
                    else
                        Initialize3(s, url.Substring(index, index2 - index), url.Substring(index2 + 1));
                }
            }
        }

        private void Initialize3(string schemeHostAndPath, string query, string fragment)
        {
            int index = (String.IsNullOrEmpty(schemeHostAndPath)) ? -1 : schemeHostAndPath.IndexOfAny(new char[] { ':', '\\', '/', '@' });
            if (index < 0)
            {
                Initialize5(null, null, schemeHostAndPath, query, fragment);
                return;
            }
            char c = schemeHostAndPath[index];
            int index2;
            if (c == ':')
            {
                index2 = schemeHostAndPath.IndexOfAny(new char[] { '\\', '/', '@', ':' });
                if (index2 < 0 || (c = schemeHostAndPath[index2]) == ':')
                {
                    // scheme:text*
                    // scheme:text:*
                    Initialize5(schemeHostAndPath.Substring(0, index), null, schemeHostAndPath.Substring(index + 1), query, fragment);
                    return;
                }
                if (c == '@')
                {
                    // user:pw@host:80/path
                    // user:pw@host/path

                    // username = schemeHostAndPath.Substring(0, index)
                    // index++;
                    // password = schemeHostAndPath.Substring(index, index2 - index1)
                    // hostAndPath = schemeHostAndPath.Substring(index2);
                    return;
                }
                
                string scheme = schemeHostAndPath.Substring(0, index);
                int portNumber;
                index++;
                if (index2 > index)
                {
                    // host:port/path
                    string host = schemeHostAndPath.Substring(index, index2 - index);
                    if (Int32.TryParse(host, out portNumber))
                        Initialize6(null, scheme, portNumber, schemeHostAndPath.Substring(index2), query, fragment);
                    else
                        Initialize5(scheme, host, schemeHostAndPath.Substring(index2), query, fragment);
                    return;
                }

                index2++;
                if (index2 < schemeHostAndPath.Length)
                {
                    c = schemeHostAndPath[index2];
                    if (c == '\\' || c == '/')
                        index2++;
                    else
                    {
                        // scheme:/path
                        Initialize5(scheme, null, schemeHostAndPath.Substring(index), query, fragment);
                        return;
                    }
                }
                // scheme://*
                Initialize4(scheme, schemeHostAndPath.Substring(index2), query, fragment);
                return;
            }

            if (c == '@')
            {
                string userName = schemeHostAndPath.Substring(0, index);
                index++;
                index2 = (index < schemeHostAndPath.Length) ? schemeHostAndPath.IndexOfAny(new char[] { ':', '\\', '/' }, index) : -1;
                if (index2 < 0)
                {
                    Initialize7(null, userName, null, schemeHostAndPath.Substring(index), null, query, fragment);
                    return;
                }

                string host = schemeHostAndPath.Substring(index, index2 - index);
                if (schemeHostAndPath[index2] == ':')
                {
                    index2++;
                    index = schemeHostAndPath.IndexOfAny(new char[] { '/', '\\' }, index2);
                    int portNumber;
                    if (index > 0 && Int32.TryParse(schemeHostAndPath.Substring(index2, index - index2), out portNumber))
                    {
                        Initialize8(null, userName, null, host, portNumber, schemeHostAndPath.Substring(index + 1), query, fragment);
                        return;
                    }
                }

                Initialize7(null, userName, null, host, schemeHostAndPath.Substring(index2 + 1), query, fragment);
                return;
            }

            if (index == 0 && schemeHostAndPath.Length > 2)
            {
                c = schemeHostAndPath[1];
                if (c == '\\' || c == '/')
                {
                    index = schemeHostAndPath.IndexOfAny(new char[] { '\\', '/' }, 2);
                    if (index < 0)
                        Initialize8(null, null, null, schemeHostAndPath.Substring(2), -1, null, query, fragment);
                    else
                        Initialize8(null, null, null, schemeHostAndPath.Substring(2, index - 2), -1, schemeHostAndPath.Substring(index), query, fragment);
                    return;
                }
            }
            Initialize8(null, null, null, null, -1, schemeHostAndPath, query, fragment);
        }
        
        private void Initialize4(string scheme, string authHostAndPath, string query, string fragment)
        {
            int index = authHostAndPath.IndexOfAny(new char[] { '@', '\\', '/' });
            if (index < 0)
            {
                Initialize8(scheme, null, null, authHostAndPath, -1, null, query, fragment);
                return;
            }
            int index2;
            if (authHostAndPath[index] == '@')
            {
                index2 = authHostAndPath.IndexOfAny(new char[] { ':', '\\', '/' });
                if (index2 < 0)
                {
                    // user@host
                    Initialize8(scheme, authHostAndPath.Substring(0, index), null, authHostAndPath.Substring(index + 1), -1, null, query, fragment);
                    return;
                }
                if (authHostAndPath[index2] == ':')
                {
                    if (index2 < index)
                    {
                        // user:pw@host:80/path
                        // user:pw@host/path
                    }
                    else
                    {
                        // user@host:80/path
                    }
                    return;
                }

                // user@host/path
                return;
            }

            if (authHostAndPath[index] == ':')
            {
                // host:80/path
                return;
            }

            if (index == 0)
                Initialize8(scheme, null, null, null, -1, authHostAndPath, query, fragment);
            else
                Initialize8(scheme, null, null, authHostAndPath.Substring(0, index), -1, authHostAndPath.Substring(index), query, fragment);
        }
        
        private void Initialize5(string scheme, string authAndHost, string path, string query, string fragment)
        {
            // user:pw@host:80
            // user@host
            // user@host:80
            // host:80
            // user:pw@host
            // host
        }
        
        private void Initialize6(string scheme, string authAndHost, int portNumber, string path, string query, string fragment)
        {
            // user:pw@host
            // user@host
            // host
        }
        
        private void Initialize7(string scheme, string userName, string password, string hostAndPort, string path, string query, string fragment)
        {

        }
        
        private void Initialize8(string scheme, string userName, string password, string host, int portNumber, string path, string query, string fragment)
        {

        }

        public UrlDetails()
        {

        }

        public static bool IsHexChar(char c)
        {
            if (c < '0' || c > 'f')
                return false;
            if (c <= '9')
                return true;
            if (c < 'A')
                return false;
            if (c < 'G')
                return true;
            if (c < 'a')
                return false;
            return c < 'g';
        }
        
        public static string UriDecode(string value)
        {
            if (value == null || value.Length < 3)
                return "";
            
            StringBuilder result = new StringBuilder();
            int startIndex = 0;
            int index;
            int end = value.Length - 3;
            while ((index = value.IndexOf('%', startIndex)) >= 0)
            {
                if (startIndex < index)
                    result.Append(value.Substring(startIndex, index - startIndex));
                if (index > end)
                    break;
                if (IsHexChar(value[index + 1]) && IsHexChar(value[index + 2]))
                {
                    result.Append((char)(Int32.Parse(value.Substring(index + 1, 2), NumberStyles.HexNumber)));
                    startIndex = index + 3;
                }
                else
                {
                    result.Append(value[index]);
                    startIndex = index + 1;
                }
            }
            if (result.Length == 0)
                return value;
            result.Append(value.Substring(startIndex));
            return result.ToString();
        }
    }
}