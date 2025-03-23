﻿#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourceGeneratorCommons.Collections.Generic;
using SourceGeneratorCommons.CSharp.Declarations;
using System.Buffers;
using System.Collections.Concurrent;
using System.Text;

namespace SourceGeneratorCommons;

internal class SourceBuilder : IDisposable
{
    public const string AutoGeneratedComment = "// <auto-generated/>";

    public const string DesignerFileSuffix = ".designer.cs";
    public const string LongGeneratedFileSuffix = ".generated.cs";
    public const string ShortGeneratedFileSuffix = ".g.cs";
    public const string GiFileSuffix = ".g.i.cs";
    public const string TemporaryGeneratedFilePrefix = "TemporaryGeneratedFile_";

    public string SourceText => _cachedSourceText ??= _buffer?.AsSpan(0, _length).ToString() ?? "";

    private string? _cachedSourceText;

    private int _length;

    private char[]? _buffer;

    private int _currentIndentCount;

    private CancellationToken _cancellationToken;

    private Action<string, string> _addSource;

    private string _hintName;

    private const string IndentText = "    ";

    private static ConcurrentDictionary<string, int> _initialBufferSizeDictionary = new ConcurrentDictionary<string, int>();

    public SourceBuilder(SourceProductionContext context, string hintName)
        :this(hintName, context.AddSource, context.CancellationToken)
    {
    }

    public SourceBuilder(IncrementalGeneratorPostInitializationContext context, string hintName)
        : this(hintName, context.AddSource, context.CancellationToken)
    {
    }

    public SourceBuilder(string hintName, Action<string, string> addSource, CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _addSource = addSource;
        _hintName = hintName;

        if (!_initialBufferSizeDictionary.TryGetValue(hintName, out var initialMinimumCapacityLength))
        {
            initialMinimumCapacityLength = 1024;
        }
        _buffer = ArrayPool<char>.Shared.Rent(initialMinimumCapacityLength);
        _length = 0;
    }

    public void Dispose()
    {
        if (_buffer is not null)
        {
            _length = 0;
            ArrayPool<char>.Shared.Return(_buffer);
            _buffer = null;
        }
    }

    public void Commit()
    {
        _addSource(_hintName, SourceText);

        _initialBufferSizeDictionary.AddOrUpdate(_hintName, _length, (_, _) => _length);

        Dispose();
    }

    void ExpandBuffer(int requiredSize)
    {
        if (_buffer is null) throw new ObjectDisposedException(null);

        _cancellationToken.ThrowIfCancellationRequested();

        DebugSGen.Assert(_buffer.Length < _length + requiredSize);

        var nextBuffer = ArrayPool<char>.Shared.Rent((_buffer.Length + requiredSize) * 2);

        _buffer.CopyTo(nextBuffer.AsSpan());

        ArrayPool<char>.Shared.Return(_buffer);

        _buffer = nextBuffer;
    }

    void InternalClear()
    {
        if (_length != 0)
        {
            _cachedSourceText = null;
            _length = 0;
        }
    }

    void InternalAppend(ReadOnlySpan<char> text)
    {
        if (_buffer is null) throw new ObjectDisposedException(null);

        _cancellationToken.ThrowIfCancellationRequested();

        if (text.Length <= 0) return;

        _cachedSourceText = null;

        if (_buffer.Length < _length + text.Length)
        {
            ExpandBuffer(text.Length);
        }

        text.CopyTo(_buffer.AsSpan(_length));
        _length += text.Length;
    }

    public void PutIndentSpace()
    {
        for (int i = 0; i < _currentIndentCount; i++)
        {
            InternalAppend(IndentText.AsSpan());
        }
    }

    public void Clear()
    {
        InternalClear();
    }

    public void Append(string text) => Append(text.AsSpan());

    public void AppendLine(string text) => AppendLine(text.AsSpan());

    public void AppendLineWithFirstIndent(string text)
    {
        PutIndentSpace();
        AppendLine(text.AsSpan());
    }

