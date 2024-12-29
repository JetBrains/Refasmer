using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using JetBrains.Refasmer.Filters;
using JetBrains.Refasmer.Importer;

namespace JetBrains.Refasmer;

public partial class MetadataImporter
{
    private bool AllowImportType( EntityHandle typeHandle )
    {
        if (typeHandle.IsNil)
            return false;

        if (Filter == null)
            return true;

        switch (typeHandle.Kind)
        {
            case HandleKind.TypeDefinition:
                return true;
            case HandleKind.TypeReference:
                return true;
            case HandleKind.TypeSpecification:
                return AllowImportType(_reader.GetGenericType((TypeSpecificationHandle)typeHandle));

            default:
                throw new ArgumentOutOfRangeException(nameof (typeHandle));
        }
    }

    private TypeDefinitionHandle ImportTypeDefinitionSkeleton(TypeDefinitionHandle srcHandle, bool omitMembers)
    {
        var src = _reader.GetTypeDefinition(srcHandle);

        var dstHandle = _builder.AddTypeDefinition(src.Attributes, ImportValue(src.Namespace), ImportValue(src.Name),
            Import(src.BaseType), NextFieldHandle(), NextMethodHandle());

        Trace?.Invoke($"Imported {_reader.ToString(src)} -> {RowId(dstHandle)}");

        if (omitMembers) return dstHandle;

        using var _ = WithLogPrefix($"[{_reader.ToString(src)}]");

        var isValueType = _reader.GetFullname(src.BaseType) == "System::ValueType";
        var forcePreservePrivateFields = isValueType && Filter?.OmitNonApiMembers == false;

        List<FieldDefinition>? importedInstanceFields = null;
        List<FieldDefinition>? skippedInstanceFields = null;

        if (forcePreservePrivateFields)
            Trace?.Invoke($"{_reader.ToString(src)} is ValueType, all fields should be imported");
        else
        {
            importedInstanceFields = [];
            skippedInstanceFields = [];
        }

        foreach (var srcFieldHandle in src.GetFields())
        {
            var srcField = _reader.GetFieldDefinition(srcFieldHandle);
            var isStatic = (srcField.Attributes & FieldAttributes.Static) != 0;
            var isForcedToInclude = forcePreservePrivateFields && !isStatic;

            if (!isForcedToInclude && Filter?.AllowImport(srcField, _reader) == false)
            {
                Trace?.Invoke($"Not imported {_reader.ToString(srcField)}");
                if (!isStatic)
                    skippedInstanceFields?.Add(srcField);

                continue;
            }

            var dstFieldHandle = _builder.AddFieldDefinition(srcField.Attributes, ImportValue(srcField.Name),
                ImportSignatureWithHeader(srcField.Signature));
            _fieldDefinitionCache.Add(srcFieldHandle, dstFieldHandle);
            Trace?.Invoke($"Imported {_reader.ToString(srcFieldHandle)} -> {RowId(dstFieldHandle)}");
            if (!isStatic)
                importedInstanceFields?.Add(srcField);
        }

        if (!forcePreservePrivateFields)
            PostProcessSkippedValueTypeFields(skippedInstanceFields!, importedInstanceFields!);

        var implementations = src.GetMethodImplementations()
            .Select(_reader.GetMethodImplementation)
            .Where(mi => AllowImportType(_reader.GetMethodClass(mi.MethodDeclaration)))
            .Select(mi => (MethodDefinitionHandle)mi.MethodBody)
            .ToImmutableHashSet();

        foreach (var srcMethodHandle in src.GetMethods())
        {
            var srcMethod = _reader.GetMethodDefinition(srcMethodHandle);

            if (!implementations.Contains(srcMethodHandle) && Filter?.AllowImport(srcMethod, _reader) == false)
            {
                Trace?.Invoke($"Not imported {_reader.ToString(srcMethod)}");
                continue;
            }

            var dstSignature = ImportSignatureWithHeader(srcMethod.Signature);

            if (dstSignature.IsNil)
            {
                Trace?.Invoke($"Not imported because of signature {_reader.ToString(srcMethod)}");
                continue;
            }

            var isAbstract = srcMethod.Attributes.HasFlag(MethodAttributes.Abstract);
            var bodyOffset = !isAbstract && MakeMock ? MakeMockBody(srcMethodHandle) : -1;

            var dstMethodHandle = _builder.AddMethodDefinition(srcMethod.Attributes, srcMethod.ImplAttributes,
                ImportValue(srcMethod.Name), dstSignature, bodyOffset, NextParameterHandle());
            _methodDefinitionCache.Add(srcMethodHandle, dstMethodHandle);
            Trace?.Invoke($"Imported {_reader.ToString(srcMethod)} -> {RowId(dstMethodHandle)}");

            using var __ = WithLogPrefix($"[{_reader.ToString(srcMethod)}]");
            foreach (var srcParameterHandle in srcMethod.GetParameters())
            {
                var srcParameter = _reader.GetParameter(srcParameterHandle);
                var dstParameterHandle = _builder.AddParameter(srcParameter.Attributes,
                    ImportValue(srcParameter.Name), srcParameter.SequenceNumber);
                _parameterCache.Add(srcParameterHandle, dstParameterHandle);
                Trace?.Invoke($"Imported {_reader.ToString(srcParameter)} -> {RowId(dstParameterHandle)}");

                var defaultValue = srcParameter.GetDefaultValue();

                if (!defaultValue.IsNil)
                    ImportDefaultValue(defaultValue, dstParameterHandle);


                if (!srcParameter.GetMarshallingDescriptor().IsNil)
                {
                    _builder.AddMarshallingDescriptor(dstParameterHandle, ImportValue(srcParameter.GetMarshallingDescriptor()));
                    Trace?.Invoke($"Imported marshalling descriptor {_reader.ToString(srcParameter.GetMarshallingDescriptor())}");
                }
            }
        }

        return dstHandle;
    }

