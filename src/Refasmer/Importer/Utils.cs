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
    }
}