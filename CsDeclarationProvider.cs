using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace SourceGeneratorCommons;

internal class CsDeclarationProvider : IDisposable
{
    private enum LockState
    {
        None,
        Read,
        Write,
    }

    private ref struct ExitUpgradableReadLock : IDisposable
    {
        private ReaderWriterLockSlim? _readerWriterLock;

        public ExitUpgradableReadLock(ReaderWriterLockSlim? readerWriterLock)
        {
            _readerWriterLock = readerWriterLock;
        }

        public void Dispose()
        {
            _readerWriterLock?.ExitUpgradeableReadLock();
            _readerWriterLock = null;
        }
    }

    private ref struct ExitWriteLock : IDisposable
    {
        private ReaderWriterLockSlim? _readerWriterLock;

        public ExitWriteLock(ReaderWriterLockSlim? readerWriterLock)
        {
            _readerWriterLock = readerWriterLock;
        }

        public void Dispose()
        {
            _readerWriterLock?.ExitWriteLock();
            _readerWriterLock = null;
        }
    }

    private Dictionary<ITypeSymbol, CsTypeDeclaration> _typeDeclarationDictionary = new Dictionary<ITypeSymbol, CsTypeDeclaration>(SymbolEqualityComparer.Default);

    private Dictionary<ITypeSymbol, CsTypeReference> _typeReferenceDictionary = new Dictionary<ITypeSymbol, CsTypeReference>(SymbolEqualityComparer.IncludeNullability);

    private ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

    public CsDeclarationProvider()
    {
    }

    public void Dispose()
    {
        _readWriteLock.Dispose();
    }

    internal CsTypeDeclaration GetTypeDeclaration(ITypeSymbol typeSymbol)
    {
        AssertLockState(_readWriteLock, LockState.None);

        return GetTypeDeclarationFromCachedTypeDeclarationFirst(typeSymbol, LockState.None);
    }

    internal CsTypeReference GetTypeReference(ITypeSymbol typeSymbol)
    {
        AssertLockState(_readWriteLock, LockState.None);

        return GetTypeReferenceFromCachedTypeReferenceFirst(typeSymbol, LockState.None);
    }

    internal CsMethodDeclaration GetMethodDeclaration(IMethodSymbol methodSymbol, CancellationToken cancellationToken)
    {
        AssertLockState(_readWriteLock, LockState.None);

        return GetMethodDeclaration(methodSymbol, LockState.None, cancellationToken);
    }

    private CsTypeDeclaration GetTypeDeclarationFromCachedTypeDeclarationFirst(ITypeSymbol typeSymbol, LockState lockState)
    {
        using var _ = EnterReadLockIfNecessary(lockState, out var currentLockState);

        if (_typeDeclarationDictionary.TryGetValue(typeSymbol, out var cachedTypeDeclaration))
            return cachedTypeDeclaration;

        return GetOrCreateTypeReferenceInWriteLock(typeSymbol, currentLockState).TypeDefinition;
    }

    private CsTypeReference GetTypeReferenceFromCachedTypeReferenceFirst(ITypeSymbol typeSymbol, LockState lockState)
    {
        using var _ = EnterReadLockIfNecessary(lockState, out var currentLockState);

        if (_typeReferenceDictionary.TryGetValue(typeSymbol, out var cachedTypeReferenceInfo))
            return cachedTypeReferenceInfo;

        return GetOrCreateTypeReferenceInWriteLock(typeSymbol, currentLockState);
    }

