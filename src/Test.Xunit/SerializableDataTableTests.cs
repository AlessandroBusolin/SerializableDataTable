namespace Test.Xunit
{
    using System.Threading;
    using System.Threading.Tasks;

    using Test.Shared;
    using Touchstone.Core;
    using global::Xunit;

    /// <summary>
    /// Collection definition disabling parallelization so mutable static state
    /// (for example <c>MarkdownConverter.NewlineCharacter</c>) is exercised deterministically.
    /// </summary>
    [CollectionDefinition("SerializableDataTable", DisableParallelization = true)]
    public sealed class SerializableDataTableCollection
    {
    }

    /// <summary>
    /// xUnit host that exposes every shared Touchstone descriptor as an individual theory row.
    /// </summary>
    [Collection("SerializableDataTable")]
    public sealed class SerializableDataTableTests
    {
        public static TheoryData<TestCaseDescriptor> TestCases()
        {
            TheoryData<TestCaseDescriptor> data = new TheoryData<TestCaseDescriptor>();

            foreach (TestSuiteDescriptor suite in SerializableDataTableSuites.All)
            {
                foreach (TestCaseDescriptor testCase in suite.Cases)
                {
                    if (!testCase.Skip) data.Add(testCase);
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task Run(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None);
        }
    }
}
