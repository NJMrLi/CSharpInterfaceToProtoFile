using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CSharpInterfaceToProtoFile
{
    public class GenerateProto
    {
        private  StringBuilder _protofile;
        private  Dictionary<string, string> _messageDic;

        public GenerateProto()
        {
            _protofile = new StringBuilder();
            _messageDic = new Dictionary<string, string>();

        }

        public string GenerateProtoFile(Type interfaceType) 
        {
            var asm = interfaceType.Assembly;
            GenerateHeader(interfaceType, asm);
            GenerateServices(interfaceType);
            GenerateMessages();

            Console.WriteLine(_protofile.ToString());

            var packageName = interfaceType.Name.TrimStart('I').Replace("Service", "");

            var dirPath = "../../Generate";

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            var path = $"{dirPath}/{packageName}.proto";


            using (var fs2 = new FileStream(path, FileMode.OpenOrCreate))
            {

                StreamWriter writer = new StreamWriter(fs2);

                writer.WriteLine(_protofile.ToString());

                writer.Close();
            }

            return _protofile.ToString();
        }


        private void GenerateHeader(Type type, Assembly asm)
        {
            var packageName = type.Name.TrimStart('I').Replace("Service", "");

            _protofile.AppendLine("syntax =\"proto3\";");
            _protofile.AppendLine("");
            _protofile.AppendLine($"option csharp_namespace = \"{asm.GetName().Name}\";");
            _protofile.AppendLine("");
            _protofile.AppendLine($"package {packageName};");
            _protofile.AppendLine("");
        }

        private void GenerateServices(Type type)
        {
            var packageName = type.Name.TrimStart('I').Replace("Service", "");
            _protofile.Append($"service {packageName}");
            _protofile.AppendLine("{");

            var methods = type.GetMethods();
            //添加方法名称
            foreach (var method in methods)
            {
                //当前方法的名称
                var methodName = method.Name;
                var inputMsgName = string.Empty;
                var outputMsgName = string.Empty;

                #region input

                //当前输入参数列表
                var inputParameters = method.GetParameters();

                //根据输入的参数，创建部分message, 如果用引用类型则返回应用类型的列表
                var refparameterList = GetInputProtoMessages(ref inputMsgName, ref methodName, inputParameters);

                #endregion

                #region output

                var returnType = method.ReturnType;

                GetOutputProtoMessage(ref inputMsgName, ref methodName, ref outputMsgName, inputParameters, returnType, refparameterList);

                #endregion
                //组装Service列表 rpc {methodName} (inputMsgName) returns ({outputMsgName});
                _protofile.AppendLine($"\trpc {methodName} ({inputMsgName}) returns ({outputMsgName});");

            }

            _protofile.AppendLine("}");

        }

        private void GenerateMessages()
        {
            foreach (var message in _messageDic.Reverse())
            {
                _protofile.AppendLine(message.Value);
            }
        }

        private List<ParameterInfo> GetInputProtoMessages(ref string inputMsgName, ref string methodName, ParameterInfo[] inputParameters)
        {
            var parameterList = new List<ParameterInfo>();
            //遍历所有的参数，是否包含引用类型，如果存在，放入到返回列表中
            foreach (var item in inputParameters)
            {
                if (item.ParameterType.IsByRef)
                {
                    parameterList.Add(item);
                }
            }

            var msgString = new StringBuilder();

            //以下代码用于构建输入参数的的各种Message
            if (inputParameters.Length == 0)
            {
                inputMsgName = $"{methodName}Input";
                msgString.Append($"message {inputMsgName}");
                msgString.AppendLine("{");
                msgString.AppendLine("}");
                _messageDic.Add(inputMsgName, msgString.ToString());
            }


            if (inputParameters.Length >= 1)
            {

                if (inputParameters.Length == 1)
                {
                    //GetDataById ,输入参数为Id
                    methodName = $"{methodName}By{inputParameters[0].Name.FirstLetterToUpper()}";
                    inputMsgName = methodName + "Input";
                }
                else
                {
                    methodName = $"{methodName}By";
                    //GetDataById ,输入参数为Id
                    foreach (var param in inputParameters)
                    {
                        methodName = methodName + param.Name.FirstLetterToUpper() + "_";
                    }
                    methodName = methodName.TrimEnd('_');

                    inputMsgName = methodName + "_Input";
                }

                //创建最外层的Message
                msgString.Append($"message {inputMsgName}");
                msgString.AppendLine("{");

                //把多个参数放入到一个Message当中
                for (int i = 0; i < inputParameters.Length; i++)
                {
                    var param = inputParameters[i];

                    var paramType = ProtoTypeHelper.GetProtoTypeFromCsharpType(param.ParameterType);
                    var paramName = param.Name;

                    msgString.AppendLine($"\t{paramType} {paramName.LetterToLower()} = {i + 1};");

                    GetProtoMessageItem(param.ParameterType, param.ParameterType.Name);

                }
                msgString.AppendLine("}");

                //放到字典里
                if (!_messageDic.ContainsKey(inputMsgName))
                {
                    _messageDic.Add(inputMsgName, msgString.ToString());
                }
            }

            return parameterList;
        }

        private  void GetOutputProtoMessage(ref string inputMsgName, ref string methodName,
            ref string outputMsgName, ParameterInfo[] inputParameters, Type returnType, List<ParameterInfo> refparameterList)
        {
            //以下代码用于构建输出参数的的各种Message
            if (inputParameters.Length == 0)
            {
                outputMsgName = $"{methodName}Output";
            }
            else if (inputParameters.Length == 1)
            {
                outputMsgName = $"{methodName}Output";
            }
            else
            {
                outputMsgName = $"{methodName}_Output";
            }

            var msgString = new StringBuilder();
            msgString.AppendLine("");
            msgString.Append($"message {outputMsgName}");
            msgString.AppendLine("{");
            var returnTypeName = "result";
            var protoreturnType = ProtoTypeHelper.GetProtoTypeFromCsharpType(returnType);

            //空返回值
            if (string.IsNullOrEmpty(protoreturnType))
            {

                for (int i = 0; i < refparameterList.Count; i++)
                {
                    var refParam = refparameterList[i];
                    var protorefParamName = refParam.ParameterType.Name.TrimEnd('&');
                    var protorefParamType = ProtoTypeHelper.GetProtoTypeFromCsharpType(refParam.ParameterType);
                    msgString.AppendLine($"\t{protorefParamType} {protorefParamName.LetterToLower()} = {i + 1 };");
                }

                msgString.AppendLine("}");

                //放到字典里
                if (!_messageDic.ContainsKey(outputMsgName))
                {
                    _messageDic.Add(outputMsgName, msgString.ToString());
                }

                return;

            }
            //不是空返回值的枪口
            else
            {
                if (protoreturnType.Contains("repeated"))
                {
                    returnTypeName = "results";
                }

                if (protoreturnType.Contains("map"))
                {
                    returnTypeName = "resultMap";
                }


                msgString.AppendLine($"\t{protoreturnType} {returnTypeName.LetterToLower()} = 1;");

                for (int i = 0; i < refparameterList.Count; i++)
                {
                    var refParam = refparameterList[i];
                    var protorefParamName = refParam.ParameterType.Name.TrimEnd('&');
                    var protorefParamType = ProtoTypeHelper.GetProtoTypeFromCsharpType(refParam.ParameterType);
                    msgString.AppendLine($"\t{protorefParamType} {protorefParamName.LetterToLower()} = {i + 2};");
                }

                msgString.AppendLine("}");

                //放到字典里
                if (!_messageDic.ContainsKey(outputMsgName))
                {
                    _messageDic.Add(outputMsgName, msgString.ToString());
                }
            }

            GetProtoMessageItem(returnType, returnType.Name);

        }

        private  void GetProtoMessageItem(Type type, string msgName)
        {

            if (ProtoTypeHelper.IsSimpleCsharpType(type))
            {
                return;
            }

            //如果传入的是引用类型，需要转换成真实类型
            if (type.IsByRef)
            {
                type = ProtoTypeHelper.GetRealTypeFromRefOrOutType(type);
            }

            //如果是泛型类型
            if (type.IsGenericType)
            {
                var genericArguments = type.GetGenericArguments();
                foreach (var argu in genericArguments)
                {
                    if (ProtoTypeHelper.IsSimpleCsharpType(argu))
                    {
                        continue;
                    }

                    GetProtoMessageItem(argu, argu.Name);
                }
            }
            //如果是数组
            else if (type.BaseType == typeof(Array))
            {
                var typeName = type.Name.Replace("[]", "");

                if (_messageDic.ContainsKey(typeName))
                {
                    return;
                }

                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var arrayType = assembly.DefinedTypes.FirstOrDefault(x => x.Name == typeName);

                    //var arrayType = assembly.GetType(typeName, false, true);
                    if (arrayType != null)
                    {
                        //包含复杂类型，嵌套递归  
                        GetProtoMessageItem(arrayType, arrayType.Name);
                        break;
                    }
                }

                Console.WriteLine("找不到类型" + typeName);
            }
            //一般object
            else
            {
                var msgString = new StringBuilder();
                msgString.AppendLine("");
                msgString.Append($"message {msgName.TrimEnd('&')}");
                msgString.AppendLine("{");

                var properties = type.GetProperties();
                for (int i = 0; i < properties.Length; i++)
                {
                    var prop = properties[i];

                    //先将该类型放入到message当中
                    var protoType = ProtoTypeHelper.GetProtoTypeFromCsharpType(prop.PropertyType);

                    //把参数进行转换
                    var protoParamName = prop.Name;
                    var protoParamType = protoType;
                    //string message = 1;

                    msgString.AppendLine($"\t{protoParamType} {protoParamName.LetterToLower()} = {i + 1};");

                    //如果是简单类型
                    if (ProtoTypeHelper.IsSimpleCsharpType(prop.PropertyType))
                    {
                        continue;
                    }

                    //包含复杂类型，嵌套递归(存入)
                    GetProtoMessageItem(prop.PropertyType, prop.PropertyType.Name);
                }
                msgString.AppendLine("}");

                //放到字典里
                if (!_messageDic.ContainsKey(msgName))
                {
                    _messageDic.Add(msgName, msgString.ToString());
                }

            }
        }

    }
}
