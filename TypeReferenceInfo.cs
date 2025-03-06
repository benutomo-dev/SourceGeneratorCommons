using System.Text;

namespace SourceGeneratorCommons;

/// <summary>
/// 型参照
/// </summary>
class TypeReferenceInfo : IEquatable<TypeReferenceInfo>
{
    public required TypeDefinitionInfo TypeDefinition { get; init; }

    public bool IsNullableAnnotated { get; init; }

    public EquatableArray<EquatableArray<TypeReferenceInfo>> TypeArgs { get; init; }

    private string? _value;

    public override bool Equals(object? obj) => obj is TypeReferenceInfo other && this.Equals(other);

    public bool Equals(TypeReferenceInfo? other)
    {
        if (other is null)
            return false;

        if (IsNullableAnnotated != other.IsNullableAnnotated)
            return false;

        if (!EqualityComparer<TypeDefinitionInfo>.Default.Equals(TypeDefinition, other.TypeDefinition))
            return false;

        if (!EqualityComparer<EquatableArray<EquatableArray<TypeReferenceInfo>>>.Default.Equals(TypeArgs, other.TypeArgs))
            return false;

        _value ??= other._value;

        other._value ??= _value;

        return true;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        hashCode.Add(TypeDefinition);
        hashCode.Add(IsNullableAnnotated);
        hashCode.Add(TypeArgs);

        return hashCode.ToHashCode();
    }

    public override string ToString()
    {
        if (_value is not null)
            return _value;

        var builder = new StringBuilder();

        if (TypeDefinition is { Name: "Nullable", Container: NameSpaceInfo { Name: "System" } } && !TypeArgs.IsDefaultOrEmpty && TypeArgs.Length == 1 && TypeArgs[0].Length == 1)
        {
            var structTypeReference = TypeArgs[^1][0];

            if (getNullableTypeKeyword(structTypeReference.TypeDefinition) is { } typeKeyword)
                return typeKeyword;

            write(builder, structTypeReference.TypeDefinition, structTypeReference.TypeArgs.AsSpan());
            builder.Append('?');
        }
        else
        {
            if (getTypeKeyword(TypeDefinition) is { } typeKeyword)
                return typeKeyword;

            write(builder, TypeDefinition, TypeArgs.AsSpan());

            if (IsNullableAnnotated)
                builder.Append('?');
        }

        _value = builder.ToString();
        

        return _value;


        void write(StringBuilder builder, TypeDefinitionInfo typeDefinition, ReadOnlySpan<EquatableArray<TypeReferenceInfo>> typeArgs)
        {
            if (typeDefinition.Container is TypeDefinitionInfo containerType)
                write(builder, containerType, typeArgs.Slice(0, Math.Max(0, typeArgs.Length - 1)));

            if (typeDefinition.Container is not null)
            {

                builder.Append(typeDefinition.Container.FullName);
                builder.Append('.');
            }

            builder.Append(typeDefinition.Name);

            var currentTypeArgs = typeArgs.Length > 0 ? typeArgs[^1] : EquatableArray<TypeReferenceInfo>.Empty;

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

        string? getNullableTypeKeyword(TypeDefinitionInfo typeDefinition)
        {
            if (typeDefinition.Container is NameSpaceInfo { Name: "System" })
            {
                switch (typeDefinition.Name)
                {
                    case "SByte":   return "sbyte?";
                    case "Int16":   return "short?";
                    case "Int32":   return "int?";
                    case "Int64":   return "long?";
                    case "Byte":    return "byte?";
                    case "UInt16":  return "ushort?";
                    case "UInt32":  return "uint?";
                    case "UInt64":  return "ulong?";
                    case "Single":  return "float?";
                    case "Double":  return "double?";
                    case "Decimal": return "decimal?";
                    case "Char":    return "char?";
                    case "Object":  return "object?";
                    default:
                        break;
                };
            }

            return null;
        }

        string? getTypeKeyword(TypeDefinitionInfo typeDefinition)
        {
            if (typeDefinition.Container is NameSpaceInfo { Name: "System" })
            {
                switch(typeDefinition.Name)
                {
                    case "SByte":   return "sbyte";
                    case "Int16":   return "short";
                    case "Int32":   return "int";
                    case "Int64":   return "long";
                    case "Byte":    return "byte";
                    case "UInt16":  return "ushort";
                    case "UInt32":  return "uint";
                    case "UInt64":  return "ulong";
                    case "Single":  return "float";
                    case "Double":  return "double";
                    case "Decimal": return "decimal";
                    case "Char":    return "char";
                    case "Object":  return "object";
                    case "Void":    return "void";
                    default:
                        break;
                };
            }

            return null;
        }
    }

}
