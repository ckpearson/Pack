using CommandLine;
using CommandLine.Text;

namespace Pack.Runner.Options
{
    public class MainOptions
    {
        [VerbOption("make", HelpText = "Makes a packed image")]
        public MakeOptions MakeVerb { get; set; }

        [VerbOption("check", HelpText = "Checks whether a file is a packed image. Returns 0 for packed image, 1 for not"
            )]
        public CheckOptions CheckVerb { get; set; }

        [VerbOption("open", HelpText = "Opens a packed image")]
        public OpenOptions OpenVerb { get; set; }

        [HelpVerbOption]
        public string DoHelpForVerb(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}