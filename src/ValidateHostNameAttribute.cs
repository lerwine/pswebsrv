
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Sockets;

namespace Erwine.Leonard.T.PSWebSrv
{
    [System.AttributeUsage(System.AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ValidateHostNameAttribute : ValidateEnumeratedArgumentsAttribute
    {
        public bool AllowNull { get; set; }

        public bool AllowEmptyString { get; set; }

        public UriHostNameType[] AllowedTypes { get; set; }

        public const string ErrorMessage_NullHost = "Null host name not allowed";
        public const string ErrorMessage_EmptyHost = "Empty host name not allowed";
        public const string ErrorMessage_InvalidHostName = "Invalid host name";
        public const string ErrorMessage_UnsupportedHostType = "Unsupported host type";
        public const string ErrorMessage_UnsupportedHostNameType = "Unsupported host name type";
        public const string ParameterName_value = "value";
        
        public static string AssertHostName(object value, bool allowNull, bool allowEmptyString, UriHostNameType[] allowedTypes,
            out UriHostNameType actualType)
        {
            if (value == null)
            {
                if (!allowNull)
                    throw new ArgumentNullException(ParameterName_value, ErrorMessage_NullHost);
                actualType = UriHostNameType.Unknown;
                return null;
            }

            object baseObject = value;
            Exception innerException;
            try
            {
                if (baseObject is PSObject)
                    baseObject = (baseObject as PSObject).BaseObject;

                if (baseObject is long)
                    baseObject = new IPAddress((long)baseObject);
                else if (baseObject is byte[])
                    baseObject = new IPAddress((byte[])baseObject);
                else if (baseObject is IEnumerable<byte>)
                    baseObject = new IPAddress(((IEnumerable<byte>)baseObject).ToArray());
                else if (baseObject is IConvertible)
                {
                    try { baseObject = (baseObject as IConvertible).ToInt64(CultureInfo.CurrentCulture); } catch { }
                    if (baseObject is long)
                        baseObject = new IPAddress((long)baseObject);
                }
                if (baseObject is IPAddress)
                {
                    IPAddress ipAddress = baseObject as IPAddress;
                    if (allowedTypes == null || allowedTypes.Length == 0)
                    {
                        if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                            actualType = UriHostNameType.IPv6;
                        else if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                            actualType = UriHostNameType.IPv4;
                        else
                            actualType = UriHostNameType.Unknown;
                        return ipAddress.ToString();
                    }
                    if (ipAddress.AddressFamily == AddressFamily.Unspecified || ipAddress.AddressFamily == AddressFamily.Unknown)
                    {
                        if (!allowedTypes.Any(a => a == UriHostNameType.Unknown))
                            throw new ArgumentException(ErrorMessage_UnsupportedHostType, ParameterName_value);
                        actualType = UriHostNameType.Unknown;
                    }
                    if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        if (!allowedTypes.Any(a => a == UriHostNameType.IPv6))
                            throw new ArgumentException(ErrorMessage_UnsupportedHostType, ParameterName_value);
                        actualType = UriHostNameType.IPv6;
                    }
                    else if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (!allowedTypes.Any(a => a == UriHostNameType.IPv4))
                            throw new ArgumentException(ErrorMessage_UnsupportedHostType, ParameterName_value);
                        actualType = UriHostNameType.IPv4;
                    }
                    else if (!allowedTypes.Any(a => a == UriHostNameType.Basic))
                        throw new ArgumentException(ErrorMessage_UnsupportedHostType, ParameterName_value);
                    else
                        actualType = UriHostNameType.Basic;
                    return ipAddress.ToString();
                }
                
                string hostName = (baseObject is IPHostEntry) ? (baseObject as IPHostEntry).HostName :
                    ((baseObject is string) ? (baseObject as string) : value.ToString());
                UriHostNameType uriHostNameType;
                if (hostName == null)
                {
                    if (!allowNull)
                        throw new ArgumentException(ErrorMessage_NullHost, ParameterName_value);
                    uriHostNameType = UriHostNameType.Unknown;
                }
                else if (hostName.Length == 0)
                {
                    if (!allowEmptyString)
                        throw new ArgumentException(ErrorMessage_EmptyHost, ParameterName_value);
                    uriHostNameType = UriHostNameType.Unknown;
                }
                else
                {
                    uriHostNameType = Uri.CheckHostName(hostName);
                    if (uriHostNameType == UriHostNameType.Unknown && !(baseObject is IPHostEntry))
                        throw new ArgumentException(ErrorMessage_InvalidHostName, ParameterName_value);
                }

                if (allowedTypes == null || allowedTypes.Length == 0 || allowedTypes.Any(a => a == uriHostNameType))
                {
                    actualType = uriHostNameType;
                    return hostName;
                }

                throw new ArgumentException((baseObject is string) ? ErrorMessage_UnsupportedHostNameType : ErrorMessage_UnsupportedHostType, ParameterName_value);
            }
            catch (ArgumentException e)
            {
                if (e.ParamName != null && e.ParamName == ParameterName_value)
                    throw;
                innerException = e;
            }
            catch (Exception e) { innerException = e; }

            string message = (baseObject is string) ? ErrorMessage_InvalidHostName : ("Invalid " + baseObject.GetType().Name);
            if (innerException == null)
                throw new ArgumentException(message, ParameterName_value);
            throw new ArgumentException(message, ParameterName_value, innerException);
        }
        
        public static string AssertHostName(object value, bool allowNull, bool allowEmptyString, out UriHostNameType actualType)
        {
            return AssertHostName(value, allowNull, allowEmptyString, new UriHostNameType[0], out actualType);
        }

        public static string AssertHostName(object value, bool allowNull, UriHostNameType[] allowedTypes, out UriHostNameType actualType)
        {
            return AssertHostName(value, allowNull, false, allowedTypes, out actualType);
        }

        public static string AssertHostName(object value, bool allowNull, out UriHostNameType actualType)
        {
            return AssertHostName(value, allowNull, false, out actualType);
        }

        public static string AssertHostName(object value, UriHostNameType[] allowedTypes, out UriHostNameType actualType)
        {
            return AssertHostName(value, false, allowedTypes, out actualType);
        }

        public static string AssertHostName(object value, out UriHostNameType actualType)
        {
            return AssertHostName(value, new UriHostNameType[0], out actualType);
        }

        protected override void ValidateElement(object element)
        {
            UriHostNameType actualType;
            try { AssertHostName(element, AllowNull, AllowEmptyString, AllowedTypes, out actualType); }
            catch (Exception e) { throw new ValidationMetadataException(e.Message, e); }
        }
    }
}