using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
// ReSharper disable All

#pragma warning disable 1998
namespace Elbowgrease.Test
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<TClass>> Post(TOther arg)
        {
            return null;
        }
        [HttpGet]
        [Route("{arg}/{opt?}")]
        public async Task<ActionResult<TClass>> Get(string arg, int opt)
        {
            return null;
        }
    }
    public class TClass {}

    public class TOther
    {
        public TClass TClass { get; set; }
        public int Number { get; set; }
        public int? NNumber { get; set; }
        public string Text { get; set; }
        public bool Boolean { get; set; }
    }
    public enum TEnum {}
    public class Tests
    {
        private readonly GeneratorContext _ctxClean = new GeneratorContext()
        {
            Indent = new Indent(string.Empty),
            ModelsPath = null,
        };
        private readonly GeneratorContext _ctxBackend = new GeneratorContext()
        {
            Indent = new Indent(string.Empty),
            ModelsPath = "./models",
            DateTimeType = "string",
            TightOptional = true,
        };
        [SetUp]
        public void Setup()
        {
            _ctxClean.HeaderComments.Clear();;
            _ctxBackend.HeaderComments.Clear();;
        }

        [TestCase(typeof(string), "string")]
        [TestCase(typeof(int), "number")]
        [TestCase(typeof(bool), "boolean")]
        [TestCase(typeof(DateTime), "DateString")]
        [TestCase(typeof(IFormFileCollection), "FormData")]
        [TestCase(typeof(List<int>), "number[]")]
        [TestCase(typeof(IEnumerable<int>), "number[]")]
        [TestCase(typeof(IList<int>), "number[]")]
        [TestCase(typeof(int?), "number | null", true)]
        [TestCase(typeof(List<int?>), "(number | null)[]")]
        [TestCase(typeof(TClass), "TClass")]
        [TestCase(typeof(TEnum), "TEnum")]
        [TestCase(typeof(int[][]), "(number[])[]")]
        [TestCase(typeof(Dictionary<string, string>), "Record<string, string>")]
        [TestCase(typeof(Dictionary<string, List<string>>), "Record<string, string[]>")]
        [TestCase(typeof(Dictionary<string, TClass>), "Record<string, TClass>")]
        [Order(0)]
        public void TypeScriptType(Type type, string result, bool checkOptional = false)
        {
            var tst = new TypeScriptType(type, _ctxClean);
            Assert.AreEqual(result, tst.Type);
            if (checkOptional)
            {
                Assert.IsTrue(tst.NameMods.EndsWith("?"));
            }
        }

        [Test]
        [Order(1)]
        public void Backend()
        {
            _ctxBackend.ReferencedTypes.Clear();
            var gen = new BackendGenerator(_ctxBackend);
            var result = gen.Generate(Assembly.GetExecutingAssembly());
            result = result.Replace("\n", "").Trim();
            Assert.IsTrue(result.Contains("import {TClass,TOther,} from \"./models\";"));
            Assert.IsTrue(result.Contains("Post(arg: TOther) {return (yield call(apiCall, `/Test/Post`, {anonymous: true,body: arg,method: \"POST\",})) as TClass;}"));
            Assert.IsTrue(result.Contains("Get(arg: string, opt: number) {return (yield call(apiCall, `/Test/Get/${arg}/${opt}`, {anonymous: true,})) as TClass;}"));
            Assert.IsTrue(result.Contains("export let Backend = {Test: "));
        }

        [Test]
        [Order(2)]
        public void BackendModel()
        {
            var gen = new BackendModelsGenerator(_ctxBackend);
            var result = gen.Generate();
            result = result.Replace("\n", "").Trim();
            Assert.IsTrue(result.Contains("export interface TOther {tClass?: TClass;number: number;nNumber?: number | null;text?: string;boolean: boolean;}"));
        }
    }
}