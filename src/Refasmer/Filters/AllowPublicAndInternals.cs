using System.Reflection;
using System.Reflection.Metadata;

namespace JetBrains.Refasmer.Filters
{
    public class AllowPublicAndInternals: IImportFilter
    {
        private readonly CachedAttributeChecker _attrChecker = new CachedAttributeChecker();
        
        public virtual bool AllowImport( TypeDefinition type, MetadataReader reader )
        {
            switch (type.Attributes & TypeAttributes.VisibilityMask)
            {
                case TypeAttributes.NotPublic:
                    return !_attrChecker.HasAttribute(reader, type.GetCustomAttributes(), AttributeNames.CompilerGenerated);
                case TypeAttributes.Public:
                    return true;
                case TypeAttributes.NestedPublic:
                case TypeAttributes.NestedAssembly:
                case TypeAttributes.NestedFamORAssem:
                    return AllowImport(reader.GetTypeDefinition(type.GetDeclaringType()), reader);
                case TypeAttributes.NestedFamily:
                case TypeAttributes.NestedFamANDAssem:
                    var declaringType = reader.GetTypeDefinition(type.GetDeclaringType());
                    return (declaringType.Attributes & TypeAttributes.Sealed) == 0 && AllowImport(declaringType, reader);
                default:
                    return false;
            }
        }
        
        public virtual bool AllowImport( MethodDefinition method, MetadataReader reader )
        {
            switch (method.Attributes & MethodAttributes.MemberAccessMask)
            {
                case MethodAttributes.Assembly:
                    if ((method.Attributes & MethodAttributes.SpecialName) != 0)
                        return true;
                    return !_attrChecker.HasAttribute(reader, method, AttributeNames.CompilerGenerated);

                case MethodAttributes.Public:
                case MethodAttributes.FamORAssem:
                    return true;
                case MethodAttributes.Family:
                case MethodAttributes.FamANDAssem:
                    var declaringType = reader.GetTypeDefinition(method.GetDeclaringType());
                    return (declaringType.Attributes & TypeAttributes.Sealed) == 0;
                default: 
                    return false;
            }
        }

        public virtual bool AllowImport( FieldDefinition field, MetadataReader reader )
        {
            switch (field.Attributes & FieldAttributes.FieldAccessMask)
            {
                case FieldAttributes.Assembly:
                    return !_attrChecker.HasAttribute(reader, field, AttributeNames.CompilerGenerated);
                case FieldAttributes.Public:
                case FieldAttributes.FamORAssem:
                    return true;
                case FieldAttributes.Family:
                case FieldAttributes.FamANDAssem:
                    var declaringType = reader.GetTypeDefinition(field.GetDeclaringType());
                    return (declaringType.Attributes & TypeAttributes.Sealed) == 0;
                default: 
                    return false;
            }
        } 
    }
}