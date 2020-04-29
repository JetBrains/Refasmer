using System.Reflection;
using System.Reflection.Metadata;

namespace JetBrains.Refasmer.Filters
{
    public class AllowPublicAndInternals: IImportFilter
    {
        public virtual bool AllowImport( TypeDefinition type, MetadataReader reader )
        {
            switch (type.Attributes & TypeAttributes.VisibilityMask)
            {
                case TypeAttributes.Public:
                case TypeAttributes.NotPublic:
                case TypeAttributes.NestedPublic:
                case TypeAttributes.NestedAssembly:
                case TypeAttributes.NestedFamORAssem:
                    return true;
                case TypeAttributes.NestedFamily:
                case TypeAttributes.NestedFamANDAssem:
                    var declaringType = reader.GetTypeDefinition(type.GetDeclaringType());
                    return (declaringType.Attributes & TypeAttributes.Sealed) == 0;
                default:
                    return false;
            }
        }
        
        public virtual bool AllowImport( MethodDefinition method, MetadataReader reader )
        {
            switch (method.Attributes & MethodAttributes.MemberAccessMask)
            {
                case MethodAttributes.Public:
                case MethodAttributes.Assembly:
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
                case FieldAttributes.Public:
                case FieldAttributes.Assembly:
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