    private void ImportTypeDefinitionAccessories( TypeDefinitionHandle srcHandle, TypeDefinitionHandle dstHandle)
    {
        var src = _reader.GetTypeDefinition(srcHandle);

        using var _ = WithLogPrefix($"[{_reader.ToString(src)}]");

        foreach (var srcInterfaceImplHandle in src.GetInterfaceImplementations())
        {
            var srcInterfaceImpl = _reader.GetInterfaceImplementation(srcInterfaceImplHandle);
            var dstInterfaceHandle = Import(srcInterfaceImpl.Interface);

            if (dstInterfaceHandle.IsNil)
            {
                Trace?.Invoke(
                    $"Not imported interface implementation {_reader.ToString(srcInterfaceImpl)}");
            }
            else
            {
                var dstInterfaceImplHandle = _builder.AddInterfaceImplementation(dstHandle, dstInterfaceHandle);
                _interfaceImplementationCache.Add(srcInterfaceImplHandle, dstInterfaceImplHandle);
                Trace?.Invoke(
                    $"Imported interface implementation {_reader.ToString(srcInterfaceImpl)} ->  {RowId(dstInterfaceHandle)} {RowId(dstInterfaceImplHandle)}");
            }
        }

        foreach (var srcMethodImplementationHandle in src.GetMethodImplementations())
            ImportEntity(srcMethodImplementationHandle, _methodImplementationCache, _reader.GetMethodImplementation,
                srcImpl =>
                {
                    var body = Import(srcImpl.MethodBody);
                    var decl = Import(srcImpl.MethodDeclaration);

                    return body.IsNil || decl.IsNil
                        ? default
                        : _builder.AddMethodImplementation(dstHandle, body, decl);
                },
                _reader.ToString, IsNil);

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
        {
            _builder.AddTypeLayout(dstHandle, (ushort) src.GetLayout().PackingSize, (uint) src.GetLayout().Size);
            Trace?.Invoke($"Imported layout Size={src.GetLayout().Size} PackingSize={src.GetLayout().PackingSize}");
        }
    }