    public _BlockEndDisposable BeginBlock(string text) => BeginBlock(text.AsSpan());

    public void Append(ReadOnlySpan<char> text)
    {
        InternalAppend(text);
    }

    public void AppendLine(ReadOnlySpan<char> text)
    {
        InternalAppend(text);
        AppendLine();
    }

    public void AppendLineWithFirstIndent(ReadOnlySpan<char> text)
    {
        PutIndentSpace();
        AppendLine(text);
    }

    public void AppendLine()
    {
        InternalAppend("\r\n".AsSpan());
    }

    public _BlockEndDisposable BeginTypeDefinitionBlock(CsTypeDeclaration typeDeclaration, TypeDefinitionBlockOptions options = default)
    {
        if (typeDeclaration is not CsUserDefinableTypeDeclaration userDefinableTypeDeclaration)
            throw new ArgumentException(null, nameof(typeDeclaration));

        return beginTypeBlock(this, typeDeclaration, isDestinationType: true, options);

        static _BlockEndDisposable beginTypeBlock(SourceBuilder self, CsTypeDeclaration typeDeclaration, bool isDestinationType, TypeDefinitionBlockOptions options)
        {
            if (typeDeclaration is not CsUserDefinableTypeDeclaration userDefinableTypeDeclaration)
                throw new NotSupportedException();

            bool hasOuterBlock = false;
            _BlockEndDisposable outerBlockEnd = default;

            if (userDefinableTypeDeclaration.Container is CsNameSpace nameSpace && !string.IsNullOrWhiteSpace(nameSpace.Name))
            {
                hasOuterBlock = true;
                outerBlockEnd = beginNameSpace(self, nameSpace);
            }
            else if (userDefinableTypeDeclaration.Container is CsTypeDeclaration typeInfo)
            {
                hasOuterBlock = true;
                outerBlockEnd = beginTypeBlock(self, typeInfo, isDestinationType: false, default);
            }

            self.PutIndentSpace();
            self.Append(userDefinableTypeDeclaration.Accessibility switch
            {
                CsAccessibility.Public            => "public ",
                CsAccessibility.Internal          => "internal ",
                CsAccessibility.Protected         => "protected ",
                CsAccessibility.ProtectedInternal => "protected internal ",
                CsAccessibility.Private           => "private ",
                _ => "",
            });

            if (userDefinableTypeDeclaration is CsClass classDeclaration)
            {
                self.Append(classDeclaration.ClassModifier switch
                {
                    CsClassModifier.Sealed => "sealed ",
                    CsClassModifier.Abstract => "abstract ",
                    CsClassModifier.Static => "static ",
                    _ => "",
                });

                if (!options.OmitPartialKeyword)
                    self.Append("partial ");

                self.Append("class ");
            }
            else if (userDefinableTypeDeclaration is CsInterface interfaceDeclaration)
            {
                if (!options.OmitPartialKeyword)
                    self.Append("partial ");

                self.Append("interface ");
            }
            else if (userDefinableTypeDeclaration is CsStruct structDeclaration)
            {
                if (structDeclaration.IsReadOnly)
                    self.Append("readonly ");
                if (structDeclaration.IsRef)
                    self.Append("ref ");

                if (!options.OmitPartialKeyword)
                    self.Append("partial ");

                self.Append("struct ");
            }
            else if (userDefinableTypeDeclaration is CsEnum enumDeclaration)
            {
                self.Append("enum ");
            }
            else
            {
                throw new NotSupportedException();
            }

            self.Append(userDefinableTypeDeclaration.Name);


            if (userDefinableTypeDeclaration is CsGenericDefinableTypeDeclaration { GenericTypeParams: { IsDefaultOrEmpty: false } genericTypeParams1 })
            {
                self.Append("<");

                for (int i = 0; i < genericTypeParams1.Length; i++)
                {
                    var genericTypeArg = genericTypeParams1[i];

                    self.Append(genericTypeArg.Name);

                    if (i < genericTypeParams1.Length - 1)
                    {
                        self.Append(", ");
                    }
                }

                self.Append(">");
            }

            if (isDestinationType)
            {
                if (userDefinableTypeDeclaration is CsEnum enumDeclaration2)
                {
                    self.Append(enumDeclaration2.UnderlyingType switch
                    {
                        CsEnumUnderlyingType.Byte => " : byte",
                        CsEnumUnderlyingType.Int16 => " : short",
                        CsEnumUnderlyingType.Int64 => " : long",
                        CsEnumUnderlyingType.SByte => " : sbyte",
                        CsEnumUnderlyingType.UInt16 => " : ushort",
                        CsEnumUnderlyingType.UInt32 => " : uint",
                        CsEnumUnderlyingType.UInt64 => " : ulong",
                        _ => "",
                    });
                }
                else
                {
                    var inheritTypeCount = 0;

                    if (!options.OmitBaseType && userDefinableTypeDeclaration is CsClass { BaseType: { } baseType })
                        inheritTypeCount += 1;
                    else
                        baseType = null;

                    if (!options.OmitInterfaces && userDefinableTypeDeclaration is CsClass { Interfaces: { IsDefaultOrEmpty: false } classInheritInterfaces })
                        inheritTypeCount += classInheritInterfaces.Length;
                    else
                        classInheritInterfaces = EquatableArray<CsTypeRef>.Empty;

                    if (!options.OmitInterfaces && userDefinableTypeDeclaration is CsStruct { Interfaces: { IsDefaultOrEmpty: false } structInheritInterfaces })
                        inheritTypeCount += structInheritInterfaces.Length;
                    else
                        structInheritInterfaces = EquatableArray<CsTypeRef>.Empty;

                    if (inheritTypeCount > 0)
                    {
                        self.Append(" : ");

                        var inheritTypes = new List<CsTypeRef>(inheritTypeCount);

                        if (baseType is not null)
                            inheritTypes.Add(baseType);

                        inheritTypes.AddRange(classInheritInterfaces.Values);
                        inheritTypes.AddRange(structInheritInterfaces.Values);

                        for (int i = 0; i < inheritTypes.Count; i++)
                        {
                            self.Append(inheritTypes[i].GlobalReference);

                            if (i != inheritTypes.Count - 1)
                                self.Append(", ");
                        }
                    }
                }

                if (options.TypeDeclarationLineTail is not null)
                {
                    self.Append(options.TypeDeclarationLineTail);
                }
            }

            self.AppendLine("");

            if (!options.OmitGenericConstraints && userDefinableTypeDeclaration is CsGenericDefinableTypeDeclaration { GenericTypeParams: { IsDefaultOrEmpty: false } genericTypeParams2 } )
                self.AppendGenericConstraintsLines(genericTypeParams2);

            if (hasOuterBlock)
                return self.BeginBlock().Combine(outerBlockEnd);
            else
                return self.BeginBlock();
        }

        static _BlockEndDisposable beginNameSpace(SourceBuilder self, CsNameSpace namespaceSymbol)
        {
            self.PutIndentSpace();
            self.Append("namespace ");
            self.Append(namespaceSymbol.Name);
            self.AppendLine("");

            return self.BeginBlock();
        }
    }

