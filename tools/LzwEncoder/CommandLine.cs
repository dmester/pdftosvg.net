using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LzwEncoder
{
    internal class CommandLine
    {
        public CommandLine(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--earlychange":
                        EarlyChange = true;
                        break;

                    case "--hex":
                        HexEncode = true;
                        break;

                    default:
                        if (InputPath == null)
                        {
                            InputPath = args[i];
                        }
                        else
                        {
                            OutputPath = args[i];
                        }
                        break;
                }
            }
        }

        public bool EarlyChange { get; set; }

        public bool HexEncode { get; set; }

        public string InputPath { get; set; }

        public string OutputPath { get; set; }
    }
}
