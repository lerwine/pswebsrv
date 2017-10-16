using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Net;

namespace Erwine.Leonard.T.PSWebSrv
{
    public class HttpHostBinding : IEquatable<HttpHostBinding>
    {
        public string Scheme { get; private set; }

        public string Host { get; private set; }

        public int Port { get; private set; }

        public UriHostNameType HostNameType { get; private set; }

        public HttpHostBinding(Uri uri, UriHostNameType hostNameType)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            if (!uri.IsAbsoluteUri)
                throw new ArgumentException("Uri must be absolute", "uri");
            
            Scheme = uri.Scheme;
            Host = uri.Host;
            Port = uri.Port;
            HostNameType = hostNameType;
        }
        
        public HttpHostBinding(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            if (!uri.IsAbsoluteUri)
                throw new ArgumentException("Uri must be absolute", "uri");
            
            Scheme = uri.Scheme;
            Host = uri.Host;
            Port = uri.Port;
            HostNameType = uri.HostNameType;
        }
        
        public Uri ToUri(string pathQueryAndFragment)
        {
            UriBuilder result = ToUriBuilder();
            if (String.IsNullOrEmpty(pathQueryAndFragment) || (pathQueryAndFragment.Length == 1 && (pathQueryAndFragment[0] == '/' || pathQueryAndFragment[0] == '\\')))
                return result.Uri;
            string uriStr = result.Uri.ToString();
            char c = uriStr[uriStr.Length - 1];
            Uri uri;
            if (c == '/' || c == '\\')
            {
                c = pathQueryAndFragment[0];
                if (c == '/' || c == '\\')
                {
                    if (Uri.TryCreate(uriStr.Substring(0, uriStr.Length - 1) + pathQueryAndFragment, UriKind.Absolute, out uri))
                        return uri;
                    pathQueryAndFragment = pathQueryAndFragment.Substring(1);
                }
                else if (Uri.TryCreate(uriStr + pathQueryAndFragment, UriKind.Absolute, out uri))
                    return uri;
            }
            else
            {
                c = pathQueryAndFragment[0];
                if (c == '/' || c == '\\')
                {
                    if (Uri.TryCreate(uriStr + pathQueryAndFragment, UriKind.Absolute, out uri))
                        return uri;
                    pathQueryAndFragment = pathQueryAndFragment.Substring(1);
                }
                else
                {
                    if (Uri.TryCreate(uriStr + "/" + pathQueryAndFragment, UriKind.Absolute, out uri))
                        return uri;
                }
                uriStr += "/";
            }

            int index = pathQueryAndFragment.IndexOf('?');
            string query = null;
            string fragment = null;
            if (index > -1)
            {
                query = pathQueryAndFragment.Substring(index + 1);
                pathQueryAndFragment = pathQueryAndFragment.Substring(0, index);
                if ((index = query.IndexOf('#')) > -1)
                {
                    fragment = query.Substring(index + 1);
                    query = query.Substring(0, index);
                }
            }
            else if ((index = pathQueryAndFragment.IndexOf('#')) > -1)
            {
                pathQueryAndFragment = pathQueryAndFragment.Substring(0, index);
                fragment = pathQueryAndFragment.Substring(index + 1);
            }
            
            if (!Uri.TryCreate(uriStr + Uri.EscapeUriString(pathQueryAndFragment), UriKind.Absolute, out uri))
                uri = new Uri(uriStr + Uri.EscapeDataString(pathQueryAndFragment), UriKind.Absolute);
            result = new UriBuilder(uri);
            if (!String.IsNullOrEmpty(query))
                result.Query = query;
            if (!String.IsNullOrEmpty(fragment))
                result.Fragment = fragment;
            return result.Uri;
        }

        public Uri ToUri() { return ToUriBuilder().Uri; }
        
        public UriBuilder ToUriBuilder(string pathQueryAndFragment)
        {
            if (String.IsNullOrEmpty(pathQueryAndFragment) || (pathQueryAndFragment.Length == 1 && (pathQueryAndFragment[0] == '/' || pathQueryAndFragment[0] == '\\')))
                return ToUriBuilder();
            
            return new UriBuilder(ToUri(pathQueryAndFragment));
        }

        public UriBuilder ToUriBuilder()
        {
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme = Scheme;
            uriBuilder.Host = Host;
            if (Port >= 0)
                uriBuilder.Port = Port;
            return uriBuilder;
        }

        public bool Equals(HttpHostBinding other)
        {
            if (other == null)
                return false;
            
            if (ReferenceEquals(this, other))
                return true;

            if (Port != other.Port)
                return false;
            StringComparer comparer = StringComparer.CurrentCultureIgnoreCase;
            return comparer.Equals(Scheme, other.Scheme) && comparer.Equals(Host, other.Host);
        }

        public override bool Equals(object obj) { return Equals(obj as HttpHostBinding); }

        public override int GetHashCode() { return ToString().ToLower().GetHashCode(); }

        public override string ToString() { return ToUriBuilder().ToString(); }
    }
}