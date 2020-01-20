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
            method != null && method.IsPrivate && !method.IsVirtual && !method.IsConstructor;
        
        public void StripType( TypeDefinition type )
        {
            Debug($"Stripping type {type.Name}");
            using (WithLogPrefix($"[{type.Name}]"))
            {

                var methodsToDelete = type.Methods.Where(CanDeleteMethod).ToList();
                Trace($"Removing {methodsToDelete.Count} methods");
                type.Methods.RemoveRange(methodsToDelete);

                var nonPublicProperties = type.Properties
                    .Where(p => CanDeleteMethod(p.GetMethod) &&
                                CanDeleteMethod(p.SetMethod) &&
                                (p.OtherMethods?.All(CanDeleteMethod) ?? true))
                    .ToList();

                Trace($"Removing {nonPublicProperties.Count} non public properties");
                type.Properties.RemoveRange(nonPublicProperties);

                if (!type.IsValueType)
                {
                    var fieldsToDelete = type.Fields.Where(m => m.IsPrivate).ToList();
                    Trace($"Removing {fieldsToDelete.Count} fields from ref type");
                    type.Fields.RemoveRange(fieldsToDelete);
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
            AssemblyNameReference attributeReference = null;
            
            foreach (var coreLibVariant in CoreLibVariants)
                foreach (var reference in assembly.MainModule.AssemblyReferences.Where(ar => ar.Name == coreLibVariant).OrderByDescending(ar => ar.Version))
                {
                    var lib = Assembly.Load(reference.FullName);
                    refAsmAttrType = lib.GetType("System.Runtime.CompilerServices.ReferenceAssemblyAttribute");

                    if (refAsmAttrType != null)
                    {
                        attributeReference = reference;                        
                        break;
                    }
                }
            
            if (refAsmAttrType != null)
            {
                Debug($"Adding ReferenceAssemblyAttribute");
                var method = assembly.MainModule.ImportReference(refAsmAttrType.GetConstructor(Type.EmptyTypes));

                var attr = new CustomAttribute(method);
                attr.AttributeType.Scope = attributeReference;
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