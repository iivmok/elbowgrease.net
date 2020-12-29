using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Elbowgrease;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace test
{
    [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public record TestType(
        string Str,
        int Int,
        List<string> List,
        IList<string> IList,
        IEnumerable<string> IEnum,
        DateTime DateTime,
        DateTime? NDateTime,
        EnumType Enum,
        OtherType OtherType,
        bool Bool,
        char Char,
        List<bool?> Weird,
        char[] CharArray,
        int[] IntArray,
        DateTime[] DateArr,
        int?[] nullableArr
    );
    [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public record OtherType(
        string Str,
        int Int,
        List<string> List,
        IList<string> IList,
        IEnumerable<string> IEnum,
        DateTime DateTime,
        DateTime? NDateTime,
        EnumType Enum,
        TestType TestType,
        bool Bool,
        char Char,
        List<bool?> Weird,
        char[] CharArray,
        int[] IntArray,
        DateTime[] DateArr,
        int?[] nullableArr
    );

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum EnumType
    {
        foo,
        bar,
        baz = 12334,
    };

    [ApiController]
    [Route("api/[controller]/[action]")]
    public class FooController : ControllerBase
    {

        [HttpPost]
        [Authorize()]
        [Route("{foobar?}")]
        #pragma warning disable 1998
        public async Task<ActionResult<OtherType>> Bar(TestType test, string foobar)
        {
            return null;
        }
        [HttpGet]
        [Route("{foobar?}")]
        public async Task<ActionResult<IEnumerable<TestType>>> Baz(int foobar)
        {
            return null;
        }
        [HttpPost]
        [Route("{dt?}")]
        public async Task<ActionResult> File(IFormFileCollection files, DateTime dt)
        {
            return null;
        }
        #pragma warning restore 1998
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            var context = new GeneratorContext()
            {
                NameFormatter = type => type.IsEnum ? type.Name : "I" + type.Name,
                TightOptional = false,
                ModelsPath = "./models",
            };
            var genModels = new BackendModelsGenerator(context);
            var genBackend = new BackendGenerator(context)
            {
                Header =
                {
                    "import { call } from 'redux-saga/effects';",
                    "import { apiCall } from './api';",
                },
            };

            // context.ReferencedTypes.Add(typeof());

            var backend = genBackend.Generate(Assembly.GetExecutingAssembly());
            File.WriteAllText("backend.ts", backend);
            Console.WriteLine(backend);

            var models = genModels.Generate();
            File.WriteAllText("models.ts", models);
            Console.WriteLine(models);
        }
    }
}