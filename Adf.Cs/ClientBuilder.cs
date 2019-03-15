using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Microsoft.CSharp;
using System.CodeDom.Compiler;

namespace Adf.Cs
{
    /// <summary>
    /// 客户端构建器
    /// </summary>
    internal static class ClientBuilder
    {
        static Dictionary<string, object> instanceCaches = new Dictionary<string, object>();

        internal static object CreateInstance(Type interfaceType, string identifier, params object[] initArgs)
        {
            var ns = "Adf.Cs.Impl_" + identifier;
            var className = interfaceType.FullName.Replace('.', '_') + "_Impl";

            //cache get
            var cacheKey = ns + "." + className + identifier;
            object instance;
            if (instanceCaches.TryGetValue(cacheKey, out instance))
            {
                return instance;
            }

            if (interfaceType.IsNested)
            {
                throw new CsException("not support nested type");
            }


            var methods = interfaceType.GetMethods();
            var methodCount = methods.Length;

            //创建编译器实例。   
            var provider = new CSharpCodeProvider();
            //设置编译参数。   
            var paras = new CompilerParameters();
            paras.GenerateExecutable = false;
            paras.GenerateInMemory = true;
            paras.CompilerOptions = "/optimize";
            paras.WarningLevel = 4;
            paras.TreatWarningsAsErrors = false;

            //加载程序集
            paras.ReferencedAssemblies.Add(typeof(Int16).Assembly.Location);
            paras.ReferencedAssemblies.Add(typeof(CSharpCodeProvider).Assembly.Location);
            paras.ReferencedAssemblies.Add(typeof(Adf.LogWriter).Assembly.Location);
            paras.ReferencedAssemblies.Add(typeof(Adf.Cs.Client).Assembly.Location);
            paras.ReferencedAssemblies.Add(interfaceType.Assembly.Location);



            //Dictionary<Assembly, string> assemblyDictionary = new Dictionary<Assembly, string>();
            //assemblyDictionary.Add(ADF_TYPE.Assembly, ADF_TYPE.Assembly.Location);
            //assemblyDictionary.Add(ADF_CS_TYPE.Assembly, ADF_CS_TYPE.Assembly.Location);
            //assemblyDictionary.Add(interfaceType.Assembly, interfaceType.Assembly.Location);

            //paras.CompilerOptions = "/optimize /warn:4 /nowarn:1701,1702";
            //// Set the level at which the compiler 
            //// should start displaying warnings.
            //paras.WarningLevel = 4;

            //// Set whether to treat all warnings as errors.
            //paras.TreatWarningsAsErrors = false;

            ////加载程序集
            //var referencedAssemblies = interfaceType.Assembly.GetReferencedAssemblies();
            //var referencedAssembliesLength = referencedAssemblies.Length;
            //var assemblysList = new List<string>(referencedAssembliesLength + 3);
            //var isAdf = false;
            //var isAdfCs = false;
            //for (int i = 0; i < referencedAssembliesLength; i++)
            //{
            //    if (referencedAssemblies[i].Name.Equals("Adf"))
            //        isAdf = true;
            //    else if (referencedAssemblies[i].Name.Equals("Adf.Cs"))
            //        isAdfCs = true;

            //    assemblysList.Add(Assembly.ReflectionOnlyLoad(referencedAssemblies[i].FullName).Location);
            //}
            //assemblysList.Add(interfaceType.Assembly.Location);
            //if (!isAdf)
            //    assemblysList.Add("Adf.dll");
            //if (!isAdfCs)
            //    assemblysList.Add("Adf.Cs.dll");
            //paras.ReferencedAssemblies.AddRange(assemblysList.ToArray());


            //创建动态代码。   
            StringBuilder classSource = new StringBuilder();
            classSource.AppendLine("namespace " + ns);
            classSource.AppendLine("{");
            classSource.AppendLine("using System;");
            classSource.AppendLine("public class " + className + " : Adf.Cs.Client," + interfaceType.FullName);
            classSource.AppendLine("{");

            //创建构建函数
            classSource.AppendLine("public " + className + "(System.String server, System.String configName)");
            classSource.AppendLine(":base(server,configName)");
            classSource.AppendLine("{}");

            classSource.AppendLine("public " + className + "(System.String server, System.String hostOrIp, System.Int32 port)");
            classSource.AppendLine(":base(server,hostOrIp,port)");
            classSource.AppendLine("{}");

            classSource.AppendLine("public " + className + "(System.String server, System.String[] hostOrIps)");
            classSource.AppendLine(":base(server,hostOrIps)");
            classSource.AppendLine("{}");

            //重写客户端名称
            classSource.AppendLine("protected override string Name { get { return \"" + interfaceType.Name.Substring(1) + "Client\"; } }");

