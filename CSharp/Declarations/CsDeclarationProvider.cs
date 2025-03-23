#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGeneratorCommons.Collections.Generic;
using SourceGeneratorCommons.Collections.Special;
using SourceGeneratorCommons.CSharp.Declarations.Internals;
using System.Collections.Immutable;
using System.Text;

using CodeAnalysysSpecialType = Microsoft.CodeAnalysis.SpecialType;

namespace SourceGeneratorCommons.CSharp.Declarations;

internal class CsDeclarationProvider
{
    public Compilation Compilation { get; }

    public ref SpecialTypes SpecialType => ref _specialType;

    private const int MaxNestCount = 100;

    private CancellationToken _rootCancellationToken;

    private HashTable<ITypeSymbol, CsTypeDeclaration> _typeDeclarationDictionary;

    private HashTable<CsTypeDeclaration, CsTypeRef> _typeReferenceDictionary;

    public SpecialTypes _specialType;


    public struct SpecialTypes
    {
        public CsTypeRef Object => _object ?? _provider.GetTypeReferenceByMetadataName("System.Object").ToNotNullWithAssert();
        public CsTypeRef String => _string ?? _provider.GetTypeReferenceByMetadataName("System.String").ToNotNullWithAssert();
        public CsTypeRef Char => _char ?? _provider.GetTypeReferenceByMetadataName("System.Char").ToNotNullWithAssert();
        public CsTypeRef Byte => _byte ?? _provider.GetTypeReferenceByMetadataName("System.Byte").ToNotNullWithAssert();
        public CsTypeRef SByte => _sByte ?? _provider.GetTypeReferenceByMetadataName("System.SByte").ToNotNullWithAssert();
        public CsTypeRef Short => _int16 ?? _provider.GetTypeReferenceByMetadataName("System.Int16").ToNotNullWithAssert();
        public CsTypeRef Int => _int32 ?? _provider.GetTypeReferenceByMetadataName("System.Int32").ToNotNullWithAssert();
        public CsTypeRef Long => _int64 ?? _provider.GetTypeReferenceByMetadataName("System.Int64").ToNotNullWithAssert();
        public CsTypeRef UShort => _uInt16 ?? _provider.GetTypeReferenceByMetadataName("System.UInt16").ToNotNullWithAssert();
        public CsTypeRef UInt => _uInt32 ?? _provider.GetTypeReferenceByMetadataName("System.UInt32").ToNotNullWithAssert();
        public CsTypeRef ULong => _uInt64 ?? _provider.GetTypeReferenceByMetadataName("System.UInt64").ToNotNullWithAssert();
        public CsTypeRef Float => _single ?? _provider.GetTypeReferenceByMetadataName("System.Single").ToNotNullWithAssert();
        public CsTypeRef Double => _double ?? _provider.GetTypeReferenceByMetadataName("System.Double").ToNotNullWithAssert();
        public CsTypeRef Decimal => _decimal ?? _provider.GetTypeReferenceByMetadataName("System.Decimal").ToNotNullWithAssert();
        public CsTypeRef Guid => _guid ?? _provider.GetTypeReferenceByMetadataName("System.Guid").ToNotNullWithAssert();
        public CsTypeRef Type => _type ?? _provider.GetTypeReferenceByMetadataName("System.Type").ToNotNullWithAssert();
        public CsTypeRef Attribute => _attribute ?? _provider.GetTypeReferenceByMetadataName("System.Attribute").ToNotNullWithAssert();
        public CsTypeRef NullableT => _nullableT ?? _provider.GetTypeReferenceByMetadataName("System.Nullable`1").ToNotNullWithAssert();

        private CsDeclarationProvider _provider;
        private CsTypeRef? _type;
        private CsTypeRef? _guid;
        private CsTypeRef? _decimal;
        private CsTypeRef? _double;
        private CsTypeRef? _single;
        private CsTypeRef? _uInt64;
        private CsTypeRef? _uInt32;
        private CsTypeRef? _uInt16;
        private CsTypeRef? _int64;
        private CsTypeRef? _int32;
        private CsTypeRef? _int16;
        private CsTypeRef? _sByte;
        private CsTypeRef? _byte;
        private CsTypeRef? _char;
        private CsTypeRef? _string;
        private CsTypeRef? _object;
        private CsTypeRef? _attribute;
        private CsTypeRef? _nullableT;

