using System;
using System.Collections.Generic;
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
            method != null && !method.IsPublic && !method.IsVirtual && !method.IsConstructor;
        
        public void StripType( TypeDefinition type )
        {
            Debug($"Stripping type {type.Name}");
            using (WithLogPrefix($"[{type.Name}]"))
            {

                var nonPublicMethods = type.Methods.Where(CanDeleteMethod).ToList();
                Trace($"Removing {nonPublicMethods.Count} non public methods");
                type.Methods.RemoveRange(nonPublicMethods);

                var nonPublicProperties = type.Properties
                    .Where(p => CanDeleteMethod(p.GetMethod) &&
                                CanDeleteMethod(p.SetMethod) &&
                                (p.OtherMethods?.All(CanDeleteMethod) ?? true))
                    .ToList();

                Trace($"Removing {nonPublicProperties.Count} non public properties");
                type.Properties.RemoveRange(nonPublicProperties);

                if (!type.IsValueType)
                {
                    var nonPublicFields = type.Fields.Where(m => !m.IsPublic).ToList();
                    Trace($"Removing {nonPublicFields.Count} non public fields from ref type");
                    type.Fields.RemoveRange(nonPublicFields);
                }

                Trace($"Removing bodies from methods");
                foreach (var method in type.Methods.Where(m => m.Body != null))
                {
                    Trace($"  {method.Name}");
                    method.Body.Instructions.Clear();
                    method.Body.Variables.Clear();
                }

                Trace($"Handling nested types");
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

        private static readonly HashSet<string> CoreLibVariants = new HashSet<string>
        {
            "netstandard",
            "mscorlib",
            "System.Private.CoreLib"
        };
        
        public void MakeRefAssembly(AssemblyDefinition assembly)
        {
            StripAssembly(assembly);

            Debug("Adding ReferenceAssemblyAttribute to assembly");
            var refs = assembly.MainModule.AssemblyReferences
                .Select(ar => ar.FullName)
                .ToHashSet();

            Type refAsmAttrType = null;
            AssemblyNameReference reference = null;
            
            foreach (var coreLibVariant in CoreLibVariants)
            {
                reference = assembly.MainModule.AssemblyReferences.SingleOrDefault(ar => ar.Name == coreLibVariant);
                
                if (reference == null)
                    continue;

                var lib = Assembly.Load(reference.FullName);
                refAsmAttrType = lib.GetType("System.Runtime.CompilerServices.ReferenceAssemblyAttribute");

                if (refAsmAttrType != null)
                    break;
            }
            
            if (refAsmAttrType != null)
            {
                Debug($"Adding ReferenceAssemblyAttribute");
                var method = assembly.MainModule.ImportReference(refAsmAttrType.GetConstructor(Type.EmptyTypes));

                var attr = new CustomAttribute(method);
                attr.AttributeType.Scope = reference;
                assembly.CustomAttributes.Add(attr);
            }
            else
            {
                Warning($"Cannot add ReferenceAssemblyAttribute, reference not found");
            }

            Debug("Patching assembly references");
            var refsToDelete = assembly.MainModule.AssemblyReferences
                .Where(ar => !refs.Contains(ar.FullName)).ToList();

            assembly.MainModule.AssemblyReferences.RemoveRange(refsToDelete);

            Debug("Removing all modules but main");
            assembly.Modules.Clear();
            assembly.Modules.Add(assembly.MainModule);
        }

    }
}