using System.Reflection;
using System.Reflection.Metadata;

namespace JetBrains.Refasmer.Filters;

public class AllowPublicAndInternals(bool omitNonApiMembers) : PartialTypeFilterBase(omitNonApiMembers)
{
    public override bool AllowImport(TypeDefinition type, MetadataReader reader)
    {
        if (!base.AllowImport(type, reader)) return false;
        if (!OmitNonApiMembers) return true;
            
        switch (type.Attributes & TypeAttributes.VisibilityMask)
        {
            case TypeAttributes.NotPublic:
                return !AttributeCache.HasAttribute(reader, type.GetCustomAttributes(), FullNames.CompilerGenerated);
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
        
    public override bool AllowImport( MethodDefinition method, MetadataReader reader )
    {
        switch (method.Attributes & MethodAttributes.MemberAccessMask)
        {
            case MethodAttributes.Assembly:
                if ((method.Attributes & MethodAttributes.SpecialName) != 0)
                    return true;
                return !AttributeCache.HasAttribute(reader, method, FullNames.CompilerGenerated);

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
                return !AttributeCache.HasAttribute(reader, field, FullNames.CompilerGenerated);
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