    private void ImportFieldDefinitionAccessories( FieldDefinitionHandle srcHandle, FieldDefinitionHandle dstHandle )
    {
        var src = _reader.GetFieldDefinition(srcHandle);

        using var _ = WithLogPrefix($"[{_reader.ToString(src)}]");

        if (!src.GetDefaultValue().IsNil)
        {
            var srcConst = _reader.GetConstant(src.GetDefaultValue());
            var value = _reader.GetBlobReader(srcConst.Value).ReadConstant(srcConst.TypeCode);

            var dstConst = _builder.AddConstant(dstHandle, value);

            Trace?.Invoke($"Imported default value {_reader.ToString(srcConst)} -> {RowId(dstConst)} = {value}");
        }

        if (!src.GetMarshallingDescriptor().IsNil)
        {
            _builder.AddMarshallingDescriptor(dstHandle, ImportValue(src.GetMarshallingDescriptor()));
            Trace?.Invoke($"Imported marshalling descriptor {_reader.ToString(src.GetMarshallingDescriptor())}");
        }

        if (src.GetOffset() != -1)
        {
            _builder.AddFieldLayout(dstHandle, src.GetOffset());
            Trace?.Invoke($"Imported offset {src.GetOffset()}");
        }

        if (src.GetRelativeVirtualAddress() != 0)
        {
            _builder.AddFieldRelativeVirtualAddress(dstHandle, src.GetRelativeVirtualAddress());
            Trace?.Invoke($"Imported relative virtual address {src.GetRelativeVirtualAddress()}");
        }

    }

    private void ImportMethodDefinitionAccessories( MethodDefinitionHandle srcHandle, MethodDefinitionHandle dstHandle )
    {
        var src = _reader.GetMethodDefinition(srcHandle);
        using var _ = WithLogPrefix($"[{_reader.ToString(src)}]");

        var srcImport = src.GetImport();

        if (!srcImport.Name.IsNil)
        {
            _builder.AddMethodImport(dstHandle, srcImport.Attributes, ImportValue(srcImport.Name),
                Import(srcImport.Module));
            Trace?.Invoke($"Imported method import {_reader.ToString(srcImport.Module)} {_reader.ToString(srcImport.Name)}");
        }
    }

    private void ImportEvent( EventDefinitionHandle srcHandle )
    {
        var src = _reader.GetEventDefinition(srcHandle);

        var accessors = src.GetAccessors();

        var adder = Import(accessors.Adder);
        var remover = Import(accessors.Remover);
        var raiser = Import(accessors.Raiser);

        var others = accessors.Others
            .Select(a => Tuple.Create(a, Import(a)))
            .Where(a => !a.Item2.IsNil)
            .ToList();

        if (adder.IsNil && remover.IsNil && raiser.IsNil && !others.Any())
        {
            Trace?.Invoke($"Not imported event {_reader.ToString(src)}");
            return;
        }

        var dstHandle = _builder.AddEvent(src.Attributes, ImportValue(src.Name), Import(src.Type));
        _eventDefinitionCache.Add(srcHandle, dstHandle);
        Trace?.Invoke($"Imported event {_reader.ToString(src)} -> {RowId(dstHandle)}");

        using var _ = WithLogPrefix($"[{_reader.ToString(src)}]");

        if (!adder.IsNil)
        {
            _builder.AddMethodSemantics(dstHandle, MethodSemanticsAttributes.Adder, adder);
            Trace?.Invoke($"Imported adder {_reader.ToString(accessors.Adder)} -> {RowId(adder)}");
        }

        if (!remover.IsNil)
        {
            _builder.AddMethodSemantics(dstHandle, MethodSemanticsAttributes.Remover, remover);
            Trace?.Invoke($"Imported remover {_reader.ToString(accessors.Remover)} -> {RowId(remover)}");
        }

        if (!raiser.IsNil)
        {
            _builder.AddMethodSemantics(dstHandle, MethodSemanticsAttributes.Raiser, raiser);
            Trace?.Invoke($"Imported raiser {_reader.ToString(accessors.Raiser)} -> {RowId(raiser)}");
        }

        foreach (var (srcAccessor, dstAccessor) in others)
        {
            _builder.AddMethodSemantics(dstHandle, MethodSemanticsAttributes.Other, dstAccessor);
            Trace?.Invoke($"Imported other {_reader.ToString(srcAccessor)} -> {RowId(dstAccessor)}");
        }

    }

