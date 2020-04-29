using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace JetBrains.Refasmer
{
    public partial class MetadataImporter
    {
        private AssemblyReferenceHandle Import( AssemblyReferenceHandle srcHandle ) => 
            ImportEntity(srcHandle, _assemblyReferenceCache, _reader.GetAssemblyReference,
                src => _builder.AddAssemblyReference(ImportValue(src.Name), src.Version, ImportValue(src.Culture),
                    ImportValue(src.PublicKeyOrToken), src.Flags, ImportValue(src.HashValue)),
                    ToString, IsNil);
        
        private AssemblyFileHandle Import( AssemblyFileHandle srcHandle ) => 
            ImportEntity(srcHandle, _assemblyFileCache, _reader.GetAssemblyFile,
                src => _builder.AddAssemblyFile(ImportValue(src.Name), ImportValue(src.HashValue), src.ContainsMetadata),    
                ToString, IsNil);

        private TypeReferenceHandle Import( TypeReferenceHandle srcHandle ) =>
            ImportEntity(srcHandle, _typeReferenceCache, _reader.GetTypeReference,
                src => _builder.AddTypeReference(Import(src.ResolutionScope), ImportValue(src.Namespace),
                    ImportValue(src.Name)),
                ToString, IsNil);

        private ModuleReferenceHandle Import( ModuleReferenceHandle srcHandle ) =>
            ImportEntity(srcHandle, _moduleReferenceCache, _reader.GetModuleReference,
                src => _builder.AddModuleReference(ImportValue(src.Name)),
                ToString, IsNil);

        private TypeSpecificationHandle Import( TypeSpecificationHandle srcHandle ) =>
            ImportEntity(srcHandle, _typeSpecificationCache, _reader.GetTypeSpecification,
                src => _builder.AddTypeSpecification(ImportTypeSignature(src.Signature)), ToString, IsNil);

        
        private CustomAttributeHandle Import( CustomAttributeHandle srcHandle ) =>
            ImportEntity(srcHandle, _customAttributeCache, _reader.GetCustomAttribute,
                src =>
                {
                    var parent = Import(src.Parent);
                    var constructor = Import(src.Constructor);
                    return parent.IsNil || constructor.IsNil
                        ? default
                        : _builder.AddCustomAttribute(parent, constructor, ImportValue(src.Value));
                },
                ToString, IsNil);
        
        private ExportedTypeHandle Import( ExportedTypeHandle srcHandle ) =>
            ImportEntity(srcHandle, _exportedTypeCache, _reader.GetExportedType,
                src =>
                {
                    var impl = Import(src.Implementation);
                    return impl.IsNil
                        ? default
                        : _builder.AddExportedType(src.Attributes, ImportValue(src.Namespace), ImportValue(src.Name),
                            impl, src.GetTypeDefinitionId());
                },
                ToString, IsNil);

        private DeclarativeSecurityAttributeHandle Import( DeclarativeSecurityAttributeHandle srcHandle ) =>
            ImportEntity(srcHandle, _declarativeSecurityAttributeCache, _reader.GetDeclarativeSecurityAttribute,
                src =>
                {
                    var parent = Import(src.Parent);
                    return parent.IsNil
                        ? default
                        : _builder.AddDeclarativeSecurityAttribute(parent, src.Action, ImportValue(src.PermissionSet));
                },
                ToString, IsNil);

        private MemberReferenceHandle Import( MemberReferenceHandle srcHandle ) => 
            ImportEntity(srcHandle, _memberReferenceCache, _reader.GetMemberReference,
                src => _builder.AddMemberReference(Import(src.Parent), ImportValue(src.Name),
                    ImportSignatureWithHeader(src.Signature)),
                ToString, IsNil);
        
        

        //---------
        
        private THandle ImportEntity<TEntity, THandle>( THandle srcHandle, IDictionary<THandle, THandle> cache,
            Func<THandle, TEntity> getEntity, Func<TEntity, THandle> import, Func<THandle, string> toString, Func<THandle, bool> isNil )
        {
            if (cache.TryGetValue(srcHandle, out var dstHandle))
                return dstHandle;

            dstHandle = import(getEntity(srcHandle));

            if (isNil(dstHandle))
            {
                Trace?.Invoke($"Not imported {toString(srcHandle)}");
                return dstHandle;
            }

            cache.Add(srcHandle, dstHandle);
            Trace?.Invoke($"Imported {toString(srcHandle)} -> {RowId(dstHandle):X}");

            return dstHandle;
        }
        
        //------------------

        private TypeDefinitionHandle Import( TypeDefinitionHandle srcHandle ) => _typeDefinitionCache.GetValueOrDefault(srcHandle);
        private FieldDefinitionHandle Import( FieldDefinitionHandle srcHandle ) => _fieldDefinitionCache.GetValueOrDefault(srcHandle);
        private MethodDefinitionHandle Import( MethodDefinitionHandle srcHandle ) => _methodDefinitionCache.GetValueOrDefault(srcHandle);
        private ParameterHandle Import( ParameterHandle srcHandle ) => _parameterCache.GetValueOrDefault(srcHandle);
        private InterfaceImplementationHandle Import( InterfaceImplementationHandle srcHandle ) => _interfaceImplementationCache.GetValueOrDefault(srcHandle);
        private EventDefinitionHandle Import( EventDefinitionHandle srcHandle ) => _eventDefinitionCache.GetValueOrDefault(srcHandle);
        private PropertyDefinitionHandle Import( PropertyDefinitionHandle srcHandle ) => _propertyDefinitionCache.GetValueOrDefault(srcHandle);
        private GenericParameterHandle Import( GenericParameterHandle srcHandle ) => _genericParameterCache.GetValueOrDefault(srcHandle);
        private GenericParameterConstraintHandle Import( GenericParameterConstraintHandle srcHandle ) => _genericParameterConstraintCache.GetValueOrDefault(srcHandle);
        private MethodImplementationHandle Import( MethodImplementationHandle srcHandle ) => _methodImplementationCache.GetValueOrDefault(srcHandle);
        
        //------------------

        private EntityHandle Import( EntityHandle srcHandle )
        {
            if (srcHandle.IsNil)
                return srcHandle;

            switch (srcHandle.Kind)
            {
                case HandleKind.TypeDefinition:
                    return Import((TypeDefinitionHandle) srcHandle);
                case HandleKind.FieldDefinition:
                    return Import((FieldDefinitionHandle) srcHandle);
                case HandleKind.MethodDefinition:
                    return Import((MethodDefinitionHandle) srcHandle);
                case HandleKind.Parameter:
                    return Import((ParameterHandle) srcHandle);
                case HandleKind.InterfaceImplementation:
                    return Import((InterfaceImplementationHandle) srcHandle);
                case HandleKind.EventDefinition:
                    return Import((EventDefinitionHandle) srcHandle);
                case HandleKind.PropertyDefinition:
                    return Import((PropertyDefinitionHandle) srcHandle);
                case HandleKind.GenericParameter:
                    return Import((GenericParameterHandle) srcHandle);
                case HandleKind.GenericParameterConstraint:
                    return Import((GenericParameterConstraintHandle) srcHandle);
                case HandleKind.TypeReference:
                    return Import((TypeReferenceHandle) srcHandle);
                case HandleKind.MemberReference:
                    return Import((MemberReferenceHandle) srcHandle);
                case HandleKind.AssemblyReference:
                    return Import((AssemblyReferenceHandle) srcHandle);
                case HandleKind.AssemblyFile:
                    return Import((AssemblyFileHandle) srcHandle);
                case HandleKind.MethodImplementation:
                    return Import((MethodImplementationHandle) srcHandle);

                case HandleKind.ExportedType:
                    return Import((ExportedTypeHandle) srcHandle);
                case HandleKind.CustomAttribute:
                    return Import((CustomAttributeHandle) srcHandle);
                case HandleKind.DeclarativeSecurityAttribute:
                    return Import((DeclarativeSecurityAttributeHandle) srcHandle);
                case HandleKind.ModuleReference:
                    return Import((ModuleReferenceHandle) srcHandle);
                case HandleKind.TypeSpecification:
                    return Import((TypeSpecificationHandle) srcHandle);

                //Globals
                case HandleKind.ModuleDefinition:
                    if (srcHandle != EntityHandle.ModuleDefinition)
                        throw new ArgumentException("Invalid module definition handle");
                    return EntityHandle.ModuleDefinition;
                case HandleKind.AssemblyDefinition:
                    if (srcHandle != EntityHandle.AssemblyDefinition)
                        throw new ArgumentException("Invalid assembly definition handle");
                    return EntityHandle.AssemblyDefinition;


                //Not supported                        
                case HandleKind.MethodSpecification:
                    break;
                case HandleKind.ManifestResource:
                    break;
                case HandleKind.Constant:
                    break;
                case HandleKind.StandaloneSignature:
                    break;
                case HandleKind.Document:
                    break;
                case HandleKind.MethodDebugInformation:
                    break;
                case HandleKind.LocalScope:
                    break;
                case HandleKind.LocalVariable:
                    break;
                case HandleKind.LocalConstant:
                    break;
                case HandleKind.ImportScope:
                    break;
                case HandleKind.CustomDebugInformation:
                    break;
                case HandleKind.UserString:
                    break;
                case HandleKind.NamespaceDefinition:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            throw new NotImplementedException();
        }
    }
}