using System.Collections.Immutable;
using System.Text;

namespace SourceGeneratorCommons;

/// <summary>
/// 型参照
/// </summary>
class TypeReferenceInfo
{
    public bool IsNullableAnnotated { get; init; }

    public required TypeDefinitionInfo TypeDefinition { get; init; }

    public ImmutableArray<ImmutableArray<TypeReferenceInfo>> TypeArgs { get; init; }

    private string? _value;

    public override string ToString()
    {
        if (_value is not null)
            return _value;

        var builder = new StringBuilder();

        write(builder, TypeDefinition, TypeArgs.AsSpan());

        void write(StringBuilder builder, TypeDefinitionInfo typeDefinition, ReadOnlySpan<ImmutableArray<TypeReferenceInfo>> typeArgs)
        {
            if (typeDefinition.Container is TypeDefinitionInfo containerType)
                write(builder, containerType, typeArgs.Slice(0, Math.Max(0, typeArgs.Length - 1)));

            if (typeDefinition.Container is not null)
            {
                builder.Append(typeDefinition.Container.FullName);
                builder.Append('.');
            }

            builder.Append(typeDefinition.Name);

            var currentTypeArgs = typeArgs.Length > 0 ? typeArgs[^1] : ImmutableArray<TypeReferenceInfo>.Empty;

            if (currentTypeArgs.Length > 0)
            {
                builder.Append('<');
                for (int i = 0; i < currentTypeArgs.Length; i++)
                {
                    builder.Append(currentTypeArgs[i]);

                    if (i != currentTypeArgs.Length - 1)
                        builder.Append(',');
                }
                builder.Append('>');
            }
        }

        if (IsNullableAnnotated)
            builder.Append('?');

        _value = builder.ToString();

        return _value;
    }
}
