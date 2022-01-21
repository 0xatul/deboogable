using CommandLine;
using dnlib.DotNet;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace deboogable
{
    class Program
    {
        private string LibDir { get; set; }
        private string OutDir { get; set; }

        public static void Main(string[] args)
        {
            Program program = new Program();
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opts =>
                {
                    if (IsFullPath(opts.LibDir) && IsFullPath(opts.OutDir)) // only accept absolute path as input 
                    {
                        program.LibDir = opts.LibDir;
                        program.OutDir = opts.OutDir;
                    }
                    else
                    {
                        Console.Error.WriteLine("Use absolute path on both lib and srcdir!");
                        return;
                    }

                    var files = Directory.EnumerateFiles(program.LibDir);
                    foreach (var file in files)
                    {
                        // TODO: change this to something reliable
                        if (IsAssembly(file)) // hacky hacky 
                        {
                            ReadAndWrite(file, program.OutDir);
                        }
                    }
                });
        }

        private static bool IsFullPath(string path)
        {
            // check if its null or not 
            if (path != null)
            {
                // return what it is if its not null
                return !String.IsNullOrWhiteSpace(path)
                       && path.IndexOfAny(Path.GetInvalidPathChars().ToArray()) == -1
                       && Path.IsPathRooted(path)
                       && !Path.GetPathRoot(path)!
                           .Equals(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
            }

            // if null return false
            return false;
        }

        private static void ReadAndWrite(string assembly, string outdir)
        {
            ModuleDef moduleDef = ModuleDefMD.Load(assembly);

            var customAttribute = moduleDef.Assembly.CustomAttributes.Find("System.Diagnostics.DebuggableAttribute");

            if (customAttribute is not null && customAttribute.ConstructorArguments.Count == 1)
            {
                /* for a debug assembly we want!
                 * [assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue)]
                 * 
                 * meanwhile a release assembly contains!
                 * [assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
                 */
                var arg = customAttribute.ConstructorArguments[0];
                var val = (int) arg.Value;
                Console.Out.Write("Debuggable({0})\n",
                    val == (int) DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints
                        ? "DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints"
                        : "Something else");
                if (arg.Type.FullName ==
                    "System.Diagnostics.DebuggableAttribute/DebuggingModes") // lets hope this is not subject to change
                {
                    // ReSharper disable once HeapView.BoxingAllocation
                    arg.Value = DebuggableAttribute.DebuggingModes.Default |
                                DebuggableAttribute.DebuggingModes.DisableOptimizations |
                                DebuggableAttribute.DebuggingModes
                                    .IgnoreSymbolStoreSequencePoints |
                                DebuggableAttribute.DebuggingModes.EnableEditAndContinue;
                    customAttribute.ConstructorArguments[0] = arg;
                    Console.Out.WriteLine("{0} patched with debug attributes!", moduleDef.Name);
                }
            }

            moduleDef.Write($"{outdir}\\{moduleDef.Name}");
            Console.Out.WriteLine("{0} is saved at {1}", moduleDef.Name, outdir);
        }

        private static bool IsAssembly(string assembly)
        {
            try
            {
                AssemblyName.GetAssemblyName(assembly);
                return true;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }
    }
}