    public _BlockEndDisposable BeginMethodDefinitionBlock(CsMethod methodDefinitionInfo, bool isPartial = true, string? methodDeclarationLineTail = null)
    {
        PutIndentSpace();
        Append(methodDefinitionInfo.Accessibility switch
        {
            CsAccessibility.Public => "public ",
            CsAccessibility.Internal => "internal ",
            CsAccessibility.Protected => "protected ",
            CsAccessibility.ProtectedInternal => "protected internal ",
            CsAccessibility.Private => "private ",
            _ => "",
        });
        if (methodDefinitionInfo.IsStatic)
            Append("static ");
        if (methodDefinitionInfo.IsAsync)
            Append("async ");
        if (methodDefinitionInfo.IsReadOnly)
            Append("readonly ");

        Append(methodDefinitionInfo.MethodModifier switch
        {
            CsMethodModifier.SealedOverride => "sealed override ",
            CsMethodModifier.Override       => "override ",
            CsMethodModifier.Virtual        => "virtual ",
            CsMethodModifier.Abstract       => "abstract ",
            _ => "",
        });
        if (isPartial)
            Append("partial ");
        Append(methodDefinitionInfo.ReturnModifier switch
        {
            CsReturnModifier.RefReadonly=> "ref readonly ",
            CsReturnModifier.Ref => "ref ",
            _ => "",
        });
        Append(methodDefinitionInfo.ReturnType.GlobalReference);
        Append(" ");
        Append(methodDefinitionInfo.Name);
        if (!methodDefinitionInfo.GenericTypeParams.IsDefaultOrEmpty)
        {
            Append("<");

            for (int i = 0; i < methodDefinitionInfo.GenericTypeParams.Length; i++)
            {
                var genericTypeArg = methodDefinitionInfo.GenericTypeParams[i];

                Append(genericTypeArg.Name);

                if (i < methodDefinitionInfo.GenericTypeParams.Length - 1)
                {
                    Append(", ");
                }
            }

            Append(">");

            var hintingTypeNameBuilder = new StringBuilder();

            hintingTypeNameBuilder.Append(methodDefinitionInfo.Name);
            hintingTypeNameBuilder.Append('{');
            hintingTypeNameBuilder.Append(string.Join("_", methodDefinitionInfo.GenericTypeParams));
            hintingTypeNameBuilder.Append('}');
        }
        Append("(");
        if (!methodDefinitionInfo.Params.IsDefaultOrEmpty)
        {
            for (int i = 0; i < methodDefinitionInfo.Params.Length; i++)
            {
                var param = methodDefinitionInfo.Params[i];

                if (!param.Attributes.IsDefaultOrEmpty)
                {
                    foreach (var attribute in param.Attributes.Values)
                    {
                        Append(attribute.SourceText);
                    }
                    Append(" ");
                }

                if (i == 0 && methodDefinitionInfo.IsExtensionMethod)
                    Append("this ");

                if (param.IsScoped)
                    Append("scoped ");

                Append(param.Modifier switch
                {
                    CsParamModifier.RefReadOnly => "ref readonly ",
                    CsParamModifier.In => "in ",
                    CsParamModifier.Ref => "ref ",
                    CsParamModifier.Out => "out ",
                    _ => "",
                });

                Append(param.Type.GlobalReference);

                Append(" ");

                Append(param.Name);

                if (param is CsMethodParamWithDefaultValue paramWithDefaultValue)
                {
                    Append(" = ");
                    Append(SymbolDisplay.FormatPrimitive(paramWithDefaultValue.DefaultValue!, quoteStrings: true, useHexadecimalNumbers: false));
                }

                if (i < methodDefinitionInfo.Params.Length - 1)
                {
                    Append(", ");
                }
            }
        }
        Append(")");
        if (methodDeclarationLineTail is not null)
        {
            Append(methodDeclarationLineTail);
        }
        AppendLine("");

        AppendGenericConstraintsLines(methodDefinitionInfo.GenericTypeParams);
        
        return BeginBlock();
    }

