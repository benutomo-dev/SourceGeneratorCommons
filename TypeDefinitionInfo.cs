using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace SourceGeneratorCommons;

/// <summary>
/// 型定義
/// </summary>
record class TypeDefinitionInfo(
    ITypeContainer? Container,
    string Name,
    TypeCategory TypeCategory,
    ImmutableArray<GenericTypeParam> GenericTypeParams = default,
    bool IsStatic = false,
    bool IsReadOnly = false,
    bool IsRef = false
    )
    : ITypeContainer, IEquatable<TypeDefinitionInfo>
{
    public string NameWithGenericParams
    {
        get
        {
            if (_nameWithGenericArgs is not null)
                return _nameWithGenericArgs;

            if (GenericTypeParams.IsEmpty)
            {
                _nameWithGenericArgs = Name;
            }
            else
            {
                _nameWithGenericArgs = $"{Name}<{string.Join(",", GenericTypeParams.Select(v => v.Name))}>";
            }

            return _nameWithGenericArgs;
        }
    }

    public string FullName
    {
        get
        {
            if (_fullName is not null)
                return _fullName;

            if (Container is null)
                _fullName = NameWithGenericParams;
            else
                _fullName = $"{Container.FullName}.{NameWithGenericParams}";

            return _fullName;
        }
    }

    public string? _nameWithGenericArgs;

    public string? _fullName;

    public string MakeStandardHintName()
    {
        var builder = new StringBuilder(256);
        append(builder, this);
        return builder.ToString();

        static void append(StringBuilder builder, ITypeContainer container)
        {
            if (container is TypeDefinitionInfo typeDefinitionInfo)
            {
                if (typeDefinitionInfo.Container is not null)
                {
                    append(builder, typeDefinitionInfo.Container);
                    builder.Append('.');
                }

                builder.Append(typeDefinitionInfo.Name);

                if (typeDefinitionInfo.GenericTypeParams.Length > 0)
                {
                    foreach (var genericArgument in typeDefinitionInfo.GenericTypeParams)
                    {
                        builder.Append('_');
                        builder.Append(genericArgument.Name);
                    }
                }
            }
            else
            {
                Debug.Assert(container is NameSpaceInfo);

                builder.Append(container.Name);
            }
        }
    }

    public virtual bool Equals(TypeDefinitionInfo? other)
    {
        if (other is null)
            return false;

        var equalsResult = true
               && EqualityComparer<ITypeContainer?>.Default.Equals(Container, other.Container)
               && Name == other.Name
               && IsStatic == other.IsStatic
               && IsReadOnly == other.IsReadOnly
               && IsRef == other.IsRef
               && TypeCategory == other.TypeCategory 
               && GenericTypeParams.SequenceEqual(other.GenericTypeParams);

        if (equalsResult)
        {
            other._nameWithGenericArgs ??= _nameWithGenericArgs;
            other._fullName ??= _fullName;

            _nameWithGenericArgs ??= other._nameWithGenericArgs;
            _fullName ??= other._fullName;
        }

        return equalsResult;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Container);
        hash.Add(Name);
        hash.Add(IsStatic);
        hash.Add(IsReadOnly);
        hash.Add(IsRef);
        hash.Add(TypeCategory);
        hash.Add(GenericTypeParams.Length);
        foreach (var args in GenericTypeParams)
            hash.Add(args);
        return hash.ToHashCode();
    }
}
