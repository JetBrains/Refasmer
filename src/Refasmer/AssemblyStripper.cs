using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace JetBrains.Refasmer
{
    public class AssemblyStripper : LoggerBase
    {
        public AssemblyStripper(LoggerBase loggerBase) : base(loggerBase)
        {
        }

        private static bool CanDeleteMethod(MethodDefinition method) => 
            method != null && !method.IsPublic && !method.IsVirtual;
        
        public void StripType( TypeDefinition type )
        {
            using (WithLogPrefix($"[{type.Name}]"))
            {
                Debug("Stripping type");

                var nonPublicMethods = type.Methods.Where(CanDeleteMethod).ToList();
                Debug($"Removing {nonPublicMethods.Count} non public methods");
                type.Methods.RemoveRange(nonPublicMethods);

                var nonPublicProperties = type.Properties
                    .Where(p => CanDeleteMethod(p.GetMethod) &&
                                CanDeleteMethod(p.SetMethod) &&
                                (p.OtherMethods?.All(CanDeleteMethod) ?? true))
                    .ToList();

                Debug($"Removing {nonPublicProperties.Count} non public properties");
                type.Properties.RemoveRange(nonPublicProperties);

                if (!type.IsValueType)
                {
                    var nonPublicFields = type.Fields.Where(m => !m.IsPublic).ToList();
                    Debug($"Removing {nonPublicFields.Count} non public fields from ref type");
                    type.Fields.RemoveRange(nonPublicFields);
                }

                Debug($"Removing bodies from methods");
                foreach (var method in type.Methods.Where(m => m.Body != null))
                {
                    Debug($"  {method.Name}");
                    method.Body.Instructions.Clear();
                    method.Body.Variables.Clear();
                }

                Debug($"Handling nested types");
                foreach (var nestedType in type.NestedTypes)
                    StripType(nestedType);
            }
        }

        public void StripAssembly(AssemblyDefinition assembly)
        {
            Debug($"Stripping assembly {assembly.Name}");
            foreach (var type in assembly.MainModule.Types)
                StripType(type);

            Debug("Removing resources");
            assembly.MainModule.Resources.Clear();
        }

        public void MakeRefAssembly(AssemblyDefinition assembly)
        {
            using (WithLogPrefix($"[{assembly.Name}]"))
            {
                StripAssembly(assembly);

                Debug("Adding ReferenceAssemblyAttribute to assembly");
                var refs = assembly.MainModule.AssemblyReferences
                    .Select(ar => ar.FullName)
                    .ToHashSet();

                var mscorlib =
                    Assembly.Load("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
                var attrType = mscorlib.GetType("System.Runtime.CompilerServices.ReferenceAssemblyAttribute");
                var method = assembly.MainModule.ImportReference(attrType.GetConstructor(Type.EmptyTypes));

                var attr = new CustomAttribute(method);
                attr.AttributeType.Scope =
                    assembly.MainModule.AssemblyReferences.Single(ar => ar.FullName == mscorlib.FullName);
                assembly.CustomAttributes.Add(attr);

                Debug("Patching assembly references");
                var refsToDelete = assembly.MainModule.AssemblyReferences
                    .Where(ar => !refs.Contains(ar.FullName)).ToList();

                assembly.MainModule.AssemblyReferences.RemoveRange(refsToDelete);
            }
        }
        
    }
}