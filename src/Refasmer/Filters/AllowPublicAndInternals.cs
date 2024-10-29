using System.Reflection;
using System.Reflection.Metadata;

namespace JetBrains.Refasmer.Filters
{
    public class AllowPublicAndInternals(bool omitNonApiTypes) : PartialTypeFilterBase(omitNonApiTypes)
    {
        private readonly CachedAttributeChecker _attrChecker = new();
        
        public override bool AllowImport( MethodDefinition method, MetadataReader reader )
        {
            switch (method.Attributes & MethodAttributes.MemberAccessMask)
            {
                case MethodAttributes.Assembly:
                    if ((method.Attributes & MethodAttributes.SpecialName) != 0)
                        return true;
                    return !_attrChecker.HasAttribute(reader, method, FullNames.CompilerGenerated);

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

        public override bool AllowImport( FieldDefinition field, MetadataReader reader )
        {
            switch (field.Attributes & FieldAttributes.FieldAccessMask)
            {
                case FieldAttributes.Assembly:
                    return !_attrChecker.HasAttribute(reader, field, FullNames.CompilerGenerated);
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