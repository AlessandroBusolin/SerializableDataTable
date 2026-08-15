namespace Test.Nunit
{
    using System.Collections;
    using System.Threading;
    using System.Threading.Tasks;

    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.NunitAdapter;
    using global::NUnit.Framework;

    /// <summary>
    /// NUnit host that exposes every shared Touchstone descriptor as an individual test case.
    /// Runs non-parallel so mutable static state is exercised deterministically.
    /// </summary>
    [NonParallelizable]
    public sealed class SerializableDataTableNUnitTests
    {
        public static IEnumerable TestCases()
        {
            return new TouchstoneTestCaseSource(SerializableDataTableSuites.All);
        }

        [TestCaseSource(nameof(TestCases))]
        public async Task Run(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
