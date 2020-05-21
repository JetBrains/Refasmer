using System;
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
    }
}