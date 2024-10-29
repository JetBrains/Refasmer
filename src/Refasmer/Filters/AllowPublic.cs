using System.Reflection;
using System.Reflection.Metadata;

namespace JetBrains.Refasmer.Filters
{
    public class AllowPublic(bool omitNonApiTypes) : PartialTypeFilterBase(omitNonApiTypes)
    {
        public override bool AllowImport( MethodDefinition method, MetadataReader reader )
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

        public override bool AllowImport( FieldDefinition field, MetadataReader reader )
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