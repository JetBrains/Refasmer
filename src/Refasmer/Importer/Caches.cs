using System.Collections.Generic;
using System.Reflection.Metadata;

namespace JetBrains.Refasmer;

public partial class MetadataImporter 
{
    private readonly Dictionary<ModuleReferenceHandle, ModuleReferenceHandle> _moduleReferenceCache = new();
    private readonly Dictionary<TypeSpecificationHandle, TypeSpecificationHandle> _typeSpecificationCache = new();
    private readonly Dictionary<DeclarativeSecurityAttributeHandle, DeclarativeSecurityAttributeHandle> _declarativeSecurityAttributeCache = new();
    private readonly Dictionary<ExportedTypeHandle, ExportedTypeHandle> _exportedTypeCache = new();
    private readonly Dictionary<CustomAttributeHandle, CustomAttributeHandle> _customAttributeCache = new();
    private readonly Dictionary<MethodImplementationHandle, MethodImplementationHandle> _methodImplementationCache = new();
    private readonly Dictionary<MemberReferenceHandle, MemberReferenceHandle> _memberReferenceCache = new();
    private readonly Dictionary<TypeReferenceHandle, TypeReferenceHandle> _typeReferenceCache = new();
    private readonly Dictionary<AssemblyReferenceHandle, AssemblyReferenceHandle> _assemblyReferenceCache = new();
    private readonly Dictionary<AssemblyFileHandle, AssemblyFileHandle> _assemblyFileCache = new();
    private readonly Dictionary<InterfaceImplementationHandle, InterfaceImplementationHandle> _interfaceImplementationCache = new();
    private readonly Dictionary<GenericParameterHandle, GenericParameterHandle> _genericParameterCache = new();
    private readonly Dictionary<GenericParameterConstraintHandle, GenericParameterConstraintHandle> _genericParameterConstraintCache = new(); 
    private readonly Dictionary<TypeDefinitionHandle, TypeDefinitionHandle> _typeDefinitionCache = new();
    private readonly Dictionary<MethodDefinitionHandle, MethodDefinitionHandle> _methodDefinitionCache = new();
    private readonly Dictionary<FieldDefinitionHandle, FieldDefinitionHandle> _fieldDefinitionCache = new();
    private readonly Dictionary<PropertyDefinitionHandle, PropertyDefinitionHandle> _propertyDefinitionCache = new();
    private readonly Dictionary<EventDefinitionHandle, EventDefinitionHandle> _eventDefinitionCache = new();
    private readonly Dictionary<ParameterHandle, ParameterHandle> _parameterCache = new();
}