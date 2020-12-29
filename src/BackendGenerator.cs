using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Elbowgrease
{
    public class BackendGenerator : Generator
    {
        public BackendGenerator(GeneratorContext context) : base(context) { }
        public string Generate(Assembly assemblyWithControllers)
        {
            var controllers = assemblyWithControllers.GetTypes().Where(type => type.GetCustomAttribute<ApiControllerAttribute>() != null).ToList();
            
            var indent = Context.Indent.Deeper;
            WriteLine("export let Backend = {");
            
            foreach (var controller in controllers)
                WriteController(controller, indent);
            
            WriteLine($"{indent}URL: {{");
            foreach (var controller in controllers)
                WriteControllerUrls(controller, indent.Deeper);
            
            WriteLine($"{indent}}},");
            WriteLine("};");

            var controllerOutput = Output.ToString();
            Output.Clear();

            var imports = string.Join('\n',  Context.ReferencedTypes.Select(t => indent + Context.NameFormatter(t) + ","));

            if(!string.IsNullOrEmpty(Context.ModelsPath))
                Header.Add($"import {{\n{imports}\n}} from \"{Context.ModelsPath}\";");
            
            WriteHeader();

            var header = Output.ToString();
            Output.Clear();

            return header + controllerOutput;
        }

        private Dictionary<Type, TypeScriptController> _cache = new();

        private void WriteControllerUrls(Type realController, Indent indent)
        {
            var controller = _cache[realController];
            WriteLine($"{indent}{controller.Name}: {{");

            indent = indent.Deeper;
            foreach (var method in controller.Methods)
            {
                Write($"{indent}{method.Name}(");
                WriteMethodArgs(method);
                WriteLine($") {{\n{indent.Deeper}return `{method.Path}`;\n{indent}}},");
            }
            
            WriteLine($"{indent.Shallower}}},");
        }

        // Controller: {
        //   methods
        // },
        private void WriteController(Type realController, Indent indent)
        {
            var controller = new TypeScriptController(realController, Context);
            _cache[realController] = controller;
            WriteLine($"{indent}{controller.Name}: {{");
            
            foreach (var method in controller.Methods)
            {
                WriteMethod(controller, method, indent.Deeper);
            }
            
            WriteLine($"{indent}}},");
        }

        // *method(args) {
        //   yield
        // },
        private void WriteMethod(TypeScriptController controller, ControllerMethod method, Indent indent)
        {
            Write($"{indent}*{method.Name}(");

            var unusedArgs = WriteMethodArgs(method);
            WriteLine(") {");
            var needsAuth = method.Auth != null || controller.Auth != null;
            
            var ret = method.ReturnType;
            var body = unusedArgs.Keys.FirstOrDefault();
            
            Write($"{indent.Deeper}");
            
            if (ret != null)
                Write($"return (");
            
            Write($"yield call(apiCall, `{method.Path}`");
            WriteParams(body, needsAuth, method.HttpMethod, indent.Deeper);
            Write(")");
            
            if (ret != null)
                WriteLine($") as {ret.Type};");
            else
                WriteLine(";");

            WriteLine($"{indent}}},");
        }
        void WriteParams(string body, bool useToken, string method, Indent i)
        {
            if(method == "GET" && useToken)
                return;
            
            body ??= "null";
            WriteLine(", {");
            
            if(!useToken)
                WriteLine($"{i.Deeper}anonymous: true,");
            
            if (method != "GET")
            {
                WriteLine($"{i.Deeper}body: {body},");
                WriteLine($"{i.Deeper}method: \"{method}\",");
            }

            Write($"{i}}}");
        }
        private Dictionary<string, string> WriteMethodArgs(ControllerMethod method)
        {
            var textArgs = new List<string>();
            var unusedArgs = new Dictionary<string, string>();
            foreach (var par in method.Parameters)
            {
                textArgs.Add($"{par.FullName}: {par.Type}");
                if (!method.Path.Contains(par.Name!))
                {
                    unusedArgs.Add(par.Name, par.Type);
                }
            }

            Write(string.Join(", ", textArgs));

            if (unusedArgs.Count > 1)
            {
                throw new Exception("Too many arguments");
            }

            return unusedArgs;
        }
    }
}
