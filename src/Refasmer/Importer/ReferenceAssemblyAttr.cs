using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace JetBrains.Refasmer
{
    public partial class MetadataImporter
    {
        private static readonly byte[] VoidValueBlob = { 1, 0, 0, 0 };
        private static readonly byte[] VoidCtorSignatureBlob = { 20, 0, 1 };
        
        private static bool CheckRefASmAttrConstructorSignature( BlobReader blobReader )
        {
            var header = blobReader.ReadSignatureHeader();

            if (header.Kind != SignatureKind.Method)
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
            EntityHandle ctorHandle = default;
            
            var attrTypeRefHandle = _reader.TypeReferences
                .SingleOrDefault(h => _reader.GetFullname(h) == AttributeNames.ReferenceAssembly);

            var attrTypeDefHandle = _reader.TypeDefinitions
                .SingleOrDefault(h => _reader.GetFullname(h) == AttributeNames.ReferenceAssembly);

            if (!IsNil(attrTypeRefHandle))
            {
                Trace?.Invoke($"Found attribute type {_reader.ToString(attrTypeRefHandle)}");
                ctorHandle = _reader.MemberReferences
                    .Select(mrh => new { mrh, mr = _reader.GetMemberReference(mrh)})
                    .Where(x => x.mr.Parent == attrTypeRefHandle)
                    .Where(x => _reader.GetString(x.mr.Name) == ".ctor")
                    .Where(x => CheckRefASmAttrConstructorSignature(_reader.GetBlobReader(x.mr.Signature)))
                    .Select(x => x.mrh)
                    .SingleOrDefault();

                Trace?.Invoke(IsNil(ctorHandle)
                    ? "Not found attribute constructor with void signature"
                    : $"Found attribute constructor with void signature {_reader.ToString(ctorHandle)}");
                
                ctorHandle = Import(ctorHandle);
            }
            else if (!IsNil(attrTypeDefHandle))
            {
                Trace?.Invoke($"Found attribute type {_reader.ToString(attrTypeRefHandle)}");
                ctorHandle = _reader.GetTypeDefinition(attrTypeDefHandle).GetMethods()
                    .Select(mdh => new { mdh, md = _reader.GetMethodDefinition(mdh)})
                    .Where(x => _reader.GetString(x.md.Name) == ".ctor")
                    .Where(x => CheckRefASmAttrConstructorSignature(_reader.GetBlobReader(x.md.Signature)))
                    .Select(x => x.mdh)
                    .SingleOrDefault();
                
                Trace?.Invoke(IsNil(ctorHandle)
                    ? "Not found attribute constructor with void signature"
                    : $"Found attribute constructor with void signature {_reader.ToString(ctorHandle)}");

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