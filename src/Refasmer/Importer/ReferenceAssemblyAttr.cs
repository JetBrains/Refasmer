using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace JetBrains.Refasmer
{
    public partial class MetadataImporter
    {
        private static readonly byte[] VoidValueBlob = { 1, 0, 0, 0 };
        private static readonly byte[] VoidCtorSignatureBlob = { 0x20, 0, 1 };
        
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

        private MethodDefinitionHandle CreateCustomReferenceAssemblyAttributeCtor()
        {
            MethodDefinitionHandle ctorHandle = default;
            
            EntityHandle objectHandle = default;
                
            if (IsNil(objectHandle))
                objectHandle = _reader.TypeReferences
                    .FirstOrDefault(h => _reader.GetFullname(h) == "System::Object");

            if (IsNil(objectHandle))
                objectHandle = _reader.TypeDefinitions
                    .FirstOrDefault(h => _reader.GetFullname(h) == "System::Object");

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

            return ctorHandle;
        }

        private EntityHandle FindOrCreateReferenceAssemblyAttributeCtor()
        {
            var ctorHandle = FindMethod(AttributeNames.ReferenceAssembly, ".ctor", CheckRefAsmAttrCtorSignature);

            if (!IsNil(ctorHandle))
            {
                Trace?.Invoke($"Found attribute constructor with void signature {_reader.ToString(ctorHandle)}");                
                return Import(ctorHandle);
            }

            var mscorlibRef = FindMscorlibReference();

            if (!IsNil(mscorlibRef))
            {
                var referenceAssemblyAttrTypeRef = _builder.AddTypeReference(mscorlibRef,
                    _builder.GetOrAddString("System.Runtime.CompilerServices"),
                    _builder.GetOrAddString("ReferenceAssemblyAttribute"));

                var ctor = new BlobBuilder();

                new BlobEncoder(ctor).MethodSignature(isInstanceMethod: true).Parameters(0, t => t.Void(), p => { });

                var ctorBlob = _builder.GetOrAddBlob(ctor);

                ctorHandle = _builder.AddMemberReference(referenceAssemblyAttrTypeRef, _builder.GetOrAddString(".ctor"),
                    ctorBlob);
                Trace?.Invoke($"Created ReferenceAssemblyAttribute constructor reference {RowId(ctorHandle)}");

                return ctorHandle;
            }

            return CreateCustomReferenceAssemblyAttributeCtor();
        }
        
        private void AddReferenceAssemblyAttribute()
        {
            Debug?.Invoke("Adding ReferenceAssembly attribute");

            var ctorHandle = FindOrCreateReferenceAssemblyAttributeCtor(); 
            
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