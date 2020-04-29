using System.Collections.Generic;
using System.Reflection.Metadata;

namespace JetBrains.Refasmer
{
    public partial class MetadataImporter 
    {
        private readonly Dictionary<ModuleReferenceHandle, ModuleReferenceHandle> _moduleReferenceCache =
            new Dictionary<ModuleReferenceHandle, ModuleReferenceHandle>();
        private readonly Dictionary<TypeSpecificationHandle, TypeSpecificationHandle> _typeSpecificationCache =
            new Dictionary<TypeSpecificationHandle, TypeSpecificationHandle>();
        private readonly Dictionary<DeclarativeSecurityAttributeHandle, DeclarativeSecurityAttributeHandle>
            _declarativeSecurityAttributeCache =
                new Dictionary<DeclarativeSecurityAttributeHandle, DeclarativeSecurityAttributeHandle>();
        private readonly Dictionary<ExportedTypeHandle, ExportedTypeHandle> _exportedTypeCache =
            new Dictionary<ExportedTypeHandle, ExportedTypeHandle>();
        private readonly Dictionary<CustomAttributeHandle, CustomAttributeHandle> _customAttributeCache =
            new Dictionary<CustomAttributeHandle, CustomAttributeHandle>();
        private readonly Dictionary<MethodImplementationHandle, MethodImplementationHandle> _methodImplementationCache =
            new Dictionary<MethodImplementationHandle, MethodImplementationHandle>();
        private readonly Dictionary<MemberReferenceHandle, MemberReferenceHandle> _memberReferenceCache =
            new Dictionary<MemberReferenceHandle, MemberReferenceHandle>();
        private readonly Dictionary<TypeReferenceHandle, TypeReferenceHandle> _typeReferenceCache =
            new Dictionary<TypeReferenceHandle, TypeReferenceHandle>();
        private readonly Dictionary<AssemblyReferenceHandle, AssemblyReferenceHandle> _assemblyReferenceCache =
            new Dictionary<AssemblyReferenceHandle, AssemblyReferenceHandle>();
        private readonly Dictionary<AssemblyFileHandle, AssemblyFileHandle> _assemblyFileCache =
            new Dictionary<AssemblyFileHandle, AssemblyFileHandle>();
        private readonly Dictionary<InterfaceImplementationHandle, InterfaceImplementationHandle>
            _interfaceImplementationCache =
                new Dictionary<InterfaceImplementationHandle, InterfaceImplementationHandle>();
        private readonly Dictionary<GenericParameterHandle, GenericParameterHandle> _genericParameterCache =
            new Dictionary<GenericParameterHandle, GenericParameterHandle>();
        private readonly Dictionary<GenericParameterConstraintHandle, GenericParameterConstraintHandle>
            _genericParameterConstraintCache =
                new Dictionary<GenericParameterConstraintHandle, GenericParameterConstraintHandle>();
        private readonly Dictionary<TypeDefinitionHandle, TypeDefinitionHandle> _typeDefinitionCache =
            new Dictionary<TypeDefinitionHandle, TypeDefinitionHandle>();
        private readonly Dictionary<MethodDefinitionHandle, MethodDefinitionHandle> _methodDefinitionCache =
            new Dictionary<MethodDefinitionHandle, MethodDefinitionHandle>();
        private readonly Dictionary<FieldDefinitionHandle, FieldDefinitionHandle> _fieldDefinitionCache =
            new Dictionary<FieldDefinitionHandle, FieldDefinitionHandle>();
        private readonly Dictionary<PropertyDefinitionHandle, PropertyDefinitionHandle> _propertyDefinitionCache =
            new Dictionary<PropertyDefinitionHandle, PropertyDefinitionHandle>();
        private readonly Dictionary<EventDefinitionHandle, EventDefinitionHandle> _eventDefinitionCache =
            new Dictionary<EventDefinitionHandle, EventDefinitionHandle>();
        private readonly Dictionary<ParameterHandle, ParameterHandle> _parameterCache =
            new Dictionary<ParameterHandle, ParameterHandle>();
        
   }
}