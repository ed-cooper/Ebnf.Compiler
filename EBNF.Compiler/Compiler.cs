using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CSharp;

namespace Ebnf.Compiler
{
    /// <summary>
    /// Provides methods for compiling a series of Extended Backus-Naur Form statements.
    /// </summary>
    /// <remarks>
    /// Uses Ebnf specification ISO/IEC 14977 : 1996(E)
    /// http://www.cl.cam.ac.uk/~mgk25/iso-14977.pdf
    /// </remarks>
    internal static class Compiler
    {
        #region Regular Expressions

        // Finds non-essential data in Ebnf (whitespace, comments)
        private static readonly Regex FindNonEssentialData = new Regex(@"\s+|\(\*.*\*\)");

        // Finds separators between different definitions (excludes separators in square brackets)
        private static readonly Regex FindDefinitions = new Regex(@"\|(?![^\[]*\])");

        // Finds concatenations (excludes concatenations inside suare or curly brackets)
        private static readonly Regex FindConcatenations = new Regex(@",(?![^\[\{]*(\]|\}))");

        #endregion

        #region Methods

        /// <summary>
        /// Compiles the provided Ebnf statement into C# code.
        /// </summary>
        /// <param name="rawData">The raw Ebnf statements.</param>
        /// <param name="namespaceName">The name of the default namespace.</param>
        /// <returns>A C# static class Validation which contains code to check whether the provided input is valid.</returns>
        public static string[] CompileEbnf(string rawData, string namespaceName)
        {
            // Init

            // Create inital list of string[], which will store all the produced code
            List<string> code = new List<string>();

            // File header

            code.Add(Resources.ReadResource("Ebnf.Compiler.Resources.Header.cs")
                .Replace("{datetime}", DateTime.Now.ToString(CultureInfo.CurrentCulture)));
            code.Add($"namespace {namespaceName}");
            code.Add("{");

            // Class header

            code.Add("\t/// <summary>");
            code.Add($"\t/// Provides methods for validating a string to the {namespaceName} specification.");
            code.Add("\t/// </summary>");
            code.Add("\tpublic static class Validation");
            code.Add("\t{");

            // Get statements

            // Filter non-essential data and split statements by semi-colon
            string[] statements = FindNonEssentialData.Replace(rawData, "").Split(';');

            // Compile statements

            // Create empty list of identifiers
            List<string> identifiers = new List<string>();

            // Loop through each statement
            foreach (string statement in statements.Where(statement => !string.IsNullOrEmpty(statement)))
            {
                code.AddRange(CompileEbnfStatement(statement, out string identifier));
                identifiers.Add(identifier);
            }

            // Add class footer

            code.Add("\t}");
            code.Add("");

            // Add node class

            code.Add(Resources.ReadResource("Ebnf.Compiler.Resources.NodeClass.cs"));

            // Create NodeType enum

            code.AddRange(CreateNodeTypeEnum(identifiers.ToArray(), statements));

            // Add file footer

            code.Add("}");

            // Output data

            return code.ToArray();
        }

        /// <summary>
        /// Compiles the specified Ebnf statement.
        /// </summary>
        /// <param name="statement">The Ebnf statement to compile.</param>
        /// <param name="identifier">The identifier of the Ebnf statement.</param>
        /// <param name="nodeType">The node type to use; defaults to the statement identifer.</param>
        /// <returns>A C# function able to determine whether an input matches the Ebnf statement.</returns>
        private static IEnumerable<string> CompileEbnfStatement(string statement, out string identifier,
            string nodeType = "")
        {
            // Find position of the defining symbol, =, separating the meta identifier and the definitions list
            int equalsPos = statement.IndexOf("=", StringComparison.Ordinal);

            if (equalsPos == -1)
                throw new ArgumentException($"Ebnf statement did not contain an equals symbol: '{statement}'");

            // Statement contains equals

            // Find meta identifier
            identifier = statement.Substring(0, equalsPos);

            // Create initial list for code output
            List<string> output = new List<string>
            {
                "\t\t/// <summary>",
                $"\t\t/// {statement}",
                "\t\t/// </summary>",
                "\t\t/// <param name=\"input\">The input string to validate.</param>",
                "\t\t/// <param name=\"remainder\">The part of the string that did not match the given rule set; empty if full match.</param>",
                "\t\t/// <param name=\"parseTree\">The parse tree produced from the input.</param>",
                "\t\t/// <param name=\"level\">The internal level of recursion; starts at 0.</param>",
                $"\t\t{(nodeType == "" ? "public" : "private")} static bool Is{identifier}(string input, out string remainder, out Node parseTree, int level = 0)",
                "\t\t{",
                $"\t\t\tparseTree = new Node(NodeType.{(nodeType == "" ? identifier : nodeType)});",
                "\t\t\tNode newNode;" // Create newNode regardless of whether a parse tree is generated, as it is a required input
            };

            // Find definitions list
            string definitionsRaw = statement.Substring(equalsPos + 1);

            // Separate definitions
            string[] definitions = FindDefinitions.Split(definitionsRaw);

            // Create list of dependencies required by the function (populated by CompileEbnfSingleDefinition)
            List<Tuple<string, string>> dependencies = new List<Tuple<string, string>>();

            // Loop through definitions
            foreach (string definition in definitions)
                CompileEbnfSingleDefinition(definition, identifier, output, dependencies);

            // Add function footer
            output.Add("\t\t\tremainder = input;");
            output.Add("\t\t\treturn false;");
            output.Add("\t\t}");
            output.Add("\t\t");

            // Loop through dependencies
            foreach (Tuple<string, string> dependency in dependencies)
            {
                // Treat dependency as individual Ebnf statement
                output.AddRange(
                    CompileEbnfStatement(
                        $"{dependency.Item1.Substring(2)}={dependency.Item2}", out string _, identifier));
            }

            // Return compiled code
            return output.ToArray();
        }

