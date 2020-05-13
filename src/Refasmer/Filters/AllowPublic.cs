using System.Reflection;
using System.Reflection.Metadata;

namespace JetBrains.Refasmer.Filters
{
    public class AllowPublic: IImportFilter
    {
        public virtual bool AllowImport( TypeDefinition type, MetadataReader reader )
        {
            switch (type.Attributes & TypeAttributes.VisibilityMask)
            {
                case TypeAttributes.Public:
                    return true;
                case TypeAttributes.NestedPublic:
                    return AllowImport(reader.GetTypeDefinition(type.GetDeclaringType()), reader);
                case TypeAttributes.NestedFamily:
                case TypeAttributes.NestedFamORAssem:
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
                case MethodAttributes.Public:
                    return true;
                case MethodAttributes.Family:
                case MethodAttributes.FamORAssem:
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
                case FieldAttributes.Public:
                    return true;
                case FieldAttributes.Family:
                case FieldAttributes.FamORAssem:
                    var declaringType = reader.GetTypeDefinition(field.GetDeclaringType());
                    return (declaringType.Attributes & TypeAttributes.Sealed) == 0;
                default: 
                    return false;
            }
        } 
    }
}