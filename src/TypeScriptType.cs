using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace Elbowgrease
{
    public class TypeScriptType
    {
        /// <summary>
        /// Set to true if you have a type that implements IEnumerable and you want it
        /// converted like an array 
        /// </summary>
        public static bool IncludeIEnumerableInterface { get; set; } = false;
        /// <summary>
        /// Will explicitly mark a type as nullable with (type | null)
        /// </summary>
        public static bool ShowNull { get; set; } = true;
        /// <summary>
        /// Add full type comment to notes
        /// </summary>
        public static bool FullTypeComment { get; set; } = false;
        
        public string Type { get; private set; }
        public string Name { get; }
        public string NameMods { get; private set; } = "";
        public string FullName => Name + NameMods;
        public List<string> Notes { get; } = new();
        public string NotesComment => $"/*{string.Join(", ", Notes)}*/";

        public TypeScriptType(PropertyInfo property, GeneratorContext context)
            :this(property.PropertyType, context, property.HasAttribute<RequiredAttribute>(), LowerFirst(property.Name))
        {
            
        }
        public TypeScriptType(ParameterInfo parameter, GeneratorContext context, bool? required = null)
            :this(parameter.ParameterType, context, required ?? parameter.HasAttribute<RequiredAttribute>(), parameter.Name)
        {
            
        }

        public TypeScriptType(Type realType, GeneratorContext context, bool required = false, string name = "")
        {
            Name = name;
            _context = context;
            _type = realType;

            while (ReduceNullable() || ReduceGeneric() || ReduceArray()) { }

            if (_type == typeof(DateTime))
            {
                Type = context.DateTimeType;
            }
            else if (_type == typeof(IFormFileCollection))
            {
                Type = "FormData";
            }
            else if (StringTypes.Contains(_type))
            {
                Type = "string";
            }
            else if (_type.IsPrimitive)
            {
                if (_type == typeof(bool))
                    Type = "boolean";
                else
                    Type = "number";
            }
            else if(Type == null)
            {
                //A reference type, so it's unknown to us, we need a reference to it
                Type = _context.NameFormatter.Invoke(_type);
                _context.ReferenceType(_type);
            }
            
            Type = _typeTemplate.Replace("{type}", Type);
            
            if (FullTypeComment)
            {
                Notes.Add(realType.ToString().Replace("System.", ""));
            }
            
            if (!required)
            {
                if (context.TightOptional && !_type.IsValueType)
                {
                    if (!NameMods.EndsWith("?"))
                        NameMods += "?";
                }
            }
            else
            {
                Notes.Add("Required");
            }
        }

        private static string LowerFirst(string name)
        {
            return name[0].ToString().ToLower() + name.Substring(1);
        }

        private Type _type;
        private string _typeTemplate = "{type}";

        private static readonly Type[] StringTypes = {typeof(string), typeof(Guid), typeof(char)};
        private static readonly Type[] FormTypes = {typeof(IFormFile), typeof(IFormFileCollection), typeof(char)};
        private static readonly Type[] ListTypes = {typeof(List<>), typeof(IList<>), typeof(IEnumerable<>)};
        private readonly GeneratorContext _context;

        private void MarkAsArray()
        {
            _typeTemplate = $"{_typeTemplate}[]";
        }

        private bool ReduceGeneric()
        {
            static bool ImplementsListInterface(Type type)
            {
                var interfaces= type.GetInterfaces();
                return interfaces.Any(i =>
                {
                    if (i.IsGenericType)
                    {
                        var genType = i.GetGenericTypeDefinition();
                        if (genType == typeof(IList<>))
                            return true;
                        if (IncludeIEnumerableInterface && genType == typeof(IEnumerable<>))
                            return true;
                    }

                    return false;
                });
            }
            
            if(!_type.IsGenericType)
                return false;
            
            var genType = _type.GetGenericTypeDefinition();
            
            if (ListTypes.Contains(genType) || ImplementsListInterface(_type) || ImplementsListInterface(genType))
            {
                _type = _type.GenericTypeArguments[0];
                MarkAsArray();
                return true;
            }

            if (genType == typeof(Dictionary<,>))
            {
                var key = new TypeScriptType(_type.GenericTypeArguments[0], _context);
                var val = new TypeScriptType(_type.GenericTypeArguments[1], _context);
                Type = $"Record<{key.Type}, {val.Type}>";
                Notes.AddRange(key.Notes);
                Notes.AddRange(val.Notes);
                return true;
            }

            throw new Exception("Unsupported generic type");
        }

        private bool ReduceArray()
        {
            if (!_type.IsArray)
                return false;
            
            _type = _type.GetElementType();
            MarkAsArray();
            return true;
        }

        private bool ReduceNullable()
        {
            var nullableType = Nullable.GetUnderlyingType(_type);

            if (nullableType == null)
                return false;
            
            _type = nullableType;
            
            NameMods += "?";
            
            if (ShowNull)
            {
                _typeTemplate = $"{_typeTemplate} | null";
            }

            return true;
        }
    }
}
