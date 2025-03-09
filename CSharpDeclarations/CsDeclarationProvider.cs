using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGeneratorCommons.Collections.Generic;
using SourceGeneratorCommons.Collections.Special;
using SourceGeneratorCommons.CSharpDeclarations.Internals;
using System.Collections.Immutable;
using System.Text;

namespace SourceGeneratorCommons.CSharpDeclarations;

internal class CsDeclarationProvider
{
    private CancellationToken _rootCancellationToken;

    private HashTable<ITypeSymbol, CsTypeDeclaration> _typeDeclarationDictionary;

    private HashTable<ITypeSymbol, CsTypeReference> _typeReferenceDictionary;

    public CsDeclarationProvider(CancellationToken rootCancellationToken)
    {
        var lockObj = new Lock();

        _rootCancellationToken = rootCancellationToken;

        _typeDeclarationDictionary = new HashTable<ITypeSymbol, CsTypeDeclaration>(lockObj, SymbolEqualityComparer.Default);

        _typeReferenceDictionary = new HashTable<ITypeSymbol, CsTypeReference>(lockObj, SymbolEqualityComparer.IncludeNullability);
    }

    internal CsTypeDeclaration GetTypeDeclaration(ITypeSymbol typeSymbol)
    {
        _rootCancellationToken.ThrowIfCancellationRequested();

        var typeDeclaration = GetTypeDeclarationFromCachedTypeDeclarationFirst(typeSymbol);

        // 並行スレッドで作られているインスタンスの生成完了を待機
        ((ILazyConstructionRoot)typeDeclaration).ConstructionFullCompleted.Wait(_rootCancellationToken);

        return typeDeclaration;
    }

    internal CsTypeReference GetTypeReference(ITypeSymbol typeSymbol)
    {
        _rootCancellationToken.ThrowIfCancellationRequested();

        var typeReference = GetTypeReferenceFromCachedTypeReferenceFirst(typeSymbol);

        // 並行スレッドで作られているインスタンスの生成完了を待機
        ((ILazyConstructionRoot)typeReference).ConstructionFullCompleted.Wait(_rootCancellationToken);

        return typeReference;
    }

    internal CsMethodDeclaration GetMethodDeclaration(IMethodSymbol methodSymbol)
    {
        _rootCancellationToken.ThrowIfCancellationRequested();

        return GetMethodDeclarationInternal(methodSymbol);
    }

    private CsTypeDeclaration GetTypeDeclarationFromCachedTypeDeclarationFirst(ITypeSymbol typeSymbol)
    {
        _rootCancellationToken.ThrowIfCancellationRequested();

        if (_typeDeclarationDictionary.TryGetValue(typeSymbol, out var cachedTypeDeclaration))
            return cachedTypeDeclaration;

        return CreateAndCacheTypeDeclaration(typeSymbol);
    }

    private CsTypeReference GetTypeReferenceFromCachedTypeReferenceFirst(ITypeSymbol typeSymbol)
    {
        _rootCancellationToken.ThrowIfCancellationRequested();

        if (_typeReferenceDictionary.TryGetValue(typeSymbol, out var cachedTypeReferenceInfo))
            return cachedTypeReferenceInfo;

        var typeDeclaration = GetTypeDeclarationFromCachedTypeDeclarationFirst(typeSymbol);

        return CreateAndCacheTypeReference(typeDeclaration, typeSymbol);
    }

