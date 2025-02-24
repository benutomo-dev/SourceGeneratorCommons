using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace SourceGeneratorCommons;

class TypeDefinitionInfo : ITypeContainer, IEquatable<TypeDefinitionInfo>
{
    public ITypeContainer Container { get; init; }

    public string Name { get; init; }

    public bool IsStatic { get; init; }

    public bool IsReadOnly { get; init; }

    public bool IsRef { get; init; }

    public TypeCategory TypeCategory { get; init; }

    public bool IsNullableAnnotated { get; init; }

    public ImmutableArray<string> GenericTypeArgs { get; init; }

    public string? NameSpace { get; }

    public string NameWithGenericArgs
    {
        get
        {
            if (_nameWithGenericArgs is not null)
                return _nameWithGenericArgs;

            if (GenericTypeArgs.IsEmpty)
            {
                _nameWithGenericArgs = Name;
            }
            else
            {
                _nameWithGenericArgs = $"{Name}<{string.Join(",", GenericTypeArgs)}>";
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

            _fullName = $"{Container.FullName}.{NameWithGenericArgs}";

            return _fullName;
        }
    }

    public string? _nameWithGenericArgs;

    public string? _fullName;

    public TypeDefinitionInfo(ITypeContainer container, string name, bool isStatic, bool isReadOnly, bool isRef, TypeCategory typeCategory, bool isNullableAnnotated, ImmutableArray<string> genericTypeArgs)
    {
        Container = container ?? throw new ArgumentNullException(nameof(container));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IsStatic = isStatic;
        IsReadOnly = isReadOnly;
        IsRef = isRef;
        TypeCategory = typeCategory;
        IsNullableAnnotated = isNullableAnnotated;
        GenericTypeArgs = genericTypeArgs;

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
              typeDefinitionInfo.IsNullableAnnotated,
              typeDefinitionInfo.GenericTypeArgs
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
                    builder.Append(".");
                }

                builder.Append(typeDefinitionInfo.Name);

                if (typeDefinitionInfo.GenericTypeArgs.Length > 0)
                {
                    foreach (var  genericArgument in typeDefinitionInfo.GenericTypeArgs)
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
               EqualityComparer<ITypeContainer>.Default.Equals(Container, other.Container) &&
               Name == other.Name &&
               IsStatic == other.IsStatic &&
               IsReadOnly == other.IsReadOnly &&
               IsRef == other.IsRef &&
               TypeCategory == other.TypeCategory &&
               IsNullableAnnotated == other.IsNullableAnnotated &&
               GenericTypeArgs.SequenceEqual(other.GenericTypeArgs);
    }

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(Container);
        hash.Add(Name);
        hash.Add(IsStatic);
        hash.Add(IsReadOnly);
        hash.Add(IsRef);
        hash.Add(TypeCategory);
        hash.Add(IsNullableAnnotated);
        hash.Add(GenericTypeArgs.Length);
        foreach (var args in GenericTypeArgs)
            hash.Add(args);
        hash.Add(NameSpace);
        return hash.ToHashCode();
    }
}
