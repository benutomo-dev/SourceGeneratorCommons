#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SourceGeneratorCommons;

internal static class DebugSGen
{
    [Conditional("DEBUG")]
    public static void Assert(bool condition, [CallerArgumentExpression(nameof(condition))] string? conditionExpression = null)
    {
        if (!ShouldBeWorking())
            return;

        Debug.Assert(condition, conditionExpression);
    }

    [Conditional("DEBUG")]
    public static void Fail(string? message = null)
    {
        if (!ShouldBeWorking())
            return;

        Debug.Fail(message);
    }

#pragma warning disable CS8777
    [Conditional("DEBUG")]
    public static void AssertIsNotNull<T>([NotNull] T? value)
    {
        if (!ShouldBeWorking())
            return;

        Debug.Assert(value is not null);
    }
#pragma warning restore CS8777

    public static T ToNotNullWithAssert<T>(this T? value) where T : class
    {
        DebugSGen.AssertIsNotNull(value);
        return value;
    }

    private static bool ShouldBeWorking()
    {
        if (Debugger.IsAttached)
            return true;

        if (string.Equals(AppDomain.CurrentDomain.FriendlyName, "testhost", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
