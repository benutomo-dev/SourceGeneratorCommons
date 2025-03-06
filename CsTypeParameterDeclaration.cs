namespace SourceGeneratorCommons;

sealed record class CsTypeParameterDeclaration(string Name) : CsTypeDeclaration(null, Name);