    private CsMethodDeclaration GetMethodDeclarationInternal(IMethodSymbol methodSymbol)
    {
        _rootCancellationToken.ThrowIfCancellationRequested();

        var returnType = GetTypeReferenceFromCachedTypeReferenceFirst(methodSymbol.ReturnType);

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

        var methodParams = methodSymbol.Parameters.Select(v => BuildMethodParam(v)).ToImmutableArray();

        var genericTypeParams = methodSymbol.TypeParameters.Select(v => BuildGenericTypeParam(v)).ToImmutableArray();

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

    private CsTypeDeclaration CreateAndCacheTypeDeclaration(ITypeSymbol typeSymbol)
    {
        _rootCancellationToken.ThrowIfCancellationRequested();

        bool isAdded;

        if (typeSymbol is ITypeParameterSymbol typeParameterSymbol)
        {
            var typeParameterDeclaration = _typeDeclarationDictionary.GetOrAdd(
                typeSymbol,
                static typeSymbol => new CsTypeParameterDeclaration(typeSymbol.Name),
                out isAdded);

            DebugSGen.Assert(((ILazyConstructionRoot)typeParameterDeclaration).ConstructionFullCompleted.IsCompleted);

            return typeParameterDeclaration;
        }

        if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
        {
            Action<ITypeContainer?, CsTypeDeclaration>? completeArrayDeclaration = null;

            var arrayDeclaration = _typeDeclarationDictionary.GetOrAdd(arrayTypeSymbol, ref completeArrayDeclaration,
                static (IArrayTypeSymbol arrayTypeSymbol, ref Action<ITypeContainer?, CsTypeDeclaration>? completeArrayDeclaration) =>
                {
                    return new CsArrayDeclaration(arrayTypeSymbol.Name, arrayTypeSymbol.Rank, out completeArrayDeclaration);
                },
                out isAdded);

            if (isAdded)
            {
                DebugSGen.AssertIsNotNull(completeArrayDeclaration);

                var container = BuildContainer(arrayTypeSymbol);

                var elementTypeDeclaration = GetTypeDeclarationFromCachedTypeDeclarationFirst(arrayTypeSymbol.ElementType);

                completeArrayDeclaration(container, elementTypeDeclaration);
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
                        return new CsEnumDeclaration(namedTypeSymbol.Name, accessibility, underlyingType, out completeEnumDeclaration);
                    },
                    out isAdded);

                if (isAdded)
                {
                    DebugSGen.AssertIsNotNull(completeEnumDeclaration);

                    var container = BuildContainer(namedTypeSymbol);

                    completeEnumDeclaration(container);
                }
                
                return enumDeclaration;
            }

            if (namedTypeSymbol.TypeKind == TypeKind.Class)
            {
                Action<ITypeContainer?, EquatableArray<GenericTypeParam>, CsTypeReference?, EquatableArray<CsTypeReference>>? completeClassDeclaration = null;

                var classDeclaration = _typeDeclarationDictionary.GetOrAdd(namedTypeSymbol, ref completeClassDeclaration,
                    static (INamedTypeSymbol namedTypeSymbol, ref Action<ITypeContainer?, EquatableArray<GenericTypeParam>, CsTypeReference?, EquatableArray<CsTypeReference>>? completeClassDeclaration) =>
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
                        return new CsClassDeclaration(
                            namedTypeSymbol.Name,
                            accessibility,
                            classModifier,
                            out completeClassDeclaration);
                    },
                    out isAdded);

                if (isAdded)
                {
                    DebugSGen.AssertIsNotNull(completeClassDeclaration);

                    var container = BuildContainer(namedTypeSymbol);

                    var genericTypeParams = BuildGenericTypeParams(namedTypeSymbol);

                    var interfaces = BuildInterfaces(namedTypeSymbol);

                    if (!(namedTypeSymbol is { BaseType: { SpecialType: not SpecialType.System_Object } baseTypeSymbol }))
                        baseTypeSymbol = null;

                    var baseTypeReference = baseTypeSymbol is null ? null : GetTypeReferenceFromCachedTypeReferenceFirst(baseTypeSymbol);

                    completeClassDeclaration(container, genericTypeParams, baseTypeReference, interfaces);
                }

                return classDeclaration;
            }

