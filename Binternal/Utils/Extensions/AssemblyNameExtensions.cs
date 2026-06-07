using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Binternal.Utils.Extensions;

internal static class AssemblyNameExtensions
{
    extension(AssemblyName)
    {
        public static AssemblyName? TryGetAssemblyName(string filePath)
        {
            try
            {
                var assemblyName = AssemblyName.GetAssemblyName(filePath);
                return assemblyName;
            }
            catch
            {
                return null;
            }
        }

        public static IReadOnlyList<AssemblyName> GetReferencedAssemblyNames(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var reader = new PEReader(stream);

            if (!reader.HasMetadata)
                return [];

            var metadata = reader.GetMetadataReader();
            if (!metadata.IsAssembly)
                return [];

            return metadata
                .AssemblyReferences.Select(metadata.GetAssemblyReference)
                .Select(r => r.GetAssemblyName())
                .ToArray();
        }

        public static IReadOnlyList<AssemblyName>? TryGetReferencedAssemblyNames(string filePath)
        {
            try
            {
                return GetReferencedAssemblyNames(filePath);
            }
            catch
            {
                return null;
            }
        }
    }
}
