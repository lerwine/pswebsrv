using System;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace Erwine.Leonard.T.PSWebSrv
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ValidateVirtualPathAttribute : ValidateEnumeratedArgumentsAttribute
    {
        public bool AllowNull { get; set; }

        public bool AllowEmptyString { get; set; }

        public ValidateVirtualPathAttribute() { }

        protected override void ValidateElement(object element)
        {
            Uri virtualPath;
            try { AssertPath(element, AllowNull, AllowEmptyString, out virtualPath); }
            catch (Exception e) { throw new ValidationMetadataException(e.Message, e); }
        }

        public const string ErrorMessage_NullPath = "Null path not allowed";
        public const string ErrorMessage_EmptyPath = "Empty path not allowed";
        public const string ErrorMessage_InvalidPath = "Invalid path";
        public const string ErrorMessage_ContainsFragment = "Path cannot contain a URI fragment";
        public const string ErrorMessage_ContainsQuery = "Path cannot contain a query";
        public const string ParameterName_value = "value";

        public static void AssertPath(object value, bool allowNull, bool allowEmptyString, out Uri virtualPath)
        {
            if (value == null)
            {
                if (!allowNull)
                    throw new ArgumentNullException(ParameterName_value, ErrorMessage_NullPath);
                virtualPath = null;
                return;
            }

            object baseObject = value;
            Exception innerException;
            try
            {
                if (baseObject is PSObject)
                    baseObject = (baseObject as PSObject).BaseObject;

                string path = (baseObject is string) ? baseObject as string : value.ToString();
                if (path == null)
                {
                    if (!allowNull)
                        throw new ArgumentException(ErrorMessage_NullPath, ParameterName_value);
                    virtualPath = null;
                    return;
                }
                if (path.Length == 0)
                {
                    if (!allowEmptyString)
                        throw new ArgumentException(ErrorMessage_EmptyPath, ParameterName_value);
                    virtualPath = null;
                    return;
                }
                path = path.Replace('\\', '/');
                virtualPath = new Uri(path.Replace('\\', '/'));
                if (!virtualPath.IsAbsoluteUri)
                    virtualPath = new Uri(new Uri("http://localhost"), virtualPath.ToString());
                UriBuilder uriBuilder = new UriBuilder(virtualPath);
                if (uriBuilder.Path.Length == 0)
                    uriBuilder.Path = "/";
                if (uriBuilder.Path[0] != '/')
                    uriBuilder.Path = "/" + uriBuilder.Path;
                if (uriBuilder.Path.Length > 1 && uriBuilder.Path[uriBuilder.Path.Length - 1] != '/')
                    uriBuilder.Path += "/";
                virtualPath = uriBuilder.Uri;
                if (String.IsNullOrEmpty(uriBuilder.Fragment) || uriBuilder.Fragment == "#")
                {
                    if (String.IsNullOrEmpty(uriBuilder.Query) || uriBuilder.Query == "?")
                        return;
                    throw new ArgumentException(ErrorMessage_ContainsFragment, ParameterName_value);
                }
                throw new ArgumentException(ErrorMessage_ContainsQuery, ParameterName_value);
            }
            catch (ArgumentException e)
            {
                if (e.ParamName != null && e.ParamName == ParameterName_value)
                    throw;
                innerException = e;
            }
            catch (Exception e) { innerException = e; }

            if (innerException == null)
                throw new ArgumentException(ErrorMessage_InvalidPath, ParameterName_value);
            throw new ArgumentException(ErrorMessage_InvalidPath, ParameterName_value, innerException);
        }

        public static void AssertPath(object element, bool allowNull, out Uri virtualPath) { AssertPath(element, allowNull, false, out virtualPath); }

        public static void AssertPath(object element, out Uri virtualPath) { AssertPath(element, false, false, out virtualPath); }
    }
}