    private void ImportProperty( PropertyDefinitionHandle srcHandle )
    {
        var src = _reader.GetPropertyDefinition(srcHandle);

        var accessors = src.GetAccessors();

        var getter = Import(accessors.Getter);
        var setter = Import(accessors.Setter);

        var others = accessors.Others
            .Select(a => Tuple.Create(a, Import(a)))
            .Where(a => !a.Item2.IsNil)
            .ToList();

        if (getter.IsNil && setter.IsNil && !others.Any())
        {
            Trace?.Invoke($"Not imported property {_reader.ToString(src)}");
            return;
        }

        var dstHandle = _builder.AddProperty(src.Attributes, ImportValue(src.Name), ImportSignatureWithHeader(src.Signature));
        _propertyDefinitionCache.Add(srcHandle, dstHandle);

        Trace?.Invoke($"Imported property {_reader.ToString(src)} -> {RowId(dstHandle)}");

        using var _ = WithLogPrefix($"[{_reader.ToString(src)}]");

        if (!getter.IsNil)
        {
            _builder.AddMethodSemantics(dstHandle, MethodSemanticsAttributes.Getter, getter);
            Trace?.Invoke($"Imported getter {_reader.ToString(accessors.Getter)} -> {RowId(getter)}");
        }

        if (!setter.IsNil)
        {
            _builder.AddMethodSemantics(dstHandle, MethodSemanticsAttributes.Setter, setter);
            Trace?.Invoke($"Imported setter {_reader.ToString(accessors.Setter)} -> {RowId(setter)}");
        }

        foreach (var (srcAccessor, dstAccessor) in others)
        {
            _builder.AddMethodSemantics(dstHandle, MethodSemanticsAttributes.Other, dstAccessor);
            Trace?.Invoke($"Imported other {_reader.ToString(srcAccessor)} -> {RowId(dstAccessor)}");
        }

        var defaultValue = src.GetDefaultValue();
        if (!defaultValue.IsNil)
            ImportDefaultValue(defaultValue, dstHandle);
    }

    private void ImportGenericConstraints( EntityHandle entityHandle, GenericParameterHandleCollection srcParams )
    {
        var srcConstraints = new List<Tuple<GenericParameterHandle, GenericParameterConstraintHandle>>();

        foreach (var srcParamHandle in srcParams)
        {
            var srcParam = _reader.GetGenericParameter(srcParamHandle);
            var dstParamHandle = _builder.AddGenericParameter(entityHandle, srcParam.Attributes,
                ImportValue(srcParam.Name), srcParam.Index);
            _genericParameterCache.Add(srcParamHandle, dstParamHandle);
            srcConstraints.AddRange(srcParam.GetConstraints().Select(x => Tuple.Create(dstParamHandle, x)));

            Trace?.Invoke($"Imported generic parameter {_reader.ToString(srcParam)} -> {RowId(dstParamHandle)}");
        }

        foreach (var (dstParam, srcConstraintHandle) in srcConstraints)
            ImportEntity(srcConstraintHandle, _genericParameterConstraintCache,
                _reader.GetGenericParameterConstraint,
                src => _builder.AddGenericParameterConstraint(dstParam, Import(src.Type)),
                _reader.ToString, IsNil);
    }

    private void ImportDefaultValue( ConstantHandle defaultValue, EntityHandle dstHandle )
    {
        if (!defaultValue.IsNil)
        {
            var srcConst = _reader.GetConstant(defaultValue);
            var value = _reader.GetBlobReader(srcConst.Value).ReadConstant(srcConst.TypeCode);
            var dstConst = _builder.AddConstant(dstHandle, value);

            Trace?.Invoke($"Imported default value {_reader.ToString(srcConst)} -> {RowId(dstConst)} = {value}");
        }
    }

