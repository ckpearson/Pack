using CommandLine;

namespace Pack.Runner.Options
{
    public class MakeOptions
    {
        [Option('i', "inputFilePath", HelpText = "The file to create a packed image for", Required = true)]
        public string InputFilePath { get; set; }

        [Option('o', "outputFilePath", HelpText = "The path to use for the produced image", Required = true)]
        public string OutputFilePath { get; set; }

        [Option('p', "palette", HelpText = "Specifies the palette to use for non-data pixels; these are named or a semi-colon separated collection of RGB values.")]
        public string Palette { get; set; }
    }
}