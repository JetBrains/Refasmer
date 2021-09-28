using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace JetBrains.Refasmer
{
    public partial class MetadataImporter
    {
        private static readonly byte[] VoidValueBlob = { 1, 0, 0, 0 };
        private static readonly byte[] VoidCtorSignatureBlob = { 20, 0, 1 };
        
        private static bool CheckRefAsmAttrCtorSignature( BlobReader blobReader )
        {
            var header = blobReader.ReadSignatureHeader();

            if (header.Kind != SignatureKind.Method)
                return false;
            if (!header.IsInstance)
                return false;
            if (header.IsGeneric)
                return false;

            var parameterCount = blobReader.ReadCompressedInteger();
            if (parameterCount != 0)
                return false;

            return true;
        }
        
        private void AddReferenceAssemblyAttribute()
        {
            Debug?.Invoke("Adding ReferenceAssembly attribute");

            var ctorHandle = FindMethod(AttributeNames.ReferenceAssembly, ".ctor", CheckRefAsmAttrCtorSignature);

            if (!IsNil(ctorHandle))
            {
                Trace?.Invoke($"Found attribute constructor with void signature {_reader.ToString(ctorHandle)}");                
                ctorHandle = Import(ctorHandle);
            }
            else
            {
                EntityHandle objectHandle = default;
                
                if (IsNil(objectHandle))
                    objectHandle = _reader.TypeReferences
                    .SingleOrDefault(h => _reader.GetFullname(h) == "System::Object");

                if (IsNil(objectHandle))
                    objectHandle = _reader.TypeDefinitions
                    .SingleOrDefault(h => _reader.GetFullname(h) == "System::Object");

                if (!IsNil(objectHandle))
                {
                    Trace?.Invoke($"Found System::Object type {_reader.ToString(objectHandle)}");

                    objectHandle = Import(objectHandle);
                    
                    _builder.AddTypeDefinition(TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.NotPublic,
                        _builder.GetOrAddString("System.Runtime.CompilerServices"),
                        _builder.GetOrAddString("ReferenceAssemblyAttribute"),
                        objectHandle, NextFieldHandle(), NextMethodHandle());

                    ctorHandle = _builder.AddMethodDefinition(
                        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                        MethodAttributes.RTSpecialName,
                        MethodImplAttributes.Managed, _builder.GetOrAddString(".ctor"), _builder.GetOrAddBlob(VoidCtorSignatureBlob), -1,
                        NextParameterHandle());

                    Trace?.Invoke($"Created attribute constructor with void signature {RowId(ctorHandle)}");
                }
                else
                {
                    Trace?.Invoke("Not found System::Object type");
                }
            }

            if (IsNil(ctorHandle))
            {
                Debug?.Invoke("Cannot add ReferenceAssembly attribute - no constructor");
            }
            else
            {
                var attrHandle = _builder.AddCustomAttribute(Import(EntityHandle.AssemblyDefinition), ctorHandle, _builder.GetOrAddBlob(VoidValueBlob));
                Debug?.Invoke($"Added ReferenceAssembly attribute {RowId(attrHandle):X}");
            }
        }   
    }
}