            if (namedTypeSymbol.TypeKind == TypeKind.Interface)
            {
                Action<ITypeContainer?, EquatableArray<GenericTypeParam>, EquatableArray<CsTypeReference>>? completeInterfaceDeclaration = null;

                var interfaceDeclaration = _typeDeclarationDictionary.GetOrAdd(namedTypeSymbol, ref completeInterfaceDeclaration,
                    static (INamedTypeSymbol namedTypeSymbol, ref Action<ITypeContainer?, EquatableArray<GenericTypeParam>, EquatableArray<CsTypeReference>>? completeInterfaceDeclaration) =>
                    {
                        var accessibility = namedTypeSymbol.DeclaredAccessibility.ToCSharpAccessibility();

                        // 自己参照する型パラメータを含むインターフェイスなどの存在による構築時の型参照の無限ループを回避するために、
                        // 型情報の参照が必要なパラメータを除いた状態で作成。
                        return new CsInterfaceDeclaration(
                            namedTypeSymbol.Name,
                            accessibility,
                            out completeInterfaceDeclaration);
                    },
                    out isAdded);

                if (isAdded)
                {
                    DebugSGen.AssertIsNotNull(completeInterfaceDeclaration);

                    var container = BuildContainer(namedTypeSymbol);

                    var genericTypeParams = BuildGenericTypeParams(namedTypeSymbol);

                    var interfaces = BuildInterfaces(namedTypeSymbol);

                    completeInterfaceDeclaration(container, genericTypeParams, interfaces);
                }

                return interfaceDeclaration;
            }