    public bool IsInternalsVisible() =>
        _reader.IsAssembly && _reader.GetAssemblyDefinition().GetCustomAttributes()
            .Select(_reader.GetCustomAttribute)
            .Select(_reader.GetCustomAttrClass)
            .Select(_reader.GetFullname)
            .Any(name => name == FullNames.InternalsVisibleTo);

    public bool IsReferenceAssembly() =>
        _reader.IsAssembly && _reader.GetAssemblyDefinition().GetCustomAttributes()
            .Select(_reader.GetCustomAttribute)
            .Select(_reader.GetCustomAttrClass)
            .Select(_reader.GetFullname)
            .Any(name => name == FullNames.ReferenceAssembly);

    public ReservedBlob<GuidHandle> Import()
    {
        if (_reader.IsAssembly)
        {
            var srcAssembly = _reader.GetAssemblyDefinition();

            _builder.AddAssembly(ImportValue(srcAssembly.Name), srcAssembly.Version,
                ImportValue(srcAssembly.Culture),
                ImportValue(srcAssembly.PublicKey), srcAssembly.Flags, srcAssembly.HashAlgorithm);
            Debug?.Invoke($"Imported assembly {_reader.ToString(srcAssembly)}");
        }

        var srcModule = _reader.GetModuleDefinition();

        var mvidBlob = _builder.ReserveGuid();

        _builder.AddModule(srcModule.Generation, ImportValue(srcModule.Name), mvidBlob.Handle,
            ImportValue(srcModule.GenerationId),
            ImportValue(srcModule.BaseGenerationId));
        Debug?.Invoke($"Imported module {_reader.ToString(srcModule)}");

        Debug?.Invoke("Importing assembly files");
        foreach (var srcHandle in _reader.AssemblyFiles)
            Import(srcHandle);

        Debug?.Invoke("Preparing type list for import");

        // 1. Process/assign numbers to the initial set of the imported types.
        var internalTypesToPreserve = new HashSet<TypeDefinitionHandle>();
        var initialTypeDefinitions = TypeImportPass(_reader, internalTypesToPreserve, Filter, Trace);

        if (Filter?.OmitNonApiMembers == true)
        {
            // 2. If required, seek for the additional internal types to import.
            Debug?.Invoke("Enumerating the internal types for import.");
            foreach (var internalTypeHandle in CalculateInternalTypesToPreserve(initialTypeDefinitions.Keys))
            {
                internalTypesToPreserve.Add(internalTypeHandle);
            }

            // 3. Enumerate the imported types again, to assign proper final numbering.
            _typeDefinitionCache = TypeImportPass(_reader, internalTypesToPreserve, Filter, Trace);
        }
        else
        {
            _typeDefinitionCache = initialTypeDefinitions;
        }

        Debug?.Invoke("Importing type definitions");
        foreach (var srcHandle in _reader.TypeDefinitions.Where(_typeDefinitionCache.ContainsKey))
        {
            var shouldOmitMembers = internalTypesToPreserve.Contains(srcHandle);
            var dstHandle = ImportTypeDefinitionSkeleton(srcHandle, shouldOmitMembers);
            if (dstHandle != _typeDefinitionCache[srcHandle])
                throw new Exception(
                    "WTF: type handle mismatch." +
                    $" Original type {_reader.GetFullname(srcHandle)}," +
                    $" already created handle {MetaUtil.RowId(srcHandle)}," +
                    $" new handle {MetaUtil.RowId(dstHandle)}.");
        }

        Debug?.Invoke("Importing type definition accessories");
        foreach (var (srcHandle, dstHandle) in _typeDefinitionCache)
            ImportTypeDefinitionAccessories(srcHandle, dstHandle);

        Debug?.Invoke("Importing method definition accessories");
        foreach (var (srcHandle, dstHandle) in _methodDefinitionCache)
            ImportMethodDefinitionAccessories(srcHandle, dstHandle);

        Debug?.Invoke("Importing field definition accessories");
        foreach (var (srcHandle, dstHandle) in _fieldDefinitionCache)
            ImportFieldDefinitionAccessories(srcHandle, dstHandle);

        Debug?.Invoke("Importing nested classes");
        var nestedTypes = _typeDefinitionCache
            .Select(kv => Tuple.Create(kv.Value, _reader.GetTypeDefinition(kv.Key).GetNestedTypes()))
            .SelectMany(x => x.Item2.Select(y => Tuple.Create(x.Item1, y, Import(y))))
            .Where(x => !x.Item3.IsNil)
            .OrderBy(x => RowId(x.Item3))
            .ToList();

        foreach (var (dstHandle, srcNested, dstNested) in nestedTypes)
        {
            _builder.AddNestedType(dstNested, dstHandle);
            Trace?.Invoke($"Imported nested type {_reader.ToString(srcNested)} -> {RowId(dstNested)}");
        }

        var generic = _typeDefinitionCache
            .Select(kv =>
                Tuple.Create((EntityHandle)kv.Value, _reader.GetTypeDefinition(kv.Key).GetGenericParameters()))
            .Concat(_methodDefinitionCache
                .Select(kv => Tuple.Create((EntityHandle)kv.Value,
                    _reader.GetMethodDefinition(kv.Key).GetGenericParameters())))
            .OrderBy(x => CodedIndex.TypeOrMethodDef(x.Item1))
            .ToList();

        Debug?.Invoke("Importing generic constraints");
        foreach (var (dstHandle, genericParams) in generic)
            ImportGenericConstraints(dstHandle, genericParams);

        Debug?.Invoke("Importing custom attributes");
        foreach (var src in _reader.CustomAttributes)
            Import(src);

        Debug?.Invoke("Importing declarative security attributes");
        foreach (var src in _reader.DeclarativeSecurityAttributes)
            Import(src);

        Debug?.Invoke("Importing exported types");
        foreach (var src in _reader.ExportedTypes)
            Import(src);

        if (!OmitReferenceAssemblyAttr && !MakeMock && !IsReferenceAssembly())
            AddReferenceAssemblyAttribute();

        if (MakeMock)
            _builder.GetOrAddBlob(_ilStream);

        Debug?.Invoke("Importing done");

        return mvidBlob;
    }

