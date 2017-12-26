using System;
using System.CodeDom.Compiler;
using System.IO;

namespace Ebnf.Compiler
{
    internal class Program
    {
        /// <summary>
        /// The entry point for the program.
        /// </summary>
        /// <param name="args">The command line arguments supplied.</param>
        private static void Main(string[] args)
        {
            // Write application header

            Console.WriteLine(Resources.ReadResource("Ebnf.Compiler.Resources.Banner.txt"));

            // Get arguments

            string inputFilePath;
            string outputFilePath;
            string namespaceName;
            string keyNameFile;

            if (args.Length >= 3)
            {
                // Valid number of arguments, apply settings

                inputFilePath = args[0]; // First argument is input file path
                outputFilePath = args[1]; // Second argument is output file path
                namespaceName = args[2]; // Third argument is the default namespace name

                keyNameFile = args.Length >= 4 ? args[3] : "";
            }
            else
            {
                if (args.Length > 0)
                {
                    // Arguments were supplied, but there was not a correct number
                    // Therefore, notify user of correct syntax
                    Console.WriteLine(string.Join("\r\n", args));
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(string.Join("\r\n",
                        "Invalid number of arguments supplied, expected 2 arguments in form:",
                        "- Input file path",
                        "- Output file path",
                        "- Default namespace"));
                }

                // Get arguments from other source

                if (Environment.UserInteractive)
                {
                    // The console window is open, so prompt user to enter arguments manually

                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("Please enter your input file path:");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    inputFilePath = Console.ReadLine();

                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("Please enter your output DLL file path:");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    outputFilePath = Console.ReadLine();

                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("Please enter your default namespace:");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    namespaceName = Console.ReadLine();

                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("Please enter your key name file path: (leave blank for no file)");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    keyNameFile = Console.ReadLine();
                }
                else
                {
                    // Use default values
                    inputFilePath = "input.txt";
                    outputFilePath = "Ebnf.dll";
                    namespaceName = "Ebnf";
                    keyNameFile = "";
                }
            }

            // Read input file

            string inputFile;
            try
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(string.Concat("Reading file '", inputFilePath, "'"));
                inputFile = File.ReadAllText(inputFilePath);
            }
            catch (Exception ex)
            {
                // Exception reading file
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                return;
            }

            // Compile Ebnf

            string[] output;
            try
            {
                Console.WriteLine("Creating C# Code");
                output = Compiler.CompileEbnf(inputFile, namespaceName);
            }
            catch (Exception ex)
            {
                // Exception compiling file
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error occured generating code:");
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                return;
            }

            // Write data to file

            try
            {
                Console.WriteLine("Saving source code");
                string outputSourceFilePath = string.Concat(outputFilePath, ".source.cs");
                File.WriteAllLines(outputSourceFilePath, output);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Concat("Source code successfully saved to '",
                    Path.GetFullPath(outputSourceFilePath), "'"));
            }
            catch (Exception ex)
            {
                // Exception writing to file
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                return;
            }

            // Compile to DLL file

            try
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Compiling DLL file");
                CompilerResults results = Compiler.CreateDll(output, outputFilePath, keyNameFile);
                if (!results.Errors.HasErrors)
                {
                    // DLL file compiled successfully
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(string.Concat("DLL file successfully saved to '",
                        Path.GetFullPath(results.PathToAssembly), "'"));
                }
                else
                {
                    // There were syntax errors in the generated code,
                    // causing compiler errors
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("The followng compiler errors were found:");
                    for (int i = 0; i < results.Errors.Count; i++)
                    {
                        Console.WriteLine(string.Concat(
                            "- ",
                            results.Errors[i].ErrorText,
                            ", Line ",
                            results.Errors[i].Line,
                            " : Column ",
                            results.Errors[i].Column));
                    }
                    Console.ReadLine();
                    return;
                }
            }
            catch (Exception ex)
            {
                // Exception compiling file
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error occured compiling DLL file:");
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                return;
            }

            // Wait for user to manually end program
            Console.ReadLine();
        }
    }
}