using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;

namespace SourceGeneratorCommons;

internal class CsDeclarationProvider
{
    private ConcurrentDictionary<ITypeSymbol, CsTypeDeclaration> _typeDeclarationDictionary = new ConcurrentDictionary<ITypeSymbol, CsTypeDeclaration>(SymbolEqualityComparer.Default);

    private ConcurrentDictionary<ITypeSymbol, CsTypeReference> _typeReferenceDictionary = new ConcurrentDictionary<ITypeSymbol, CsTypeReference>(SymbolEqualityComparer.IncludeNullability);

    internal (CsTypeDeclaration DefinitionInfo, CsTypeReference ReferenceInfo) BuildTypeDefinitionInfo(ITypeSymbol typeSymbol)
    {
        if (_typeReferenceDictionary.TryGetValue(typeSymbol, out var cachedTypeReferenceInfo))
            return (cachedTypeReferenceInfo.TypeDefinition, cachedTypeReferenceInfo);

        if (_typeDeclarationDictionary.TryGetValue(typeSymbol, out var cachedTypeDeclaration))
        {
            cachedTypeReferenceInfo = PerformCacheTypeReference(cachedTypeDeclaration, typeSymbol);

            return (cachedTypeReferenceInfo.TypeDefinition, cachedTypeReferenceInfo);
        }

        if (typeSymbol is ITypeParameterSymbol typeParameterSymbol)
        {
            var typeParameterDeclaration = _typeDeclarationDictionary.GetOrAdd(
                typeSymbol,
                static typeSymbol => new CsTypeParameterDeclaration(typeSymbol.Name)
                );

            cachedTypeReferenceInfo = PerformCacheTypeReference(typeParameterDeclaration, typeSymbol);

            return (cachedTypeReferenceInfo.TypeDefinition, cachedTypeReferenceInfo);
        }

        ITypeContainer? container;

        if (typeSymbol.ContainingType is null)
        {
            var namespaceBuilder = new StringBuilder();
            SymbolExtensions.AppendFullNamespace(namespaceBuilder, typeSymbol.ContainingNamespace);

            container = new NameSpaceInfo(namespaceBuilder.ToString());
        }
        else
        {
            (container, var containerReference) = BuildTypeDefinitionInfo(typeSymbol.ContainingType);
        }

        if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
        {
            var arrayDeclaration = _typeDeclarationDictionary.GetOrAdd(
                arrayTypeSymbol,
                typeSymbol =>
                {
                    var arrayTypeSymbol = (IArrayTypeSymbol)typeSymbol;
                    var elementTypeDeclaration = BuildTypeDefinitionInfo(arrayTypeSymbol.ElementType).DefinitionInfo;
                    return new CsArrayDeclaration(container, typeSymbol.Name, elementTypeDeclaration, arrayTypeSymbol.Rank);
                });

            cachedTypeReferenceInfo = PerformCacheTypeReference(arrayDeclaration, typeSymbol);

            return (cachedTypeReferenceInfo.TypeDefinition, cachedTypeReferenceInfo);
        }

        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.TypeKind == TypeKind.Enum)
            {
                var enumDeclaration = _typeDeclarationDictionary.GetOrAdd(
                    namedTypeSymbol,
                    typeSymbol =>
                    {
                        var namedTypeSymbol = (INamedTypeSymbol)typeSymbol;

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

                        return new CsEnumDeclaration(container, namedTypeSymbol.Name, accessibility, underlyingType); ;
                    }); 

                cachedTypeReferenceInfo = PerformCacheTypeReference(enumDeclaration, typeSymbol);

                return (cachedTypeReferenceInfo.TypeDefinition, cachedTypeReferenceInfo);
            }

