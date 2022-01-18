using System;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace JetBrains.Refasmer
{
    public partial class MetadataImporter
    {
        private int GetNextToken( TableIndex index ) => _builder.GetRowCount(index) + 1;

        private TypeDefinitionHandle NextTypeHandle() =>
            MetadataTokens.TypeDefinitionHandle(GetNextToken(TableIndex.TypeDef));

        private FieldDefinitionHandle NextFieldHandle() =>
            MetadataTokens.FieldDefinitionHandle(GetNextToken(TableIndex.Field));

        private MethodDefinitionHandle NextMethodHandle() =>
            MetadataTokens.MethodDefinitionHandle(GetNextToken(TableIndex.MethodDef));

        private ParameterHandle NextParameterHandle() => MetadataTokens.ParameterHandle(GetNextToken(TableIndex.Param));

        private EventDefinitionHandle NextEventHandle() =>
            MetadataTokens.EventDefinitionHandle(GetNextToken(TableIndex.Event));

        private PropertyDefinitionHandle NextPropertyHandle() =>
            MetadataTokens.PropertyDefinitionHandle(GetNextToken(TableIndex.Property));

        private static readonly Func<object, int?> RowId = MetaUtil.RowId;

        private EntityHandle FindMethod( string fullTypeName, string methodName, Func<BlobReader, bool> checkSignature )
        {
            var typeRefHandle = _reader.TypeReferences
                .SingleOrDefault(h => _reader.GetFullname(h) == fullTypeName);

            if (!IsNil(typeRefHandle))
            {
                return _reader.MemberReferences
                    .Select(mrh => new { mrh, mr = _reader.GetMemberReference(mrh)})
                    .Where(x => x.mr.Parent == typeRefHandle)
                    .Where(x => _reader.GetString(x.mr.Name) == methodName)
                    .Where(x => checkSignature(_reader.GetBlobReader(x.mr.Signature)))
                    .Select(x => x.mrh)
                    .SingleOrDefault();
            }

            var typeDefHandle = _reader.TypeDefinitions
                .SingleOrDefault(h => _reader.GetFullname(h) == fullTypeName);

            if (!IsNil(typeDefHandle))
            {
                return  _reader.GetTypeDefinition(typeDefHandle).GetMethods()
                    .Select(mdh => new { mdh, md = _reader.GetMethodDefinition(mdh)})
                    .Where(x => _reader.GetString(x.md.Name) == methodName)
                    .Where(x => checkSignature(_reader.GetBlobReader(x.md.Signature)))
                    .Select(x => x.mdh)
                    .SingleOrDefault();
            }

            return default;
        }

        private static readonly byte[] MscorlibPublicKeyBlob = { 0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89 };

        private AssemblyReferenceHandle FindMscorlibReference() =>
            _reader.AssemblyReferences
                .SingleOrDefault(r => _reader.GetString(_reader.GetAssemblyReference(r).Name) == "mscorlib");

        private AssemblyReferenceHandle FindSystemRuntimeReference() =>
            _reader.AssemblyReferences
                .SingleOrDefault(r => _reader.GetString(_reader.GetAssemblyReference(r).Name) == "System.Runtime");

        private AssemblyReferenceHandle FindRuntimeReference()
        {
            var result = FindMscorlibReference();
            if (!IsNil(result))
                return result;
            return FindSystemRuntimeReference();
        }
        
        private AssemblyReferenceHandle FindOrCreateMscorlibReference()
        {
            var mscorlibRef = FindMscorlibReference();

            if (!IsNil(mscorlibRef))
                return mscorlibRef;

            mscorlibRef = _builder.AddAssemblyReference(
                _builder.GetOrAddString("mscorlib"),
                new Version(4, 0, 0, 0),
                default, _builder.GetOrAddBlob(MscorlibPublicKeyBlob),
                default, default);
                
            Trace?.Invoke($"Created mscorlib assembly reference {RowId(mscorlibRef)}");

            return mscorlibRef;
        }
    }
}