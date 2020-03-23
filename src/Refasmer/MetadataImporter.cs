using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace JetBrains.Refasmer
{
    public class MetadataImporter : LoggerBase
    {
        private readonly MetadataReader _reader;
        private readonly MetadataBuilder _builder;

        public Func<MethodDefinition, bool> MethodFilter;
        public Func<FieldDefinition, bool> FieldFilter;
        
        public MetadataImporter(MetadataReader reader, MetadataBuilder builder, LoggerBase logger) : base(logger)
        {
            _reader = reader;
            _builder = builder;
        }

        public void Import()
        {
            var srcAssembly = _reader.GetAssemblyDefinition();
            _builder.AddAssembly(ImportValue(srcAssembly.Name), new Version(), ImportValue(srcAssembly.Culture), ImportValue(srcAssembly.PublicKey),
                srcAssembly.Flags, srcAssembly.HashAlgorithm);
            Debug($"Imported assembly {_reader.ToString(srcAssembly)}");

            var srcModule = _reader.GetModuleDefinition();
            _builder.AddModule(srcModule.Generation, ImportValue(srcModule.Name), ImportValue(srcModule.Mvid), ImportValue(srcModule.GenerationId),
                ImportValue(srcModule.BaseGenerationId));
            Debug($"Imported module {_reader.ToString(srcModule)}");

            Debug($"Importing assembly references");
            foreach (var srcHandle in _reader.AssemblyReferences)
                ImportEntity(srcHandle, _assemblyReferenceCache, _reader.GetAssemblyReference,
                    src => _builder.AddAssemblyReference(ImportValue(src.Name), src.Version, ImportValue(src.Culture), ImportValue(src.PublicKeyOrToken), src.Flags, ImportValue(src.HashValue)),
                    src => _reader.ToString(src));
    
            Debug($"Importing assembly files");
            foreach (var srcHandle in _reader.AssemblyFiles)
                ImportEntity(srcHandle, _assemblyFileCache, _reader.GetAssemblyFile,
                    src => _builder.AddAssemblyFile(ImportValue(src.Name), ImportValue(src.HashValue), src.ContainsMetadata),
                    src => _reader.ToString(src));
            
            Debug($"Importing type references");
            foreach (var srcHandle in _reader.TypeReferences)
                ImportEntity(srcHandle, _typeReferenceCache, _reader.GetTypeReference,
                    src => _builder.AddTypeReference(Get(src.ResolutionScope), ImportValue(src.Namespace), ImportValue(src.Name)),
                    src => _reader.ToString(src));

            Debug($"Importing member references");
            foreach (var srcHandle in _reader.MemberReferences)
                ImportEntity(srcHandle, _memberReferenceCache, _reader.GetMemberReference,
                    src => _builder.AddMemberReference(GetOrImport(src.Parent), ImportValue(src.Name), ImportValue(src.Signature)),
                    src => _reader.ToString(src));

            Debug($"Importing type definitions");
            foreach (var srcHandle in _reader.TypeDefinitions)
                ImportTypeDefinitionSkeleton(srcHandle);

            foreach (var srcHandle in _reader.TypeDefinitions)
                ImportTypeDefinitionAccessories(srcHandle);

            Debug($"Importing method definitions");
            foreach (var srcMethodHandle in _reader.MethodDefinitions)
                ImportMethodDefinitionAccessories(srcMethodHandle);

            Debug($"Importing field definitions");
            foreach (var srcFieldHandle in _reader.FieldDefinitions)
                ImportFieldDefinitionAccessories(srcFieldHandle);

            var generic = _reader.TypeDefinitions
                .Select(x => Tuple.Create((EntityHandle)Get(x), _reader.GetTypeDefinition(x).GetGenericParameters()))
                .Concat(_reader.MethodDefinitions
                    .Select(x =>
                        Tuple.Create((EntityHandle)Get(x), _reader.GetMethodDefinition(x).GetGenericParameters())))
                .Where(x => !x.Item1.IsNil)
                .OrderBy(x => MetaUtil.RowId(x.Item1))
                .ToList();

            Debug($"Importing generic constraints");
            foreach (var (dstHandle, genericParams) in generic)
                ImportGenericConstraints(dstHandle, genericParams);

            Debug($"Importing custom attributes");
            foreach (var src in _reader.CustomAttributes)
                GetOrImport(src);

            Debug($"Importing declarative security attributes");
            foreach (var src in _reader.DeclarativeSecurityAttributes)
                GetOrImport(src);

            Debug($"Importing exported types");
            foreach (var src in _reader.ExportedTypes)
                GetOrImport(src);

            Debug($"Importing done");
        }
        
        private void ImportTypeDefinitionSkeleton(TypeDefinitionHandle srcHandle)
        {
            var src = _reader.GetTypeDefinition(srcHandle);

            var dst = _builder.AddTypeDefinition(src.Attributes, ImportValue(src.Namespace), ImportValue(src.Name),
                Get(src.BaseType), NextFieldHandle(), NextMethodHandle());

            Trace($"Imported {_reader.ToString(srcHandle)} -> {MetaUtil.RowId(dst):X}");
            
            if (dst != srcHandle)
                throw new Exception("WTF: Direct type mapping broken");

            foreach (var srcFieldHandle in src.GetFields())
            {
                var srcField = _reader.GetFieldDefinition(srcFieldHandle);
                
                if (FieldFilter?.Invoke(srcField) == false)
                    continue;
                
                var dstFieldHandle = _builder.AddFieldDefinition(srcField.Attributes, ImportValue(srcField.Name), ImportValue(srcField.Signature));
                Trace($"Imported {_reader.ToString(srcFieldHandle)} -> {MetaUtil.RowId(dstFieldHandle):X}");
                _fieldDefinitionCache.Add(srcFieldHandle, dstFieldHandle);
            }

            foreach (var srcMethodHandle in src.GetMethods())
            {
                var srcMethod = _reader.GetMethodDefinition(srcMethodHandle);

                if (MethodFilter?.Invoke(srcMethod) == false)
                    continue;

                var dstMethodHandle = _builder.AddMethodDefinition(srcMethod.Attributes, srcMethod.ImplAttributes, ImportValue(srcMethod.Name),
                    ImportValue(srcMethod.Signature), 0, NextParameterHandle());
                Trace($"Imported {_reader.ToString(srcMethodHandle)} -> {MetaUtil.RowId(dstMethodHandle):X}");
                _methodDefinitionCache.Add(srcMethodHandle, dstMethodHandle);

                foreach (var srcParameterHandle in srcMethod.GetParameters())
                {
                    var srcParameter = _reader.GetParameter(srcParameterHandle);
                    var dstParameterHandle = _builder.AddParameter(srcParameter.Attributes, ImportValue(srcParameter.Name), srcParameter.SequenceNumber);
                    Trace($"Imported {_reader.ToString(srcParameterHandle)} -> {MetaUtil.RowId(dstParameterHandle):X}");
                    _parameterCache.Add(srcParameterHandle, dstParameterHandle);
                }
            }
        }

        private void ImportTypeDefinitionAccessories(TypeDefinitionHandle srcHandle)
        {
            var src = _reader.GetTypeDefinition(srcHandle);
            var dstHandle = Get(srcHandle);
            
            if (dstHandle.IsNil)
                return;
            
            using var _ = WithLogPrefix($"[{_reader.ToString(src)}]");

            foreach (var srcNested in src.GetNestedTypes())
            {
                var dstNested = Get(srcNested);
                _builder.AddNestedType(dstNested, dstHandle);
                Trace($"Imported nested type {_reader.ToString(srcNested)} -> {MetaUtil.RowId(dstNested):X}");
            }
            
            var interfaceImpls = src.GetInterfaceImplementations()
                .Select(x => Tuple.Create(x, Get(_reader.GetInterfaceImplementation(x).Interface)))
                .Where(x => !x.Item2.IsNil)
                .OrderBy(x => MetaUtil.RowId(x.Item2));

            foreach (var (srcInterfaceImplHandle, dstInterface) in interfaceImpls)
            {
                var dstInterfaceImplHandle = _builder.AddInterfaceImplementation(dstHandle, dstInterface);
                _interfaceImplementationCache.Add(srcInterfaceImplHandle, dstInterfaceImplHandle);
                Trace($"Imported interface implementation {_reader.ToString(srcInterfaceImplHandle)} -> {MetaUtil.RowId(dstInterfaceImplHandle):X}");
            }

            foreach (var srcMethodImplementationHandle in src.GetMethodImplementations())
                ImportEntity(srcMethodImplementationHandle, _methodImplementationCache, _reader.GetMethodImplementation,
                    srcImpl =>
                    {
                        var body = Get(srcImpl.MethodBody);
                        var decl = Get(srcImpl.MethodDeclaration);
                        
                        return body.IsNil || decl.IsNil ? default : 
                            _builder.AddMethodImplementation(dstHandle, body, decl);
                    },
                    srcImpl => _reader.ToString(srcImpl));

            if (src.GetEvents().Any())
            {
                _builder.AddEventMap(dstHandle, NextEventHandle());
                foreach (var eventHandle in src.GetEvents())
                    ImportEvent(eventHandle);
            }

            if (src.GetProperties().Any())
            {
                _builder.AddPropertyMap(dstHandle, NextPropertyHandle());
                foreach (var propertyHandle in src.GetProperties())
                    ImportProperty(propertyHandle);
            }

            if (!src.GetLayout().IsDefault)
                _builder.AddTypeLayout(dstHandle, (ushort)src.GetLayout().PackingSize, (uint)src.GetLayout().Size);
        }

        private void ImportFieldDefinitionAccessories(FieldDefinitionHandle srcHandle)
        {
            var src = _reader.GetFieldDefinition(srcHandle);
            var dstHandle = Get(srcHandle);

            if (dstHandle.IsNil)
                return;
            
            using var _ = WithLogPrefix($"[{_reader.ToString(src)}]");

            if (!src.GetDefaultValue().IsNil)
            {
                var srcConst = _reader.GetConstant(src.GetDefaultValue());
                var value = _reader.GetBlobReader(srcConst.Value).ReadConstant(srcConst.TypeCode);

                _builder.AddConstant(dstHandle, value);
                Trace($"Imported default value {value}");
            }

            if (!src.GetMarshallingDescriptor().IsNil)
            {
                _builder.AddMarshallingDescriptor(dstHandle, ImportValue(src.GetMarshallingDescriptor()));
                Trace($"Imported marshalling descriptor {_reader.ToString(src.GetMarshallingDescriptor())}");
            }

            if (src.GetOffset() != -1)
            {
                _builder.AddFieldLayout(dstHandle, src.GetOffset());
                Trace($"Importing offset {src.GetOffset()}");
            }

            if (src.GetRelativeVirtualAddress() != 0)
            {
                _builder.AddFieldRelativeVirtualAddress(dstHandle, src.GetRelativeVirtualAddress());
                Trace($"Imported relative virtual address {src.GetRelativeVirtualAddress()}");
            }
           
        }

        private void ImportMethodDefinitionAccessories(MethodDefinitionHandle srcHandle)
        {
            var src = _reader.GetMethodDefinition(srcHandle);
            var dstHandle = Get(srcHandle);

            if (dstHandle.IsNil)
                return;
            
            using var _ = WithLogPrefix($"[{_reader.ToString(src)}]");
            
            var srcImport = src.GetImport();

            if (!srcImport.Name.IsNil)
            {
                _builder.AddMethodImport(dstHandle, srcImport.Attributes, ImportValue(srcImport.Name), GetOrImport(srcImport.Module));
                Trace($"Imported method import {_reader.ToString(srcImport.Module)} {_reader.ToString(srcImport.Name)}");
            }
        }

        private void ImportEvent(EventDefinitionHandle srcHandle)
        {
            var src = _reader.GetEventDefinition(srcHandle);
            
            var accessors = src.GetAccessors();

            var adder = Get(accessors.Adder);
            var remover = Get(accessors.Remover);
            var raiser = Get(accessors.Raiser);

            var others = accessors.Others.Select(Get).Where(a => !a.IsNil).ToList();

            if (adder.IsNil && remover.IsNil && raiser.IsNil && !others.Any(x => x.IsNil))
            {
                Trace($"Not imported event {_reader.ToString(src)}");
                return;
            }
            
            var dstHandle = _builder.AddEvent(src.Attributes, ImportValue(src.Name), Get(src.Type));
            _eventDefinitionCache.Add(srcHandle, dstHandle);
            
            if (!adder.IsNil)
                _builder.AddMethodSemantics(dstHandle, MethodSemanticsAttributes.Adder, adder);
            if (!remover.IsNil)
                _builder.AddMethodSemantics(dstHandle, MethodSemanticsAttributes.Remover, remover);
            if (!raiser.IsNil)
                _builder.AddMethodSemantics(dstHandle, MethodSemanticsAttributes.Raiser, raiser);

            foreach (var accessor in others)
                _builder.AddMethodSemantics(dstHandle, MethodSemanticsAttributes.Other, accessor);

            Trace($"Imported event {_reader.ToString(src)} -> {MetaUtil.RowId(dstHandle):X}");
        }

        private void ImportProperty(PropertyDefinitionHandle srcHandle)
        {
            var src = _reader.GetPropertyDefinition(srcHandle);
            
            var accessors = src.GetAccessors();

            var getter = Get(accessors.Getter);
            var setter = Get(accessors.Setter);
            var others = accessors.Others.Select(Get).Where(a => !a.IsNil).ToList();

            if (getter.IsNil && setter.IsNil && !others.Any(x => x.IsNil))
            {
                Trace($"Not imported property {_reader.ToString(src)}");
                return;
            }

            var dstHandle = _builder.AddProperty(src.Attributes, ImportValue(src.Name), ImportValue(src.Signature));
            _propertyDefinitionCache.Add(srcHandle, dstHandle);

            if (!getter.IsNil)
                _builder.AddMethodSemantics(dstHandle, MethodSemanticsAttributes.Getter, getter);
            if (!setter.IsNil)
                _builder.AddMethodSemantics(dstHandle, MethodSemanticsAttributes.Setter, setter);

            foreach (var accessor in others)
                _builder.AddMethodSemantics(dstHandle, MethodSemanticsAttributes.Other, accessor);

            var defaultValue = src.GetDefaultValue(); 
            
            if (!defaultValue.IsNil)
            {
                var srcConst = _reader.GetConstant(defaultValue);
                var value = _reader.GetBlobReader(srcConst.Value).ReadConstant(srcConst.TypeCode);

                _builder.AddConstant(dstHandle, value);
            }
            Trace($"Imported property {_reader.ToString(src)} -> {MetaUtil.RowId(dstHandle):X}");
        }

        private void ImportGenericConstraints(EntityHandle entityHandle, GenericParameterHandleCollection srcParams)
        {
            var srcConstraints = new List<Tuple<GenericParameterHandle, GenericParameterConstraintHandle>>(); 
            
            foreach (var srcParamHandle in srcParams)
            {
                var srcParam = _reader.GetGenericParameter(srcParamHandle);
                var dstParamHandle = _builder.AddGenericParameter(entityHandle, srcParam.Attributes, ImportValue(srcParam.Name), srcParam.Index);
                _genericParameterCache.Add(srcParamHandle, dstParamHandle);
                srcConstraints.AddRange(srcParam.GetConstraints().Select(x => Tuple.Create(dstParamHandle, x)));

                Trace($"Imported generic parameter {_reader.ToString(srcParam)} -> {MetaUtil.RowId(dstParamHandle):X}");
            }

            foreach (var (dstParam, srcConstraintHandle) in srcConstraints)
                ImportEntity(srcConstraintHandle, _genericParameterConstraintCache,
                    _reader.GetGenericParameterConstraint,
                    src => _builder.AddGenericParameterConstraint(dstParam, GetOrImport(src.Type)),
                    src => _reader.ToString(src));
        }

        #region Values import
        private StringHandle ImportValue(StringHandle src) => _builder.GetOrAddString(_reader.GetString(src));
        private GuidHandle ImportValue(GuidHandle src) => _builder.GetOrAddGuid(_reader.GetGuid(src));
        private BlobHandle ImportValue(BlobHandle src) => _builder.GetOrAddBlob(_reader.GetBlobContent(src));
        #endregion

        #region Utils
        private int GetNextToken(TableIndex index) => _builder.GetRowCount(index) + 1;
        private FieldDefinitionHandle NextFieldHandle() => MetadataTokens.FieldDefinitionHandle(GetNextToken(TableIndex.Field)); 
        private MethodDefinitionHandle NextMethodHandle() => MetadataTokens.MethodDefinitionHandle(GetNextToken(TableIndex.MethodDef)); 
        private ParameterHandle NextParameterHandle() => MetadataTokens.ParameterHandle(GetNextToken(TableIndex.Param)); 
        private EventDefinitionHandle NextEventHandle() => MetadataTokens.EventDefinitionHandle(GetNextToken(TableIndex.Event)); 
        private PropertyDefinitionHandle NextPropertyHandle() => MetadataTokens.PropertyDefinitionHandle(GetNextToken(TableIndex.Property)); 
        #endregion
        
        #region Simple cached imports
        private THandle ImportEntity<TEntity, THandle>(THandle srcHandle, IDictionary<THandle, THandle> cache,
            Func<THandle, TEntity> getEntity, Func<TEntity, THandle> import, Func<THandle, string> toString)
        {
            var dstHandle = import(getEntity(srcHandle));

            if (MetaUtil.IsNil(dstHandle))
            {
                Trace($"Not imported {toString(srcHandle)}");
                return dstHandle;
            }
            
            cache.Add(srcHandle, dstHandle);
            Trace($"Imported {toString(srcHandle)} -> {MetaUtil.RowId(dstHandle):X}");

            return dstHandle;
        }

        private THandle GetOrImportEntity<TEntity, THandle>(THandle srcHandle, IDictionary<THandle, THandle> cache,
                Func<THandle, TEntity> getEntity, Func<TEntity, THandle> import, Func<THandle, string> toString) =>
            cache.TryGetValue(srcHandle, out var dstHandle) ? dstHandle : ImportEntity(srcHandle, cache, getEntity, import, toString);

        private readonly Dictionary<ModuleReferenceHandle, ModuleReferenceHandle> _moduleReferenceCache = new Dictionary<ModuleReferenceHandle, ModuleReferenceHandle>();
        private ModuleReferenceHandle Get(ModuleReferenceHandle srcHandle) => _moduleReferenceCache.GetValueOrDefault(srcHandle);
        private ModuleReferenceHandle GetOrImport(ModuleReferenceHandle srcHandle) =>
            GetOrImportEntity(srcHandle, _moduleReferenceCache, _reader.GetModuleReference,
                src => _builder.AddModuleReference(ImportValue(src.Name)),
                src => _reader.ToString(src));

        private readonly Dictionary<TypeSpecificationHandle, TypeSpecificationHandle> _typeSpecificationCache = new Dictionary<TypeSpecificationHandle, TypeSpecificationHandle>();
        private TypeSpecificationHandle Get(TypeSpecificationHandle srcHandle) => _typeSpecificationCache.GetValueOrDefault(srcHandle);
        private TypeSpecificationHandle GetOrImport(TypeSpecificationHandle srcHandle) =>
            GetOrImportEntity(srcHandle, _typeSpecificationCache, _reader.GetTypeSpecification,
                src => _builder.AddTypeSpecification(ImportValue(src.Signature)), src => _reader.ToString(src));

        private readonly Dictionary<DeclarativeSecurityAttributeHandle, DeclarativeSecurityAttributeHandle> _declarativeSecurityAttributeCache = new Dictionary<DeclarativeSecurityAttributeHandle, DeclarativeSecurityAttributeHandle>();
        private DeclarativeSecurityAttributeHandle Get(DeclarativeSecurityAttributeHandle srcHandle) => _declarativeSecurityAttributeCache.GetValueOrDefault(srcHandle);
        private DeclarativeSecurityAttributeHandle GetOrImport(DeclarativeSecurityAttributeHandle srcHandle) =>
            GetOrImportEntity(srcHandle, _declarativeSecurityAttributeCache, _reader.GetDeclarativeSecurityAttribute,
                src =>
                {
                    var parent = Get(src.Parent);
                    return parent.IsNil ? default : 
                        _builder.AddDeclarativeSecurityAttribute(parent, src.Action, ImportValue(src.PermissionSet));
                },
                src => _reader.ToString(src));
        

        private readonly Dictionary<ExportedTypeHandle, ExportedTypeHandle> _exportedTypeCache = new Dictionary<ExportedTypeHandle, ExportedTypeHandle>();
        private ExportedTypeHandle Get(ExportedTypeHandle srcHandle) => _exportedTypeCache.GetValueOrDefault(srcHandle);
        private ExportedTypeHandle GetOrImport(ExportedTypeHandle srcHandle) =>
            GetOrImportEntity(srcHandle, _exportedTypeCache, _reader.GetExportedType,
                src =>
                {
                    var impl = Get(src.Implementation);
                    return impl.IsNil ? default : 
                        _builder.AddExportedType(src.Attributes, ImportValue(src.Namespace), ImportValue(src.Name), impl, src.GetTypeDefinitionId());
                },
                src => _reader.ToString(src));

        private readonly Dictionary<CustomAttributeHandle, CustomAttributeHandle> _customAttributeCache = new Dictionary<CustomAttributeHandle, CustomAttributeHandle>();
        private CustomAttributeHandle Get(CustomAttributeHandle srcHandle) => _customAttributeCache.GetValueOrDefault(srcHandle);
        private CustomAttributeHandle GetOrImport(CustomAttributeHandle srcHandle) =>
            GetOrImportEntity(srcHandle, _customAttributeCache, _reader.GetCustomAttribute,
                src =>
                {
                    var parent = Get(src.Parent);
                    var constructor = Get(src.Constructor);
                    return parent.IsNil || constructor.IsNil ? default : 
                        _builder.AddCustomAttribute(parent, constructor, ImportValue(src.Value));
                },
                src => _reader.ToString(src));

        #endregion

        #region Caches
        private readonly Dictionary<MethodImplementationHandle, MethodImplementationHandle> _methodImplementationCache = new Dictionary<MethodImplementationHandle, MethodImplementationHandle>();
        private MethodImplementationHandle Get(MethodImplementationHandle srcHandle) => _methodImplementationCache.GetValueOrDefault(srcHandle);

        private readonly Dictionary<MemberReferenceHandle, MemberReferenceHandle> _memberReferenceCache = new Dictionary<MemberReferenceHandle, MemberReferenceHandle>();
        private MemberReferenceHandle Get(MemberReferenceHandle srcHandle) => _memberReferenceCache.GetValueOrDefault(srcHandle); 
            
        private readonly Dictionary<TypeReferenceHandle, TypeReferenceHandle> _typeReferenceCache = new Dictionary<TypeReferenceHandle, TypeReferenceHandle>();
        private TypeReferenceHandle Get(TypeReferenceHandle srcHandle) => _typeReferenceCache.GetValueOrDefault(srcHandle);

        private readonly Dictionary<AssemblyReferenceHandle, AssemblyReferenceHandle> _assemblyReferenceCache = new Dictionary<AssemblyReferenceHandle, AssemblyReferenceHandle>();
        private AssemblyReferenceHandle Get(AssemblyReferenceHandle srcHandle) => _assemblyReferenceCache.GetValueOrDefault(srcHandle);

        private readonly Dictionary<AssemblyFileHandle, AssemblyFileHandle> _assemblyFileCache = new Dictionary<AssemblyFileHandle, AssemblyFileHandle>();
        private AssemblyFileHandle Get(AssemblyFileHandle srcHandle) => _assemblyFileCache.GetValueOrDefault(srcHandle);

        private readonly Dictionary<InterfaceImplementationHandle, InterfaceImplementationHandle> _interfaceImplementationCache = new Dictionary<InterfaceImplementationHandle, InterfaceImplementationHandle>();
        private InterfaceImplementationHandle Get(InterfaceImplementationHandle srcHandle) => _interfaceImplementationCache.GetValueOrDefault(srcHandle);
        
        private readonly Dictionary<GenericParameterHandle, GenericParameterHandle> _genericParameterCache = new Dictionary<GenericParameterHandle, GenericParameterHandle>();
        private GenericParameterHandle Get(GenericParameterHandle srcHandle) => _genericParameterCache.GetValueOrDefault(srcHandle);

        private readonly Dictionary<GenericParameterConstraintHandle, GenericParameterConstraintHandle> _genericParameterConstraintCache = new Dictionary<GenericParameterConstraintHandle, GenericParameterConstraintHandle>();
        private GenericParameterConstraintHandle Get(GenericParameterConstraintHandle srcHandle) => _genericParameterConstraintCache.GetValueOrDefault(srcHandle);

        private TypeDefinitionHandle Get(TypeDefinitionHandle srcHandle) => srcHandle; // Direct type mapping

        private readonly Dictionary<MethodDefinitionHandle, MethodDefinitionHandle> _methodDefinitionCache = new Dictionary<MethodDefinitionHandle, MethodDefinitionHandle>();
        private MethodDefinitionHandle Get(MethodDefinitionHandle srcHandle) => _methodDefinitionCache.GetValueOrDefault(srcHandle);

        private readonly Dictionary<FieldDefinitionHandle, FieldDefinitionHandle> _fieldDefinitionCache = new Dictionary<FieldDefinitionHandle, FieldDefinitionHandle>();
        private FieldDefinitionHandle Get(FieldDefinitionHandle srcHandle) => _fieldDefinitionCache.GetValueOrDefault(srcHandle);
        
        private readonly Dictionary<PropertyDefinitionHandle, PropertyDefinitionHandle> _propertyDefinitionCache = new Dictionary<PropertyDefinitionHandle, PropertyDefinitionHandle>();
        private PropertyDefinitionHandle Get(PropertyDefinitionHandle srcHandle) => _propertyDefinitionCache.GetValueOrDefault(srcHandle);

        private readonly Dictionary<EventDefinitionHandle, EventDefinitionHandle> _eventDefinitionCache = new Dictionary<EventDefinitionHandle, EventDefinitionHandle>();
        private EventDefinitionHandle Get(EventDefinitionHandle srcHandle) => _eventDefinitionCache.GetValueOrDefault(srcHandle);

        private readonly Dictionary<ParameterHandle, ParameterHandle> _parameterCache = new Dictionary<ParameterHandle, ParameterHandle>();
        private ParameterHandle Get(ParameterHandle srcHandle) => _parameterCache.GetValueOrDefault(srcHandle);

        #endregion

        private EntityHandle Get(EntityHandle srcHandle) => GetOrImport(srcHandle, false);
        private EntityHandle GetOrImport(EntityHandle srcHandle) => GetOrImport(srcHandle, true);
        private EntityHandle GetOrImport(EntityHandle srcHandle, bool allowImport)
        {
            if (srcHandle.IsNil)
                return srcHandle;
            
            switch (srcHandle.Kind)
            {
                case HandleKind.TypeDefinition:
                    return Get((TypeDefinitionHandle) srcHandle);
                case HandleKind.FieldDefinition:
                    return Get((FieldDefinitionHandle) srcHandle);
                case HandleKind.MethodDefinition:
                    return Get((MethodDefinitionHandle) srcHandle);
                case HandleKind.Parameter:
                    return Get((ParameterHandle) srcHandle);
                case HandleKind.InterfaceImplementation:
                    return Get((InterfaceImplementationHandle) srcHandle);
                case HandleKind.EventDefinition:
                    return Get((EventDefinitionHandle) srcHandle);
                case HandleKind.PropertyDefinition:
                    return Get((PropertyDefinitionHandle) srcHandle);
                case HandleKind.GenericParameter:
                    return Get((GenericParameterHandle) srcHandle);
                case HandleKind.GenericParameterConstraint:
                    return Get((GenericParameterConstraintHandle) srcHandle);
                case HandleKind.TypeReference:
                    return Get((TypeReferenceHandle) srcHandle);
                case HandleKind.MemberReference:
                    return Get((MemberReferenceHandle) srcHandle);
                case HandleKind.AssemblyReference:
                    return Get((AssemblyReferenceHandle) srcHandle);
                case HandleKind.AssemblyFile:
                    return Get((AssemblyFileHandle) srcHandle);
                case HandleKind.MethodImplementation:
                    return Get((MethodImplementationHandle) srcHandle);
                
                case HandleKind.ExportedType:
                    return allowImport ? GetOrImport((ExportedTypeHandle) srcHandle) : Get((ExportedTypeHandle) srcHandle);
                case HandleKind.CustomAttribute:
                    return allowImport ? GetOrImport((CustomAttributeHandle) srcHandle) : Get((CustomAttributeHandle) srcHandle);
                case HandleKind.DeclarativeSecurityAttribute:
                    return allowImport ? GetOrImport((DeclarativeSecurityAttributeHandle) srcHandle) : Get((DeclarativeSecurityAttributeHandle) srcHandle);
                case HandleKind.ModuleReference:
                    return allowImport ? GetOrImport((ModuleReferenceHandle) srcHandle) : Get((ModuleReferenceHandle) srcHandle);
                case HandleKind.TypeSpecification:
                    return allowImport ? GetOrImport((TypeSpecificationHandle) srcHandle) : Get((TypeSpecificationHandle) srcHandle);

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