using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpInterfaceToProtoFile
{
    public static class CsharpToProtoStringExtension
    {
        public static string FirstLetterToUpper(this string str)
        {
            if (str == null)
                return null;
            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);
            return str.ToUpper();
        }

        public static string FirstLetterToLower(this string str)
        {
            if (str == null)
                return null;
            if (str.Length > 1)
                return char.ToLower(str[0]) + str.Substring(1);
            return str.ToLower();
        }

        public static string LetterToLower(this string str)
        {
            if (str == null)
                return null;
            if (str.Length > 3)
                return char.ToLower(str[0]) + str.Substring(1);

            return str.ToLower();
        }

    }
}