    public _BlockEndDisposable BeginBlock(ReadOnlySpan<char> blockHeadLine)
    {
        PutIndentSpace();
        InternalAppend(blockHeadLine);
        AppendLine();
        return BeginBlock();
    }

    public _BlockEndDisposable BeginBlock()
    {
        PutIndentSpace();
        InternalAppend("{".AsSpan());
        AppendLine();
        _currentIndentCount++;

        return new _BlockEndDisposable(this);
    }

    private void EndBlock()
    {
        _currentIndentCount--;
        PutIndentSpace();
        InternalAppend("}".AsSpan());
        AppendLine();
    }

    public _IndentDisposable BeginIndent()
    {
        _currentIndentCount++;

        return new _IndentDisposable(this);
    }

    private void EndIndent()
    {
        _currentIndentCount--;
    }

    private void AppendGenericConstraintsLines(EquatableArray<CsGenericTypeParam> genericTypeParams)
    {
        if (genericTypeParams.IsDefaultOrEmpty)
            return;

        if (!genericTypeParams.Values.Any(v => v.Where.HasValue && !v.Where.Value.IsAny))
            return;

        using (BeginIndent())
        {
            foreach (var genericTypeParam in genericTypeParams.Values.Where(v => v.Where.HasValue && !v.Where.Value.IsAny))
            {
                PutIndentSpace();
                Append("where ");
                Append(genericTypeParam.Name);
                Append(" : ");

                var constraints = genericTypeParam.Where!.Value;

                bool existsLeadingConstraint = false;

                appendConstraint(this, ref existsLeadingConstraint, constraints.TypeCategory switch
                {
                    CsGenericConstraintTypeCategory.Struct => "struct",
                    CsGenericConstraintTypeCategory.Class => "class",
                    CsGenericConstraintTypeCategory.NullableClass => "class?",
                    CsGenericConstraintTypeCategory.NotNull => "notnull",
                    CsGenericConstraintTypeCategory.Unmanaged => "unmanaged",
                    _ => null,
                });

                appendConstraint(this, ref existsLeadingConstraint, constraints.HaveDefaultConstructor switch
                {
                    true => "new()",
                    _ => null,
                });

                appendConstraint(this, ref existsLeadingConstraint, constraints.BaseType?.GlobalReference);

                foreach (var interfaceConstraint in constraints.Interfaces.Values)
                {
                    appendConstraint(this, ref existsLeadingConstraint, interfaceConstraint.GlobalReference);
                }

                AppendLine("");
            }
        }

        static void appendConstraint(SourceBuilder self, ref bool existsLeadingConstraint, string? constraint)
        {
            if (constraint is null)
                return;

            if (existsLeadingConstraint)
                self.Append(", ");

            self.Append(constraint);
            existsLeadingConstraint = true;
        }
    }

