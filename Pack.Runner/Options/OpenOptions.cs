using CommandLine;

namespace Pack.Runner.Options
{
    public class OpenOptions
    {
        [Option('i', "inputFilePath", HelpText = "The image to open")]
        public string InputFilePath { get; set; }

        [Option('o', "outputFilePath", Required = true,
            HelpText = "The path to output to; if this is a directory the file name metadata will be used.")]
        public string OutputFilePath { get; set; }
    }
}