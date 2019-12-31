using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace JetBrains.Refasmer
{
    public class AssemblyResolver : BaseAssemblyResolver
    {
        private readonly Dictionary<string, AssemblyDefinition> _assemblies = new Dictionary<string, AssemblyDefinition>();

        public AssemblyResolver(IEnumerable<string> referencePaths, bool addSystemPath = true)
        {
            EmptyDirectories();
            
            if (addSystemPath)
                AddSearchDirectory(Path.GetDirectoryName(typeof(string).Assembly.Location));

            foreach (var referencePath in referencePaths)
                AddSearchDirectory(referencePath);
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference reference, ReaderParameters parameters)
        {
            var key = reference.FullName;


            if (_assemblies.TryGetValue(key, out var result))
                return result;
            
            result = base.Resolve(reference, parameters);

            _assemblies[key] = result;
            return result;
        }

        private void EmptyDirectories()
        {
            var field = typeof(BaseAssemblyResolver).GetField("directories", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw new Exception("Can't directories field.");

            var directories = (Collection<string>) field.GetValue(this);
            directories.Clear();

        }
    }
}