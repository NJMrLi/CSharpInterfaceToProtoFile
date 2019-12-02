using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CSharpInterfaceToProtoFile
{
    public static class ProtoTypeHelper
    {

        public static string GetProtoTypeFromCsharpType(Type paramType)
        {
            var typeName = paramType.FullName;

            switch (typeName)
            {
                case "System.Void":
                    return "";
                //普通类型
                case "System.String":
                    return "string";
                case "System.Int16":
                case "System.Int32":
                    return "int32";
                case "System.Int64":
                    return "int64";
                case "System.Boolean":
                    return "bool";
                case "System.Double":
                    return "double";
                case "System.Single":
                    return "float";
                case "System.Byte[]":
                    return "bytes";

                //Array类型
                case "System.String[]":
                    return "repeated string";
                case "System.Int16[]":
                case "System.Int32[]":
                    return "repeated int32";
                case "System.Int64[]":
                    return "repeated int64";
                case "System.SByte[]":
                    return "repeated bytes";
                case "System.Boolean[]":
                    return "repeated bool";
                case "System.Double[]":
                    return "repeated double";
                case "System.Single[]":
                    return "repeated float";

                //时间
                case "System.Date":
                case "System.DateTime":
                    return "int64";

            }

            //如果是引用类型
            if (paramType.IsByRef)
            {

                var withOutRefType = GetRealTypeFromRefOrOutType(paramType);

                if (withOutRefType != null)
                {
                    return GetProtoTypeFromCsharpType(withOutRefType);
                }

            }


            //枚举
            if (paramType.BaseType == typeof(Enum))
            {
                return "int32";
            }

            //一般数组
            if (paramType.BaseType == typeof(Array))
            {
                return $"repeated {paramType.Name.Replace("[]", "")}";
            }

            if (typeName.IndexOf("System.Nullable`1") == 0)
            {
                return GetProtoTypeFromCsharpType(paramType.GenericTypeArguments[0]);
            }

            //列表
            if (typeName.IndexOf("System.Collections.Generic.List`1[") == 0 || typeName.IndexOf("System.Collections.Generic.IList`1[") == 0 ||
                typeName.IndexOf("System.Collections.Generic.IEnumerable`1[") == 0 ||
                typeName.IndexOf("System.Collections.Generic.ICollection`1[") == 0)
            {

                return $"repeated {GetProtoTypeFromCsharpType(paramType.GenericTypeArguments[0])}";
            }

            //字典 map<string, Project> projects = 3;
            if (typeName.IndexOf("System.Collections.Generic.Dictionary`2") == 0)
            {
                var keyType = GetProtoTypeFromCsharpType(paramType.GenericTypeArguments[0]);
                var valueType = GetProtoTypeFromCsharpType(paramType.GenericTypeArguments[1]);
                return $"map<{keyType}, {valueType}>";
            }

            //object类型
            return paramType.Name;
        }

        public static bool IsSimpleCsharpType(Type type)
        {
            var typeName = type.FullName;

            switch (typeName)
            {
                //普通类型
                case "System.String":
                case "System.Int16":
                case "System.Int32":
                case "System.Int64":
                case "System.Boolean":
                case "System.Double":
                case "System.Single":
                case "System.Byte[]":
                case "System.String[]":
                case "System.Int16[]":
                case "System.Int32[]":
                case "System.Int64[]":
                case "System.SByte[]":
                case "System.Boolean[]":
                case "System.Double[]":
                case "System.Single[]":
                case "System.DateTime":
                case "System.Date":
                    return true;
            }


            //如果是引用类型
            if (type.IsByRef)
            {

                var withOutRefType = GetRealTypeFromRefOrOutType(type);

                if (withOutRefType != null)
                {
                    return IsSimpleCsharpType(withOutRefType);
                }

            }

            //枚举
            if (type.BaseType == typeof(Enum))
            {
                return true;
            }

            //一般数组
            if (type.BaseType == typeof(Array))
            {
                return false;
            }


            //列表
            if (typeName.IndexOf("System.Collections.Generic.List`1[") == 0 || typeName.IndexOf("System.Collections.Generic.IList`1[") == 0 ||
                typeName.IndexOf("System.Collections.Generic.IEnumerable`1[") == 0 ||
                typeName.IndexOf("System.Collections.Generic.ICollection`1[") == 0)
            {

                if (IsSimpleCsharpType(type.GenericTypeArguments[0]))
                {
                    return true;
                }
            }

            //字典 map<string, Project> projects = 3;
            if (typeName.IndexOf("System.Collections.Generic.Dictionary`2") == 0)
            {
                var keyType = IsSimpleCsharpType(type.GenericTypeArguments[0]);
                var valueType = IsSimpleCsharpType(type.GenericTypeArguments[1]);
                if (keyType && valueType)
                {
                    return true;
                }
            }

            return false;
        }

        public static TypeInfo GetRealTypeFromRefOrOutType(Type refType)
        {
            var withOutRefTypeName = refType.Name.TrimEnd('&');

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var withOutRefType = assembly.DefinedTypes.FirstOrDefault(x => x.Name == withOutRefTypeName);

                if (withOutRefType != null)
                {
                    return withOutRefType;
                }
            }

            return null;

        }
    }
}
