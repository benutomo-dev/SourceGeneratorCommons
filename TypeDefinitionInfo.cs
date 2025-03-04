using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace SourceGeneratorCommons;

/// <summary>
/// 型定義
/// </summary>
class TypeDefinitionInfo : ITypeContainer, IEquatable<TypeDefinitionInfo>
{
    public ITypeContainer? Container { get; init; }

    public string Name { get; init; }

    public bool IsStatic { get; init; }

    public bool IsReadOnly { get; init; }

    public bool IsRef { get; init; }

    public TypeCategory TypeCategory { get; init; }

    public ImmutableArray<GenericTypeParam> GenericTypeParams { get; init; }

    public string? NameSpace { get; }

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

    public TypeDefinitionInfo(ITypeContainer? container, string name, bool isStatic, bool isReadOnly, bool isRef, TypeCategory typeCategory, ImmutableArray<GenericTypeParam> genericTypeParams)
    {
        Container = container;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IsStatic = isStatic;
        IsReadOnly = isReadOnly;
        IsRef = isRef;
        TypeCategory = typeCategory;
        GenericTypeParams = genericTypeParams;

        var rootTypeDefinition = this;
        while (rootTypeDefinition.Container is TypeDefinitionInfo parent)
            rootTypeDefinition = parent;

        NameSpace = (rootTypeDefinition.Container as NameSpaceInfo)?.FullName;
    }

    public TypeDefinitionInfo(TypeDefinitionInfo typeDefinitionInfo)
        : this(
              typeDefinitionInfo.Container,
              typeDefinitionInfo.Name,
              typeDefinitionInfo.IsStatic,
              typeDefinitionInfo.IsReadOnly,
              typeDefinitionInfo.IsRef,
              typeDefinitionInfo.TypeCategory,
              typeDefinitionInfo.GenericTypeParams
              )
    {
    }

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
                    foreach (var  genericArgument in typeDefinitionInfo.GenericTypeParams)
                    {
                        builder.Append('_');
                        builder.Append(genericArgument);
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

    public override bool Equals(object? obj) => Equals(obj as TypeDefinitionInfo);

    public bool Equals(TypeDefinitionInfo? other)
    {
        return other is not null &&
               EqualityComparer<ITypeContainer?>.Default.Equals(Container, other.Container) &&
               Name == other.Name &&
               IsStatic == other.IsStatic &&
               IsReadOnly == other.IsReadOnly &&
               IsRef == other.IsRef &&
               TypeCategory == other.TypeCategory &&
               GenericTypeParams.SequenceEqual(other.GenericTypeParams);
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
        hash.Add(NameSpace);
        return hash.ToHashCode();
    }
}