            if (namedTypeSymbol.TypeKind == TypeKind.Class)
            {
                var classDeclaration = _typeDeclarationDictionary.GetOrAdd(
                    namedTypeSymbol,
                    typeSymbol =>
                    {
                        var namedTypeSymbol = (INamedTypeSymbol)typeSymbol;

                        var genericTypeParams = BuildGenericTypeParams(namedTypeSymbol);

                        var interfaces = BuildInterfaces(namedTypeSymbol);

                        if (!(namedTypeSymbol is { BaseType: { SpecialType: not SpecialType.System_Object } baseTypeSymbol }))
                            baseTypeSymbol = null;
                        
                        var baseTypeDeclaration = baseTypeSymbol is null ? null : BuildTypeDefinitionInfo(baseTypeSymbol).ReferenceInfo;

                        var accessibility = namedTypeSymbol.DeclaredAccessibility.ToCSharpAccessibility();

                        var classModifier = namedTypeSymbol switch
                        {
                            { IsVirtual: true } => ClassModifier.Abstract,
                            { IsSealed: true } => ClassModifier.Sealed,
                            { IsStatic: true } => ClassModifier.Static,
                            _ => ClassModifier.Default,
                        };

                        return new CsClassDeclaration(
                            container,
                            namedTypeSymbol.Name,
                            genericTypeParams,
                            baseTypeDeclaration,
                            interfaces,
                            accessibility,
                            classModifier);
                    }); 

                cachedTypeReferenceInfo = PerformCacheTypeReference(classDeclaration, typeSymbol);

                return (cachedTypeReferenceInfo.TypeDefinition, cachedTypeReferenceInfo);
            }

            if (namedTypeSymbol.TypeKind == TypeKind.Interface)
            {
                var interfaceDeclaration = _typeDeclarationDictionary.GetOrAdd(
                    namedTypeSymbol,
                    typeSymbol =>
                    {
                        var namedTypeSymbol = (INamedTypeSymbol)typeSymbol;

                        var genericTypeParams = BuildGenericTypeParams(namedTypeSymbol);

                        var interfaces = BuildInterfaces(namedTypeSymbol);

                        var accessibility = namedTypeSymbol.DeclaredAccessibility.ToCSharpAccessibility();

                        return new CsInterfaceDeclaration(
                            container,
                            namedTypeSymbol.Name,
                            genericTypeParams,
                            interfaces,
                            accessibility);
                    });


                cachedTypeReferenceInfo = PerformCacheTypeReference(interfaceDeclaration, typeSymbol);

                return (cachedTypeReferenceInfo.TypeDefinition, cachedTypeReferenceInfo);
            }

