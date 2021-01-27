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
    public static class EmbeddedResource
    {
        public static string GetContent(string relativePath, Assembly? assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();
            var baseName = assembly.GetName().Name;
            var resourceName = relativePath
                .TrimStart('.')
                .Replace(Path.DirectorySeparatorChar, '.')
                .Replace(Path.AltDirectorySeparatorChar, '.');

            using var stream = assembly.GetManifestResourceStream($"{baseName}.{resourceName}");

            if (stream == null)
            {
                throw new NotSupportedException("Unable to get embedded resource content, because the stream was null");
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
