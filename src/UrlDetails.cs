using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Erwine.Leonard.T.PSWebSrv
{
    public class UrlDetails
    {
        public char PathSeparator = '/';
        public char AltPathSeparator = '\\';
        public char SchemeSeparator = ':';
        public char QuerySeparator = '?';
        public char FragmentSeparator = '#';
        private string _scheme;
        private string _host;
        private string _fragment;

        public UrlDetails(string url)
        {
            Func<string, int, int> getPathIndex = (str, startIndex) => 
            {
                int x = url.IndexOf(PathSeparator, startIndex);
                int y = url.IndexOf(AltPathSeparator, startIndex);
                return (x < 0) ? y : ((y < 0 || x < y) ? x : y);
            };
            int pathIndex = getPathIndex(url, 0);
            int schemeIndex = url.IndexOf(SchemeSeparator);
            int queryIndex = url.IndexOf(QuerySeparator);
            int fragmentIndex = url.IndexOf(FragmentSeparator);
            if (fragmentIndex >= 0)
            {
                if (pathIndex > fragmentIndex)
                    pathIndex = -1;
                if (schemeIndex > fragmentIndex)
                    schemeIndex = -1;
                if (queryIndex > fragmentIndex)
                    queryIndex = -1;
            }
            if (queryIndex >= 0)
            {
                if (pathIndex > queryIndex)
                    pathIndex = -1;
                if (schemeIndex > queryIndex)
                    schemeIndex = -1;
            }
            if (pathIndex >= 0 && schemeIndex > pathIndex)
                schemeIndex = -1;
            if (schemeIndex >= 0)
            {
                _scheme = UriDecode(url.Substring(0, schemeIndex));
                schemeIndex++;
            }
            else
            {
                schemeIndex = 0;
                _scheme = null;
            }
            string path, query;
            if (queryIndex < 0)
            {
                query = null;
                if (fragmentIndex < 0)
                {
                    _fragment = null;
                    path = url.Substring(schemeIndex);
                }
                else
                {
                    _fragment = url.Substring(fragmentIndex + 1);
                    path = url.Substring(schemeIndex, fragmentIndex - schemeIndex);
                }
            }
            else
            {
                path = url.Substring(schemeIndex, queryIndex - schemeIndex);
                queryIndex++;
                if (fragmentIndex < 0)
                {
                    _fragment = null;
                    query = url.Substring(queryIndex);
                }
                else
                {
                    _fragment = url.Substring(fragmentIndex + 1);
                    query = url.Substring(queryIndex, fragmentIndex - queryIndex);
                }
            }

            string host;
            if (_scheme != null && path.Length > 1 && (path[0] == PathSeparator || path[0] == AltPathSeparator) && (path[1] == PathSeparator || path[1] == AltPathSeparator))
            {
                pathIndex = getPathIndex(path, 2);
                if (pathIndex < 0)
                    host = path.Substring(2);
                else
                {   
                    host = path.Substring(2, pathIndex - schemeIndex);
                    pathIndex = getPathIndex(path, pathIndex + 1);
                }
            }
            else
                host = null;

            List<string> pathNodes = new List<string>();
            while (pathIndex > -1)
            {

            }
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