        public SpecialTypes(CsDeclarationProvider provider) => _provider = provider;
    }

    public CsDeclarationProvider(Compilation compilation, CancellationToken rootCancellationToken)
    {
        var lockObj = new Lock();

        Compilation = compilation;

        _rootCancellationToken = rootCancellationToken;

        _typeDeclarationDictionary = new HashTable<ITypeSymbol, CsTypeDeclaration>(lockObj, SymbolEqualityComparer.Default);

        _typeReferenceDictionary = new HashTable<CsTypeDeclaration, CsTypeRef>(lockObj, ReferenceEqualityComparer<CsTypeDeclaration>.Default);

        _specialType = new SpecialTypes(this);
    }

    internal CsTypeDeclaration GetTypeDeclaration(ITypeSymbol typeSymbol)
    {
        _rootCancellationToken.ThrowIfCancellationRequested();

        var typeDeclaration = GetTypeDeclarationFromCachedTypeDeclarationFirst(typeSymbol, nest: 0);

        // 並行スレッドで作られているインスタンスの生成完了を待機
        ((ILazyConstructionRoot)typeDeclaration).ConstructionFullCompleted.Wait(_rootCancellationToken);

        return typeDeclaration;
    }

    internal CsTypeRefWithAnnotation GetTypeReference(ITypeSymbol typeSymbol)
    {
        _rootCancellationToken.ThrowIfCancellationRequested();

        var typeReference = GetTypeReferenceFromCachedTypeReferenceFirst(typeSymbol, nest: 0);

        // 並行スレッドで作られているインスタンスの生成完了を待機
        ((ILazyConstructionRoot)typeReference.Type).ConstructionFullCompleted.Wait(_rootCancellationToken);

        return typeReference;
    }

    internal CsTypeRef? GetTypeReferenceByMetadataName(string fullyQualifiedMetadataName)
    {
        var typeSymbol = Compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);

        if (typeSymbol is null)
            return null;