    public ref struct _BlockEndDisposable
    {
        private SourceBuilder? _sourceBuilder;

        private int _nestCount;

        internal _BlockEndDisposable(SourceBuilder? sourceBuilder)
        {    
            _sourceBuilder = sourceBuilder;
            _nestCount = 1;
        }

        public _BlockEndDisposable Combine(_BlockEndDisposable other)
        {
            if (other._sourceBuilder is null && _nestCount == 0) return this;

            if (_nestCount <= 0) throw new InvalidOperationException();
            if (_sourceBuilder != other._sourceBuilder) throw new ArgumentException(null, nameof(other));
            if (other._nestCount <= 0) throw new ArgumentException(null, nameof(other));

            return new _BlockEndDisposable
            {
                _sourceBuilder = _sourceBuilder,
                _nestCount = _nestCount + other._nestCount,
            };
        }

        public void Dispose()
        {
            if (_sourceBuilder is not null)
            {
                for (int i = 0; i < _nestCount; i++)
                {
                    _sourceBuilder.EndBlock();
                }
            }
            _sourceBuilder = null;
            _nestCount = 0;
        }
    }

    public ref struct _IndentDisposable
    {
        private SourceBuilder? _sourceBuilder;

        private int _nestCount;

        internal _IndentDisposable(SourceBuilder? sourceBuilder)
        {
            _sourceBuilder = sourceBuilder;
            _nestCount = 1;
        }

        public void Dispose()
        {
            if (_sourceBuilder is not null)
            {
                for (int i = 0; i < _nestCount; i++)
                {
                    _sourceBuilder.EndIndent();
                }
            }
            _sourceBuilder = null;
            _nestCount = 0;
        }
    }
}
