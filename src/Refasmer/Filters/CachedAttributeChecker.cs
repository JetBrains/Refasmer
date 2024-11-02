using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;

namespace JetBrains.Refasmer.Filters;

public class CachedAttributeChecker
{
    private readonly Dictionary<string, HashSet<EntityHandle>> _attributeConstructors = new();

    public bool HasAttribute( MetadataReader reader, TypeDefinitionHandle typeHandle, string attributeFullName ) =>
        HasAttribute(reader, reader.GetTypeDefinition(typeHandle), attributeFullName);

    public bool HasAttribute( MetadataReader reader, TypeDefinition type, string attributeFullName ) =>
        HasAttribute(reader, type.GetCustomAttributes(), attributeFullName);

    public bool HasAttribute( MetadataReader reader, MethodDefinitionHandle typeHandle, string attributeFullName ) =>
        HasAttribute(reader, reader.GetMethodDefinition(typeHandle), attributeFullName);

    public bool HasAttribute( MetadataReader reader, MethodDefinition type, string attributeFullName ) =>
        HasAttribute(reader, type.GetCustomAttributes(), attributeFullName);

    public bool HasAttribute( MetadataReader reader, FieldDefinitionHandle typeHandle, string attributeFullName ) =>
        HasAttribute(reader, reader.GetFieldDefinition(typeHandle), attributeFullName);

    public bool HasAttribute( MetadataReader reader, FieldDefinition type, string attributeFullName ) =>
        HasAttribute(reader, type.GetCustomAttributes(), attributeFullName);

    public bool HasAttribute( MetadataReader reader, CustomAttributeHandleCollection attrHandles, string attributeFullName )
    {
        if (!_attributeConstructors.TryGetValue(attributeFullName, out var constructorSet))
        {
            constructorSet = new HashSet<EntityHandle>();
            _attributeConstructors[attributeFullName] = constructorSet;
        }
            
        var attrs = attrHandles.Select(reader.GetCustomAttribute).ToList();
                
        if (attrs.Any(attr => constructorSet.Contains(attr.Constructor)))
            return true;

        var compilerGeneratedAttr = attrs
            .Where(attr => reader.GetFullname(reader.GetCustomAttrClass(attr)) == attributeFullName)
            .Select(attr => (CustomAttribute?) attr)
            .FirstOrDefault();

        if (compilerGeneratedAttr == null)
            return false;

        constructorSet.Add(compilerGeneratedAttr.Value.Constructor);
        return true;
    }
}