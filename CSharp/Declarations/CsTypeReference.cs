#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;
using SourceGeneratorCommons.CSharp.Declarations.Internals;
using System.Text;

namespace SourceGeneratorCommons.CSharp.Declarations;

/// <summary>
/// 型参照
/// </summary>
class CsTypeReference : IEquatable<CsTypeReference>, ILazyConstructionRoot, ILazyConstructionOwner
{
    public CsTypeDeclaration TypeDefinition { get; }

    public bool IsNullableAnnotated { get; }

    public EquatableArray<EquatableArray<CsTypeReference>> TypeArgs { get; }

    private Task ConstructionFullCompleted { get; }

    Task ILazyConstructionRoot.ConstructionFullCompleted => ConstructionFullCompleted;

    private string? _value;

    public CsTypeReference(CsTypeDeclaration typeDefinition, bool isNullableAnnotated)
        : this(typeDefinition, isNullableAnnotated, EquatableArray<EquatableArray<CsTypeReference>>.Empty)
    {
    }

    public CsTypeReference(CsTypeDeclaration typeDefinition, bool isNullableAnnotated, EquatableArray<EquatableArray<CsTypeReference>> typeArgs)
    {
        TypeDefinition = typeDefinition ?? throw new ArgumentNullException(nameof(typeDefinition));
        IsNullableAnnotated = isNullableAnnotated;
        TypeArgs = typeArgs;

        if (typeArgs.Values.IsDefault)
            throw new ArgumentException(null, nameof(typeArgs));

        foreach (var innerTypeArgs in typeArgs.Values)
        {
            if (innerTypeArgs.Values.IsDefault)
                throw new ArgumentException(null, nameof(typeArgs));
        }

        if (GetConstructionFullCompleteFactors(true) is { } factors)
            ConstructionFullCompleted = Task.WhenAll(factors.Select(v => v.SelfConstructionCompleted));
        else
            ConstructionFullCompleted = Task.CompletedTask;
    }

    public IEnumerable<IConstructionFullCompleteFactor>? GetConstructionFullCompleteFactors(bool rejectAlreadyCompletedFactor)
    {
        IEnumerable<IConstructionFullCompleteFactor>? factors = null;

        if (!rejectAlreadyCompletedFactor || !((ILazyConstructionRoot)TypeDefinition).ConstructionFullCompleted.IsCompleted)
            factors = [TypeDefinition];

        if (!TypeArgs.Values.IsEmpty)
        {
            foreach (var innerTypeArg in TypeArgs.Values)
            {
                foreach (var typeArg in innerTypeArg.Values)
                {
                    if (typeArg.GetConstructionFullCompleteFactors(rejectAlreadyCompletedFactor) is { } typeArgFactors)
                    {
                        factors ??= [];
                        factors = factors.Concat(typeArgFactors);
                    }
                }
            }
        }

        return factors;
    }

    public CsTypeReference ToNullableIfReferenceType()
    {
        if (!TypeDefinition.IsReferenceType || IsNullableAnnotated)
            return this;

        return new CsTypeReference(TypeDefinition, isNullableAnnotated: true, TypeArgs);
    }

    public override bool Equals(object? obj) => obj is CsTypeReference other && this.Equals(other);

    public bool Equals(CsTypeReference? other)
    {
        if (other is null)
            return false;

        if (IsNullableAnnotated != other.IsNullableAnnotated)
            return false;

        if (!EqualityComparer<CsTypeDeclaration>.Default.Equals(TypeDefinition, other.TypeDefinition))
            return false;

        if (!EqualityComparer<EquatableArray<EquatableArray<CsTypeReference>>>.Default.Equals(TypeArgs, other.TypeArgs))
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

        if (TypeDefinition is { Name: "Nullable", Container: CsNameSpace { Name: "System" } } && !TypeArgs.IsDefaultOrEmpty && TypeArgs.Length == 1 && TypeArgs[0].Length == 1)
        {
            var structTypeReference = TypeArgs[^1][0];

            if (getNullableTypeKeyword(structTypeReference.TypeDefinition) is { } typeKeyword)
                return typeKeyword;

            write(builder, structTypeReference.TypeDefinition, structTypeReference.TypeArgs.AsSpan());
            builder.Append('?');
        }
        else
        {
            if (IsNullableAnnotated)
            {
                if (getNullableTypeKeyword(TypeDefinition) is { } typeKeyword)
                    return typeKeyword;
            }
            else
            {
                if (getTypeKeyword(TypeDefinition) is { } typeKeyword)
                    return typeKeyword;
            }


            write(builder, TypeDefinition, TypeArgs.AsSpan());

            if (IsNullableAnnotated)
                builder.Append('?');
        }

        _value = builder.ToString();
        

        return _value;


        void write(StringBuilder builder, CsTypeDeclaration typeDefinition, ReadOnlySpan<EquatableArray<CsTypeReference>> typeArgs)
        {
            if (typeDefinition.Container is CsTypeDeclaration containerType)
            {
                write(builder, containerType, typeArgs.Slice(0, Math.Max(0, typeArgs.Length - 1)));
                builder.Append('.');
            }
            else if (typeDefinition.Container is not null)
            {
                builder.Append(typeDefinition.Container.FullName);

                if (!string.IsNullOrWhiteSpace(typeDefinition.Container.Name))
                    builder.Append('.');
            }

            builder.Append(typeDefinition.Name);

            var currentTypeArgs = typeArgs.Length > 0 ? typeArgs[^1] : EquatableArray<CsTypeReference>.Empty;

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

        string? getNullableTypeKeyword(CsTypeDeclaration typeDefinition)
        {
            if (typeDefinition.Container is CsNameSpace { Name: "System" })
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

        string? getTypeKeyword(CsTypeDeclaration typeDefinition)
        {
            if (typeDefinition.Container is CsNameSpace { Name: "System" })
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
