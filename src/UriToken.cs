using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace Erwine.Leonard.T.PSWebSrv
{
    public class UriToken
    {
        public UriToken(UriTokenType type, string content, int trailingTextLength)
        {
        }

        public UriTokenType Type { get; private set; }



        public static LinkedList<UriToken> Parse(string uri)
        {
            LinkedList<UriToken> result = new LinkedList<UriToken>();
            if (String.IsNullOrEmpty(uri))
                return result;
            LinkedList<Tuple<char, int>> tokenIndexes = new LinkedList<Tuple<char, int>>();

            int lastIndex = uri.IndexOfAny(new char[] { '\\', '/', ':', '@', '?', '#'});
            if (lastIndex < 0)
            {
                result.AddLast(new UriToken(UriTokenType.PathName, Uri.UnescapeDataString(uri), 0));
                return result;
            }
            int startIndex = lastIndex + 1;
            tokenIndexes.AddLast(new Tuple<char, int>(uri[lastIndex], lastIndex));
            if (uri[lastIndex] == ':')
            {
                lastIndex = uri.IndexOfAny(new char[] { '\\', '/', ':', '@', '?', '#' }, startIndex);
                if (lastIndex > -1)
                {
                    startIndex = lastIndex + 1;
                    tokenIndexes.AddLast(new Tuple<char, int>(uri[lastIndex], lastIndex));
                    if (lastIndex == startIndex && (uri[lastIndex] == '/' || uri[lastIndex] == '\\'))
                    {
                        lastIndex = uri.IndexOfAny(new char[] { '\\', '/', ':', '@', '?', '#' }, startIndex);
                        if (lastIndex > -1)
                        {
                            startIndex = lastIndex + 1;
                            tokenIndexes.AddLast(new Tuple<char, int>(uri[lastIndex], lastIndex));
                            if (lastIndex == startIndex && (uri[lastIndex] == '/' || uri[lastIndex] == '\\'))
                            {
                                lastIndex = uri.IndexOfAny(new char[] { '\\', '/', ':', '@', '?', '#' }, startIndex);
                                if (lastIndex > -1)
                                {
                                    startIndex = lastIndex + 1;
                                    if (uri[lastIndex] == ':')
                                    {
                                        int nextIndex = uri.IndexOfAny(new char[] { '\\', '/', ':', '@', '?', '#' }, startIndex);
                                        if (nextIndex < 0)
                                        {
                                            startIndex = uri.Length;
                                            tokenIndexes.AddLast(new Tuple<char, int>(uri[lastIndex], lastIndex));
                                        }
                                        else
                                        {
                                            if (uri[nextIndex] == '@')
                                                tokenIndexes.AddLast(new Tuple<char, int>(uri[lastIndex], lastIndex));
                                            tokenIndexes.AddLast(new Tuple<char, int>(uri[nextIndex], lastIndex));
                                            startIndex = nextIndex + 1;
                                        }
                                        lastIndex = nextIndex;
                                    }
                                    else
                                        tokenIndexes.AddLast(new Tuple<char, int>(uri[lastIndex], lastIndex));
                                }
                                else
                                    startIndex = uri.Length;
                            }
                        }
                        else
                            startIndex = uri.Length;
                    }
                }
                else
                    startIndex = uri.Length;
            }

            if (lastIndex > -1)
            {
                while (tokenIndexes.Last.Value.Item1 != '#' && tokenIndexes.Last.Value.Item1 != '?')
                {
                    lastIndex = uri.IndexOfAny(new char[] { '\\', '/', ':', '?', '#' }, startIndex);
                    if (lastIndex < 0)
                        break;
                    tokenIndexes.AddLast(new Tuple<char, int>(uri[lastIndex], lastIndex));
                    startIndex = lastIndex + 1;
                }
            }

            if (lastIndex > -1 && tokenIndexes.Last.Value.Item1 == '?')
            {
                while (tokenIndexes.Last.Value.Item1 != '#')
                {
                    lastIndex = uri.IndexOfAny(new char[] { '&', '=', '#' }, startIndex);
                    if (lastIndex < 0)
                        break;
                    tokenIndexes.AddLast(new Tuple<char, int>(uri[lastIndex], lastIndex));
                    startIndex = lastIndex + 1;
                }
            }

            LinkedListNode<Tuple<char, int>> node = tokenIndexes.First;
            if (node.Value.Item1 == ':')
            {
                LinkedListNode<Tuple<char, int>> n = node.Next;
                int i;
                if (n != null && (i = n.Value.Item2) == node.Value.Item2 + 1 && (n.Value.Item1 == '/' || n.Value.Item1 == '\\') &&
                    (n = n.Next) != null && n.Value.Item2 == i + 1 && (n.Value.Item1 == '/' || n.Value.Item1 == '\\'))
                {
                    result.AddLast(new UriToken(UriTokenType.UrlScheme, uri.Substring(0, node.Value.Item2), 0));
                    if ((node = n.Next) != null)
                    {
                        if (node.Value.Item1 == '@')
                        {
                            i = node.Previous.Value.Item2 + 1;
                            result.AddLast(new UriToken(UriTokenType.UserName, uri.Substring(i, node.Value.Item2 - i), 0));
                            if ((node = node.Next) == null)
                                return result;
                        }
                        else if (node.Value.Item1 == ':')
                        {
                            if ((n = node.Next) == null)
                            {
                                i = node.Previous.Value.Item2 + 1;
                                result.AddLast(new UriToken(UriTokenType.UserName, uri.Substring(i, node.Value.Item2 - i), 0));
                                i = node.Value.Item2 + 1;
                                result.AddLast(new UriToken(UriTokenType.Password, uri.Substring(i, n.Value.Item2 - i), 0));
                                if ((node = n.Next) == null)
                                    return result;
                            }

                            if ((n = node.Next) != null && n.Value.Item1 == '@')
                            {
                                i = node.Previous.Value.Item2 + 1;
                                result.AddLast(new UriToken(UriTokenType.UserName, uri.Substring(i, node.Value.Item2 - i), 0));
                                i = node.Value.Item2 + 1;
                                result.AddLast(new UriToken(UriTokenType.Password, uri.Substring(i, n.Value.Item2 - i), 0));
                                if ((node = n.Next) == null)
                                    return result;
                            }
                        }
                    }
                }
                else
                {
                    result.AddLast(new UriToken(UriTokenType.Scheme, uri.Substring(0, node.Value.Item2), 0));
                    node = node.Next;
                }
                if (node == null)
                    return result;
            }
        }

        private static void ParseHostName(string v, LinkedList<UriToken> result)
        {
            throw new NotImplementedException();
        }

        private static void ParsePath(string v, LinkedList<UriToken> result)
        {
            throw new NotImplementedException();
        }

        private static void ParseQuery(string v, LinkedList<UriToken> result)
        {
            throw new NotImplementedException();
        }
    }
}