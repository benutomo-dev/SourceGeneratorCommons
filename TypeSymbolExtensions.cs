using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace SourceGeneratorCommons;

internal static partial class TypeSymbolExtensions
{
    internal static bool IsInterlockedExchangeable(this ITypeSymbol typeSymbol)
    {
        return typeSymbol switch
        {
            { IsReferenceType: true } => true,
            { SpecialType: SpecialType.System_Int32 } => true,
            { SpecialType: SpecialType.System_Int64 } => true,
            { SpecialType: SpecialType.System_IntPtr } => true,
            { SpecialType: SpecialType.System_Single } => true,
            { SpecialType: SpecialType.System_Double } => true,
            _ => false,
        };
    }

    internal static (CsTypeDeclaration DefinitionInfo, CsTypeReference ReferenceInfo) BuildTypeDefinitionInfo(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol is ITypeParameterSymbol typeParameterSymbol)
        {
            var typeParamerterDeclaration = new CsTypeParameterDeclaration(typeSymbol.Name);

            var referenceInfo = new CsTypeReference
            {
                TypeDefinition = typeParamerterDeclaration,
                IsNullableAnnotated = typeParameterSymbol.NullableAnnotation == NullableAnnotation.Annotated,
                TypeArgs = EquatableArray<EquatableArray<CsTypeReference>>.Empty,
            };

            return (typeParamerterDeclaration, referenceInfo);
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
            var elementTypeDeclaration = arrayTypeSymbol.ElementType.BuildTypeDefinitionInfo().DefinitionInfo;

            var arrayDeclaration = new CsArrayDeclaration(container, typeSymbol.Name, elementTypeDeclaration, arrayTypeSymbol.Rank);

            var referenceInfo = new CsTypeReference
            {
                TypeDefinition = arrayDeclaration,
                IsNullableAnnotated = arrayTypeSymbol.NullableAnnotation == NullableAnnotation.Annotated,
                TypeArgs = EquatableArray<EquatableArray<CsTypeReference>>.Empty,
            };

            return (arrayDeclaration, referenceInfo);
        }

        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            var accessibility = namedTypeSymbol.DeclaredAccessibility.ToCSharpAccessibility();

            if (namedTypeSymbol.TypeKind == TypeKind.Enum)
            {
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

                var arrayDeclaration = new CsEnumDeclaration(container, namedTypeSymbol.Name, accessibility, underlyingType);

                var referenceInfo = new CsTypeReference
                {
                    TypeDefinition = arrayDeclaration,
                    IsNullableAnnotated = namedTypeSymbol.NullableAnnotation == NullableAnnotation.Annotated,
                    TypeArgs = EquatableArray<EquatableArray<CsTypeReference>>.Empty,
                };

                return (arrayDeclaration, referenceInfo);
            }

            EquatableArray<GenericTypeParam> genericTypeParams;

            EquatableArray<EquatableArray<CsTypeReference>> typeArgs = default;

            if (!namedTypeSymbol.TypeArguments.IsDefaultOrEmpty)
            {
                var originalDefinitionTypeSymbol = namedTypeSymbol.OriginalDefinition;

                var typeParamsBuilder = ImmutableArray.CreateBuilder<GenericTypeParam>(originalDefinitionTypeSymbol.TypeParameters.Length);

                for (int i = 0; i < namedTypeSymbol.TypeParameters.Length; i++)
                {
                    var genericTypeParam = namedTypeSymbol.TypeParameters[i].BuildGenericTypeParam();

                    typeParamsBuilder.Add(genericTypeParam);
                }

                genericTypeParams = typeParamsBuilder.MoveToImmutable();

                var typeArgsBuilder = ImmutableArray.CreateBuilder<EquatableArray<CsTypeReference>>(countTypeArgsLength(namedTypeSymbol));
                fillTypeArgs(typeArgsBuilder, namedTypeSymbol);
                typeArgs = typeArgsBuilder.MoveToImmutable();
            }
            else
            {
                genericTypeParams = EquatableArray<GenericTypeParam>.Empty;
            }

            var interfaces = namedTypeSymbol.Interfaces.Select(v => v.BuildTypeDefinitionInfo().ReferenceInfo).ToImmutableArray().ToEquatableArray();


            if (namedTypeSymbol.TypeKind == TypeKind.Class)
            {
                if (!(namedTypeSymbol is { BaseType: { SpecialType: not SpecialType.System_Object } baseTypeSymbol }))
                    baseTypeSymbol = null;

                var baseTypeDeclaration = baseTypeSymbol?.BuildTypeDefinitionInfo().ReferenceInfo;

                var classModifier = namedTypeSymbol switch
                {
                    { IsVirtual: true } => ClassModifier.Abstract,
                    { IsSealed: true } => ClassModifier.Sealed,
                    { IsStatic: true } => ClassModifier.Static,
                    _ => ClassModifier.Default,
                };

                var definitionInfo = new CsClassDeclaration(
                    container,
                    namedTypeSymbol.Name,
                    genericTypeParams,
                    baseTypeDeclaration,
                    interfaces,
                    accessibility,
                    classModifier);

                var referenceInfo = new CsTypeReference
                {
                    TypeDefinition = definitionInfo,
                    IsNullableAnnotated = namedTypeSymbol.NullableAnnotation == NullableAnnotation.Annotated,
                    TypeArgs = typeArgs.IsDefaultOrEmpty ? EquatableArray<EquatableArray<CsTypeReference>>.Empty : typeArgs,
                };

                return (definitionInfo, referenceInfo);
            }

