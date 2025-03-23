#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
namespace SourceGeneratorCommons.CSharp.Declarations;

class CsNameSpace : ITypeContainer, IEquatable<CsNameSpace>
{
    public string Name { get; }

    public string FullNameWithNameSpaceAlias => $"global::{Name}";

    public bool IsRoot => Name == "";

    public bool IsGlobal => Name == "";

    public bool IsSystem => Name == "System";

    public bool IsDefinedUnderSystemNameSpace { get; private set; }

    public Task ConstructionFullCompleted => Task.CompletedTask;

    public Task SelfConstructionCompleted => Task.CompletedTask;


    public CsNameSpace(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));

        IsDefinedUnderSystemNameSpace = IsSystem || name.StartsWith("System.", StringComparison.Ordinal);
    }

    public override string ToString() => $"{GetType().Name}{{{Name}}}";

    public override bool Equals(object? obj)
    {
        return Equals(obj as CsNameSpace);
    }

    public bool Equals(CsNameSpace? other)
    {
        return other is not null &&
               Name == other.Name;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name);
    }
}
