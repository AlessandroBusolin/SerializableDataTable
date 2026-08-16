namespace Test.Shared
{
    /// <summary>
    /// Simple public POCO used to exercise the JSON-object reconstruction path in
    /// <c>SerializableDataTable.ToDataTable</c> (an <c>Object</c> column whose value is a
    /// custom reference type carried across a JSON round-trip via its <c>OriginalType</c>).
    /// </summary>
    public sealed class SamplePoint
    {
        /// <summary>
        /// X coordinate.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Y coordinate.
        /// </summary>
        public int Y { get; set; }
    }
}
