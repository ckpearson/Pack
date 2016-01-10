using CommandLine;

namespace Pack.Runner.Options
{
    public class CheckOptions
    {
        [Option('i', "inputFilePath", HelpText = "The image to check", Required = true)]
        public string InputFilePath { get; set; }
    }
}