            //创建接口方法
            var voidType = typeof(void);
            for (int i = 0; i < methodCount; i++)
            {
                var method = methods[i];

                if (method.ReturnType.IsGenericType)
                    throw new CsException("return not allow generic type for " + method.Name);

                if (voidType.Equals(method.ReturnType))
                    throw new CsException("return type is not allow void for " + method.Name);

                var parameters = method.GetParameters();

                var attrs = method.GetCustomAttributes(false);
                ClientHashKeyAttribute hashKeyAttribute = null;
                foreach (var attr in attrs)
                {
                    if (attr is ClientHashKeyAttribute)
                    {
                        hashKeyAttribute = (ClientHashKeyAttribute)attr;
                        break;
                    }
                }

                classSource.AppendLine("public " + method.ReturnType.FullName + " " + method.Name + "(");
                classSource.AppendLine(CreateParameters(parameters));
                classSource.AppendLine("){");
                classSource.AppendLine(CreateOutputParametersInitialize(parameters));
                classSource.AppendLine(CreateParametersArray(parameters));

                if (hashKeyAttribute != null && hashKeyAttribute.Parameters.Length > 0)
                {
                    classSource.AppendLine("string hashKey = string.Concat(" + string.Join(",\".\",", hashKeyAttribute.Parameters) + ");");
                    classSource.AppendLine(method.ReturnType.FullName + " result = base.HashCommand<" + method.ReturnType.FullName + ">(\"" + method.Name + "\",hashKey, parameters);");
                }
                else
                {
                    classSource.AppendLine(method.ReturnType.FullName + " result = base.Command<" + method.ReturnType.FullName + ">(\"" + method.Name + "\", parameters);");
                }

                classSource.AppendLine(CreateOutputParametersAssignment(parameters));
                classSource.AppendLine("return result;");
                classSource.AppendLine("}");
            }

            classSource.AppendLine("}}");

            //编译代码。   
            CompilerResults result = provider.CompileAssemblyFromSource(paras, classSource.ToString());
            if (result.Errors.Count > 0)
            {
                throw new ClientBuilderException(result.Errors[0].ErrorText) { ClientSource = classSource.ToString(), ReferencedAssemblies = paras.ReferencedAssemblies };

                //for (int i = 0; i < result.Errors.Count; i++)
                //throw new CsException(result.Errors[i].ToString());
            }

            //获取编译后的程序集。   
            Assembly assembly = result.CompiledAssembly;
            try
            {
                instance = assembly.CreateInstance(ns + "." + className, false, BindingFlags.Default, null, initArgs, null, null);
            }
            catch (TargetInvocationException target)
            {
                throw new ClientBuilderException(target.InnerException) { ClientSource = classSource.ToString(), ReferencedAssemblies = paras.ReferencedAssemblies };
            }

            //加入实例缓存
            lock (instanceCaches)
            {
                if (instanceCaches.ContainsKey(cacheKey) == false)
                {
                    instanceCaches.Add(cacheKey, instance);
                }
            }

            return instance;
        }

        private static string ReplaceRefParameter(Type type)
        {
            //过滤掉out/ref引用类型上的符号
            return type.FullName.Replace("&", "");
        }

        private static string CreateParametersArray(ParameterInfo[] parameters)
        {
            var count = parameters.Length;
            if (count == 0)
            {
                return "object[] parameters = new object[0];";
            }

            var result = new string[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = parameters[i].Name;
            }

            return "object[] parameters = new object[]{" + string.Join(",", result) + "};";
        }

        private static string CreateOutputParametersAssignment(ParameterInfo[] parameters)
        {
            var count = parameters.Length;
            if (count == 0)
            {
                return "";
            }

            var result = new string[count];
            for (int i = 0; i < count; i++)
            {
                if (parameters[i].IsOut)
                {
                    result[i] = parameters[i].Name + "=(" + ReplaceRefParameter(parameters[i].ParameterType) + ")parameters[" + i + "];";
                }
            }

            return string.Join(Environment.NewLine, result);
        }

        private static string CreateOutputParametersInitialize(ParameterInfo[] parameters)
        {
            var count = parameters.Length;
            if (count == 0)
            {
                return "";
            }

            var result = new string[count];
            for (int i = 0; i < count; i++)
            {
                if (parameters[i].IsOut)
                {
                    if (parameters[i].ParameterType.IsByRef)
                    {
                        var refTypeName = ReplaceRefParameter(parameters[i].ParameterType) + "," + parameters[i].ParameterType.Assembly.GetName().Name;
                        var refType = Type.GetType(refTypeName);
                        if (refType.IsValueType)
                        {
                            result[i] = parameters[i].Name + "=new " + refType.FullName + "();";
                        }
                        else if (refType.IsArray)
                        {
                            result[i] = parameters[i].Name + "=new "+ refType.GetElementType().FullName +"[0];";
                        }
                        else
                        {
                            result[i] = parameters[i].Name + "=null;";
                        }
                    }
                    else if (parameters[i].ParameterType.IsValueType)
                    {
                        result[i] = parameters[i].Name + "=new " + ReplaceRefParameter(parameters[i].ParameterType) + "();";
                    }
                    else if (parameters[i].ParameterType.IsArray)
                    {
                        result[i] = parameters[i].Name + "=new " + parameters[i].ParameterType.GetElementType().FullName + "[0];";
                    }
                    else
                    {
                        result[i] = parameters[i].Name + "=null;";
                    }
                }
            }

            return string.Join(Environment.NewLine, result);
        }

        private static string CreateParameters(ParameterInfo[] parameters)
        {
            var count = parameters.Length;
            if (count == 0)
            {
                return "";
            }

            var result = new string[count];
            for (int i = 0; i < count; i++)
            {
                if (parameters[i].ParameterType.IsGenericType)
                    throw new CsException("Parameter not allow generic type for " + parameters[i].Name);

                if (parameters[i].IsOut)
                    result[i] = "out " + ReplaceRefParameter(parameters[i].ParameterType) + " " + parameters[i].Name;
                else
                    result[i] = parameters[i].ParameterType.FullName + " " + parameters[i].Name;
            }

            return string.Join(",", result);
        }
    }
}