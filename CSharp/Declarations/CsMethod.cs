#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using Microsoft.CodeAnalysis;
using SourceGeneratorCommons.Collections.Generic;
using System.Text;

namespace SourceGeneratorCommons.CSharp.Declarations;

record class CsMethod(
    string Name,
    CsTypeRefWithNullability ReturnType,
    CsReturnModifier ReturnModifier = CsReturnModifier.Default,
    bool IsStatic = false,
    bool IsAsync = false,
    bool IsReadOnly = false,
    EquatableArray<CsMethodParam> Params = default,
    EquatableArray<CsGenericTypeParam> GenericTypeParams = default,
    CsAccessibility Accessibility = CsAccessibility.Default,
    CsMethodModifier MethodModifier = CsMethodModifier.Default
    )
{
    public bool IsVoidMethod => ReturnType.ToString() == "void";

    public bool IsExtensionMethod => this is CsExtensionMethod;

    public string Cref => _cref ??= BuildCref();


    private string? _cref;

    public string BuildCref()
    {
        StringBuilder builder = new StringBuilder(256);

        builder.Append(Name);
        if (!GenericTypeParams.IsDefaultOrEmpty)
        {
            builder.Append('{');
            builder.Append(GenericTypeParams[0].Name);
            for (int i = 1; i < GenericTypeParams.Length; i++)
            {
                builder.Append(", ");
                builder.Append(GenericTypeParams[i].Name);
            }
            builder.Append('}');
        }
        builder.Append('(');
        if (!Params.IsDefaultOrEmpty)
        {
            for (int i = 0; i < Params.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                switch (Params[i].Modifier)
                {
                    case CsParamModifier.Ref:
                        builder.Append("ref ");
                        break;
                    case CsParamModifier.In:
                        builder.Append("in ");
                        break;
                    case CsParamModifier.Out:
                        builder.Append("out ");
                        break;
                    case CsParamModifier.RefReadOnly:
                        builder.Append("ref readonly ");
                        break;
                }

                builder.Append(Params[i].Type.Cref);
            }
        }
        builder.Append(')');

        var value = builder.ToString();

        return value;
    }
}
