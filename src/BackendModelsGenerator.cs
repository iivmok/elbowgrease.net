using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Elbowgrease
{
    public static class ElbowgreaseExtensions
    {
        public static bool HasAttribute<T>(this ICustomAttributeProvider member) where T: Attribute
        {
            return member.GetCustomAttributes(typeof(T), true).Length > 0;
        }

        public static Type StripGeneric(this Type type, params Type[] toStrip)
        {
            while(type.IsGenericType)
            {
                var genType = type.GetGenericTypeDefinition();
                if(toStrip.Contains(genType))
                    type = type.GetGenericArguments()[0];
                else
                {
                    break;
                }
            }

            return type;
        }
    }
    public record Indent(string Tab = "  ", int Level = 0)
    {
        public override string ToString()
        {
            return string.Concat(Enumerable.Repeat(Tab, Level));
        }
        public Indent Deeper => this with { Level = Level + 1};
        public Indent Shallower => this with { Level = Level - 1};
    }
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class BackendModelsGenerator : Generator
    {
        public List<string> GlobalExtends { get; } = new();
        public Dictionary<string, string> Extends { get; } = new();
        public ModelKeyword ModelKeyword { get; init; } = ModelKeyword.@interface;
        private Indent Indent => Context.Indent;
        public BackendModelsGenerator(GeneratorContext context) : base(context) { }
        private HashSet<Type> _done = new(); 

        public string Generate()
        {
            WriteHeader();
            
            while(Context.ReferencedTypes.Count > 0)
            {
                var type = Context.ReferencedTypes.First();
                Context.ReferencedTypes.Remove(type);
                if(_done.Contains(type))
                    continue;
                WriteLine($"// {type}");
                if (type.IsEnum)
                    WriteEnum(type);
                else
                    WriteModel(type);
                _done.Add(type);
            }

            foreach (var (k, v) in Extends)
            {
                throw new Exception($"Unused extends-direction: {k} extends {v}");
            }

            return Output.ToString();
        }
        
        private void WriteEnum(Type type)
        {
            WriteLine($"export enum {Context.NameFormatter(type)} {{");
            var realType = type.GetEnumUnderlyingType();
            foreach (var val in type.GetEnumValues())
            {
                WriteLine($"{Indent.Deeper}{Enum.GetName(type, val)} = {Convert.ChangeType(val, realType)},");
            }
                
            WriteLine($"}}\n");
        }

        private void WriteModel(Type type)
        {
            Write($"export {ModelKeyword} {Context.NameFormatter(type)}");
            var extends = new List<string>();
            extends.AddRange(GlobalExtends);

            if (Extends.TryGetValue(type.Name, out var val))
            {
                extends.Add(val);
                Extends.Remove(type.Name);
            }

            if (extends.Count > 0)
                Write($" extends {string.Join(", ", extends)}");
            
            WriteLine(" {");
            
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if(property.HasAttribute<JsonIgnoreAttribute>())
                    continue;

                var tsProp = new TypeScriptType(property, Context);
                WriteProperty(tsProp, Indent.Deeper);
            }
                
            WriteLine($"}}\n");
        }
        private void WriteProperty(TypeScriptType type, Indent indent)
        {
            Write(indent);
            Write($"{type.FullName}: {type.Type};");
            if(type.Notes.Count > 0)
                Write($" // {string.Join(", ", type.Notes)}");
            WriteLine();
        }

    }
    public enum ModelKeyword
    {
        @interface,
        @class,
        @type,
    }
}