    private static Dictionary<TypeDefinitionHandle, TypeDefinitionHandle> TypeImportPass(
        MetadataReader reader,
        HashSet<TypeDefinitionHandle> internalTypesToPreserve,
        IImportFilter? filter,
        Action<string>? traceLog)
    {
        var checker = new CachedAttributeChecker();
        var typeDefinitions = new Dictionary<TypeDefinitionHandle, TypeDefinitionHandle>();
        var index = 1;
        foreach (var srcHandle in reader.TypeDefinitions)
        {
            bool shouldImport;

            var src = reader.GetTypeDefinition(srcHandle);

            // Special <Module> type
            if (srcHandle.GetHashCode() == 1 && reader.GetString(src.Name) == "<Module>")
            {
                shouldImport = true;
            }
            else if (checker.HasAttribute(reader, src, FullNames.Embedded) &&
                     checker.HasAttribute(reader, src, FullNames.CompilerGenerated))
            {
                traceLog?.Invoke($"Embedded type found {reader.ToString(srcHandle)}");
                shouldImport = true;
            }
            else if (reader.GetString(src.Namespace) == FullNames.CompilerServices &&
                     reader.GetFullname(src.BaseType) == FullNames.Attribute)
            {
                traceLog?.Invoke($"CompilerServices attribute found {reader.ToString(srcHandle)}");
                shouldImport = true;
            }
            else if (reader.GetString(src.Namespace) == FullNames.CodeAnalysis &&
                     reader.GetFullname(src.BaseType) == FullNames.Attribute)
            {
                traceLog?.Invoke($"CodeAnalysis attribute found {reader.ToString(srcHandle)}");
                shouldImport = true;
            }
            else if (internalTypesToPreserve.Contains(srcHandle))
            {
                shouldImport = true;
            }
            else
            {
                shouldImport = filter?.AllowImport(reader.GetTypeDefinition(srcHandle), reader) != false;
            }

            if (shouldImport)
            {
                typeDefinitions[srcHandle] = MetadataTokens.TypeDefinitionHandle(index++);
            }
            else
            {
                traceLog?.Invoke($"Type filtered and will not be imported {reader.ToString(srcHandle)}");
            }
        }

        return typeDefinitions;
    }

