using System;
using System.Collections.Generic;

namespace Elbowgrease
{
    public class GeneratorContext
    {
        public bool FullTypeComment { get; init; } = false;
        public Indent Indent { get; init; } = new ();
        public List<string> HeaderComments { get; } = new() { "Generated by Elbowgrease, do not modify by hand." };
        public Func<Type, string> NameFormatter { get; init; } = type => type.Name;
        /// <summary>
        /// Mark everything as optional (? after name in TypeScript) that isn't [Require]d
        /// </summary>
        public bool TightOptional { get; init; } = true;
        /// <summary>
        /// Type to generate for DateTime. If DateString is used, a DateString = string type will be added.
        /// </summary>
        public string DateTimeType { get; set; } = "DateString";
        /// <summary>
        /// From where to import used models. If set to null, no imports are generated.
        /// </summary>
        public string ModelsPath { get; init; } = "./backend-models";

        /// <summary>
        /// Reference a type for generating later on.
        /// </summary>
        public void ReferenceType(Type reference)
        {
            ReferencedTypes.Add(reference);
        }
        public HashSet<Type> ReferencedTypes { get; } = new();
    }
}
