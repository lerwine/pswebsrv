using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Net;

namespace Erwine.Leonard.T.PSWebSrv
{
	public class PathHttpHandlerInfo : HttpHandlerInfo
	{
        public string PagePath { get; private set; }

        public PathHttpHandlerInfo(string name, string pagePath, string virtualPath, string pattern, string exclude)
            : base(name, virtualPath, pattern, exclude)
        {
            if (pagePath == null)
                throw new ArgumentNullException("pagePath");
            if (pagePath.Trim().Length == 0)
                throw new ArgumentException("Page path cannot be empty", "pagePath");
            PagePath = pagePath;
        }
        
        protected override void SetResponse(HttpRequestInfo request, PSCmdlet hostCmdlet)
        {
            string fullParentPath = String.Join("\\", request.ParentPath.Split('/')
                .Select(n => Uri.UnescapeDataString(n).Replace("\\", "%5C").Replace("/", "%2F")).ToArray());
            if (fullParentPath.StartsWith("\\"))
                fullParentPath = fullParentPath.Substring(1);
            fullParentPath = (fullParentPath.Length == 0) ? PagePath : hostCmdlet.SessionState.Path.Combine(PagePath, fullParentPath);            // request.LeafName + request.LeafExtension
            ItemCmdletProviderIntrinsics item = hostCmdlet.InvokeProvider.Item;
            if (item.Exists(fullParentPath, true, true))
            {
                if (item.IsContainer(fullParentPath))
                {
                    throw new NotImplementedException();
                }
                else
                {
                    ContentCmdletProviderIntrinsics content = hostCmdlet.InvokeProvider.Content;
                    Collection<IContentReader> collection = content.GetReader(new string[] { fullParentPath }, true, true);
                    foreach (IContentReader reader in collection)
                    {
                        IList list = reader.Read(0);
                        throw new NotImplementedException();
                    }
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override bool Equals(HttpHandlerInfo other)
        {
            if (other != null && other is PathHttpHandlerInfo)
            {
                if (ReferenceEquals(this, other) || (NameComparer.Equals(Name, other.Name) &&
                        NameComparer.Equals(VirtualPath, other.VirtualPath) &&
                        ((Pattern == null) ? other.Pattern == null : other.Pattern != null && Pattern.ToString() == other.Pattern.ToString()) &&
                        ((Exclude == null) ? other.Exclude == null : other.Exclude != null && Exclude.ToString() == other.Exclude.ToString()) &&
                        NameComparer.Equals(PagePath, (other as PathHttpHandlerInfo).PagePath)))
                    return true;
            }
            return false;
        }
    }
}