    /// <remarks>
    /// The point of this method is to make a value type non-empty in case we've decided to skip all its fields.
    /// </remarks>
    private void PostProcessSkippedValueTypeFields(
        List<FieldDefinition> skippedFields,
        List<FieldDefinition> importedFields)
    {
        if (importedFields.Count > 0) return; // we have imported some fields, no need to make the struct non-empty
        if (skippedFields.Count == 0) return; // we haven't skipped any fields; the struct was empty to begin with

        // We have skipped all fields, so we need to add a dummy field to make the struct non-empty.
        _builder.AddFieldDefinition(
            FieldAttributes.Private,
            _builder.GetOrAddString("<SyntheticNonEmptyStructMarker>"),
            _builder.GetOrAddBlob(new[] { (byte)SignatureKind.Field, (byte)SignatureTypeCode.Int32 }));
    }

    private IEnumerable<TypeDefinitionHandle> CalculateInternalTypesToPreserve(
        IReadOnlyCollection<TypeDefinitionHandle> importedTypeHandles)
    {
        var preservedTypes = new HashSet<TypeDefinitionHandle>(importedTypeHandles);
        var result = new List<TypeDefinitionHandle>();
        foreach (var importedTypeHandle in importedTypeHandles)
        {
            var candidateTypes = new List<TypeDefinitionHandle>();
            var type = _reader.GetTypeDefinition(importedTypeHandle);
            var collector = new UsedTypeCollector(candidateTypes);

            var parentTypes = new List<EntityHandle>();
            if (!type.BaseType.IsNil) parentTypes.Add(type.BaseType);
            foreach (var interfaceImplHandle in type.GetInterfaceImplementations())
            {
                var interfaceImpl = _reader.GetInterfaceImplementation(interfaceImplHandle);
                parentTypes.Add(interfaceImpl.Interface);
            }

            foreach (var parentTypeHandle in parentTypes)
            {
                switch (parentTypeHandle.Kind)
                {
                    case HandleKind.TypeDefinition:
                        candidateTypes.Add((TypeDefinitionHandle)parentTypeHandle);
                        break;
                    case HandleKind.TypeSpecification:
                        var specification = _reader.GetTypeSpecification((TypeSpecificationHandle)parentTypeHandle);
                        AcceptTypeSignature(specification.Signature, collector);
                        break;
                }
            }

            foreach (var fieldHandle in type.GetFields())
            {
                var field = _reader.GetFieldDefinition(fieldHandle);
                if (Filter == null || Filter.AllowImport(field, _reader))
                    AcceptFieldSignature(field, collector);
            }

            foreach (var methodHandle in type.GetMethods())
            {
                var method = _reader.GetMethodDefinition(methodHandle);
                if (Filter == null || Filter.AllowImport(method, _reader))
                    AcceptMethodSignature(method, collector);
            }

            foreach (var typeHandle in candidateTypes)
            {
                if (preservedTypes.Add(typeHandle))
                {
                    result.Add(typeHandle);
                    Debug?.Invoke($"Exposing internal type {_reader.GetFullname(typeHandle)}.");
                }
            }
        }

        return result;
    }

    private class UsedTypeCollector(List<TypeDefinitionHandle> collectedTypes) : ISignatureVisitor<object?>
    {
        public void VisitReader(BlobReader reader) { }

        public void WriteByte(byte @byte) { }

        public void WriteCompressedInteger(int integer) { }

        public void WriteCompressedSignedInteger(int integer) { }

        public void VisitTypeHandle(EntityHandle srcHandle)
        {
            switch (srcHandle.Kind)
            {
                case HandleKind.TypeReference:
                case HandleKind.TypeSpecification:
                    break;
                case HandleKind.TypeDefinition:
                    collectedTypes.Add((TypeDefinitionHandle)srcHandle);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(srcHandle),
                        $"Unexpected type handle kind: {srcHandle.Kind}.");
            }
        }

        public object? GetResult() => null;
    }
}