            if (namedTypeSymbol.TypeKind == TypeKind.Interface)
            {
                var definitionInfo = new CsInterfaceDeclaration(
                    container,
                    namedTypeSymbol.Name,
                    genericTypeParams,
                    interfaces,
                    accessibility);

                var referenceInfo = new CsTypeReference
                {
                    TypeDefinition = definitionInfo,
                    IsNullableAnnotated = namedTypeSymbol.NullableAnnotation == NullableAnnotation.Annotated,
                    TypeArgs = typeArgs.IsDefaultOrEmpty ? EquatableArray<EquatableArray<CsTypeReference>>.Empty : typeArgs,
                };

                return (definitionInfo, referenceInfo);
            }

            if (namedTypeSymbol.TypeKind == TypeKind.Struct)
            {
                var definitionInfo = new CsStructDeclaration(
                    container,
                    namedTypeSymbol.Name,
                    genericTypeParams,
                    interfaces,
                    accessibility,
                    namedTypeSymbol.IsReadOnly,
                    namedTypeSymbol.IsRefLikeType);

                var referenceInfo = new CsTypeReference
                {
                    TypeDefinition = definitionInfo,
                    IsNullableAnnotated = namedTypeSymbol.NullableAnnotation == NullableAnnotation.Annotated,
                    TypeArgs = typeArgs.IsDefaultOrEmpty ? EquatableArray<EquatableArray<CsTypeReference>>.Empty : typeArgs,
                };

                return (definitionInfo, referenceInfo);
            }
        }

         
        throw new NotSupportedException();



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

            typeArgsBuilder.Add(namedTypeSymbol.TypeArguments.Select(v => v.BuildTypeDefinitionInfo().ReferenceInfo!).ToImmutableArray());
        }
    }

    internal static GenericTypeParam BuildGenericTypeParam(this ITypeParameterSymbol typeParameterSymbol)
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
            BaseType = typeParameterSymbol.ConstraintTypes.FirstOrDefault(v => !v.IsAbstract)?.BuildTypeDefinitionInfo().ReferenceInfo,
            Interfaces = typeParameterSymbol.ConstraintTypes.Where(v => v.IsAbstract).Select(v => v.BuildTypeDefinitionInfo().ReferenceInfo!).ToImmutableArray(),
        };

        var genericTypeParam = new GenericTypeParam
        {
            Name = typeParameterSymbol.Name,
            Where = constraints,
        };

        return genericTypeParam;
    }

    internal static bool IsXSymbolImpl(ITypeSymbol? typeSymbol, string ns1, string typeName)
    {
        Debug.Assert(!ns1.Contains('.'));
        Debug.Assert(!typeName.Contains('.'));

        if (typeSymbol is null) return false;

        if (typeSymbol.Name != typeName) return false;

        var containingNamespaceSymbol = typeSymbol.ContainingNamespace;

        if (containingNamespaceSymbol is null) return false;

        if (containingNamespaceSymbol.Name != ns1) return false;

        if (containingNamespaceSymbol.ContainingNamespace is null) return false;

        if (!containingNamespaceSymbol.ContainingNamespace.IsGlobalNamespace) return false;

        return true;
    }


    internal static bool IsAssignableToIXImpl(ITypeSymbol? typeSymbol, Func<ITypeSymbol, bool> isXTypeFunc)
    {
        if (typeSymbol is null) return false;

        if (isXTypeFunc(typeSymbol)) return true;

        if (typeSymbol.AllInterfaces.Any((Func<INamedTypeSymbol, bool>)isXTypeFunc)) return true;

        // ジェネリック型の型パラメータの場合は型パラメータの制約を再帰的に確認
        if (typeSymbol is ITypeParameterSymbol typeParameterSymbol && typeParameterSymbol.ConstraintTypes.Any(constraintType => IsAssignableToIXImpl(constraintType, isXTypeFunc)))
        {
            return true;
        }

        return false;
    }

    internal static bool IsXAttributedMemberImpl(ISymbol? symbol, Func<INamedTypeSymbol, bool> isXAttributeSymbol)
    {
        if (symbol is null) return false;

        foreach (var attributeData in symbol.GetAttributes())
        {
            if (attributeData.AttributeClass is not null && isXAttributeSymbol(attributeData.AttributeClass))
            {
                return true;
            }
        }

        return false;
    }

}