        /// <summary>
        /// Compiles a single definition within an Ebnf statement.
        /// </summary>
        /// <param name="definition">The single definition to compile.</param>
        /// <param name="identifier">The identifier of the Ebnf statement.</param>
        /// <param name="code">The list containing C# code to append to.</param>
        /// <param name="dependencies">The list of all the dependencies required by the single definition.</param>
        private static void CompileEbnfSingleDefinition(string definition, string identifier, ICollection<string> code,
            ICollection<Tuple<string, string>> dependencies)
        {
            // Reset remainder as no match found previously
            code.Add("\t\t\tremainder = input;");

            // Find all concatenation (,) operations and split the defintion accordingly
            string[] concatenations = FindConcatenations.Split(definition);

            // The current level of indentation
            int indentLevel = 0;

            // For each concatenation
            foreach (string concatenation in concatenations)
            {
                // Create comment containing the Ebnf for the concatenation
                code.Add($"{new string('\t', 3 + indentLevel)}// {concatenation}");

                // Test the type of the concatenation
                switch (concatenation[0])
                {
                    case '"': // Terminal (constant string value)
                        code.Add($"{new string('\t', 3 + indentLevel)}if (remainder.StartsWith({concatenation}))");
                        code.Add($"{new string('\t', 3 + indentLevel)}{{");
                        indentLevel++;
                        code.Add($"{new string('\t', 3 + indentLevel)}remainder = remainder.Substring({concatenation.Length - 2});");
                        break;
                    case '!': // Regex Value
                        
                        code.Add($"var regex = new Regex({concatenation.Remove(0, 1)});");
                        code.Add($"{new string('\t', 3 + indentLevel)}var match = regex.Match(remainder);");
                        code.Add($"{new string('\t', 3 + indentLevel)}if (match.Success)");
                        code.Add($"{new string('\t', 3 + indentLevel)}{{");
                        indentLevel++;
                        code.Add($"{new string('\t', 3 + indentLevel)}if (remainder.StartsWith(\" \"))");
                        code.Add($"{new string('\t', 3 + indentLevel)}" + "{");
                        indentLevel++;
                        code.Add($"{new string('\t', 3 + indentLevel)}remainder = remainder.Remove(0, match.Groups[0].Value.Length + 2);");
                        indentLevel--;
                        code.Add($"{new string('\t', 3 + indentLevel)}}}");
                        code.Add($"{new string('\t', 3 + indentLevel)}else");
                        code.Add($"{new string('\t', 3 + indentLevel)}" + "{");
                        indentLevel++;
                        code.Add($"{new string('\t', 3 + indentLevel)}remainder = remainder.Remove(0, match.Groups[0].Value.Length);");
                        indentLevel--;

                    code.Add($"{new string('\t', 3 + indentLevel)}" + "}");

                        code.Add($"{new string('\t', 3 + indentLevel)}parseTree.Value = match.Groups[0].Value;");
                        code.Add($"{new string('\t', 3 + indentLevel)}return true;");

                        // Close all nested if statements
                        for (int i = indentLevel - 1; i >= 0; i--)
                            code.Add($"{new string('\t', 3 + i)}}}");

                        // Reset parseTree children if match was not found
                        code.Add("\t\t\tparseTree.Children.Clear();");

                        return;
                        break;
                    case '[': // Optional (can be used 0 or 1 times)
                    {
                        // Use dependency function
                        string functionName = $"Is{identifier}SubDef{dependencies.Count + 1}";
                        code.Add($"{new string('\t', 3 + indentLevel)}if ({functionName}(remainder, out remainder, out newNode, level))");
                        code.Add($"{new string('\t', 4 + indentLevel)}parseTree.Children.AddRange(newNode.Children);");
                        dependencies.Add(new Tuple<string, string>(
                            functionName,
                            concatenation.Substring(1, concatenation.Length - 2)
                        ));
                        break;
                    }
                    case '{': // Repetition (can be used 0 or more times)
                    {
                        // Use dependency function
                        string functionName = $"Is{identifier}SubDef{dependencies.Count + 1}";
                        code.Add($"{new string('\t', 3 + indentLevel)}while ({functionName}(remainder, out remainder, out newNode, level))");
                        code.Add($"{new string('\t', 4 + indentLevel)}parseTree.Children.AddRange(newNode.Children);");
                        dependencies.Add(new Tuple<string, string>(
                            functionName,
                            concatenation.Substring(1, concatenation.Length - 2)
                        ));
                        break;
                    }
                    default: // Non-terminal (calls another function)
                        code.Add($"{new string('\t', 3 + indentLevel)}if (Is{concatenation}(remainder, out remainder, out newNode, level + 1))");
                        code.Add($"{new string('\t', 3 + indentLevel)}{{");
                        indentLevel++;
                        code.Add($"{new string('\t', 3 + indentLevel)}parseTree.Children.Add(newNode);");
                        break;
                }
            }

            // Create code for successul outcomes
            code.Add($"{new string('\t', 3 + indentLevel)}parseTree.Value = input.Substring(0, input.Length - remainder.Length);");
            code.Add($"{new string('\t', 3 + indentLevel)}return true;");

            // Close all nested if statements
            for (int i = indentLevel - 1; i >= 0; i--)
                code.Add($"{new string('\t', 3 + i)}}}");

            // Reset parseTree children if match was not found
            code.Add("\t\t\tparseTree.Children.Clear();");
        }

