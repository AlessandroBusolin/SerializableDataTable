namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using Touchstone.Core;

    /// <summary>
    /// Builds the Touchstone <see cref="TestSuiteDescriptor"/> collection from
    /// <see cref="SerializableDataTableScenarios"/>. Every public static, parameterless method that
    /// returns void or Task is discovered automatically and grouped into a suite by the portion of
    /// its name preceding the first underscore. This is the single collection consumed by the
    /// CLI runner (Test.Automated), the xUnit adapter (Test.Xunit) and the NUnit adapter (Test.Nunit).
    /// </summary>
    public static class SerializableDataTableSuites
    {
        private static readonly IReadOnlyDictionary<string, string> _SuiteDisplayNames = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "Column", "SerializableColumn" },
            { "Enum", "ColumnValueTypeEnum" },
            { "Construction", "Construction And Properties" },
            { "FromDataTable", "FromDataTable Conversion" },
            { "ToDataTable", "ToDataTable Conversion" },
            { "Types", "Data Type Round-Trips" },
            { "Arrays", "Array Type Preservation" },
            { "Json", "JSON Serialization" },
            { "Pgvector", "Custom Type (Pgvector) Reconstruction" },
            { "Nulls", "Null And DBNull Handling" },
            { "Markdown", "Markdown Conversion" },
        };

        private static readonly IReadOnlyList<TestSuiteDescriptor> _All = BuildSuites();

        /// <summary>
        /// All discovered suites, ordered by suite identifier.
        /// </summary>
        public static IReadOnlyList<TestSuiteDescriptor> All
        {
            get { return _All; }
        }

        private static IReadOnlyList<TestSuiteDescriptor> BuildSuites()
        {
            IEnumerable<MethodInfo> methods = typeof(SerializableDataTableScenarios)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.ReturnType == typeof(void) || m.ReturnType == typeof(Task))
                .Where(m => m.GetParameters().Length == 0)
                .Where(m => m.Name.IndexOf('_') > 0);

            IEnumerable<IGrouping<string, MethodInfo>> groups = methods
                .GroupBy(m => m.Name.Substring(0, m.Name.IndexOf('_')), StringComparer.Ordinal)
                .OrderBy(g => g.Key, StringComparer.Ordinal);

            List<TestSuiteDescriptor> suites = new List<TestSuiteDescriptor>();

            foreach (IGrouping<string, MethodInfo> group in groups)
            {
                string suiteId = group.Key;
                string displayName = _SuiteDisplayNames.ContainsKey(suiteId) ? _SuiteDisplayNames[suiteId] : suiteId;

                List<TestCaseDescriptor> cases = group
                    .OrderBy(m => m.Name, StringComparer.Ordinal)
                    .Select(m => BuildCase(suiteId, m))
                    .ToList();

                suites.Add(new TestSuiteDescriptor(suiteId, displayName, cases));
            }

            return suites;
        }

        private static TestCaseDescriptor BuildCase(string suiteId, MethodInfo method)
        {
            string caseName = method.Name.Substring(method.Name.IndexOf('_') + 1);

            return new TestCaseDescriptor(
                suiteId,
                method.Name,
                ToDisplayName(caseName),
                token =>
                {
                    try
                    {
                        object result = method.Invoke(null, null);
                        if (result is Task task) return task;
                        return Task.CompletedTask;
                    }
                    catch (TargetInvocationException e) when (e.InnerException != null)
                    {
                        return Task.FromException(e.InnerException);
                    }
                });
        }

        private static string ToDisplayName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            List<char> chars = new List<char>(name.Length + 8);
            chars.Add(name[0]);

            for (int i = 1; i < name.Length; i++)
            {
                char current = name[i];
                if (char.IsUpper(current) && !char.IsUpper(name[i - 1]))
                {
                    chars.Add(' ');
                }

                chars.Add(current);
            }

            return new string(chars.ToArray());
        }
    }
}