    private CsTypeReference GetOrCreateTypeReferenceInWriteLock(ITypeSymbol typeSymbol, LockState lockState)
    {
        using var _ = EnterWriteLockIfNecessary(lockState, out var currentLockState);

        if (_typeReferenceDictionary.TryGetValue(typeSymbol, out var cachedTypeReferenceInfo))
            return cachedTypeReferenceInfo;

        if (!_typeDeclarationDictionary.TryGetValue(typeSymbol, out var cachedTypeDeclaration))
        {
            cachedTypeDeclaration = CreateAndCacheTypeDeclaration(typeSymbol, currentLockState);
        }

        if (_typeReferenceDictionary.TryGetValue(typeSymbol, out cachedTypeReferenceInfo))
        {
            Debug.Assert(ReferenceEquals(cachedTypeDeclaration, cachedTypeReferenceInfo.TypeDefinition));
            return cachedTypeReferenceInfo;
        }

        cachedTypeReferenceInfo = CreateAndCacheTypeReference(cachedTypeDeclaration, typeSymbol, currentLockState);

        return cachedTypeReferenceInfo;
    }

    private CsMethodDeclaration GetMethodDeclaration(IMethodSymbol methodSymbol, LockState lockState, CancellationToken cancellationToken)
    {
        using var _ = EnterReadLockIfNecessary(lockState, out var currentLockState);

        var returnType = GetTypeReferenceFromCachedTypeReferenceFirst(methodSymbol.ReturnType, currentLockState);

        var methodModifier = (methodSymbol.IsSealed, methodSymbol.IsOverride, methodSymbol.IsAbstract, methodSymbol.IsVirtual) switch
        {
            (_, _, _, true) => MethodModifier.Virtual,
            (_, _, true, _) => MethodModifier.Abstract,
            (true, true, _, _) => MethodModifier.SealedOverride,
            (_, true, _, _) => MethodModifier.Override,
            _ => MethodModifier.Default,
        };

        var returnModifier = (methodSymbol.ReturnsByRef, methodSymbol.ReturnsByRefReadonly) switch
        {
            (_, true) => ReturnModifier.RefReadonly,
            (true, _) => ReturnModifier.Ref,
            _ => ReturnModifier.Default,
        };

        var methodParams = methodSymbol.Parameters.Select(v => BuildMethodParam(v, currentLockState)).ToImmutableArray();

        var genericTypeParams = methodSymbol.TypeParameters.Select(v => BuildGenericTypeParam(v, currentLockState)).ToImmutableArray();

        bool isReadOnly;
        CsAccessibility accessibility;
        if (methodSymbol.IsPartialDefinition && !methodSymbol.DeclaringSyntaxReferences.IsDefaultOrEmpty)
        {
            var methodDeclarationSyntax = (MethodDeclarationSyntax)methodSymbol.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
            (isReadOnly, accessibility) = FromMethodDeclarationSyntax(methodDeclarationSyntax);
        }
        else
        {
            isReadOnly = methodSymbol.IsReadOnly;
            accessibility = methodSymbol.DeclaredAccessibility.ToCSharpAccessibility();
        }

        return new CsMethodDeclaration(methodSymbol.Name, returnType, returnModifier, methodSymbol.IsStatic, methodSymbol.IsAsync, isReadOnly, methodParams, genericTypeParams, accessibility, methodModifier);


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

    private CsTypeDeclaration CreateAndCacheTypeDeclaration(ITypeSymbol typeSymbol, LockState lockState)
    {
        Debug.Assert(lockState == LockState.Write);
        AssertLockState(_readWriteLock, LockState.Write);

        if (_typeDeclarationDictionary.TryGetValue(typeSymbol, out var cachedTypeDeclaration))
        {
            Debug.Fail($"書き込みロックを取得した状態でキャッシュ済みの{nameof(CsTypeDeclaration)}がないときに呼び出されることがこのメソッドの実装の前提");
            return cachedTypeDeclaration;
        }

        if (typeSymbol is ITypeParameterSymbol typeParameterSymbol)
        {
            var typeParameterDeclaration = new CsTypeParameterDeclaration(typeSymbol.Name);

            _typeDeclarationDictionary.Add(typeSymbol, typeParameterDeclaration);

            return typeParameterDeclaration;
        }

        if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
        {
            var arrayDeclaration = new CsArrayDeclaration(typeSymbol.Name, arrayTypeSymbol.Rank, out var completeArrayDeclaration);
            _typeDeclarationDictionary.Add(arrayTypeSymbol, arrayDeclaration);

            var container = BuildContainer(arrayTypeSymbol, lockState);

            var elementTypeDeclaration = GetOrCreateTypeReferenceInWriteLock(arrayTypeSymbol.ElementType, lockState).TypeDefinition;

            completeArrayDeclaration(container, elementTypeDeclaration);

            return arrayDeclaration;
        }

        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.TypeKind == TypeKind.Enum)
            {
                var accessibility = namedTypeSymbol.DeclaredAccessibility.ToCSharpAccessibility();

                var underlyingType = namedTypeSymbol.EnumUnderlyingType switch
                {
                    { SpecialType: SpecialType.System_Byte } => EnumUnderlyingType.Byte,
                    { SpecialType: SpecialType.System_Int16 } => EnumUnderlyingType.Int16,
                    { SpecialType: SpecialType.System_Int32 } => EnumUnderlyingType.Int32,
                    { SpecialType: SpecialType.System_Int64 } => EnumUnderlyingType.Int64,
                    { SpecialType: SpecialType.System_SByte } => EnumUnderlyingType.SByte,
                    { SpecialType: SpecialType.System_UInt16 } => EnumUnderlyingType.UInt16,
                    { SpecialType: SpecialType.System_UInt32 } => EnumUnderlyingType.UInt32,
                    { SpecialType: SpecialType.System_UInt64 } => EnumUnderlyingType.UInt64,
                    _ => throw new NotSupportedException(),
                };

                // 自己参照する型パラメータを含むインターフェイスなどの存在による構築時の型参照の無限ループを回避するために、
                // 型情報の参照が必要なパラメータを除いた状態で作成。
                var enumDeclaration = new CsEnumDeclaration(namedTypeSymbol.Name, accessibility, underlyingType, out var completeEnumDeclaration);

                _typeDeclarationDictionary.Add(namedTypeSymbol, enumDeclaration);

                var container = BuildContainer(namedTypeSymbol, lockState);

                completeEnumDeclaration(container);

                return enumDeclaration;
            }

            if (namedTypeSymbol.TypeKind == TypeKind.Class)
            {
                var accessibility = namedTypeSymbol.DeclaredAccessibility.ToCSharpAccessibility();

                var classModifier = namedTypeSymbol switch
                {
                    { IsVirtual: true } => ClassModifier.Abstract,
                    { IsSealed: true } => ClassModifier.Sealed,
                    { IsStatic: true } => ClassModifier.Static,
                    _ => ClassModifier.Default,
                };

                // 自己参照する型パラメータを含むインターフェイスなどの存在による構築時の型参照の無限ループを回避するために、
                // 型情報の参照が必要なパラメータを除いた状態で作成。
                var classDeclaration = new CsClassDeclaration(
                            namedTypeSymbol.Name,
                            accessibility,
                            classModifier,
                            out var completeClassDeclaration);

                _typeDeclarationDictionary.Add(namedTypeSymbol, classDeclaration);

                var container = BuildContainer(namedTypeSymbol, lockState);

                var genericTypeParams = BuildGenericTypeParams(namedTypeSymbol, lockState);

                var interfaces = BuildInterfaces(namedTypeSymbol, lockState);

                if (!(namedTypeSymbol is { BaseType: { SpecialType: not SpecialType.System_Object } baseTypeSymbol }))
                    baseTypeSymbol = null;

                var baseTypeDeclaration = baseTypeSymbol is null ? null : GetOrCreateTypeReferenceInWriteLock(baseTypeSymbol, lockState);

                completeClassDeclaration(container, genericTypeParams, baseTypeDeclaration, interfaces);

                return classDeclaration;
            }

            if (namedTypeSymbol.TypeKind == TypeKind.Interface)
            {
                var accessibility = namedTypeSymbol.DeclaredAccessibility.ToCSharpAccessibility();

                // 自己参照する型パラメータを含むインターフェイスなどの存在による構築時の型参照の無限ループを回避するために、
                // 型情報の参照が必要なパラメータを除いた状態で作成。
                var interfaceDeclaration = new CsInterfaceDeclaration(
                    namedTypeSymbol.Name,
                    accessibility,
                    out var completeInterfaceDeclaration);

                _typeDeclarationDictionary.Add(namedTypeSymbol, interfaceDeclaration);

                var container = BuildContainer(namedTypeSymbol, lockState);

                var genericTypeParams = BuildGenericTypeParams(namedTypeSymbol, lockState);

                var interfaces = BuildInterfaces(namedTypeSymbol, lockState);

                completeInterfaceDeclaration(container, genericTypeParams, interfaces);

                return interfaceDeclaration;
            }

            if (namedTypeSymbol.TypeKind == TypeKind.Struct)
            {
                var accessibility = namedTypeSymbol.DeclaredAccessibility.ToCSharpAccessibility();

                // 自己参照する型パラメータを含むインターフェイスなどの存在による構築時の型参照の無限ループを回避するために、
                // 型情報の参照が必要なパラメータを除いた状態で作成。
                var structDeclaration = new CsStructDeclaration(
                    namedTypeSymbol.Name,
                    accessibility,
                    namedTypeSymbol.IsReadOnly,
                    namedTypeSymbol.IsRefLikeType,
                    out var completeStructDeclaration);

                _typeDeclarationDictionary.Add(namedTypeSymbol, structDeclaration);

                var container = BuildContainer(namedTypeSymbol, lockState);

                var genericTypeParams = BuildGenericTypeParams(namedTypeSymbol, lockState);

                var interfaces = BuildInterfaces(namedTypeSymbol, lockState);

                completeStructDeclaration(container, genericTypeParams, interfaces);

                return structDeclaration;
            }
        }