        return GetTypeReference(typeSymbol).Type;
    }

    internal CsTypeRefWithAnnotation MakeNullableTypeReference(CsTypeRefWithAnnotation typeReference)
    {
        return MakeNullableTypeReference(typeReference.Type);
    }

    internal CsTypeRefWithAnnotation MakeNullableTypeReference(CsTypeRef typeReference)
    {
        _rootCancellationToken.ThrowIfCancellationRequested();

        if (typeReference.TypeDefinition.IsValueType)
        {
            // エラー型は暫定的に参照型と同じ扱いにするので値型としてハンドリングしない
            DebugSGen.Assert(typeReference.TypeDefinition is not CsErrorType);

            if (typeReference.TypeDefinition.Is(CsSpecialType.NullableT))
            {
                // typeRefernceが元々Nullable<T>
                return typeReference.WithAnnotation(true);
            }
            else
            {
                // 生の値型をNullable<T>でラップ
                var nullableValueType = SpecialType.NullableT.WithTypeArgs(EquatableArray.Create(EquatableArray.Create(typeReference.WithAnnotation(false))));
                return nullableValueType.WithAnnotation(true);
            }
        }
        else
        {
            // 参照型
            return typeReference.WithAnnotation(true);
        }
    }

    internal CsMethod GetMethodDeclaration(IMethodSymbol methodSymbol)
    {
        _rootCancellationToken.ThrowIfCancellationRequested();

        return GetMethodDeclarationInternal(methodSymbol, nest: 0);
    }

    private CsTypeDeclaration GetTypeDeclarationFromCachedTypeDeclarationFirst(ITypeSymbol typeSymbol, int nest)
    {
        nest++;
        if (nest > MaxNestCount) throw new InvalidOperationException("呼出しの再帰が深すぎます。");
        _rootCancellationToken.ThrowIfCancellationRequested();

        if (_typeDeclarationDictionary.TryGetValue(typeSymbol, out var cachedTypeDeclaration))
            return cachedTypeDeclaration;

        return CreateAndCacheTypeDeclaration(typeSymbol, nest);
    }

    private CsTypeRefWithAnnotation GetTypeReferenceFromCachedTypeReferenceFirst(ITypeSymbol typeSymbol, int nest)
    {
        nest++;
        if (nest > MaxNestCount) throw new InvalidOperationException("呼出しの再帰が深すぎます。");
        _rootCancellationToken.ThrowIfCancellationRequested();

        bool isNullableIfRefereceType = !typeSymbol.IsValueType && typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;

        var typeDeclaration = GetTypeDeclarationFromCachedTypeDeclarationFirst(typeSymbol, nest);

        if (_typeReferenceDictionary.TryGetValue(typeDeclaration, out var cachedTypeReference))
            return new (cachedTypeReference, isNullableIfRefereceType);

        return new (CreateAndCacheTypeReference(typeDeclaration, typeSymbol, nest), isNullableIfRefereceType);
    }

    private CsMethod GetMethodDeclarationInternal(IMethodSymbol methodSymbol, int nest)
    {
        nest++;
        if (nest > MaxNestCount) throw new InvalidOperationException("呼出しの再帰が深すぎます。");
        _rootCancellationToken.ThrowIfCancellationRequested();

        var returnType = GetTypeReferenceFromCachedTypeReferenceFirst(methodSymbol.ReturnType, nest);

        var methodModifier = methodSymbol.ToCsMethodModifier();

        var returnModifier = methodSymbol.ToCsReturnModifier();

        var methodParams = methodSymbol.Parameters.Select(v => BuildMethodParam(v, nest)).ToImmutableArray();

        var isSystemSymbolParameter = IsSystemSymbol(methodSymbol.ContainingType);

        var genericTypeParams = methodSymbol.TypeParameters.Select(v => BuildGenericTypeParam(v, isSystemSymbolParameter, nest)).ToImmutableArray();

        bool isReadOnly;
        CsAccessibility accessibility;
        if (methodSymbol.IsPartialDefinition && !methodSymbol.DeclaringSyntaxReferences.IsDefaultOrEmpty)
        {
            var methodDeclarationSyntax = (MethodDeclarationSyntax)methodSymbol.DeclaringSyntaxReferences[0].GetSyntax(_rootCancellationToken);
            (isReadOnly, accessibility) = FromMethodDeclarationSyntax(methodDeclarationSyntax);
        }
        else
        {
            isReadOnly = methodSymbol.IsReadOnly;
            accessibility = methodSymbol.DeclaredAccessibility.ToCSharpAccessibility();
        }

        return new CsMethod(methodSymbol.Name, returnType, returnModifier, methodSymbol.IsStatic, methodSymbol.IsAsync, isReadOnly, methodParams, genericTypeParams, accessibility, methodModifier);


        (bool isReadOnly, CsAccessibility accessibility) FromMethodDeclarationSyntax(MethodDeclarationSyntax methodDeclarationSyntax)
        {
            var haveReadOnly = false;
            var havePublic = false;
            var haveProtected = false;
            var haveInternal = false;
            var havePrivate = false;

            for (int i = 0; i < methodDeclarationSyntax.Modifiers.Count; i++)
            {
                var modifier = methodDeclarationSyntax.Modifiers[i];

                if (modifier.IsKind(SyntaxKind.ReadOnlyKeyword))
                    haveReadOnly = true;
                if (modifier.IsKind(SyntaxKind.PublicKeyword))
                    havePublic = true;
                if (modifier.IsKind(SyntaxKind.ProtectedKeyword))
                    haveProtected = true;
                if (modifier.IsKind(SyntaxKind.InternalKeyword))
                    haveInternal = true;
                if (modifier.IsKind(SyntaxKind.PrivateKeyword))
                    havePrivate = true;
            }

            accessibility = (havePublic, haveProtected, haveInternal, havePrivate) switch
            {
                (true, false, false, false) => CsAccessibility.Public,
                (false, true, true, false) => CsAccessibility.ProtectedInternal,
                (false, false, true, false) => CsAccessibility.Internal,
                (false, true, false, false) => CsAccessibility.Protected,
                (false, false, false, true) => CsAccessibility.Private,
                _ => CsAccessibility.Default,
            };

            return (haveReadOnly, accessibility);
        }
    }

    private CsTypeDeclaration CreateAndCacheTypeDeclaration(ITypeSymbol typeSymbol, int nest)
    {
        nest++;
        if (nest > MaxNestCount) throw new InvalidOperationException("呼出しの再帰が深すぎます。");
        _rootCancellationToken.ThrowIfCancellationRequested();

        bool isAdded;

        if (typeSymbol is ITypeParameterSymbol typeParameterSymbol)
        {
            var typeParameterDeclaration = _typeDeclarationDictionary.GetOrAdd(
                typeParameterSymbol,
                static typeParameterSymbol => new CsTypeParameterDeclaration(GetNameWithInternIfSystem(typeParameterSymbol)),
                out isAdded);

            DebugSGen.Assert(((ILazyConstructionRoot)typeParameterDeclaration).ConstructionFullCompleted.IsCompleted);

            return typeParameterDeclaration;
        }

        if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
        {
            Action<ITypeContainer?, CsTypeRefWithAnnotation>? completeArrayDeclaration = null;

            var arrayDeclaration = _typeDeclarationDictionary.GetOrAdd(arrayTypeSymbol, ref completeArrayDeclaration,
                static (IArrayTypeSymbol arrayTypeSymbol, ref Action<ITypeContainer?, CsTypeRefWithAnnotation>? completeArrayDeclaration) =>
                {
                    return new CsArray(GetNameWithInternIfSystem(arrayTypeSymbol), arrayTypeSymbol.Rank, out completeArrayDeclaration);
                },
                out isAdded);

            if (isAdded)
            {
                DebugSGen.AssertIsNotNull(completeArrayDeclaration);

                var container = BuildContainer(arrayTypeSymbol, nest);

                var elementTypeReference = GetTypeReferenceFromCachedTypeReferenceFirst(arrayTypeSymbol.ElementType, nest);

                completeArrayDeclaration(container, elementTypeReference);
            }
            
            return arrayDeclaration;
        }

        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.TypeKind == TypeKind.Enum)
            {
                Action<ITypeContainer?>? completeEnumDeclaration = null;

                var enumDeclaration = _typeDeclarationDictionary.GetOrAdd(namedTypeSymbol, ref completeEnumDeclaration,
                    static (INamedTypeSymbol namedTypeSymbol, ref Action<ITypeContainer?>? completeEnumDeclaration) =>
                    {
                        var accessibility = namedTypeSymbol.DeclaredAccessibility.ToCSharpAccessibility();

                        var underlyingType = namedTypeSymbol.EnumUnderlyingType switch
                        {
                            { SpecialType: CodeAnalysysSpecialType.System_Byte } => CsEnumUnderlyingType.Byte,
                            { SpecialType: CodeAnalysysSpecialType.System_Int16 } => CsEnumUnderlyingType.Int16,
                            { SpecialType: CodeAnalysysSpecialType.System_Int32 } => CsEnumUnderlyingType.Int32,
                            { SpecialType: CodeAnalysysSpecialType.System_Int64 } => CsEnumUnderlyingType.Int64,
                            { SpecialType: CodeAnalysysSpecialType.System_SByte } => CsEnumUnderlyingType.SByte,
                            { SpecialType: CodeAnalysysSpecialType.System_UInt16 } => CsEnumUnderlyingType.UInt16,
                            { SpecialType: CodeAnalysysSpecialType.System_UInt32 } => CsEnumUnderlyingType.UInt32,
                            { SpecialType: CodeAnalysysSpecialType.System_UInt64 } => CsEnumUnderlyingType.UInt64,
                            _ => throw new NotSupportedException(),
                        };

                        // 自己参照する型パラメータを含むインターフェイスなどの存在による構築時の型参照の無限ループを回避するために、
                        // 型情報の参照が必要なパラメータを除いた状態で作成。
                        return new CsEnum(GetNameWithInternIfSystem(namedTypeSymbol), accessibility, underlyingType, out completeEnumDeclaration);
                    },
                    out isAdded);

                if (isAdded)
                {
                    DebugSGen.AssertIsNotNull(completeEnumDeclaration);

                    var container = BuildContainer(namedTypeSymbol, nest);

                    completeEnumDeclaration(container);
                }
                
                return enumDeclaration;
            }

            if (namedTypeSymbol.TypeKind == TypeKind.Class)
            {
                Action<ITypeContainer?, EquatableArray<CsGenericTypeParam>, CsTypeRef?, EquatableArray<CsTypeRef>>? completeClassDeclaration = null;

                var classDeclaration = _typeDeclarationDictionary.GetOrAdd(namedTypeSymbol, ref completeClassDeclaration,
                    static (INamedTypeSymbol namedTypeSymbol, ref Action<ITypeContainer?, EquatableArray<CsGenericTypeParam>, CsTypeRef?, EquatableArray<CsTypeRef>>? completeClassDeclaration) =>
                    {
                        var accessibility = namedTypeSymbol.DeclaredAccessibility.ToCSharpAccessibility();

                        var classModifier = namedTypeSymbol switch
                        {
                            { IsVirtual: true } => CsClassModifier.Abstract,
                            { IsSealed: true } => CsClassModifier.Sealed,
                            { IsStatic: true } => CsClassModifier.Static,
                            _ => CsClassModifier.Default,
                        };

                        // 自己参照する型パラメータを含むインターフェイスなどの存在による構築時の型参照の無限ループを回避するために、
                        // 型情報の参照が必要なパラメータを除いた状態で作成。
                        return new CsClass(
                            GetNameWithInternIfSystem(namedTypeSymbol),
                            accessibility,
                            classModifier,
                            out completeClassDeclaration);
                    },
                    out isAdded);

                if (isAdded)
                {
                    DebugSGen.AssertIsNotNull(completeClassDeclaration);

                    var container = BuildContainer(namedTypeSymbol, nest);

                    var genericTypeParams = BuildGenericTypeParams(namedTypeSymbol, nest);

                    var interfaces = BuildInterfaces(namedTypeSymbol, nest);

                    if (!(namedTypeSymbol is { BaseType: { SpecialType: not CodeAnalysysSpecialType.System_Object } baseTypeSymbol }))
                        baseTypeSymbol = null;

                    var baseTypeReference = baseTypeSymbol is null ? null : GetTypeReferenceFromCachedTypeReferenceFirst(baseTypeSymbol, nest).Type;

                    completeClassDeclaration(container, genericTypeParams, baseTypeReference, interfaces);
                }

                return classDeclaration;
            }

            if (namedTypeSymbol.TypeKind == TypeKind.Interface)
            {
                Action<ITypeContainer?, EquatableArray<CsGenericTypeParam>, EquatableArray<CsTypeRef>>? completeInterfaceDeclaration = null;

                var interfaceDeclaration = _typeDeclarationDictionary.GetOrAdd(namedTypeSymbol, ref completeInterfaceDeclaration,
                    static (INamedTypeSymbol namedTypeSymbol, ref Action<ITypeContainer?, EquatableArray<CsGenericTypeParam>, EquatableArray<CsTypeRef>>? completeInterfaceDeclaration) =>
                    {
                        var accessibility = namedTypeSymbol.DeclaredAccessibility.ToCSharpAccessibility();

                        // 自己参照する型パラメータを含むインターフェイスなどの存在による構築時の型参照の無限ループを回避するために、
                        // 型情報の参照が必要なパラメータを除いた状態で作成。
                        return new CsInterface(
                            GetNameWithInternIfSystem(namedTypeSymbol),
                            accessibility,
                            out completeInterfaceDeclaration);
                    },
                    out isAdded);

                if (isAdded)
                {
                    DebugSGen.AssertIsNotNull(completeInterfaceDeclaration);

                    var container = BuildContainer(namedTypeSymbol, nest);

                    var genericTypeParams = BuildGenericTypeParams(namedTypeSymbol, nest);

                    var interfaces = BuildInterfaces(namedTypeSymbol, nest);

                    completeInterfaceDeclaration(container, genericTypeParams, interfaces);
                }

                return interfaceDeclaration;
            }

            if (namedTypeSymbol.TypeKind == TypeKind.Struct)
            {
                Action<ITypeContainer?, EquatableArray<CsGenericTypeParam>, EquatableArray<CsTypeRef>>? completeStructDeclaration = null;

                var structDeclaration = _typeDeclarationDictionary.GetOrAdd(namedTypeSymbol, ref completeStructDeclaration,
                    static (INamedTypeSymbol namedTypeSymbol, ref Action<ITypeContainer?, EquatableArray<CsGenericTypeParam>, EquatableArray<CsTypeRef>>? completeStructDeclaration) =>
                    {
                        var accessibility = namedTypeSymbol.DeclaredAccessibility.ToCSharpAccessibility();

                        // 自己参照する型パラメータを含むインターフェイスなどの存在による構築時の型参照の無限ループを回避するために、
                        // 型情報の参照が必要なパラメータを除いた状態で作成。
                        return new CsStruct(
                            GetNameWithInternIfSystem(namedTypeSymbol),
                            accessibility,
                            namedTypeSymbol.IsReadOnly,
                            namedTypeSymbol.IsRefLikeType,
                            out completeStructDeclaration);
                    },
                    out isAdded);

                if (isAdded)
                {
                    DebugSGen.AssertIsNotNull(completeStructDeclaration);

                    var container = BuildContainer(namedTypeSymbol, nest);

                    var genericTypeParams = BuildGenericTypeParams(namedTypeSymbol, nest);

                    var interfaces = BuildInterfaces(namedTypeSymbol, nest);

                    completeStructDeclaration(container, genericTypeParams, interfaces);
                }

                return structDeclaration;
            }


            if (namedTypeSymbol.TypeKind == TypeKind.Delegate)
            {
                Action<ITypeContainer?, CsTypeRefWithAnnotation, EquatableArray<CsMethodParam>, EquatableArray<CsGenericTypeParam>>? completeDelegateDeclaration = null;

                var delegateDeclaration = _typeDeclarationDictionary.GetOrAdd(namedTypeSymbol, ref completeDelegateDeclaration,
                    static (INamedTypeSymbol namedTypeSymbol, ref Action<ITypeContainer?, CsTypeRefWithAnnotation, EquatableArray<CsMethodParam>, EquatableArray<CsGenericTypeParam>>? completeInterfaceDeclaration) =>
                    {
                        var accessibility = namedTypeSymbol.DeclaredAccessibility.ToCSharpAccessibility();

                        DebugSGen.AssertIsNotNull(namedTypeSymbol.OriginalDefinition.DelegateInvokeMethod);

                        var returnModifier = namedTypeSymbol.OriginalDefinition.DelegateInvokeMethod.ToCsReturnModifier();

                        // 自己参照する型パラメータを含むインターフェイスなどの存在による構築時の型参照の無限ループを回避するために、
                        // 型情報の参照が必要なパラメータを除いた状態で作成。
                        return new CsDelegate(
                            GetNameWithInternIfSystem(namedTypeSymbol),
                            accessibility,
                            returnModifier,
                            out completeInterfaceDeclaration);
                    },
                    out isAdded);

                if (isAdded)
                {
                    DebugSGen.AssertIsNotNull(completeDelegateDeclaration);

                    var container = BuildContainer(namedTypeSymbol, nest);

                    DebugSGen.AssertIsNotNull(namedTypeSymbol.OriginalDefinition.DelegateInvokeMethod);

                    var returnType = GetTypeReferenceFromCachedTypeReferenceFirst(namedTypeSymbol.OriginalDefinition.DelegateInvokeMethod.ReturnType, nest);

                    var methodParams = namedTypeSymbol.OriginalDefinition.DelegateInvokeMethod.Parameters.Select(v => BuildMethodParam(v, nest)).ToImmutableArray();

                    var genericTypeParams = BuildGenericTypeParams(namedTypeSymbol, nest);

                    completeDelegateDeclaration(container, returnType, methodParams, genericTypeParams);
                }

                return delegateDeclaration;
            }
        }

        if (typeSymbol.TypeKind == TypeKind.Error)
        {
            var errorType = _typeDeclarationDictionary.GetOrAdd(
                typeSymbol,
                static typeSymbol => new CsErrorType(typeSymbol.Name), // エラータイプの名前のintern化は常に不要
                out isAdded);

            DebugSGen.Assert(((ILazyConstructionRoot)errorType).ConstructionFullCompleted.IsCompleted);

            return errorType;
        }

        throw new NotSupportedException();
    }

    private CsTypeRef CreateAndCacheTypeReference(CsTypeDeclaration csTypeDeclaration, ITypeSymbol typeSymbol, int nest)
    {
        nest++;
        if (nest > MaxNestCount) throw new InvalidOperationException("呼出しの再帰が深すぎます。");
        _rootCancellationToken.ThrowIfCancellationRequested();

        var typeReference = _typeReferenceDictionary.GetOrAdd(csTypeDeclaration, (self: this, csTypeDeclaration, typeSymbol),
            static (csTypeDeclaration, createArg) =>
            {
                if (createArg.csTypeDeclaration is CsGenericDefinableTypeDeclaration && createArg.typeSymbol is INamedTypeSymbol { TypeArguments: { IsDefaultOrEmpty: false } typeArguments })
                {
                    if (createArg.typeSymbol is not INamedTypeSymbol namedTypeSymbol)
                        throw new InvalidOperationException();

                    var typeArgsBuilder = ImmutableArray.CreateBuilder<EquatableArray<CsTypeRefWithAnnotation>>(countTypeArgsLength(namedTypeSymbol));
                    fillTypeArgs(createArg.self, typeArgsBuilder, namedTypeSymbol);
                    var typeArgs = typeArgsBuilder.MoveToImmutable();

                    return new CsTypeRef(
                        createArg.csTypeDeclaration,
                        typeArgs);
                }
                else
                {
                    return new CsTypeRef(
                        createArg.csTypeDeclaration,
                        EquatableArray<EquatableArray<CsTypeRefWithAnnotation>>.Empty
                        );
                }
            }, out _);

        return typeReference;

        static int countTypeArgsLength(INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.ContainingType is not null)
                return countTypeArgsLength(namedTypeSymbol.ContainingType) + 1;
            else
                return 1;
        }

        static void fillTypeArgs(CsDeclarationProvider self, ImmutableArray<EquatableArray<CsTypeRefWithAnnotation>>.Builder typeArgsBuilder, INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.ContainingType is not null)
                fillTypeArgs(self, typeArgsBuilder, namedTypeSymbol.ContainingType);

            typeArgsBuilder.Add(namedTypeSymbol.TypeArguments.Select(self.GetTypeReferenceFromCachedTypeReferenceFirst).ToImmutableArray());
        }
    }

    private CsGenericTypeParam BuildGenericTypeParam(ITypeParameterSymbol typeParameterSymbol, bool isSystemSymbolParameter, int nest)
    {
        nest++;
        if (nest > MaxNestCount) throw new InvalidOperationException("呼出しの再帰が深すぎます。");
        _rootCancellationToken.ThrowIfCancellationRequested();

        CsGenericConstraintTypeCategory genericConstraintTypeCategory;
        if (typeParameterSymbol.HasReferenceTypeConstraint)
        {
            if (typeParameterSymbol.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated)
                genericConstraintTypeCategory = CsGenericConstraintTypeCategory.NullableClass;
            else
                genericConstraintTypeCategory = CsGenericConstraintTypeCategory.Class;
        }
        else if (typeParameterSymbol.HasValueTypeConstraint)
            genericConstraintTypeCategory = CsGenericConstraintTypeCategory.Struct;
        else if (typeParameterSymbol.HasNotNullConstraint)
            genericConstraintTypeCategory = CsGenericConstraintTypeCategory.NotNull;
        else if (typeParameterSymbol.HasUnmanagedTypeConstraint)
            genericConstraintTypeCategory = CsGenericConstraintTypeCategory.Unmanaged;
        else
            genericConstraintTypeCategory = CsGenericConstraintTypeCategory.Any;

        var baseType = typeParameterSymbol.ConstraintTypes.FirstOrDefault(v => !v.IsAbstract);
        var interfaces = typeParameterSymbol.ConstraintTypes.Where(v => v.IsAbstract);

        var constraints = new CsGenericTypeConstraints
        {
            TypeCategory = genericConstraintTypeCategory,
            HaveDefaultConstructor = typeParameterSymbol.HasConstructorConstraint,
#if CODE_ANALYSYS4_12_2_OR_GREATER
            AllowRefStruct = typeParameterSymbol.AllowsRefLikeType,
#endif

            BaseType = baseType is null ? null : GetTypeReferenceFromCachedTypeReferenceFirst(baseType, nest).Type,
            Interfaces = typeParameterSymbol.ConstraintTypes
                .Where(v => v.IsAbstract)
                .Select(v => GetTypeReferenceFromCachedTypeReferenceFirst(v, nest).Type)
                .ToImmutableArray(),
        };

        var genericTypeParam = new CsGenericTypeParam
        {
            Name  = GetInterned(typeParameterSymbol.Name, withIntern: isSystemSymbolParameter),
            Where = constraints,
        };

        return genericTypeParam;
    }

    private CsMethodParam BuildMethodParam(IParameterSymbol parameterSymbol, int nest)
    {
        nest++;
        if (nest > MaxNestCount) throw new InvalidOperationException("呼出しの再帰が深すぎます。");
        _rootCancellationToken.ThrowIfCancellationRequested();

        var paramType = GetTypeReferenceFromCachedTypeReferenceFirst(parameterSymbol.Type, nest);

        var paramModifier = parameterSymbol.RefKind switch
        {
            RefKind.Ref => CsParamModifier.Ref,
            RefKind.In => CsParamModifier.In,
            RefKind.Out => CsParamModifier.Out,
#if CODE_ANALYSYS4_8_0_OR_GREATER
            RefKind.RefReadOnlyParameter => CsParamModifier.RefReadOnly,
#endif
            _ => CsParamModifier.Default,
        };

        bool isScoped = false;
#if CODE_ANALYSYS4_4_0_OR_GREATER
        isScoped = parameterSymbol.ScopedKind == ScopedKind.ScopedRef;
#endif

        if (parameterSymbol.HasExplicitDefaultValue)
        {
            DebugSGen.Assert(isScoped == false);
            return new CsMethodParamWithDefaultValue(paramType, parameterSymbol.Name, parameterSymbol.ExplicitDefaultValue, paramModifier);
        }
        else
        {
            return new CsMethodParam(paramType, parameterSymbol.Name, paramModifier, isScoped);
        }
    }

    private ITypeContainer? BuildContainer(ITypeSymbol typeSymbol, int nest)
    {
        nest++;
        if (nest > MaxNestCount) throw new InvalidOperationException("呼出しの再帰が深すぎます。");
        _rootCancellationToken.ThrowIfCancellationRequested();

        if (typeSymbol.ContainingType is not null)
            return GetTypeDeclarationFromCachedTypeDeclarationFirst(typeSymbol.ContainingType, nest);

        if (typeSymbol.ContainingNamespace is not null)
        {
            var namespaceBuilder = new StringBuilder();
            SymbolExtensions.AppendFullNamespace(namespaceBuilder, typeSymbol.ContainingNamespace);

            return new CsNameSpace(namespaceBuilder.ToString());
        }

        return null;
    }

    private EquatableArray<CsGenericTypeParam> BuildGenericTypeParams(INamedTypeSymbol namedTypeSymbol, int nest)
    {
        nest++;
        if (nest > MaxNestCount) throw new InvalidOperationException("呼出しの再帰が深すぎます。");
        _rootCancellationToken.ThrowIfCancellationRequested();

        EquatableArray<CsGenericTypeParam> genericTypeParams;

        if (!namedTypeSymbol.TypeArguments.IsDefaultOrEmpty)
        {
            var originalDefinitionTypeSymbol = namedTypeSymbol.OriginalDefinition;

            var isSystemSymbolParameter = IsSystemSymbol(namedTypeSymbol);

            var typeParamsBuilder = ImmutableArray.CreateBuilder<CsGenericTypeParam>(originalDefinitionTypeSymbol.TypeParameters.Length);

            for (int i = 0; i < namedTypeSymbol.TypeParameters.Length; i++)
            {
                var genericTypeParam = BuildGenericTypeParam(namedTypeSymbol.TypeParameters[i], isSystemSymbolParameter, nest);

                typeParamsBuilder.Add(genericTypeParam);
            }

            genericTypeParams = typeParamsBuilder.MoveToImmutable();
        }
        else
        {
            genericTypeParams = EquatableArray<CsGenericTypeParam>.Empty;
        }

        return genericTypeParams;
    }

    private EquatableArray<CsTypeRef> BuildInterfaces(INamedTypeSymbol namedTypeSymbol, int nest)
    {
        nest++;
        if (nest > MaxNestCount) throw new InvalidOperationException("呼出しの再帰が深すぎます。");
        _rootCancellationToken.ThrowIfCancellationRequested();

        var interfaces = namedTypeSymbol.Interfaces.Select(v => GetTypeReferenceFromCachedTypeReferenceFirst(v, nest).Type).ToImmutableArray().ToEquatableArray();
        return interfaces;
    }


    private static string GetInterned(string value, bool withIntern)
    {
        var internedValue = withIntern ? string.Intern(value) : string.IsInterned(value) ?? value;
        return internedValue;
    }

    private static string GetNameWithInternIfSystem(ISymbol symbol)
    {
        bool isSystemType = IsSystemSymbol(symbol);
        var symbolName = GetInterned(symbol.Name, withIntern: isSystemType);
        return symbolName;
    }

    private static bool IsSystemSymbol(ISymbol symbol)
    {
        if (symbol is { Kind: SymbolKind.Namespace, Name: "System", ContainingNamespace: { IsGlobalNamespace: true } })
            return true;

        if (symbol.ContainingNamespace is not null)
            return IsSystemSymbol(symbol.ContainingNamespace);

        return false;
    }
}
