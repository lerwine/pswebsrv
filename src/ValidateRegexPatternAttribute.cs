using System;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace Erwine.Leonard.T.PSWebSrv
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ValidateRegexPatternAttribute : ValidateEnumeratedArgumentsAttribute
    {
        public bool AllowNull { get; set; }

        public bool AllowEmptyString { get; set; }

        public ValidateRegexPatternAttribute() { }

        protected override void ValidateElement(object element)
        {
            Regex regex;
            try { AssertRegexPattern(element, AllowNull, AllowEmptyString, out regex); }
            catch (Exception e) { throw new ValidationMetadataException(e.Message, e); }
        }

        public const string ErrorMessage_NullPattern = "Null pattern not allowed";
        public const string ErrorMessage_EmptyPattern = "Empty pattern not allowed";
        public const string ErrorMessage_InvalidPattern = "Invalid regular expression pattern";
        public const string ParameterName_value = "value";

        public static void AssertRegexPattern(object value, bool allowNull, bool allowEmptyString, out Regex regex)
        {
            if (value == null)
            {
                if (!allowNull)
                    throw new ArgumentNullException(ParameterName_value, ErrorMessage_NullPattern);
                regex = null;
                return;
            }

            object baseObject = value;
            Exception innerException;
            try
            {
                if (baseObject is PSObject)
                    baseObject = (baseObject as PSObject).BaseObject;

                string pattern = (baseObject is string) ? baseObject as string : value.ToString();
                if (pattern == null)
                {
                    if (!allowNull)
                        throw new ArgumentException(ErrorMessage_NullPattern, ParameterName_value);
                    regex = null;
                    return;
                }
                
                if (pattern.Length == 0)
                {
                    if (!allowEmptyString)
                        throw new ArgumentException(ErrorMessage_EmptyPattern, ParameterName_value);
                    regex = null;
                    return;
                }

                try
                {
                    regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    return;
                }
                catch (Exception exc) { throw new ArgumentException(ErrorMessage_InvalidPattern, ParameterName_value, exc); }
            }
            catch (ArgumentException e)
            {
                if (e.ParamName != null && e.ParamName == ParameterName_value)
                    throw;
                innerException = e;
            }
            catch (Exception e) { innerException = e; }

            string message = (baseObject is string) ? ErrorMessage_InvalidPattern : ("Invalid " + baseObject.GetType().Name);
            if (innerException == null)
                throw new ArgumentException(message, ParameterName_value);
            throw new ArgumentException(message, ParameterName_value, innerException);
        }

        public static void AssertRegexPattern(object element, bool allowNull, out Regex regex) { AssertRegexPattern(element, allowNull, false, out regex); }

        public static void AssertRegexPattern(object element, out Regex regex) { AssertRegexPattern(element, false, false, out regex); }
    }
}