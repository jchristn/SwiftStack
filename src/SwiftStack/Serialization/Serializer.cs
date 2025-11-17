namespace SwiftStack.Serialization
{
    using System;
    using System.IO;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Serializer.
    /// </summary>
    public class Serializer : ISerializer
    {
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.

        private ExceptionConverter<Exception> _ExceptionConverter = new ExceptionConverter<Exception>();
        private NameValueCollectionConverter _NameValueCollectionConverter = new NameValueCollectionConverter();
        private DateTimeConverter _DateTimeConverter = new DateTimeConverter();
        private IPAddressConverter _IPAddressConverter = new IPAddressConverter();
        private StrictEnumConverterFactory _StrictEnumConverter = new StrictEnumConverterFactory();

        /// <summary>
        /// Serializer.
        /// </summary>
        public Serializer()
        {
            InstantiateConverters();
        }

        /// <summary>
        /// Instantiation method to support fixups for various environments, e.g. Unity.
        /// </summary>
        public void InstantiateConverters()
        {
            try
            {
                Activator.CreateInstance<ExceptionConverter<Exception>>();
                Activator.CreateInstance<NameValueCollectionConverter>();
                Activator.CreateInstance<DateTimeConverter>();
                Activator.CreateInstance<IPAddressConverter>();
                Activator.CreateInstance<StrictEnumConverterFactory>();
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Deserialize JSON to an instance.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="json">JSON string.</param>
        /// <returns>Instance.</returns>
        public T DeserializeJson<T>(string json)
        {
            if (String.IsNullOrEmpty(json)) throw new ArgumentNullException(nameof(json));

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.AllowTrailingCommas = true;
            options.ReadCommentHandling = JsonCommentHandling.Skip;
            options.NumberHandling = JsonNumberHandling.AllowReadingFromString;

            options.Converters.Add(_ExceptionConverter);
            options.Converters.Add(_NameValueCollectionConverter);
            options.Converters.Add(_DateTimeConverter);
            options.Converters.Add(_IPAddressConverter);
            options.Converters.Add(_StrictEnumConverter);

            return JsonSerializer.Deserialize<T>(json, options);
        }

        /// <summary>
        /// Deserialize JSON to an instance.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="bytes">Bytes containing JSON.</param>
        /// <returns>Instance.</returns>
        public T DeserializeJson<T>(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 1) throw new ArgumentNullException(nameof(bytes));
            return DeserializeJson<T>(Encoding.UTF8.GetString(bytes));
        }

        /// <summary>
        /// Serialize object to JSON.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <param name="pretty">Pretty print.</param>
        /// <returns>JSON.</returns>
        public string SerializeJson(object obj, bool pretty = true)
        {
            if (obj == null) return null;

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

            // see https://github.com/dotnet/runtime/issues/43026
            options.Converters.Add(_ExceptionConverter);
            options.Converters.Add(_NameValueCollectionConverter);
            options.Converters.Add(_DateTimeConverter);
            options.Converters.Add(_IPAddressConverter);
            options.Converters.Add(_StrictEnumConverter);

            if (!pretty)
            {
                options.WriteIndented = false;
                return JsonSerializer.Serialize(obj, options);
            }
            else
            {
                options.WriteIndented = true;
                return JsonSerializer.Serialize(obj, options);
            }
        }

        /// <summary>
        /// Copy an object.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="o">Object.</param>
        /// <returns>Instance.</returns>
        public T CopyObject<T>(object o)
        {
            if (o == null) return default(T);
            string json = SerializeJson(o, false);
            T ret = DeserializeJson<T>(json);
            return ret;
        }

#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
    }
}