using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Elbowgrease
{
    public class Generator
    {
        public List<string> Header { get; } = new();
        
        protected readonly StringBuilder Output = new();
        protected TextWriter Out = TextWriter.Null;
        protected readonly GeneratorContext Context;

        protected void Write(object obj)
        {
            Out.Write(obj.ToString());
        }
        
        protected void WriteLine(object obj = null)
        {
            Out.Write(obj?.ToString());
            Out.Write('\n');
        }
        
        public Generator(GeneratorContext context)
        {
            Context = context;
            Reset();
        }

        private void Reset()
        {
            Output.Clear();
            Out = new StringWriter(Output);
        }

        protected void WriteHeader()
        {
            WriteLine(string.Join("\n", Context.HeaderComments.Select(l => "// " + l)));
            WriteLine();
            WriteLine(string.Join("\n", Header));
            WriteLine();
            if (Context.DateTimeType == "DateString")
            {
                WriteLine("export type DateString = string;");
                WriteLine();
            }
        }
    }
}