            if (namedTypeSymbol.TypeKind == TypeKind.Struct)
            {
                Action<ITypeContainer?, EquatableArray<GenericTypeParam>, EquatableArray<CsTypeReference>>? completeStructDeclaration = null;

                var structDeclaration = _typeDeclarationDictionary.GetOrAdd(namedTypeSymbol, ref completeStructDeclaration,
                    static (INamedTypeSymbol namedTypeSymbol, ref Action<ITypeContainer?, EquatableArray<GenericTypeParam>, EquatableArray<CsTypeReference>>? completeStructDeclaration) =>
                    {
                        var accessibility = namedTypeSymbol.DeclaredAccessibility.ToCSharpAccessibility();

                        // 自己参照する型パラメータを含むインターフェイスなどの存在による構築時の型参照の無限ループを回避するために、
                        // 型情報の参照が必要なパラメータを除いた状態で作成。
                        return new CsStructDeclaration(
                            namedTypeSymbol.Name,
                            accessibility,
                            namedTypeSymbol.IsReadOnly,
                            namedTypeSymbol.IsRefLikeType,
                            out completeStructDeclaration);
                    },
                    out isAdded);

                if (isAdded)
                {
                    DebugSGen.AssertIsNotNull(completeStructDeclaration);

                    var container = BuildContainer(namedTypeSymbol);

                    var genericTypeParams = BuildGenericTypeParams(namedTypeSymbol);

                    var interfaces = BuildInterfaces(namedTypeSymbol);

                    completeStructDeclaration(container, genericTypeParams, interfaces);
                }

                return structDeclaration;
            }
        }

        throw new NotSupportedException();
    }

    private CsTypeReference CreateAndCacheTypeReference(CsTypeDeclaration csTypeDeclaration, ITypeSymbol typeSymbol)
    {
        _rootCancellationToken.ThrowIfCancellationRequested();

        var typeReference = _typeReferenceDictionary.GetOrAdd(typeSymbol, (self: this, csTypeDeclaration, typeSymbol),
            static (typeSymbol, createArg) =>
            {
                if (createArg.csTypeDeclaration is CsGenericDefinableTypeDeclaration && typeSymbol is INamedTypeSymbol { TypeArguments: { IsDefaultOrEmpty: false } typeArguments })
                {
                    if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
                        throw new InvalidOperationException();

                    var typeArgsBuilder = ImmutableArray.CreateBuilder<EquatableArray<CsTypeReference>>(countTypeArgsLength(namedTypeSymbol));
                    fillTypeArgs(createArg.self, typeArgsBuilder, namedTypeSymbol);
                    var typeArgs = typeArgsBuilder.MoveToImmutable();

                    return new CsTypeReference(
                        createArg.csTypeDeclaration,
                        namedTypeSymbol.NullableAnnotation == NullableAnnotation.Annotated,
                        typeArgs);
                }
                else
                {
                    return new CsTypeReference(
                        createArg.csTypeDeclaration,
                        typeSymbol.NullableAnnotation == NullableAnnotation.Annotated,
                        EquatableArray<EquatableArray<CsTypeReference>>.Empty
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

        static void fillTypeArgs(CsDeclarationProvider self, ImmutableArray<EquatableArray<CsTypeReference>>.Builder typeArgsBuilder, INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.ContainingType is not null)
                fillTypeArgs(self, typeArgsBuilder, namedTypeSymbol.ContainingType);

            typeArgsBuilder.Add(namedTypeSymbol.TypeArguments.Select(self.GetTypeReferenceFromCachedTypeReferenceFirst).ToImmutableArray());
        }
    }

    private GenericTypeParam BuildGenericTypeParam(ITypeParameterSymbol typeParameterSymbol)
    {
        _rootCancellationToken.ThrowIfCancellationRequested();

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

        var baseType = typeParameterSymbol.ConstraintTypes.FirstOrDefault(v => !v.IsAbstract);
        var interfaces = typeParameterSymbol.ConstraintTypes.Where(v => v.IsAbstract);

        var constraints = new GenericTypeConstraints
        {
            TypeCategory = genericConstraintTypeCategory,
            HaveDefaultConstructor = typeParameterSymbol.HasConstructorConstraint,
#if CODE_ANALYSYS4_12_2_OR_GREATER
            AllowRefStruct = typeParameterSymbol.AllowsRefLikeType,
#endif

            BaseType = baseType is null ? null : GetTypeReferenceFromCachedTypeReferenceFirst(baseType),
            Interfaces = typeParameterSymbol.ConstraintTypes
                .Where(v => v.IsAbstract)
                .Select(v => GetTypeReferenceFromCachedTypeReferenceFirst(v))
                .ToImmutableArray(),
        };

        var genericTypeParam = new GenericTypeParam
        {
            Name = typeParameterSymbol.Name,
            Where = constraints,
        };

        return genericTypeParam;
    }

    private MethodParam BuildMethodParam(IParameterSymbol parameterSymbol)
    {
        _rootCancellationToken.ThrowIfCancellationRequested();

        var paramType = GetTypeReferenceFromCachedTypeReferenceFirst(parameterSymbol.Type);

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

    private ITypeContainer BuildContainer(ITypeSymbol typeSymbol)
    {
        _rootCancellationToken.ThrowIfCancellationRequested();

        ITypeContainer? container;

        if (typeSymbol.ContainingType is null)
        {
            var namespaceBuilder = new StringBuilder();
            SymbolExtensions.AppendFullNamespace(namespaceBuilder, typeSymbol.ContainingNamespace);

            container = new NameSpaceInfo(namespaceBuilder.ToString());
        }
        else
        {
            container = GetTypeDeclarationFromCachedTypeDeclarationFirst(typeSymbol.ContainingType);
        }

        return container;
    }

    private EquatableArray<GenericTypeParam> BuildGenericTypeParams(INamedTypeSymbol namedTypeSymbol)
    {
        _rootCancellationToken.ThrowIfCancellationRequested();

        EquatableArray<GenericTypeParam> genericTypeParams;

        if (!namedTypeSymbol.TypeArguments.IsDefaultOrEmpty)
        {
            var originalDefinitionTypeSymbol = namedTypeSymbol.OriginalDefinition;

            var typeParamsBuilder = ImmutableArray.CreateBuilder<GenericTypeParam>(originalDefinitionTypeSymbol.TypeParameters.Length);

            for (int i = 0; i < namedTypeSymbol.TypeParameters.Length; i++)
            {
                var genericTypeParam = BuildGenericTypeParam(namedTypeSymbol.TypeParameters[i]);

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

    private EquatableArray<CsTypeReference> BuildInterfaces(INamedTypeSymbol namedTypeSymbol)
    {
        _rootCancellationToken.ThrowIfCancellationRequested();

        var interfaces = namedTypeSymbol.Interfaces.Select(GetTypeReferenceFromCachedTypeReferenceFirst).ToImmutableArray().ToEquatableArray();
        return interfaces;
    }
}
