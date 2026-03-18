using System.Reflection;
using TUnit.Core.Enums;

namespace TUnit.PairwiseDataSource.Tests;

internal static class PairwiseDataSourceInProcessRunner
{
    public static async Task<IReadOnlyList<object?[]?>> GetRowsAsync(MethodInfo methodInfo, object? testClassInstance = null)
    {
        var dataSource = new PairwiseDataSourceAttribute();
        var metadata = CreateMetadata(methodInfo, dataSource, testClassInstance);
        var rows = new List<object?[]?>();

        await foreach (var rowFactory in dataSource.GetDataRowsAsync(metadata))
        {
            rows.Add(await rowFactory());
        }

        return rows;
    }

    public static async Task<Exception> GetGenerationExceptionAsync(MethodInfo methodInfo, object? testClassInstance = null)
    {
        try
        {
            await GetRowsAsync(methodInfo, testClassInstance);
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new InvalidOperationException($"Expected data generation for '{methodInfo.Name}' to fail, but it succeeded.");
    }

    private static DataGeneratorMetadata CreateMetadata(
        MethodInfo methodInfo,
        IDataSourceAttribute dataSource,
        object? testClassInstance)
    {
        var methodMetadata = CreateMethodMetadata(methodInfo);
        var context = new TestBuilderContext
        {
            TestMetadata = methodMetadata,
            DataSourceAttribute = dataSource,
            Events = new TestContextEvents(),
            StateBag = []
        };

        return new DataGeneratorMetadata
        {
            TestBuilderContext = new TestBuilderContextAccessor(context),
            MembersToGenerate = [.. methodMetadata.Parameters],
            TestInformation = methodMetadata,
            Type = DataGeneratorType.TestParameters,
            TestSessionId = "pairwise-in-process-tests",
            TestClassInstance = testClassInstance,
            ClassInstanceArguments = null
        };
    }

    private static MethodMetadata CreateMethodMetadata(MethodInfo methodInfo)
    {
        if (methodInfo.IsGenericMethodDefinition)
        {
            throw new InvalidOperationException(
                $"Generic method definitions are not supported by {nameof(PairwiseDataSourceInProcessRunner)}.");
        }

        var declaringType = methodInfo.DeclaringType
            ?? throw new InvalidOperationException($"Method '{methodInfo.Name}' has no declaring type.");

        var parameters = methodInfo
            .GetParameters()
            .Select(CreateParameterMetadata)
            .ToArray();

        return MethodMetadataFactory.Create(
            methodInfo.Name,
            declaringType,
            methodInfo.ReturnType,
            CreateClassMetadata(declaringType),
            parameters: parameters);
    }

    private static ParameterMetadata CreateParameterMetadata(ParameterInfo parameterInfo)
    {
        var parameterType = parameterInfo.ParameterType;
        var isNullable = Nullable.GetUnderlyingType(parameterType) != null;

        return ParameterMetadataFactory.Create(
            parameterType,
            parameterInfo.Name ?? $"arg{parameterInfo.Position}",
            new ConcreteType(parameterType),
            isNullable,
            reflectionInfoFactory: () => parameterInfo) with
        {
            Position = parameterInfo.Position
        };
    }

    private static ClassMetadata CreateClassMetadata(Type type)
    {
        return new ClassMetadata
        {
            Type = type,
            TypeInfo = new ConcreteType(type),
            Name = type.Name,
            Namespace = type.Namespace ?? string.Empty,
            Assembly = new AssemblyMetadata
            {
                Name = type.Assembly.GetName().Name ?? type.Assembly.GetName().FullName ?? "Unknown"
            },
            Parameters = [],
            Properties = [],
            Parent = null
        };
    }
}
