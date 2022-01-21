using CommandLine;

public class Options
{
    [Option('l', "libdir", Required = true, HelpText = "Directory with assemblies")]
    public string LibDir { get; set; }
    [Option('o',"outdir",Required =true, HelpText ="Directory to save the debuggable assemblies in")]
    public string OutDir { get; set; }

}
