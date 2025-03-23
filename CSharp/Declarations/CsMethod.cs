#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;
using System.Text;

namespace SourceGeneratorCommons.CSharp.Declarations;

record class CsMethod(
    string Name,
    CsTypeRefWithAnnotation ReturnType,
    CsReturnModifier ReturnModifier = CsReturnModifier.Default,
    bool IsStatic = false,
    bool IsAsync = false,
    bool IsReadOnly = false,
    EquatableArray<CsMethodParam> Params = default,
    EquatableArray<CsTypeParameterDeclaration> GenericTypeParams = default,
    CsAccessibility Accessibility = CsAccessibility.Default,
    CsMethodModifier MethodModifier = CsMethodModifier.Default
    )
{
    /// <summary>
    /// `void Xxx();`の様な純粋なvoidメソッドである場合に<see langword="true"/>。
    /// </summary>
    public bool IsPureVoidMethod => ReturnType.ToString() == "void";

    /// <summary>
    /// `void Xxx();`の様な純粋なvoidメソッドであるか、型引数のない<see cref="Task"/>または<see cref="ValueTask"/>を返す場合に<see langword="true"/>。
    /// </summary>
    /// <remarks>
    /// 今はまだTaskライクな独自のawait可能型に対する判定までは未実装。
    /// </remarks>
    public bool IsVoidLikeMethod
    {
        get
        {
            if (IsPureVoidMethod)
                return true;

            if (ReturnType.Type.TypeDefinition.Is(CsSpecialType.Task))
                return true;

            if (ReturnType.Type.TypeDefinition.Is(CsSpecialType.ValueTask))
                return true;

            return false;
        }
    }

    /// <summary>
    /// 戻り値の型が以下のいずれかに該当するメソッドである場合に<see langword="true"/>。
    /// <list type="number">
    /// <item><see cref="Task"/></item>
    /// <item><see cref="ValueTask"/></item>
    /// <item><see cref="Task{TResult}"/></item>
    /// <item><see cref="ValueTask{TResult}"/></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// 今はまだTaskライクな独自のawait可能型に対する判定までは未実装。
    /// </remarks>
    public bool IsAwaitableMethod
    {
        get
        {
            if (ReturnType.Type.TypeDefinition.Is(CsSpecialType.Task))
                return true;

            if (ReturnType.Type.TypeDefinition.Is(CsSpecialType.ValueTask))
                return true;

            if (ReturnType.Type.TypeDefinition.Is(CsSpecialType.TaskT))
                return true;

            if (ReturnType.Type.TypeDefinition.Is(CsSpecialType.ValueTaskT))
                return true;

            return false;
        }
    }


    public bool IsExtensionMethod => this is CsExtensionMethod;

    public string Cref => _cref ??= BuildCref();


    private string? _cref;

    public string BuildCref()
    {
        StringBuilder builder = new StringBuilder(256);

        builder.Append(Name);
        if (!GenericTypeParams.IsDefaultOrEmpty)
        {
            builder.Append('{');
            builder.Append(GenericTypeParams[0].Name);
            for (int i = 1; i < GenericTypeParams.Length; i++)
            {
                builder.Append(", ");
                builder.Append(GenericTypeParams[i].Name);
            }
            builder.Append('}');
        }
        builder.Append('(');
        if (!Params.IsDefaultOrEmpty)
        {
            for (int i = 0; i < Params.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                switch (Params[i].Modifier)
                {
                    case CsParamModifier.Ref:
                        builder.Append("ref ");
                        break;
                    case CsParamModifier.In:
                        builder.Append("in ");
                        break;
                    case CsParamModifier.Out:
                        builder.Append("out ");
                        break;
                    case CsParamModifier.RefReadOnly:
                        builder.Append("ref readonly ");
                        break;
                }

                builder.Append(Params[i].Type.Cref);
            }
        }
        builder.Append(')');

        var value = builder.ToString();

        return value;
    }
}
