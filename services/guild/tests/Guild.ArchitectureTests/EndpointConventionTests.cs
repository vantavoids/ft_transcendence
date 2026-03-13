using System.Reflection;
using Carter;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Guild.ArchitectureTests;

public sealed class EndpointConventionTests
{
    private static readonly Type IResultType = typeof(IResult);
    private static readonly Type TaskType    = typeof(Task<>);

    private static IEnumerable<MethodInfo> HandlerMethodsIn(Assembly assembly) =>
        assembly.GetTypes()
                .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(ICarterModule)))
                .SelectMany(t => t.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.DeclaredOnly))
                .Where(m => m.Name != nameof(ICarterModule.AddRoutes));

    private static bool ReturnsPlainIResult(MethodInfo method)
    {
        var returnType = method.ReturnType;

        if (returnType == IResultType)
            return true;

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == TaskType)
            return returnType.GetGenericArguments()[0] == IResultType;

        return false;
    }

    [Fact]
    public void EndpointHandlers_MustNotReturn_PlainIResult()
    {
        var violations = HandlerMethodsIn(Assemblies.Presentation)
            .Where(ReturnsPlainIResult)
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name} returns {m.ReturnType.Name}")
            .ToList();

        Assert.True(violations.Count == 0,
            "The following handlers return plain IResult, breaking OpenAPI type inference:\n  - " +
            string.Join("\n  - ", violations));
    }
}
