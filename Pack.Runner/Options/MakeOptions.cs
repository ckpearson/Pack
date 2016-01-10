using CommandLine;

namespace Pack.Runner.Options
{
    public class MakeOptions
    {
        [Option('i', "inputFilePath", HelpText = "The file to create a packed image for", Required = true)]
        public string InputFilePath { get; set; }

        [Option('o', "outputFilePath", HelpText = "The path to use for the produced image", Required = true)]
        public string OutputFilePath { get; set; }
    }
}