namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using System.Text.Json;

    using Pgvector;

    using SerializableDataTables;

    /// <summary>
    /// Central source of truth for all SerializableDataTable test cases.
    /// Every public static, parameterless, void-returning method is an atomic test case
    /// discovered by <see cref="SerializableDataTableSuites"/> and exposed identically through
    /// the Touchstone CLI runner (Test.Automated), the xUnit adapter (Test.Xunit) and the
    /// NUnit adapter (Test.Nunit).
    ///
    /// Naming convention: {SuiteId}_{CaseName}. The text before the first underscore selects
    /// the suite; the remainder becomes the human-readable display name.
    /// </summary>
    public static class SerializableDataTableScenarios
    {
        #region SerializableColumn

        public static void Column_DefaultNameIsMyTable()
        {
            SerializableColumn col = new SerializableColumn();
            TestAssert.Equal("MyTable", col.Name);
        }

        public static void Column_DefaultTypeIsString()
        {
            SerializableColumn col = new SerializableColumn();
            TestAssert.Equal(ColumnValueTypeEnum.String, col.Type);
        }

        public static void Column_DefaultOriginalTypeIsNull()
        {
            SerializableColumn col = new SerializableColumn();
            TestAssert.Null(col.OriginalType);
        }

        public static void Column_SetNameValid()
        {
            SerializableColumn col = new SerializableColumn { Name = "Employee" };
            TestAssert.Equal("Employee", col.Name);
        }

        public static void Column_SetNameNullThrows()
        {
            TestAssert.Throws<ArgumentNullException>(() =>
            {
                SerializableColumn col = new SerializableColumn();
                col.Name = null;
            });
        }

        public static void Column_SetNameEmptyThrows()
        {
            TestAssert.Throws<ArgumentNullException>(() =>
            {
                SerializableColumn col = new SerializableColumn();
                col.Name = string.Empty;
            });
        }

        public static void Column_SetType()
        {
            SerializableColumn col = new SerializableColumn { Type = ColumnValueTypeEnum.Guid };
            TestAssert.Equal(ColumnValueTypeEnum.Guid, col.Type);
        }

        public static void Column_SetOriginalType()
        {
            SerializableColumn col = new SerializableColumn { OriginalType = "System.Int32[]" };
            TestAssert.Equal("System.Int32[]", col.OriginalType);
        }

        #endregion

        #region ColumnValueTypeEnum

        public static void Enum_HasTwentyValues()
        {
            int count = Enum.GetValues(typeof(ColumnValueTypeEnum)).Length;
            TestAssert.Equal(20, count);
        }

        public static void Enum_SerializesAsString()
        {
            SerializableColumn col = new SerializableColumn { Name = "C", Type = ColumnValueTypeEnum.Int32 };
            string json = JsonSerializer.Serialize(col);
            TestAssert.Contains(json, "\"Int32\"");
            TestAssert.DoesNotContain(json, "\"Type\":2");
        }

        public static void Enum_DeserializesFromString()
        {
            string json = "{\"Name\":\"C\",\"Type\":\"Guid\"}";
            SerializableColumn col = JsonSerializer.Deserialize<SerializableColumn>(json);
            TestAssert.Equal(ColumnValueTypeEnum.Guid, col.Type);
        }

        public static void Enum_AllNamesRoundTripThroughJson()
        {
            foreach (ColumnValueTypeEnum value in Enum.GetValues(typeof(ColumnValueTypeEnum)).Cast<ColumnValueTypeEnum>())
            {
                SerializableColumn col = new SerializableColumn { Name = "C", Type = value };
                string json = JsonSerializer.Serialize(col);
                TestAssert.Contains(json, "\"" + value.ToString() + "\"");
                SerializableColumn back = JsonSerializer.Deserialize<SerializableColumn>(json);
                TestAssert.Equal(value, back.Type, "Enum round-trip failed for " + value);
            }
        }

        public static void Enum_UnknownStringThrows()
        {
            TestAssert.Throws<JsonException>(() =>
            {
                JsonSerializer.Deserialize<SerializableColumn>("{\"Name\":\"C\",\"Type\":\"NotARealType\"}");
            });
        }

        #endregion

        #region Construction and properties

        public static void Construction_DefaultNameIsNull()
        {
            SerializableDataTable sdt = new SerializableDataTable();
            TestAssert.Null(sdt.Name);
        }

        public static void Construction_NameFromConstructor()
        {
            SerializableDataTable sdt = new SerializableDataTable("Orders");
            TestAssert.Equal("Orders", sdt.Name);
        }

        public static void Construction_EmptyConstructorNameLeavesNull()
        {
            SerializableDataTable sdt = new SerializableDataTable(string.Empty);
            TestAssert.Null(sdt.Name);
        }

        public static void Construction_ColumnsInitializedEmpty()
        {
            SerializableDataTable sdt = new SerializableDataTable();
            TestAssert.NotNull(sdt.Columns);
            TestAssert.Equal(0, sdt.Columns.Count);
        }

        public static void Construction_RowsInitializedEmpty()
        {
            SerializableDataTable sdt = new SerializableDataTable();
            TestAssert.NotNull(sdt.Rows);
            TestAssert.Equal(0, sdt.Rows.Count);
        }

        public static void Construction_SetColumnsNullCoalescesToEmpty()
        {
            SerializableDataTable sdt = new SerializableDataTable();
            sdt.Columns = null;
            TestAssert.NotNull(sdt.Columns);
            TestAssert.Equal(0, sdt.Columns.Count);
        }

        public static void Construction_SetRowsNullCoalescesToEmpty()
        {
            SerializableDataTable sdt = new SerializableDataTable();
            sdt.Rows = null;
            TestAssert.NotNull(sdt.Rows);
            TestAssert.Equal(0, sdt.Rows.Count);
        }

        public static void Construction_SetColumnsValid()
        {
            SerializableDataTable sdt = new SerializableDataTable();
            sdt.Columns = new List<SerializableColumn> { new SerializableColumn { Name = "A" } };
            TestAssert.Equal(1, sdt.Columns.Count);
            TestAssert.Equal("A", sdt.Columns[0].Name);
        }

        public static void Construction_SetRowsValid()
        {
            SerializableDataTable sdt = new SerializableDataTable();
            sdt.Rows = new List<Dictionary<string, object>> { new Dictionary<string, object> { { "A", 1 } } };
            TestAssert.Equal(1, sdt.Rows.Count);
        }

        public static void Construction_NameSettable()
        {
            SerializableDataTable sdt = new SerializableDataTable();
            sdt.Name = "Renamed";
            TestAssert.Equal("Renamed", sdt.Name);
        }

        #endregion

        #region FromDataTable

        public static void FromDataTable_NullThrows()
        {
            TestAssert.Throws<ArgumentNullException>(() => SerializableDataTable.FromDataTable(null));
        }

        public static void FromDataTable_PreservesTableName()
        {
            DataTable dt = new DataTable("Employees");
            dt.Columns.Add("Id", typeof(int));
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            TestAssert.Equal("Employees", sdt.Name);
        }

        public static void FromDataTable_PreservesColumnCountAndNames()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            TestAssert.Equal(2, sdt.Columns.Count);
            TestAssert.Equal("Id", sdt.Columns[0].Name);
            TestAssert.Equal("Name", sdt.Columns[1].Name);
        }

        public static void FromDataTable_MapsColumnTypes()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("S", typeof(string));
            dt.Columns.Add("I", typeof(int));
            dt.Columns.Add("B", typeof(bool));
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            TestAssert.Equal(ColumnValueTypeEnum.String, sdt.Columns[0].Type);
            TestAssert.Equal(ColumnValueTypeEnum.Int32, sdt.Columns[1].Type);
            TestAssert.Equal(ColumnValueTypeEnum.Boolean, sdt.Columns[2].Type);
        }

        public static void FromDataTable_PreservesRowCountAndValues()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add(1, "Alice");
            dt.Rows.Add(2, "Bob");
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            TestAssert.Equal(2, sdt.Rows.Count);
            TestAssert.Equal(1, (int)sdt.Rows[0]["Id"]);
            TestAssert.Equal("Alice", (string)sdt.Rows[0]["Name"]);
            TestAssert.Equal("Bob", (string)sdt.Rows[1]["Name"]);
        }

        public static void FromDataTable_DBNullBecomesNull()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add(DBNull.Value);
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            TestAssert.True(sdt.Rows[0].ContainsKey("Name"));
            TestAssert.Null(sdt.Rows[0]["Name"]);
        }

        public static void FromDataTable_EmptyTableNoColumns()
        {
            DataTable dt = new DataTable("Empty");
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            TestAssert.Equal(0, sdt.Columns.Count);
            TestAssert.Equal(0, sdt.Rows.Count);
        }

        public static void FromDataTable_ColumnsButNoRows()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Id", typeof(int));
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            TestAssert.Equal(1, sdt.Columns.Count);
            TestAssert.Equal(0, sdt.Rows.Count);
        }

        public static void FromDataTable_ObjectArrayColumnSetsOriginalType()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Embedding", typeof(object));
            dt.Rows.Add(1, new float[] { 0.1f, 0.2f });
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            SerializableColumn col = sdt.Columns.First(c => c.Name == "Embedding");
            TestAssert.Equal(ColumnValueTypeEnum.Object, col.Type);
            TestAssert.NotNull(col.OriginalType);
            TestAssert.Contains(col.OriginalType, "Single");
        }

        public static void FromDataTable_ObjectColumnAllNullNoOriginalType()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Data", typeof(object));
            dt.Rows.Add(1, DBNull.Value);
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            SerializableColumn col = sdt.Columns.First(c => c.Name == "Data");
            TestAssert.Equal(ColumnValueTypeEnum.Object, col.Type);
            TestAssert.Null(col.OriginalType);
        }

        public static void FromDataTable_ExplicitArrayColumnStoresType()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Values", typeof(int[]));
            dt.Rows.Add(new object[] { new int[] { 1, 2, 3 } });
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            SerializableColumn col = sdt.Columns[0];
            TestAssert.NotNull(col.OriginalType);
            TestAssert.Contains(col.OriginalType, "Int32");
        }

        public static void FromDataTable_ByteArrayColumnMapsToByteArrayEnum()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Blob", typeof(byte[]));
            dt.Rows.Add(new object[] { new byte[] { 1, 2, 3 } });
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            SerializableColumn col = sdt.Columns[0];
            TestAssert.Equal(ColumnValueTypeEnum.ByteArray, col.Type);
            TestAssert.Null(col.OriginalType);
        }

        public static void FromDataTable_UnknownTypeMapsToObjectWithOriginalType()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Link", typeof(Uri));
            dt.Rows.Add(new object[] { new Uri("https://example.com") });
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            SerializableColumn col = sdt.Columns[0];
            TestAssert.Equal(ColumnValueTypeEnum.Object, col.Type);
            TestAssert.NotNull(col.OriginalType);
            TestAssert.Contains(col.OriginalType, "System.Uri");
        }

        public static void FromDataTable_ArrayTypeDetectedFromLaterRowAfterNull()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Data", typeof(object));
            dt.Rows.Add(DBNull.Value);
            dt.Rows.Add(new object[] { new double[] { 1.0, 2.0 } });
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            SerializableColumn col = sdt.Columns[0];
            TestAssert.NotNull(col.OriginalType);
            TestAssert.Contains(col.OriginalType, "Double");
        }

        #endregion

        #region ToDataTable

        public static void ToDataTable_EmptyProducesEmpty()
        {
            SerializableDataTable sdt = new SerializableDataTable("Empty");
            DataTable dt = sdt.ToDataTable();
            TestAssert.Equal(0, dt.Columns.Count);
            TestAssert.Equal(0, dt.Rows.Count);
        }

        public static void ToDataTable_PreservesName()
        {
            SerializableDataTable sdt = new SerializableDataTable("Widgets");
            sdt.Columns.Add(new SerializableColumn { Name = "Id", Type = ColumnValueTypeEnum.Int32 });
            DataTable dt = sdt.ToDataTable();
            TestAssert.Equal("Widgets", dt.TableName);
        }

        public static void ToDataTable_CreatesColumnsWithMappedTypes()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "S", Type = ColumnValueTypeEnum.String });
            sdt.Columns.Add(new SerializableColumn { Name = "I", Type = ColumnValueTypeEnum.Int32 });
            sdt.Columns.Add(new SerializableColumn { Name = "G", Type = ColumnValueTypeEnum.Guid });
            DataTable dt = sdt.ToDataTable();
            TestAssert.Equal(typeof(string), dt.Columns["S"].DataType);
            TestAssert.Equal(typeof(int), dt.Columns["I"].DataType);
            TestAssert.Equal(typeof(Guid), dt.Columns["G"].DataType);
        }

        public static void ToDataTable_PreservesRowValues()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "Id", Type = ColumnValueTypeEnum.Int32 });
            sdt.Columns.Add(new SerializableColumn { Name = "Name", Type = ColumnValueTypeEnum.String });
            sdt.Rows.Add(new Dictionary<string, object> { { "Id", 7 }, { "Name", "Gadget" } });
            DataTable dt = sdt.ToDataTable();
            TestAssert.Equal(1, dt.Rows.Count);
            TestAssert.Equal(7, (int)dt.Rows[0]["Id"]);
            TestAssert.Equal("Gadget", (string)dt.Rows[0]["Name"]);
        }

        public static void ToDataTable_NullValueBecomesDBNull()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "Name", Type = ColumnValueTypeEnum.String });
            sdt.Rows.Add(new Dictionary<string, object> { { "Name", null } });
            DataTable dt = sdt.ToDataTable();
            TestAssert.Equal(DBNull.Value, dt.Rows[0]["Name"]);
        }

        public static void ToDataTable_UnknownColumnInRowThrows()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "Known", Type = ColumnValueTypeEnum.Int32 });
            sdt.Rows.Add(new Dictionary<string, object> { { "Unknown", 1 } });
            TestAssert.Throws<ArgumentException>(() => sdt.ToDataTable());
        }

        public static void ToDataTable_InvalidColumnTypeThrows()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "Bad", Type = (ColumnValueTypeEnum)999 });
            TestAssert.Throws<ArgumentException>(() => sdt.ToDataTable());
        }

        #endregion

        #region Scalar type round-trips (in-memory DataTable -> SerializableDataTable -> DataTable)

        public static void Types_String()
        {
            AssertScalarRoundTrip(typeof(string), "hello world", ColumnValueTypeEnum.String);
        }

        public static void Types_Int16()
        {
            AssertScalarRoundTrip(typeof(short), (short)-12345, ColumnValueTypeEnum.Int16);
        }

        public static void Types_Int32()
        {
            AssertScalarRoundTrip(typeof(int), 2147483647, ColumnValueTypeEnum.Int32);
        }

        public static void Types_Int64()
        {
            AssertScalarRoundTrip(typeof(long), 9223372036854775807L, ColumnValueTypeEnum.Int64);
        }

        public static void Types_UInt16()
        {
            AssertScalarRoundTrip(typeof(ushort), (ushort)65000, ColumnValueTypeEnum.UInt16);
        }

        public static void Types_UInt32()
        {
            AssertScalarRoundTrip(typeof(uint), 4000000000U, ColumnValueTypeEnum.UInt32);
        }

        public static void Types_UInt64()
        {
            AssertScalarRoundTrip(typeof(ulong), 18446744073709551615UL, ColumnValueTypeEnum.UInt64);
        }

        public static void Types_Decimal()
        {
            AssertScalarRoundTrip(typeof(decimal), 123456.789012m, ColumnValueTypeEnum.Decimal);
        }

        public static void Types_Double()
        {
            AssertScalarRoundTrip(typeof(double), 3.14159265358979, ColumnValueTypeEnum.Double);
        }

        public static void Types_Float()
        {
            AssertScalarRoundTrip(typeof(float), 2.71828f, ColumnValueTypeEnum.Float);
        }

        public static void Types_Boolean()
        {
            AssertScalarRoundTrip(typeof(bool), true, ColumnValueTypeEnum.Boolean);
        }

        public static void Types_DateTime()
        {
            AssertScalarRoundTrip(typeof(DateTime), new DateTime(2024, 6, 15, 14, 30, 45), ColumnValueTypeEnum.DateTime);
        }

        public static void Types_DateTimeOffset()
        {
            AssertScalarRoundTrip(typeof(DateTimeOffset), new DateTimeOffset(2024, 6, 15, 14, 30, 45, TimeSpan.FromHours(-5)), ColumnValueTypeEnum.DateTimeOffset);
        }

        public static void Types_TimeSpan()
        {
            AssertScalarRoundTrip(typeof(TimeSpan), TimeSpan.FromHours(25.5), ColumnValueTypeEnum.TimeSpan);
        }

        public static void Types_Byte()
        {
            AssertScalarRoundTrip(typeof(byte), (byte)255, ColumnValueTypeEnum.Byte);
        }

        public static void Types_SByte()
        {
            AssertScalarRoundTrip(typeof(sbyte), (sbyte)-128, ColumnValueTypeEnum.SByte);
        }

        public static void Types_Char()
        {
            AssertScalarRoundTrip(typeof(char), 'Z', ColumnValueTypeEnum.Char);
        }

        public static void Types_Guid()
        {
            AssertScalarRoundTrip(typeof(Guid), Guid.NewGuid(), ColumnValueTypeEnum.Guid);
        }

        public static void Types_ByteArray()
        {
            byte[] original = new byte[] { 1, 2, 3, 250, 255 };
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Blob", typeof(byte[]));
            dt.Rows.Add(new object[] { original });
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            TestAssert.Equal(ColumnValueTypeEnum.ByteArray, sdt.Columns[0].Type);
            DataTable back = sdt.ToDataTable();
            TestAssert.Equal(typeof(byte[]), back.Columns[0].DataType);
            byte[] result = (byte[])back.Rows[0]["Blob"];
            TestAssert.SequenceEqual(original, result);
        }

        public static void Types_ObjectColumnMapsToObjectEnum()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("O", typeof(object));
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            TestAssert.Equal(ColumnValueTypeEnum.Object, sdt.Columns[0].Type);
            DataTable back = sdt.ToDataTable();
            TestAssert.Equal(typeof(object), back.Columns[0].DataType);
        }

        #endregion

        #region Array type preservation (in-memory)

        public static void Arrays_ObjectColumnFloatArrayNormalized()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("V", typeof(object));
            dt.Rows.Add(new object[] { new float[] { 1.5f, 2.5f } });
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            object stored = sdt.Rows[0]["V"];
            TestAssert.True(stored is float[], "Expected float[] stored, got " + (stored == null ? "null" : stored.GetType().Name));
        }

        public static void Arrays_OriginalTypeNullForScalarColumn()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add(1, "x");
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            TestAssert.Null(sdt.Columns.First(c => c.Name == "Id").OriginalType);
            TestAssert.Null(sdt.Columns.First(c => c.Name == "Name").OriginalType);
        }

        public static void Arrays_NullArrayInObjectColumnStaysNull()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("V", typeof(object));
            dt.Rows.Add(1, DBNull.Value);
            dt.Rows.Add(2, new float[] { 1.0f });
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            TestAssert.Null(sdt.Rows[0]["V"]);
            TestAssert.True(sdt.Rows[1]["V"] is float[]);
        }

        #endregion

        #region JSON serialization round-trips

        public static void Json_EmptyTableRoundTrip()
        {
            SerializableDataTable sdt = new SerializableDataTable("Empty");
            SerializableDataTable back = JsonRoundTrip(sdt);
            TestAssert.Equal("Empty", back.Name);
            TestAssert.Equal(0, back.Columns.Count);
            TestAssert.Equal(0, back.Rows.Count);
        }

        public static void Json_ColumnMetadataRoundTrip()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "Id", Type = ColumnValueTypeEnum.Int32 });
            sdt.Columns.Add(new SerializableColumn { Name = "V", Type = ColumnValueTypeEnum.Object, OriginalType = "System.Single[]" });
            SerializableDataTable back = JsonRoundTrip(sdt);
            TestAssert.Equal(2, back.Columns.Count);
            TestAssert.Equal("Id", back.Columns[0].Name);
            TestAssert.Equal(ColumnValueTypeEnum.Int32, back.Columns[0].Type);
            TestAssert.Equal(ColumnValueTypeEnum.Object, back.Columns[1].Type);
            TestAssert.Equal("System.Single[]", back.Columns[1].OriginalType);
        }

        public static void Json_FloatArrayRoundTripToDataTable()
        {
            AssertArrayJsonRoundTrip<float>("Embedding", new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f });
        }

        public static void Json_DoubleArrayRoundTripToDataTable()
        {
            AssertArrayJsonRoundTrip<double>("Values", new double[] { 1.1, 2.2, 3.3 });
        }

        public static void Json_IntArrayRoundTripToDataTable()
        {
            AssertArrayJsonRoundTrip<int>("Tags", new int[] { 1, 2, 3, 4, 5 });
        }

        public static void Json_LongArrayRoundTripToDataTable()
        {
            AssertArrayJsonRoundTrip<long>("Big", new long[] { 100000000000L, 200000000000L });
        }

        public static void Json_DecimalArrayRoundTripToDataTable()
        {
            AssertArrayJsonRoundTrip<decimal>("Precise", new decimal[] { 1.234567890123456789m, 9.876543210987654321m });
        }

        public static void Json_ShortArrayRoundTripToDataTable()
        {
            AssertArrayJsonRoundTrip<short>("Small", new short[] { 1, 2, 3 });
        }

        public static void Json_StringArrayRoundTripToDataTable()
        {
            AssertArrayJsonRoundTrip<string>("Words", new string[] { "hello", "world", "test" });
        }

        public static void Json_BoolArrayRoundTripToDataTable()
        {
            AssertArrayJsonRoundTrip<bool>("Flags", new bool[] { true, false, true });
        }

        public static void Json_GuidArrayRoundTripToDataTable()
        {
            AssertArrayJsonRoundTrip<Guid>("Ids", new Guid[] { Guid.NewGuid(), Guid.NewGuid() });
        }

        public static void Json_DateTimeArrayRoundTripToDataTable()
        {
            AssertArrayJsonRoundTrip<DateTime>("Stamps", new DateTime[]
            {
                new DateTime(2024, 1, 15, 10, 30, 0),
                new DateTime(2024, 12, 31, 23, 59, 59)
            });
        }

        public static void Json_EmptyArrayRoundTrip()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("V", typeof(object));
            dt.Rows.Add(1, new float[0]);
            DataTable result = FullJsonRoundTrip(dt);
            object value = result.Rows[0]["V"];
            TestAssert.True(value is float[], "Expected float[]");
            TestAssert.Equal(0, ((float[])value).Length);
        }

        public static void Json_NullArrayPreservedAsDBNull()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("V", typeof(object));
            dt.Rows.Add(1, DBNull.Value);
            dt.Rows.Add(2, new float[] { 1.0f });
            DataTable result = FullJsonRoundTrip(dt);
            TestAssert.Equal(DBNull.Value, result.Rows[0]["V"]);
            TestAssert.True(result.Rows[1]["V"] is float[]);
        }

        public static void Json_MixedArrayTypesInSameTable()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("F", typeof(object));
            dt.Columns.Add("I", typeof(object));
            dt.Columns.Add("D", typeof(object));
            dt.Rows.Add(1, new float[] { 0.1f, 0.2f }, new int[] { 1, 2, 3 }, new double[] { 1.1, 2.2 });
            DataTable result = FullJsonRoundTrip(dt);
            TestAssert.True(result.Rows[0]["F"] is float[], "F should be float[]");
            TestAssert.True(result.Rows[0]["I"] is int[], "I should be int[]");
            TestAssert.True(result.Rows[0]["D"] is double[], "D should be double[]");
        }

        public static void Json_FloatValueAccuracyPreserved()
        {
            float[] original = new float[] { 0.123456789f, -0.987654321f, 0.0f, float.MaxValue, float.MinValue };
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("V", typeof(object));
            dt.Rows.Add(1, original);
            DataTable result = FullJsonRoundTrip(dt);
            float[] resultArray = (float[])result.Rows[0]["V"];
            TestAssert.FloatArrayClose(original, resultArray);
        }

        public static void Json_BackwardCompatNoOriginalTypeInfersDoubleArray()
        {
            string oldFormatJson = "{\"Name\":\"T\",\"Columns\":[{\"Name\":\"Id\",\"Type\":\"Int32\"},{\"Name\":\"Data\",\"Type\":\"Object\"}],\"Rows\":[{\"Id\":1,\"Data\":[1.5,2.5,3.5]}]}";
            SerializableDataTable sdt = JsonSerializer.Deserialize<SerializableDataTable>(oldFormatJson);
            DataTable dt = sdt.ToDataTable();
            object value = dt.Rows[0]["Data"];
            TestAssert.True(value is double[], "Expected double[] inferred for numeric array without OriginalType");
            double[] arr = (double[])value;
            TestAssert.Equal(3, arr.Length);
            TestAssert.True(Math.Abs(arr[0] - 1.5) < 0.0001);
        }

        public static void Json_BackwardCompatNoOriginalTypeInfersStringArray()
        {
            string oldFormatJson = "{\"Name\":\"T\",\"Columns\":[{\"Name\":\"Data\",\"Type\":\"Object\"}],\"Rows\":[{\"Data\":[\"a\",\"b\",\"c\"]}]}";
            SerializableDataTable sdt = JsonSerializer.Deserialize<SerializableDataTable>(oldFormatJson);
            DataTable dt = sdt.ToDataTable();
            object value = dt.Rows[0]["Data"];
            TestAssert.True(value is string[], "Expected string[] inferred");
            TestAssert.SequenceEqual(new string[] { "a", "b", "c" }, (string[])value);
        }

        public static void Json_BackwardCompatNoOriginalTypeInfersBoolArray()
        {
            string oldFormatJson = "{\"Name\":\"T\",\"Columns\":[{\"Name\":\"Data\",\"Type\":\"Object\"}],\"Rows\":[{\"Data\":[true,false,true]}]}";
            SerializableDataTable sdt = JsonSerializer.Deserialize<SerializableDataTable>(oldFormatJson);
            DataTable dt = sdt.ToDataTable();
            object value = dt.Rows[0]["Data"];
            TestAssert.True(value is bool[], "Expected bool[] inferred");
            TestAssert.SequenceEqual(new bool[] { true, false, true }, (bool[])value);
        }

        public static void Json_ScientificNotationParsedAsDouble()
        {
            string json = "{\"Name\":\"T\",\"Columns\":[{\"Name\":\"Data\",\"Type\":\"Object\"},{\"Name\":\"Original\",\"Type\":\"Double\"}],\"Rows\":[{\"Data\":[3.4028235E+38],\"Original\":1.0}]}";
            SerializableDataTable sdt = JsonSerializer.Deserialize<SerializableDataTable>(json);
            DataTable dt = sdt.ToDataTable();
            object value = dt.Rows[0]["Data"];
            TestAssert.True(value is double[], "Expected double[] for scientific-notation numeric array");
            TestAssert.True(((double[])value)[0] > 1e38);
        }

        public static void Json_EmptyArrayInferredAsObjectArray()
        {
            string json = "{\"Name\":\"T\",\"Columns\":[{\"Name\":\"Data\",\"Type\":\"Object\"}],\"Rows\":[{\"Data\":[]}]}";
            SerializableDataTable sdt = JsonSerializer.Deserialize<SerializableDataTable>(json);
            DataTable dt = sdt.ToDataTable();
            object value = dt.Rows[0]["Data"];
            TestAssert.True(value is object[], "Empty array without OriginalType should infer object[]");
            TestAssert.Equal(0, ((object[])value).Length);
        }

        #endregion

        #region Pgvector / unknown custom type reconstruction (no live database required)

        public static void Pgvector_ColumnMapsToObjectWithOriginalType()
        {
            DataTable dt = BuildVectorDataTable();
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            SerializableColumn col = sdt.Columns.First(c => c.Name == "Embedding");
            TestAssert.Equal(ColumnValueTypeEnum.Object, col.Type);
            TestAssert.NotNull(col.OriginalType);
            TestAssert.Contains(col.OriginalType, "Pgvector.Vector");
        }

        public static void Pgvector_CellNormalizedToFloatArray()
        {
            DataTable dt = BuildVectorDataTable();
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            object stored = sdt.Rows[0]["Embedding"];
            TestAssert.True(stored is float[], "Vector should normalize to float[] via ToArray().");
            TestAssert.FloatArrayClose(new float[] { 1.0f, 2.0f, 3.0f }, (float[])stored);
        }

        public static void Pgvector_JsonRoundTripReconstructsVectorValues()
        {
            DataTable dt = BuildVectorDataTable();
            DataTable result = FullJsonRoundTrip(dt);
            float[] values = ExtractFloatArray(result.Rows[0]["Embedding"]);
            TestAssert.NotNull(values, "Expected the round-tripped cell to expose float values (Vector or float[]).");
            TestAssert.FloatArrayClose(new float[] { 1.0f, 2.0f, 3.0f }, values);
        }

        public static void Pgvector_NullVectorRoundTripsAsDBNull()
        {
            DataTable dt = new DataTable("embeddings");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Embedding", typeof(Vector));
            dt.Rows.Add(1, DBNull.Value);
            dt.Rows.Add(2, new Vector(new float[] { 4.0f, 5.0f }));
            DataTable result = FullJsonRoundTrip(dt);
            TestAssert.Equal(DBNull.Value, result.Rows[0]["Embedding"]);
            float[] values = ExtractFloatArray(result.Rows[1]["Embedding"]);
            TestAssert.FloatArrayClose(new float[] { 4.0f, 5.0f }, values);
        }

        #endregion

        #region Null and DBNull handling

        public static void Nulls_RoundTripAllNullRow()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("A", typeof(string));
            dt.Columns.Add("B", typeof(int));
            dt.Rows.Add(DBNull.Value, DBNull.Value);
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            TestAssert.Null(sdt.Rows[0]["A"]);
            TestAssert.Null(sdt.Rows[0]["B"]);
            DataTable back = sdt.ToDataTable();
            TestAssert.Equal(DBNull.Value, back.Rows[0]["A"]);
            TestAssert.Equal(DBNull.Value, back.Rows[0]["B"]);
        }

        public static void Nulls_PartialNullRowsPreserved()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Amount", typeof(decimal));
            dt.Rows.Add("Complete", 100.50m);
            dt.Rows.Add("Partial", DBNull.Value);
            dt.Rows.Add(DBNull.Value, 200.00m);
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            DataTable back = sdt.ToDataTable();
            TestAssert.Equal("Complete", (string)back.Rows[0]["Name"]);
            TestAssert.Equal(DBNull.Value, back.Rows[1]["Amount"]);
            TestAssert.Equal(DBNull.Value, back.Rows[2]["Name"]);
            TestAssert.Equal(200.00m, (decimal)back.Rows[2]["Amount"]);
        }

        #endregion

        #region MarkdownConverter

        public static void Markdown_NullDataTableReturnsNull()
        {
            TestAssert.Null(MarkdownConverter.Convert((DataTable)null));
        }

        public static void Markdown_NullSerializableDataTableReturnsNull()
        {
            TestAssert.Null(MarkdownConverter.Convert((SerializableDataTable)null));
        }

        public static void Markdown_EmptyDataTableNoColumnsReturnsNull()
        {
            TestAssert.Null(MarkdownConverter.Convert(new DataTable("Empty")));
        }

        public static void Markdown_EmptySerializableDataTableNoColumnsReturnsNull()
        {
            TestAssert.Null(MarkdownConverter.Convert(new SerializableDataTable("Empty")));
        }

        public static void Markdown_DataTableColumnsButNoRowsReturnsHeaders()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Col1", typeof(string));
            dt.Columns.Add("Col2", typeof(int));
            string md = MarkdownConverter.Convert(dt);
            TestAssert.NotNull(md);
            TestAssert.Contains(md, "| Col1 |");
            TestAssert.Contains(md, "|---|");
        }

        public static void Markdown_SerializableDataTableColumnsButNoRowsReturnsHeaders()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "Col1", Type = ColumnValueTypeEnum.String });
            string md = MarkdownConverter.Convert(sdt);
            TestAssert.NotNull(md);
            TestAssert.Contains(md, "| Col1 |");
        }

        public static void Markdown_DataTableIncludesTableNameHeader()
        {
            DataTable dt = new DataTable("My Report");
            dt.Columns.Add("Id", typeof(int));
            dt.Rows.Add(1);
            string md = MarkdownConverter.Convert(dt);
            TestAssert.Contains(md, "# My Report");
        }

        public static void Markdown_SerializableDataTableIncludesTableNameHeader()
        {
            SerializableDataTable sdt = new SerializableDataTable("My Report");
            sdt.Columns.Add(new SerializableColumn { Name = "Id", Type = ColumnValueTypeEnum.Int32 });
            sdt.Rows.Add(new Dictionary<string, object> { { "Id", 1 } });
            string md = MarkdownConverter.Convert(sdt);
            TestAssert.Contains(md, "# My Report");
        }

        public static void Markdown_DataTableDataRowValuesRendered()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add(1, "John Doe");
            string md = MarkdownConverter.Convert(dt);
            TestAssert.Contains(md, "John Doe");
            TestAssert.Contains(md, "| Id |");
        }

        public static void Markdown_ConvertHeadersDataTable()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            string headers = MarkdownConverter.ConvertHeaders(dt);
            TestAssert.Contains(headers, "ID");
            TestAssert.Contains(headers, "Name");
            TestAssert.Contains(headers, "|---|");
        }

        public static void Markdown_ConvertHeadersSerializableDataTable()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "ID", Type = ColumnValueTypeEnum.Int32 });
            string headers = MarkdownConverter.ConvertHeaders(sdt);
            TestAssert.Contains(headers, "ID");
        }

        public static void Markdown_ConvertHeadersNullDataTableReturnsNull()
        {
            TestAssert.Null(MarkdownConverter.ConvertHeaders((DataTable)null));
        }

        public static void Markdown_ConvertHeadersNullSerializableDataTableReturnsNull()
        {
            TestAssert.Null(MarkdownConverter.ConvertHeaders((SerializableDataTable)null));
        }

        public static void Markdown_ConvertRowDataTable()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Alice");
            dt.Rows.Add("Bob");
            string row = MarkdownConverter.ConvertRow(dt, 1);
            TestAssert.Contains(row, "Bob");
        }

        public static void Markdown_ConvertRowSerializableDataTable()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "Name", Type = ColumnValueTypeEnum.String });
            sdt.Rows.Add(new Dictionary<string, object> { { "Name", "Alice" } });
            string row = MarkdownConverter.ConvertRow(sdt, 0);
            TestAssert.Contains(row, "Alice");
        }

        public static void Markdown_ConvertRowDataTableNullThrows()
        {
            TestAssert.Throws<ArgumentNullException>(() => MarkdownConverter.ConvertRow((DataTable)null, 0));
        }

        public static void Markdown_ConvertRowSerializableDataTableNullThrows()
        {
            TestAssert.Throws<ArgumentNullException>(() => MarkdownConverter.ConvertRow((SerializableDataTable)null, 0));
        }

        public static void Markdown_ConvertRowDataTableIndexTooLargeThrows()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("V", typeof(string));
            dt.Rows.Add("x");
            TestAssert.Throws<ArgumentOutOfRangeException>(() => MarkdownConverter.ConvertRow(dt, 1));
        }

        public static void Markdown_ConvertRowDataTableNegativeIndexThrows()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("V", typeof(string));
            dt.Rows.Add("x");
            TestAssert.Throws<ArgumentOutOfRangeException>(() => MarkdownConverter.ConvertRow(dt, -1));
        }

        public static void Markdown_ConvertRowSerializableDataTableIndexTooLargeThrows()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "V", Type = ColumnValueTypeEnum.String });
            sdt.Rows.Add(new Dictionary<string, object> { { "V", "x" } });
            TestAssert.Throws<ArgumentOutOfRangeException>(() => MarkdownConverter.ConvertRow(sdt, 1));
        }

        public static void Markdown_ByteArrayRenderedAsBase64DataTable()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Blob", typeof(byte[]));
            dt.Rows.Add(new object[] { new byte[] { 1, 2, 3 } });
            string md = MarkdownConverter.Convert(dt);
            TestAssert.Contains(md, Convert.ToBase64String(new byte[] { 1, 2, 3 }));
        }

        public static void Markdown_ByteArrayRenderedAsBase64SerializableDataTable()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "Blob", Type = ColumnValueTypeEnum.ByteArray });
            sdt.Rows.Add(new Dictionary<string, object> { { "Blob", new byte[] { 1, 2, 3 } } });
            string md = MarkdownConverter.Convert(sdt);
            TestAssert.Contains(md, Convert.ToBase64String(new byte[] { 1, 2, 3 }));
        }

        public static void Markdown_ArrayRenderedAsJsonDataTable()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("V", typeof(object));
            dt.Rows.Add(new object[] { new int[] { 1, 2, 3 } });
            string md = MarkdownConverter.Convert(dt);
            TestAssert.Contains(md, "[1,2,3]");
        }

        public static void Markdown_ArrayRenderedAsJsonSerializableDataTable()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "V", Type = ColumnValueTypeEnum.Object });
            sdt.Rows.Add(new Dictionary<string, object> { { "V", new int[] { 1, 2, 3 } } });
            string md = MarkdownConverter.Convert(sdt);
            TestAssert.Contains(md, "[1,2,3]");
        }

        public static void Markdown_StringArrayRenderedWithQuotes()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "V", Type = ColumnValueTypeEnum.Object });
            sdt.Rows.Add(new Dictionary<string, object> { { "V", new string[] { "a", "b" } } });
            string md = MarkdownConverter.Convert(sdt);
            TestAssert.Contains(md, "[\"a\",\"b\"]");
        }

        public static void Markdown_DateTimeRenderedWithMicrosecondPrecision()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "When", Type = ColumnValueTypeEnum.DateTime });
            sdt.Rows.Add(new Dictionary<string, object> { { "When", new DateTime(2024, 6, 15, 14, 30, 45) } });
            string md = MarkdownConverter.Convert(sdt);
            TestAssert.Contains(md, "2024-06-15 14:30:45.000000");
        }

        public static void Markdown_DateTimeOffsetRenderedWithOffset()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "When", Type = ColumnValueTypeEnum.DateTimeOffset });
            sdt.Rows.Add(new Dictionary<string, object> { { "When", new DateTimeOffset(2024, 6, 15, 14, 30, 45, TimeSpan.FromHours(-5)) } });
            string md = MarkdownConverter.Convert(sdt);
            TestAssert.Contains(md, "2024-06-15 14:30:45.000000 -05:00");
        }

        public static void Markdown_PipeCharactersEscapedDataTable()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Text", typeof(string));
            dt.Rows.Add("Value | With | Pipes");
            string md = MarkdownConverter.Convert(dt);
            TestAssert.Contains(md, "\\|");
        }

        public static void Markdown_PipeCharactersEscapedSerializableDataTable()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "Text", Type = ColumnValueTypeEnum.String });
            sdt.Rows.Add(new Dictionary<string, object> { { "Text", "a | b" } });
            string md = MarkdownConverter.Convert(sdt);
            TestAssert.Contains(md, "\\|");
        }

        public static void Markdown_NullValueRendersEmptyCellSerializableDataTable()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "V", Type = ColumnValueTypeEnum.Int32 });
            sdt.Rows.Add(new Dictionary<string, object> { { "V", null } });
            string row = MarkdownConverter.ConvertRow(sdt, 0);
            TestAssert.NotNull(row);
            TestAssert.DoesNotContain(row, "null");
        }

        public static void Markdown_MissingColumnKeyRendersEmptyCellSerializableDataTable()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "A", Type = ColumnValueTypeEnum.Int32 });
            sdt.Columns.Add(new SerializableColumn { Name = "B", Type = ColumnValueTypeEnum.Int32 });
            sdt.Rows.Add(new Dictionary<string, object> { { "A", 1 } });
            string row = MarkdownConverter.ConvertRow(sdt, 0);
            TestAssert.Contains(row, "1");
        }

        public static void Markdown_IterateRowsDataTableCountAndOrder()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("First");
            dt.Rows.Add("Second");
            dt.Rows.Add("Third");
            List<string> rows = MarkdownConverter.IterateRows(dt).ToList();
            TestAssert.Equal(3, rows.Count);
            TestAssert.Contains(rows[0], "First");
            TestAssert.Contains(rows[2], "Third");
        }

        public static void Markdown_IterateRowsSerializableDataTableCount()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "Name", Type = ColumnValueTypeEnum.String });
            sdt.Rows.Add(new Dictionary<string, object> { { "Name", "Alpha" } });
            sdt.Rows.Add(new Dictionary<string, object> { { "Name", "Beta" } });
            List<string> rows = MarkdownConverter.IterateRows(sdt).ToList();
            TestAssert.Equal(2, rows.Count);
            TestAssert.Contains(rows[0], "Alpha");
        }

        public static void Markdown_IterateRowsEmptyDataTableYieldsNothing()
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Name", typeof(string));
            List<string> rows = MarkdownConverter.IterateRows(dt).ToList();
            TestAssert.Equal(0, rows.Count);
        }

        public static void Markdown_NewlineCharacterUnix()
        {
            string previous = MarkdownConverter.NewlineCharacter;
            try
            {
                MarkdownConverter.NewlineCharacter = "\n";
                DataTable dt = new DataTable("T");
                dt.Columns.Add("Id", typeof(int));
                dt.Rows.Add(1);
                string md = MarkdownConverter.Convert(dt);
                TestAssert.Contains(md, "\n");
                TestAssert.DoesNotContain(md, "\r\n");
            }
            finally
            {
                MarkdownConverter.NewlineCharacter = previous;
            }
        }

        public static void Markdown_NewlineCharacterWindowsLongerThanUnix()
        {
            string previous = MarkdownConverter.NewlineCharacter;
            try
            {
                DataTable dt = new DataTable("T");
                dt.Columns.Add("Id", typeof(int));
                dt.Rows.Add(1);

                MarkdownConverter.NewlineCharacter = "\n";
                string unix = MarkdownConverter.Convert(dt);

                MarkdownConverter.NewlineCharacter = "\r\n";
                string windows = MarkdownConverter.Convert(dt);

                TestAssert.Contains(windows, "\r\n");
                TestAssert.True(windows.Length > unix.Length, "Windows newline output should be longer than Unix.");
            }
            finally
            {
                MarkdownConverter.NewlineCharacter = previous;
            }
        }

        public static void Markdown_ToMarkdownMatchesConvert()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            sdt.Columns.Add(new SerializableColumn { Name = "Id", Type = ColumnValueTypeEnum.Int32 });
            sdt.Rows.Add(new Dictionary<string, object> { { "Id", 1 } });
            TestAssert.Equal(MarkdownConverter.Convert(sdt), sdt.ToMarkdown());
        }

        public static void Markdown_ToMarkdownNoColumnsReturnsNull()
        {
            SerializableDataTable sdt = new SerializableDataTable("T");
            TestAssert.Null(sdt.ToMarkdown());
        }

        #endregion

        #region Helpers

        private static void AssertScalarRoundTrip(Type clrType, object value, ColumnValueTypeEnum expectedEnum)
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Col", clrType);
            dt.Rows.Add(value);

            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            TestAssert.Equal(expectedEnum, sdt.Columns[0].Type, "Column type mapping failed for " + clrType.Name);

            DataTable back = sdt.ToDataTable();
            TestAssert.Equal(clrType, back.Columns[0].DataType, "Column CLR type not restored for " + clrType.Name);
            TestAssert.Equal(value, back.Rows[0]["Col"], "Value not preserved for " + clrType.Name);
        }

        private static void AssertArrayJsonRoundTrip<T>(string columnName, T[] original)
        {
            DataTable dt = new DataTable("T");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add(columnName, typeof(object));
            dt.Rows.Add(1, original);

            DataTable result = FullJsonRoundTrip(dt);
            object value = result.Rows[0][columnName];
            TestAssert.True(value is T[], "Expected " + typeof(T).Name + "[] but got " + (value == null ? "null" : value.GetType().Name));
            TestAssert.SequenceEqual(original, (T[])value);
        }

        private static SerializableDataTable JsonRoundTrip(SerializableDataTable sdt)
        {
            string json = JsonSerializer.Serialize(sdt);
            return JsonSerializer.Deserialize<SerializableDataTable>(json);
        }

        private static DataTable FullJsonRoundTrip(DataTable dt)
        {
            SerializableDataTable sdt = SerializableDataTable.FromDataTable(dt);
            string json = JsonSerializer.Serialize(sdt);
            SerializableDataTable back = JsonSerializer.Deserialize<SerializableDataTable>(json);
            return back.ToDataTable();
        }

        private static DataTable BuildVectorDataTable()
        {
            DataTable dt = new DataTable("embeddings");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Embedding", typeof(Vector));
            dt.Rows.Add(1, new Vector(new float[] { 1.0f, 2.0f, 3.0f }));
            return dt;
        }

        private static float[] ExtractFloatArray(object value)
        {
            if (value == null || value == DBNull.Value) return null;
            if (value is float[] floats) return floats;

            if (value is double[] doubles)
            {
                float[] converted = new float[doubles.Length];
                for (int i = 0; i < doubles.Length; i++) converted[i] = (float)doubles[i];
                return converted;
            }

            MethodInfo toArray = value.GetType().GetMethod("ToArray", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (toArray != null && toArray.ReturnType == typeof(float[]))
            {
                return (float[])toArray.Invoke(value, null);
            }

            return null;
        }

        #endregion
    }
}
