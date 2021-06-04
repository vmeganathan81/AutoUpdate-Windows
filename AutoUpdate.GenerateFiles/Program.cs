using AutoUpdates.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoUpdate.GenerateFiles
{
    class Program
    {
        static Arguments cmdArgs = new Arguments();

        static void Main(string[] args)
        {

            if (args.Length > 0)
            {
                try
                {
                    Regex cmdRegEx = new Regex(@"/(?<name>.+?):(?<val>.+)");
                    foreach (string s in args)
                    {
                        Match m = cmdRegEx.Match(s);
                        if (m.Success)
                        {
                            ProcessCommandArgs(m.Groups[1].Value, m.Groups[2].Value);
                        }
                    }

                    EncryptDecrypt.EncryptFile(cmdArgs.InputFile, Path.GetFileNameWithoutExtension(cmdArgs.InputFile) +".dat", "ecKey123");
                    EncryptDecrypt.EncryptFile(cmdArgs.InputManifest, Path.GetFileNameWithoutExtension(cmdArgs.InputManifest) + ".dat", "ecKey123");
                }
                catch (Exception ex)
                {
                    Console.Write("Error Message: " + ex.Message.ToString());
                }
                finally
                {
                }
            }


        }

        private static void ProcessCommandArgs(string key, string value)
        {
            switch (key)
            {
                case "InputFile":
                    cmdArgs.InputFile = value;
                    break;
                case "InputManifest":
                    cmdArgs.InputManifest = value;
                    break;
            }

        }



        public class Arguments
        {
            #region Private Properties
            private string _sInputFile;
            private string _sInputManifest;
            #endregion

            #region Constructors
            public Arguments()
            {
                _sInputFile = "";
                _sInputManifest = "";
            }
            #endregion

            #region Property Methods
            public string InputFile
            {
                get
                {
                    return _sInputFile;
                }
                set
                {
                    _sInputFile = value;
                }
            }

            public string InputManifest
            {
                get
                {
                    return _sInputManifest;
                }
                set
                {
                    _sInputManifest = value;
                }
            }

  
            #endregion
        }
    }
}