        /// <summary>
        /// Creates the NodeType enum, which enumerates all the possible types of node.
        /// </summary>
        /// <param name="identifiers">All the Ebnf identifiers found, including any identifiers of dependency functions.</param>
        /// <param name="statements">The corresponding Ebnf statement to each identifier.</param>
        private static IEnumerable<string> CreateNodeTypeEnum(IReadOnlyList<string> identifiers,
            IReadOnlyList<string> statements)
        {
            // Create list to contain C# code output
            List<string> output = new List<string>
            {
                "\t/// <summary>",
                "\t/// Represents all the possible types of node in the parse tree.",
                "\t/// </summary>",
                "\tpublic enum NodeType",
                "\t{"
            };

            // Create enum header

            // List enum items
            for (int i = 0; i < identifiers.Count; i++)
            {
                output.Add("\t\t/// <summary>");
                output.Add($"\t\t/// {statements[i]}");
                output.Add("\t\t/// </summary>");
                output.Add($"\t\t{identifiers[i]},");
            }

            // Enum footer
            output.Add("\t}");

            // Output code
            return output.ToArray();
        }

        /// <summary>
        /// Creates a DLL program from the supplied code.
        /// </summary>
        /// <param name="code">The code to compile into the DLL file.</param>
        /// <param name="filePath">The path to save the DLL file to.</param>
        /// <param name="keyNameFile">TThe path to the SNK file used to sign the assembly.</param>
        public static CompilerResults CreateDll(string[] code, string filePath, string keyNameFile = "")
        {
            var providerOptions = new Dictionary<string, string>();
            providerOptions.Add("CompilerVersion", "v3.5");

            var codeProvider = new CSharpCodeProvider(providerOptions);

            // Create the parameters to use for compiling
            CompilerParameters parameters = new CompilerParameters
            {
                GenerateExecutable = false, // We just want a dll - not an exe file
                OutputAssembly = filePath, // Save the dll to our supplied file path
                GenerateInMemory = false // Generates the dll as a physical file on the disk
            };

            // Compiler options:
            // - doc: Generates the xml documentation file
            // - optimize: Optimises the dll file
            // - keyfile: The file path of the snk file

            parameters.CompilerOptions =
                $"/doc:\"{Path.GetDirectoryName(Path.GetFullPath(filePath))}\\{Path.GetFileNameWithoutExtension(filePath)}.xml\" /optimize";
            parameters.ReferencedAssemblies.Add(typeof(Regex).Assembly.FullName);

            if (!string.IsNullOrEmpty(keyNameFile))
                parameters.CompilerOptions += $" /keyfile:\"{keyNameFile}\"";

            // Compile the DLL and return the results
            return codeProvider.CompileAssemblyFromSource(parameters, string.Join("\r\n", code));
        }

        #endregion
    }
}
