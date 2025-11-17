namespace SwiftStack.Serialization
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// ArraySegment&lt;byte&gt; converter.
    /// </summary>
    public class ArraySegmentByteConverter : JsonConverter<ArraySegment<byte>>
    {
        /// <summary>
        /// Read.
        /// </summary>
        /// <param name="reader">Reader.</param>
        /// <param name="typeToConvert">Type to convert.</param>
        /// <param name="options">JSON serializer options.</param>
        /// <returns>ArraySegment&lt;byte&gt;.</returns>
        public override ArraySegment<byte> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new ArraySegment<byte>(Array.Empty<byte>());
            }

            // Deserialize as byte array (System.Text.Json handles base64 automatically)
            byte[] bytes = JsonSerializer.Deserialize<byte[]>(ref reader, options);
            if (bytes == null || bytes.Length == 0)
            {
                return new ArraySegment<byte>(Array.Empty<byte>());
            }

            return new ArraySegment<byte>(bytes);
        }

        /// <summary>
        /// Write.
        /// </summary>
        /// <param name="writer">Writer.</param>
        /// <param name="value">Value.</param>
        /// <param name="options">JSON serializer options.</param>
        public override void Write(
            Utf8JsonWriter writer,
            ArraySegment<byte> value,
            JsonSerializerOptions options)
        {
            if (value.Array == null || value.Count == 0)
            {
                writer.WriteNullValue();
                return;
            }

            // Extract the actual bytes from the ArraySegment
            byte[] bytes = new byte[value.Count];
            Array.Copy(value.Array, value.Offset, bytes, 0, value.Count);

            // Serialize as byte array (System.Text.Json handles base64 automatically)
            JsonSerializer.Serialize(writer, bytes, options);
        }
    }
}
