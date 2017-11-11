using System;
using System.IO;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace Erwine.Leonard.T.PSWebSrv
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ValidateLocalPathAttribute : ValidateEnumeratedArgumentsAttribute
    {
        public bool AllowNull { get; set; }

        public bool AllowEmptyString { get; set; }

        public ValidateLocalPathAttribute() { }

        protected override void ValidateElement(object element)
        {
            string resolvedPath;
            try { AssertPath(element, AllowNull, AllowEmptyString, out resolvedPath); }
            catch (Exception e) { throw new ValidationMetadataException(e.Message, e); }
        }

        public const string ErrorMessage_NullPath = "Null path not allowed";
        public const string ErrorMessage_EmptyPath = "Empty path not allowed";
        public const string ErrorMessage_InvalidPath = "Invalid path";
        public const string ErrorMessage_FilePath = "Path must be for a subdirectory, not a file.";
        public const string ErrorMessage_NotFound = "Path was not found";
        public const string ErrorMessage_IOError = "Error accessing path";
        public const string ParameterName_value = "value";

        public static void AssertPath(object value, bool allowNull, bool allowEmptyString, out string resolvedPath)
        {
            if (value == null)
            {
                if (!allowNull)
                    throw new ArgumentNullException(ParameterName_value, ErrorMessage_NullPath);
                resolvedPath = null;
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
                    resolvedPath = null;
                    return;
                }
                if (path.Length == 0)
                {
                    if (!allowEmptyString)
                        throw new ArgumentException(ErrorMessage_EmptyPath, ParameterName_value);
                    resolvedPath = null;
                    return;
                }
                bool dirExists, fileExists;
                try
                {
                    resolvedPath = Path.GetFullPath(path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
                    dirExists = Directory.Exists(resolvedPath);
                    fileExists = !dirExists && File.Exists(resolvedPath);
                    if (resolvedPath[resolvedPath.Length - 1] != Path.DirectorySeparatorChar)
                        resolvedPath += Path.DirectorySeparatorChar;
                }
                catch (Exception exc) { throw new ArgumentException(ErrorMessage_InvalidPath, ParameterName_value, exc); }
                if (dirExists)
                    return;
                if (fileExists)
                    throw new ArgumentException(ErrorMessage_FilePath, ParameterName_value);
                throw new ArgumentException(ErrorMessage_NotFound, ParameterName_value);
            }
            catch (ArgumentException e)
            {
                if (e.ParamName != null && e.ParamName == ParameterName_value)
                    throw;
                innerException = e;
            }
            catch (Exception e) { innerException = e; }

            if (innerException == null)
                throw new ArgumentException(ErrorMessage_IOError, ParameterName_value);
            throw new ArgumentException(ErrorMessage_IOError, ParameterName_value, innerException);
        }

        public static void AssertPath(object element, bool allowNull, out string resolvedPath) { AssertPath(element, allowNull, false, out resolvedPath); }

        public static void AssertPath(object element, out string resolvedPath) { AssertPath(element, false, false, out resolvedPath); }
    }
}