            if (namedTypeSymbol.TypeKind == TypeKind.Struct)
            {
                var structDeclaration = _typeDeclarationDictionary.GetOrAdd(
                    namedTypeSymbol,
                    typeSymbol =>
                    {
                        var namedTypeSymbol = (INamedTypeSymbol)typeSymbol;

                        var genericTypeParams = BuildGenericTypeParams(namedTypeSymbol);

                        var interfaces = BuildInterfaces(namedTypeSymbol);

                        var accessibility = namedTypeSymbol.DeclaredAccessibility.ToCSharpAccessibility();

                        return new CsStructDeclaration(
                            container,
                            namedTypeSymbol.Name,
                            genericTypeParams,
                            interfaces,
                            accessibility,
                            namedTypeSymbol.IsReadOnly,
                            namedTypeSymbol.IsRefLikeType);
                    });


                cachedTypeReferenceInfo = PerformCacheTypeReference(structDeclaration, typeSymbol);

                return (cachedTypeReferenceInfo.TypeDefinition, cachedTypeReferenceInfo);
            }
        }

        throw new NotSupportedException();
    }

    internal GenericTypeParam BuildGenericTypeParam(ITypeParameterSymbol typeParameterSymbol)
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

        var baseType = typeParameterSymbol.ConstraintTypes.FirstOrDefault(v => !v.IsAbstract);

        var constraints = new GenericTypeConstraints
        {
            TypeCategory = genericConstraintTypeCategory,
            HaveDefaultConstructor = typeParameterSymbol.HasConstructorConstraint,
#if CODE_ANALYSYS4_12_2_OR_GREATER
            AllowRefStruct = typeParameterSymbol.AllowsRefLikeType,
#endif
            BaseType = baseType is null ? null : BuildTypeDefinitionInfo(baseType).ReferenceInfo,
            Interfaces = typeParameterSymbol.ConstraintTypes.Where(v => v.IsAbstract).Select(v => BuildTypeDefinitionInfo(v).ReferenceInfo!).ToImmutableArray(),
        };

        var genericTypeParam = new GenericTypeParam
        {
            Name = typeParameterSymbol.Name,
            Where = constraints,
        };

        return genericTypeParam;
    }

    internal CsMethodDeclaration BuildMethodDefinitionInfo(IMethodSymbol methodSymbol, CancellationToken cancellationToken)
    {
        var returnType = BuildTypeDefinitionInfo(methodSymbol.ReturnType).ReferenceInfo;

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

        var methodParams = methodSymbol.Parameters.Select(buildMethodParam).ToImmutableArray();

        var genericTypeParams = methodSymbol.TypeParameters.Select(v => BuildGenericTypeParam(v)).ToImmutableArray();

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


        MethodParam buildMethodParam(IParameterSymbol parameterSymbol)
        {
            var paramType = BuildTypeDefinitionInfo(parameterSymbol.Type).ReferenceInfo;

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
    }

    private EquatableArray<GenericTypeParam> BuildGenericTypeParams(INamedTypeSymbol namedTypeSymbol)
    {
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
        var interfaces = namedTypeSymbol.Interfaces.Select(v => BuildTypeDefinitionInfo(v).ReferenceInfo).ToImmutableArray().ToEquatableArray();
        return interfaces;
    }

    private CsTypeReference PerformCacheTypeReference(CsTypeDeclaration csTypeDeclaration, ITypeSymbol typeSymbol)
    {
        var cachedTypeReferenceInfo = _typeReferenceDictionary.GetOrAdd(typeSymbol, _ =>
        {
            var typeReference = buildTypeReference(csTypeDeclaration, typeSymbol);
            return typeReference;
        });

        return cachedTypeReferenceInfo;

        CsTypeReference buildTypeReference(CsTypeDeclaration csTypeDeclaration, ITypeSymbol typeSymbol)
        {
            if (csTypeDeclaration is CsGenericDefinableTypeDeclaration && typeSymbol is INamedTypeSymbol { TypeArguments: { IsDefaultOrEmpty: false } typeArguments })
            {
                if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
                    throw new InvalidOperationException();

                var typeArgsBuilder = ImmutableArray.CreateBuilder<EquatableArray<CsTypeReference>>(countTypeArgsLength(namedTypeSymbol));
                fillTypeArgs(typeArgsBuilder, namedTypeSymbol);
                var typeArgs = typeArgsBuilder.MoveToImmutable();

                var referenceInfo = new CsTypeReference
                {
                    TypeDefinition = csTypeDeclaration,
                    IsNullableAnnotated = namedTypeSymbol.NullableAnnotation == NullableAnnotation.Annotated,
                    TypeArgs = typeArgs,
                };

                return referenceInfo;
            }
            else
            {
                var referenceInfo = new CsTypeReference
                {
                    TypeDefinition = csTypeDeclaration,
                    IsNullableAnnotated = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated,
                    TypeArgs = EquatableArray<EquatableArray<CsTypeReference>>.Empty,
                };

                return referenceInfo;
            }
        }

        int countTypeArgsLength(INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.ContainingType is not null)
                return countTypeArgsLength(namedTypeSymbol.ContainingType) + 1;
            else
                return 1;
        }

        void fillTypeArgs(ImmutableArray<EquatableArray<CsTypeReference>>.Builder typeArgsBuilder, INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.ContainingType is not null)
                fillTypeArgs(typeArgsBuilder, namedTypeSymbol.ContainingType);

            typeArgsBuilder.Add(namedTypeSymbol.TypeArguments.Select(v => BuildTypeDefinitionInfo(v).ReferenceInfo!).ToImmutableArray());
        }
    }
}
