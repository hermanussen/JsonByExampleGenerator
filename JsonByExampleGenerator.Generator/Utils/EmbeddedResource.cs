using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace JsonByExampleGenerator.Generator.Utils
{
    /// <summary>
    /// Implementation of this helper method adapted from:
    /// https://www.cazzulino.com/source-generators.html
    /// </summary>
    internal static class EmbeddedResource
    {
        public static string GetContent(string relativePath)
        {
            var baseName = Assembly.GetExecutingAssembly().GetName().Name;
            var resourceName = relativePath
                .TrimStart('.')
                .Replace(Path.DirectorySeparatorChar, '.')
                .Replace(Path.AltDirectorySeparatorChar, '.');

            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(baseName + "." + resourceName);

            if (stream == null)
            {
                throw new NotSupportedException("Unable to get embedded resource content, because the stream was null");
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
