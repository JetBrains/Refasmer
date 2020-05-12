using System.Collections;
using System.Reflection.Metadata;

namespace JetBrains.Refasmer
{
    public static class MetaUtil
    {
        public static string GetName( this MetadataReader reader, EntityHandle entityHandle )
        {
            if (entityHandle.IsNil)
                return null;

            switch (entityHandle.Kind)
            {
                case HandleKind.TypeReference:
                    return reader.GetString(reader.GetTypeReference((TypeReferenceHandle) entityHandle).Name);
                case HandleKind.TypeDefinition:
                    return reader.GetString(reader.GetTypeDefinition((TypeDefinitionHandle) entityHandle).Name);
                default:
                    return null;
            }            
        }

        public static string GetFullname( this MetadataReader reader, EntityHandle entityHandle )
        {
            if (entityHandle.IsNil)
                return null;
            
            switch (entityHandle.Kind)
            {
                case HandleKind.TypeDefinition:
                    var typeDef = reader.GetTypeDefinition((TypeDefinitionHandle) entityHandle);
                    return $"{reader.GetString(typeDef.Namespace)}.{reader.GetString(typeDef.Name)}";
                case HandleKind.TypeReference:
                    var typeRef = reader.GetTypeReference((TypeReferenceHandle) entityHandle);
                    return $"{reader.GetString(typeRef.Namespace)}.{reader.GetString(typeRef.Name)}";
                default:
                    return null;
            }            
        }
        
        public static EntityHandle GetCustomAttrClass( this MetadataReader reader, CustomAttributeHandle attrHandle ) =>
            reader.GetCustomAttrClass(reader.GetCustomAttribute(attrHandle));

        public static EntityHandle GetCustomAttrClass( this MetadataReader reader, CustomAttribute attr )
        {
            switch (attr.Constructor.Kind)
            {
                case HandleKind.MemberReference:
                    return reader.GetMemberReference((MemberReferenceHandle) attr.Constructor).Parent;
                case HandleKind.MethodDefinition:
                    return reader.GetMethodDefinition((MethodDefinitionHandle) attr.Constructor).GetDeclaringType();
                default:
                    return default;
            }
        }
    }
}