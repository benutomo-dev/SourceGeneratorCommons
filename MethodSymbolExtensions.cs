using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace SourceGeneratorCommons;

static class MethodSymbolExtensions
{
    internal static MethodDefinitionInfo BuildMethodDefinitionInfo(this IMethodSymbol methodSymbol, CancellationToken cancellationToken)
    {
        var returnType = methodSymbol.ReturnType.BuildTypeDefinitionInfo().ReferenceInfo;

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

        var genericTypeParams = methodSymbol.TypeParameters.Select(v => v.BuildGenericTypeParam()).ToImmutableArray();

        bool isReadOnly;
        CSharpAccessibility accessibility;
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

        return new MethodDefinitionInfo(methodSymbol.Name, returnType, returnModifier, methodSymbol.IsStatic, methodSymbol.IsAsync, isReadOnly, methodParams, genericTypeParams, accessibility, methodModifier);


        (bool isReadOnly, CSharpAccessibility accessibility) FromMethodDeclarationSyntax(MethodDeclarationSyntax methodDeclarationSyntax)
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
                (true, false, false, false) => CSharpAccessibility.Public,
                (false, true, true, false) => CSharpAccessibility.ProtectedInternal,
                (false, false, true, false) => CSharpAccessibility.Internal,
                (false, true, false, false) => CSharpAccessibility.Protected,
                (false, false, false, true) => CSharpAccessibility.Private,
                _ => CSharpAccessibility.Default,
            };

            return (haveReadOnly, accessibility);
        }


        MethodParam buildMethodParam(IParameterSymbol parameterSymbol)
        {
            var paramType = parameterSymbol.Type.BuildTypeDefinitionInfo().ReferenceInfo;

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
}
