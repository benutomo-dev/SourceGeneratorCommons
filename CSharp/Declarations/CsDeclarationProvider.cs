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

namespace SourceGeneratorCommons.CSharp.Declarations;

internal class CsDeclarationProvider
{
    public Compilation Compilation { get; }

    private const int MaxNestCount = 30;

    private CancellationToken _rootCancellationToken;

    private HashTable<ITypeSymbol, CsTypeDeclaration> _typeDeclarationDictionary;

    private HashTable<ITypeSymbol, CsTypeReference> _typeReferenceDictionary;

    public CsDeclarationProvider(Compilation compilation, CancellationToken rootCancellationToken)
    {
        var lockObj = new Lock();

        Compilation = compilation;

        _rootCancellationToken = rootCancellationToken;

        _typeDeclarationDictionary = new HashTable<ITypeSymbol, CsTypeDeclaration>(lockObj, SymbolEqualityComparer.Default);

        _typeReferenceDictionary = new HashTable<ITypeSymbol, CsTypeReference>(lockObj, SymbolEqualityComparer.IncludeNullability);
    }

    internal CsTypeDeclaration GetTypeDeclaration(ITypeSymbol typeSymbol)
    {
        _rootCancellationToken.ThrowIfCancellationRequested();

        var typeDeclaration = GetTypeDeclarationFromCachedTypeDeclarationFirst(typeSymbol, nest: 0);

        // 並行スレッドで作られているインスタンスの生成完了を待機
        ((ILazyConstructionRoot)typeDeclaration).ConstructionFullCompleted.Wait(_rootCancellationToken);

        return typeDeclaration;
    }

    internal CsTypeRefWithNullability GetTypeReference(ITypeSymbol typeSymbol)
    {
        _rootCancellationToken.ThrowIfCancellationRequested();

        var typeReference = GetTypeReferenceFromCachedTypeReferenceFirst(typeSymbol, nest: 0);

        // 並行スレッドで作られているインスタンスの生成完了を待機
        ((ILazyConstructionRoot)typeReference.Type).ConstructionFullCompleted.Wait(_rootCancellationToken);

        return typeReference;
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

    private CsTypeRefWithNullability GetTypeReferenceFromCachedTypeReferenceFirst(ITypeSymbol typeSymbol, int nest)
    {
        nest++;
        if (nest > MaxNestCount) throw new InvalidOperationException("呼出しの再帰が深すぎます。");
        _rootCancellationToken.ThrowIfCancellationRequested();

        bool isNullableIfRefereceType = typeSymbol.IsReferenceType && typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;

        if (_typeReferenceDictionary.TryGetValue(typeSymbol, out var cachedTypeReference))
            return new (cachedTypeReference, isNullableIfRefereceType);

        var typeDeclaration = GetTypeDeclarationFromCachedTypeDeclarationFirst(typeSymbol, nest);

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

        var genericTypeParams = methodSymbol.TypeParameters.Select(v => BuildGenericTypeParam(v, nest)).ToImmutableArray();

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
                typeSymbol,
                static typeSymbol => new CsTypeParameterDeclaration(typeSymbol.Name),
                out isAdded);

            DebugSGen.Assert(((ILazyConstructionRoot)typeParameterDeclaration).ConstructionFullCompleted.IsCompleted);

            return typeParameterDeclaration;
        }

