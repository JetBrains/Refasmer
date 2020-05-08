using System.Collections.Generic;
using System.Reflection;
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

        private EntityHandle GetCustomAttrClass( CustomAttributeHandle attrHandle ) =>
            GetCustomAttrClass(_reader.GetCustomAttribute(attrHandle));
        
        private EntityHandle GetCustomAttrClass( CustomAttribute attr )
        {
            switch (attr.Constructor.Kind)
            {
                case HandleKind.MemberReference:
                    return _reader.GetMemberReference((MemberReferenceHandle) attr.Constructor).Parent;
                case HandleKind.MethodDefinition:
                    return _reader.GetMethodDefinition((MethodDefinitionHandle) attr.Constructor).GetDeclaringType();
                default:
                    return default;
            }
            
        }
        
    }
    
    
    
    public static class MetadataReaderExtensions
    {
        public static IEnumerable<TypeSpecificationHandle> TypeSpecifications( this MetadataReader reader )
        {
            for (var n = 1; n <= reader.GetTableRowCount(TableIndex.TypeSpec); n++)
                yield return MetadataTokens.TypeSpecificationHandle(n);
        }
    }
}