        throw new NotSupportedException();
    }

    private CsTypeReference CreateAndCacheTypeReference(CsTypeDeclaration csTypeDeclaration, ITypeSymbol typeSymbol, LockState lockState)
    {
        Debug.Assert(lockState == LockState.Write);
        AssertLockState(_readWriteLock, LockState.Write);

        if (_typeReferenceDictionary.TryGetValue(typeSymbol, out var cachedTypeReferenceInfo))
        {
            Debug.Fail($"書き込みロックを取得した状態でキャッシュ済みの{nameof(CsTypeReference)}がないときに呼び出されることがこのメソッドの実装の前提");
            return cachedTypeReferenceInfo;
        }

        CsTypeReference typeReference;
        if (csTypeDeclaration is CsGenericDefinableTypeDeclaration && typeSymbol is INamedTypeSymbol { TypeArguments: { IsDefaultOrEmpty: false } typeArguments })
        {
            if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
                throw new InvalidOperationException();

            var typeArgsBuilder = ImmutableArray.CreateBuilder<EquatableArray<CsTypeReference>>(countTypeArgsLength(namedTypeSymbol));
            fillTypeArgs(typeArgsBuilder, namedTypeSymbol, lockState);
            var typeArgs = typeArgsBuilder.MoveToImmutable();

            typeReference = new CsTypeReference
            {
                TypeDefinition = csTypeDeclaration,
                IsNullableAnnotated = namedTypeSymbol.NullableAnnotation == NullableAnnotation.Annotated,
                TypeArgs = typeArgs,
            };
        }
        else
        {
            typeReference = new CsTypeReference
            {
                TypeDefinition = csTypeDeclaration,
                IsNullableAnnotated = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated,
                TypeArgs = EquatableArray<EquatableArray<CsTypeReference>>.Empty,
            };
        }

        _typeReferenceDictionary.Add(typeSymbol, typeReference);

        return typeReference;

        int countTypeArgsLength(INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.ContainingType is not null)
                return countTypeArgsLength(namedTypeSymbol.ContainingType) + 1;
            else
                return 1;
        }

        void fillTypeArgs(ImmutableArray<EquatableArray<CsTypeReference>>.Builder typeArgsBuilder, INamedTypeSymbol namedTypeSymbol, LockState lockState)
        {
            if (namedTypeSymbol.ContainingType is not null)
                fillTypeArgs(typeArgsBuilder, namedTypeSymbol.ContainingType, lockState);

            typeArgsBuilder.Add(namedTypeSymbol.TypeArguments.Select(v => GetTypeReferenceFromCachedTypeReferenceFirst(v, lockState)).ToImmutableArray());
        }
    }

    private GenericTypeParam BuildGenericTypeParam(ITypeParameterSymbol typeParameterSymbol, LockState lockState)
    {
        GenericConstraintTypeCategory genericConstraintTypeCategory;
        if (typeParameterSymbol.HasReferenceTypeConstraint)
        {
            if (typeParameterSymbol.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated)
                genericConstraintTypeCategory = GenericConstraintTypeCategory.NullableClass;
            else
                genericConstraintTypeCategory = GenericConstraintTypeCategory.Class;
        }
        else if (typeParameterSymbol.HasValueTypeConstraint)
            genericConstraintTypeCategory = GenericConstraintTypeCategory.Struct;
        else if (typeParameterSymbol.HasNotNullConstraint)
            genericConstraintTypeCategory = GenericConstraintTypeCategory.NotNull;
        else if (typeParameterSymbol.HasUnmanagedTypeConstraint)
            genericConstraintTypeCategory = GenericConstraintTypeCategory.Unmanaged;
        else
            genericConstraintTypeCategory = GenericConstraintTypeCategory.Any;

        var constraints = new GenericTypeConstraints
        {
            TypeCategory = genericConstraintTypeCategory,
            HaveDefaultConstructor = typeParameterSymbol.HasConstructorConstraint,
#if CODE_ANALYSYS4_12_2_OR_GREATER
            AllowRefStruct = typeParameterSymbol.AllowsRefLikeType,
#endif

            // 存在する場合は後で設定
            BaseType = null,
            Interfaces = EquatableArray<CsTypeReference>.Empty,
        };

        var baseType = typeParameterSymbol.ConstraintTypes.FirstOrDefault(v => !v.IsAbstract);
        var interfaces = typeParameterSymbol.ConstraintTypes.Where(v => v.IsAbstract);

        if (baseType is not null || interfaces.Any())
        {
            using (EnterReadLockIfNecessary(lockState, out var currentLockState))
            {
                constraints = constraints with
                {
                    BaseType = baseType is null ? null : GetTypeReferenceFromCachedTypeReferenceFirst(baseType, currentLockState),
                    Interfaces = typeParameterSymbol.ConstraintTypes
                        .Where(v => v.IsAbstract)
                        .Select(v => GetTypeReferenceFromCachedTypeReferenceFirst(v, currentLockState))
                        .ToImmutableArray(),
                };
            }
        }

        var genericTypeParam = new GenericTypeParam
        {
            Name = typeParameterSymbol.Name,
            Where = constraints,
        };

        return genericTypeParam;
    }

    private MethodParam BuildMethodParam(IParameterSymbol parameterSymbol, LockState lockState)
    {
        var paramType = GetTypeReferenceFromCachedTypeReferenceFirst(parameterSymbol.Type, lockState);

        var paramModifier = parameterSymbol.RefKind switch
        {
            RefKind.Ref => ParamModifier.Ref,
            RefKind.In => ParamModifier.In,
            RefKind.Out => ParamModifier.Out,
#if CODE_ANALYSYS4_8_0_OR_GREATER
            RefKind.RefReadOnlyParameter => ParamModifier.RefReadOnly,
#endif
            _ => ParamModifier.Default,
        };

        bool isScoped = false;
#if CODE_ANALYSYS4_4_0_OR_GREATER
        isScoped = parameterSymbol.ScopedKind == ScopedKind.ScopedRef;
#endif

        return new MethodParam(paramType, parameterSymbol.Name, paramModifier, isScoped);
    }

    private ITypeContainer BuildContainer(ITypeSymbol typeSymbol, LockState lockState)
    {
        ITypeContainer? container;

        if (typeSymbol.ContainingType is null)
        {
            var namespaceBuilder = new StringBuilder();
            SymbolExtensions.AppendFullNamespace(namespaceBuilder, typeSymbol.ContainingNamespace);

            container = new NameSpaceInfo(namespaceBuilder.ToString());
        }
        else
        {
            container = GetTypeDeclarationFromCachedTypeDeclarationFirst(typeSymbol.ContainingType, lockState);
        }

        return container;
    }

    private EquatableArray<GenericTypeParam> BuildGenericTypeParams(INamedTypeSymbol namedTypeSymbol, LockState lockState)
    {
        EquatableArray<GenericTypeParam> genericTypeParams;

        if (!namedTypeSymbol.TypeArguments.IsDefaultOrEmpty)
        {
            var originalDefinitionTypeSymbol = namedTypeSymbol.OriginalDefinition;

            var typeParamsBuilder = ImmutableArray.CreateBuilder<GenericTypeParam>(originalDefinitionTypeSymbol.TypeParameters.Length);

            for (int i = 0; i < namedTypeSymbol.TypeParameters.Length; i++)
            {
                var genericTypeParam = BuildGenericTypeParam(namedTypeSymbol.TypeParameters[i], lockState);

                typeParamsBuilder.Add(genericTypeParam);
            }

            genericTypeParams = typeParamsBuilder.MoveToImmutable();
        }
        else
        {
            genericTypeParams = EquatableArray<GenericTypeParam>.Empty;
        }

        return genericTypeParams;
    }

    private EquatableArray<CsTypeReference> BuildInterfaces(INamedTypeSymbol namedTypeSymbol, LockState lockState)
    {
        using (EnterReadLockIfNecessary(lockState, out var currentLockState))
        {
            var interfaces = namedTypeSymbol.Interfaces.Select(v => GetTypeReferenceFromCachedTypeReferenceFirst(v, currentLockState)).ToImmutableArray().ToEquatableArray();
            return interfaces;
        }
    }


    [Conditional("DEBUG")]
    private static void AssertLockState(ReaderWriterLockSlim readerWriterLock, LockState expectedState)
    {
        switch (expectedState)
        {
            case LockState.Write:
                Debug.Assert(readerWriterLock.IsWriteLockHeld);
                break;
            case LockState.Read:
                Debug.Assert(readerWriterLock.IsUpgradeableReadLockHeld);
                break;
            default:
                Debug.Assert(readerWriterLock is { IsReadLockHeld: false, IsUpgradeableReadLockHeld: false, IsWriteLockHeld: false });
                break;
        }
    }

    private ExitUpgradableReadLock EnterReadLockIfNecessary(LockState prevState, out LockState currentState)
    {
        return EnterReadLockIfNecessary(_readWriteLock, prevState, out currentState);
    }

    private ExitWriteLock EnterWriteLockIfNecessary(LockState prevState, out LockState currentState)
    {
        return EnterWriteLockIfNecessary(_readWriteLock, prevState, out currentState);
    }

    private static ExitUpgradableReadLock EnterReadLockIfNecessary(ReaderWriterLockSlim readerWriterLock, LockState prevState, out LockState currentState)
    {
        AssertLockState(readerWriterLock, prevState);

        if (prevState == LockState.None)
        {
            readerWriterLock.EnterUpgradeableReadLock();
            currentState = LockState.Read;
            return new ExitUpgradableReadLock(readerWriterLock);
        }
        else
        {
            currentState = prevState;
            return new ExitUpgradableReadLock(null);
        }
    }

    private static ExitWriteLock EnterWriteLockIfNecessary(ReaderWriterLockSlim readerWriterLock, LockState prevState, out LockState currentState)
    {
        AssertLockState(readerWriterLock, prevState);

        if (prevState != LockState.Write)
        {
            readerWriterLock.EnterWriteLock();
            currentState = LockState.Write;
            return new ExitWriteLock(readerWriterLock);
        }
        else
        {
            currentState = prevState;
            return new ExitWriteLock(null);
        }
    }
}
