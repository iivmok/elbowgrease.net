using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Elbowgrease
{
    public class ControllerMethod
    {
        public string Name { get; }
        public string HttpMethod { get; }
        public string Path { get; }
        public IEnumerable<TypeScriptType> Parameters { get; }
        public TypeScriptType ReturnType { get; }
        public AuthorizeAttribute Auth { get; }
       
        public ControllerMethod(MethodInfo method, TypeScriptController controller)
        {
            var httpMethodAttr = method.GetCustomAttribute<HttpMethodAttribute>();
            if (httpMethodAttr == null || httpMethodAttr.HttpMethods.Count() > 1)
            {
                throw new Exception("Unsupported amount of http methods");
            }
            var route = method.GetCustomAttribute<RouteAttribute>();
            Auth = method.GetCustomAttribute<AuthorizeAttribute>();
            HttpMethod = httpMethodAttr.HttpMethods.First();
            Name = method.Name;
            Path = controller.Path.Replace("[action]", Name);
            var returnType = MapReturnType(method.ReturnType);
            if (returnType != null)
            {
                controller.Context.ReferenceType(returnType.StripGeneric(typeof(IEnumerable<>)));
                ReturnType = new TypeScriptType(returnType, controller.Context);
            }
            
            if (route != null)
            {
                Path += "/" + route.Template.Replace("{", "${");
                //TODO removing optional params
                Path = Path.Replace("?}", "}");
            }

            Parameters = method.GetParameters().Select(param =>
                //TODO
                //We mark parameters as always required, because we don't mark our actions with [Required]
                //This means we don't support optional arguments right now
                new TypeScriptType(param, controller.Context, true)
            );
        }

        private static Type MapReturnType(Type type)
        {
            var ret = type.StripGeneric(typeof(Task<>), typeof(ActionResult<>));

            if (new[] {typeof(IActionResult), typeof(ActionResult)}.Contains(ret))
            {
                return null;
            }

            return ret;
        }

    }
    public class TypeScriptController
    {
        public string Name { get; }
        public string Path { get; }
        public List<ControllerMethod> Methods { get; } = new();
        public GeneratorContext Context { get; }
        public AuthorizeAttribute? Auth { get; }
        
        public TypeScriptController(Type controller, GeneratorContext context)
        {
            Context = context;
            Name = controller.Name.Replace("Controller", "");
            var route = controller.GetCustomAttribute<RouteAttribute>();
            Auth = controller.GetCustomAttribute<AuthorizeAttribute>();
            if (route == null)
            {
                throw new Exception("Expecting Controller route");
            }
            Path = "/" + route.Template.Replace("[controller]", Name);    
            foreach (var method in controller.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
            {
                var httpMethodAttr = method.GetCustomAttribute<HttpMethodAttribute>();
                if (httpMethodAttr == null || httpMethodAttr.HttpMethods.Count() > 1)
                {
                    throw new Exception("Unsupported amount of http methods");
                }

                Methods.Add(new ControllerMethod(method, this));
            }
        }

    }
}
