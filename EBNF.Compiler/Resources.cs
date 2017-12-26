using System.IO;
using System.Reflection;

namespace Ebnf.Compiler
{
    /// <summary>
    /// Provides functions for reading manifest resources.
    /// </summary>
    internal static class Resources
    {
        /// <summary>
        /// Reads the specified manifest resource as a string.
        /// </summary>
        /// <param name="resourceName">The name of the resource to read.</param>
        public static string ReadResource(string resourceName)
        {
            // Get the current assembly
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Get the stream for the specified resource path
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
                // Read the resource and return the contents
                return reader.ReadToEnd();
        }
    }
}