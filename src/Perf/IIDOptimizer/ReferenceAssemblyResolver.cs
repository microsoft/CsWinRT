using Mono.Cecil;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GuidPatch
{
    class ReferenceAssemblyResolver : DefaultAssemblyResolver
    {
        public ReferenceAssemblyResolver(IEnumerable<FileInfo> references)
        {
            // Typically reference assemblies come in "ref packs" so all of the files in `references` live in the same folder,
            // we can do a small optimization here by only adding unique directories to our custom AssemblyResolver 
            var uniqueDirectories = references
                .Select((reference) => reference.Directory!.FullName)
                .Distinct();

            foreach (var dir in uniqueDirectories) { AddSearchDirectory(dir); }
        }
    }
}