        if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
        {
            Action<ITypeContainer?, CsTypeRefWithNullability>? completeArrayDeclaration = null;

            var arrayDeclaration = _typeDeclarationDictionary.GetOrAdd(arrayTypeSymbol, ref completeArrayDeclaration,
                static (IArrayTypeSymbol arrayTypeSymbol, ref Action<ITypeContainer?, CsTypeRefWithNullability>? completeArrayDeclaration) =>
                {
                    return new CsArray(arrayTypeSymbol.Name, arrayTypeSymbol.Rank, out completeArrayDeclaration);
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
                            { SpecialType: SpecialType.System_Byte } => CsEnumUnderlyingType.Byte,
                            { SpecialType: SpecialType.System_Int16 } => CsEnumUnderlyingType.Int16,
                            { SpecialType: SpecialType.System_Int32 } => CsEnumUnderlyingType.Int32,
                            { SpecialType: SpecialType.System_Int64 } => CsEnumUnderlyingType.Int64,
                            { SpecialType: SpecialType.System_SByte } => CsEnumUnderlyingType.SByte,
                            { SpecialType: SpecialType.System_UInt16 } => CsEnumUnderlyingType.UInt16,
                            { SpecialType: SpecialType.System_UInt32 } => CsEnumUnderlyingType.UInt32,
                            { SpecialType: SpecialType.System_UInt64 } => CsEnumUnderlyingType.UInt64,
                            _ => throw new NotSupportedException(),
                        };

                        // 自己参照する型パラメータを含むインターフェイスなどの存在による構築時の型参照の無限ループを回避するために、
                        // 型情報の参照が必要なパラメータを除いた状態で作成。
                        return new CsEnum(namedTypeSymbol.Name, accessibility, underlyingType, out completeEnumDeclaration);
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
                Action<ITypeContainer?, EquatableArray<CsGenericTypeParam>, CsTypeReference?, EquatableArray<CsTypeReference>>? completeClassDeclaration = null;

                var classDeclaration = _typeDeclarationDictionary.GetOrAdd(namedTypeSymbol, ref completeClassDeclaration,
                    static (INamedTypeSymbol namedTypeSymbol, ref Action<ITypeContainer?, EquatableArray<CsGenericTypeParam>, CsTypeReference?, EquatableArray<CsTypeReference>>? completeClassDeclaration) =>
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
                            namedTypeSymbol.Name,
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

                    if (!(namedTypeSymbol is { BaseType: { SpecialType: not SpecialType.System_Object } baseTypeSymbol }))
                        baseTypeSymbol = null;

                    var baseTypeReference = baseTypeSymbol is null ? null : GetTypeReferenceFromCachedTypeReferenceFirst(baseTypeSymbol, nest).Type;

                    completeClassDeclaration(container, genericTypeParams, baseTypeReference, interfaces);
                }

                return classDeclaration;
            }

            if (namedTypeSymbol.TypeKind == TypeKind.Interface)
            {
                Action<ITypeContainer?, EquatableArray<CsGenericTypeParam>, EquatableArray<CsTypeReference>>? completeInterfaceDeclaration = null;

                var interfaceDeclaration = _typeDeclarationDictionary.GetOrAdd(namedTypeSymbol, ref completeInterfaceDeclaration,
                    static (INamedTypeSymbol namedTypeSymbol, ref Action<ITypeContainer?, EquatableArray<CsGenericTypeParam>, EquatableArray<CsTypeReference>>? completeInterfaceDeclaration) =>
                    {
                        var accessibility = namedTypeSymbol.DeclaredAccessibility.ToCSharpAccessibility();

                        // 自己参照する型パラメータを含むインターフェイスなどの存在による構築時の型参照の無限ループを回避するために、
                        // 型情報の参照が必要なパラメータを除いた状態で作成。
                        return new CsInterface(
                            namedTypeSymbol.Name,
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
                Action<ITypeContainer?, EquatableArray<CsGenericTypeParam>, EquatableArray<CsTypeReference>>? completeStructDeclaration = null;

                var structDeclaration = _typeDeclarationDictionary.GetOrAdd(namedTypeSymbol, ref completeStructDeclaration,
                    static (INamedTypeSymbol namedTypeSymbol, ref Action<ITypeContainer?, EquatableArray<CsGenericTypeParam>, EquatableArray<CsTypeReference>>? completeStructDeclaration) =>
                    {
                        var accessibility = namedTypeSymbol.DeclaredAccessibility.ToCSharpAccessibility();

                        // 自己参照する型パラメータを含むインターフェイスなどの存在による構築時の型参照の無限ループを回避するために、
                        // 型情報の参照が必要なパラメータを除いた状態で作成。
                        return new CsStruct(
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

                    var container = BuildContainer(namedTypeSymbol, nest);

                    var genericTypeParams = BuildGenericTypeParams(namedTypeSymbol, nest);

                    var interfaces = BuildInterfaces(namedTypeSymbol, nest);

                    completeStructDeclaration(container, genericTypeParams, interfaces);
                }

                return structDeclaration;
            }


            if (namedTypeSymbol.TypeKind == TypeKind.Delegate)
            {
                Action<ITypeContainer?, CsTypeRefWithNullability, EquatableArray<CsMethodParam>, EquatableArray<CsGenericTypeParam>>? completeDelegateDeclaration = null;

                var delegateDeclaration = _typeDeclarationDictionary.GetOrAdd(namedTypeSymbol, ref completeDelegateDeclaration,
                    static (INamedTypeSymbol namedTypeSymbol, ref Action<ITypeContainer?, CsTypeRefWithNullability, EquatableArray<CsMethodParam>, EquatableArray<CsGenericTypeParam>>? completeInterfaceDeclaration) =>
                    {
                        var accessibility = namedTypeSymbol.DeclaredAccessibility.ToCSharpAccessibility();

                        var returnModifier = namedTypeSymbol.DelegateInvokeMethod.ToCsReturnModifier();

                        // 自己参照する型パラメータを含むインターフェイスなどの存在による構築時の型参照の無限ループを回避するために、
                        // 型情報の参照が必要なパラメータを除いた状態で作成。
                        return new CsDelegate(
                            namedTypeSymbol.Name,
                            accessibility,
                            returnModifier,
                            out completeInterfaceDeclaration);
                    },
                    out isAdded);

                if (isAdded)
                {
                    DebugSGen.AssertIsNotNull(completeDelegateDeclaration);

                    var container = BuildContainer(namedTypeSymbol, nest);

                    var returnType = GetTypeReferenceFromCachedTypeReferenceFirst(namedTypeSymbol.DelegateInvokeMethod.ReturnType, nest);

                    var methodParams = namedTypeSymbol.DelegateInvokeMethod.Parameters.Select(v => BuildMethodParam(v, nest)).ToImmutableArray();

                    var genericTypeParams = namedTypeSymbol.DelegateInvokeMethod.TypeParameters.Select(v => BuildGenericTypeParam(v, nest)).ToImmutableArray();

                    completeDelegateDeclaration(container, returnType, methodParams, genericTypeParams);
                }

                return delegateDeclaration;
            }
        }

        throw new NotSupportedException();
    }

    private CsTypeReference CreateAndCacheTypeReference(CsTypeDeclaration csTypeDeclaration, ITypeSymbol typeSymbol, int nest)
    {
        nest++;
        if (nest > MaxNestCount) throw new InvalidOperationException("呼出しの再帰が深すぎます。");
        _rootCancellationToken.ThrowIfCancellationRequested();

        var typeReference = _typeReferenceDictionary.GetOrAdd(typeSymbol, (self: this, csTypeDeclaration, typeSymbol),
            static (typeSymbol, createArg) =>
            {
                if (createArg.csTypeDeclaration is CsGenericDefinableTypeDeclaration && typeSymbol is INamedTypeSymbol { TypeArguments: { IsDefaultOrEmpty: false } typeArguments })
                {
                    if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
                        throw new InvalidOperationException();

                    var typeArgsBuilder = ImmutableArray.CreateBuilder<EquatableArray<CsTypeRefWithNullability>>(countTypeArgsLength(namedTypeSymbol));
                    fillTypeArgs(createArg.self, typeArgsBuilder, namedTypeSymbol);
                    var typeArgs = typeArgsBuilder.MoveToImmutable();

                    return new CsTypeReference(
                        createArg.csTypeDeclaration,
                        typeArgs);
                }
                else
                {
                    return new CsTypeReference(
                        createArg.csTypeDeclaration,
                        EquatableArray<EquatableArray<CsTypeRefWithNullability>>.Empty
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

        static void fillTypeArgs(CsDeclarationProvider self, ImmutableArray<EquatableArray<CsTypeRefWithNullability>>.Builder typeArgsBuilder, INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.ContainingType is not null)
                fillTypeArgs(self, typeArgsBuilder, namedTypeSymbol.ContainingType);

            typeArgsBuilder.Add(namedTypeSymbol.TypeArguments.Select(self.GetTypeReferenceFromCachedTypeReferenceFirst).ToImmutableArray());
        }
    }

    private CsGenericTypeParam BuildGenericTypeParam(ITypeParameterSymbol typeParameterSymbol, int nest)
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
            Name = typeParameterSymbol.Name,
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

        return new CsMethodParam(paramType, parameterSymbol.Name, paramModifier, isScoped);
    }

    private ITypeContainer BuildContainer(ITypeSymbol typeSymbol, int nest)
    {
        nest++;
        if (nest > MaxNestCount) throw new InvalidOperationException("呼出しの再帰が深すぎます。");
        _rootCancellationToken.ThrowIfCancellationRequested();

        ITypeContainer? container;

        if (typeSymbol.ContainingType is null)
        {
            var namespaceBuilder = new StringBuilder();
            SymbolExtensions.AppendFullNamespace(namespaceBuilder, typeSymbol.ContainingNamespace);

            container = new CsNameSpace(namespaceBuilder.ToString());
        }
        else
        {
            container = GetTypeDeclarationFromCachedTypeDeclarationFirst(typeSymbol.ContainingType, nest);
        }

        return container;
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

            var typeParamsBuilder = ImmutableArray.CreateBuilder<CsGenericTypeParam>(originalDefinitionTypeSymbol.TypeParameters.Length);

            for (int i = 0; i < namedTypeSymbol.TypeParameters.Length; i++)
            {
                var genericTypeParam = BuildGenericTypeParam(namedTypeSymbol.TypeParameters[i], nest);

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

    private EquatableArray<CsTypeReference> BuildInterfaces(INamedTypeSymbol namedTypeSymbol, int nest)
    {
        nest++;
        if (nest > MaxNestCount) throw new InvalidOperationException("呼出しの再帰が深すぎます。");
        _rootCancellationToken.ThrowIfCancellationRequested();

        var interfaces = namedTypeSymbol.Interfaces.Select(v => GetTypeReferenceFromCachedTypeReferenceFirst(v, nest).Type).ToImmutableArray().ToEquatableArray();
        return interfaces;
    }
}
