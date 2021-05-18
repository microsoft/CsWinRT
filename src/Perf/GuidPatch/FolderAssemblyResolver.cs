using Mono.Cecil;
using System;
using System.IO;
using System.Linq;

namespace GuidPatch
{
    class FolderAssemblyResolver : DefaultAssemblyResolver
    {
        public FolderAssemblyResolver(params DirectoryInfo[] directories)
        {
            foreach (var file in directories.SelectMany(d => d.EnumerateFiles("*.dll")))
            {
                try
                {
                    var definition = AssemblyDefinition.ReadAssembly(file.FullName, new ReaderParameters(ReadingMode.Deferred)
                    {
                        InMemory = true,
                        ReadWrite = false,
                        AssemblyResolver = this
                    });
                    RegisterAssembly(definition);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
