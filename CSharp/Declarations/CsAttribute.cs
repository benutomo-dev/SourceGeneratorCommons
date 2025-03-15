#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Polyfills;
using SourceGeneratorCommons.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SourceGeneratorCommons.CSharp.Declarations;

record struct CsAttribute(
    CsTypeReference AttributeType,
    object?[] ConstructerArgs = null,
    EquatableDictionary<string, object?>? NamedArgs = null,
    CsAttributeTarget Target = CsAttributeTarget.Default
    )
{
    public string SourceText => _sourceText ??= BuildSourceText();


    private string? _sourceText;

    public string BuildSourceText()
    {
        var attributeReference = AttributeType.GlobalReference;
        var attributeReferenceLength = attributeReference.Length;

        var hasArgs = ConstructerArgs?.Length > 0 || NamedArgs?.Count > 0;

        if (attributeReference.EndsWith("Attribute", StringComparison.Ordinal))
            attributeReferenceLength = attributeReference.Length - "Attribute".Length;
    
        StringBuilder builder = new StringBuilder(256);

        builder.Append('[');

        if (Target != CsAttributeTarget.Default)
        {
            var targetToken = Target switch
            {
                CsAttributeTarget.Assembly => "assembly",
                CsAttributeTarget.Module   => "module",
                CsAttributeTarget.Field    => "field",
                CsAttributeTarget.Event    => "event",
                CsAttributeTarget.Method   => "method",
                CsAttributeTarget.Param    => "param",
                CsAttributeTarget.Property => "property",
                CsAttributeTarget.Return   => "return",
                CsAttributeTarget.Type     => "type",
                _ => throw new NotSupportedException(),
            };

            builder.Append(targetToken);
            builder.Append(": ");
        }

        builder.Append(attributeReference, 0, attributeReferenceLength);
        if (hasArgs) builder.Append('(');

        bool isFirstArg = true;

        foreach (var constructerArg in ConstructerArgs ?? [])
        {
            if (!isFirstArg)
                builder.Append(", ");

            isFirstArg = false;

            builder.Append(SymbolDisplay.FormatPrimitive(constructerArg, quoteStrings: true, useHexadecimalNumbers: false));
        }

        foreach (var namedArg in NamedArgs.AsEnumerable() ?? [])
        {
            if (!isFirstArg)
                builder.Append(", ");

            isFirstArg = false;

            builder.Append(SymbolDisplay.FormatLiteral(namedArg.Key, quote: true));
            builder.Append(" = ");
            builder.Append(SymbolDisplay.FormatPrimitive(namedArg.Value, quoteStrings: true, useHexadecimalNumbers: false));
        }

        if (hasArgs) builder.Append(')');
        builder.Append(']');

        var value = builder.ToString();

        return value;
    }

    public override string ToString() => SourceText;
}
