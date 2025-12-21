namespace SwiftStack.Rest.OpenApi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text.Json.Serialization;

    /// <summary>
    /// The Schema Object allows the definition of input and output data types.
    /// </summary>
    public class OpenApiSchemaMetadata
    {
        #region Public-Members

        /// <summary>
        /// The data type. Common values: "string", "number", "integer", "boolean", "array", "object".
        /// </summary>
        [JsonPropertyName("type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Type { get; set; } = null;

        /// <summary>
        /// The format of the data type. Examples: "int32", "int64", "float", "double", "date", "date-time", "email", "uri", "uuid".
        /// </summary>
        [JsonPropertyName("format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Format { get; set; } = null;

        /// <summary>
        /// A title for the schema.
        /// </summary>
        [JsonPropertyName("title")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Title { get; set; } = null;

        /// <summary>
        /// A description of the schema.
        /// </summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; } = null;

        /// <summary>
        /// Whether the value can be null.
        /// </summary>
        [JsonPropertyName("nullable")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Nullable { get; set; } = false;

        /// <summary>
        /// An example value for the schema.
        /// </summary>
        [JsonPropertyName("example")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object Example { get; set; } = null;

        /// <summary>
        /// The default value for the schema.
        /// </summary>
        [JsonPropertyName("default")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object Default { get; set; } = null;

        /// <summary>
        /// A list of allowed values for the schema (for enums).
        /// </summary>
        [JsonPropertyName("enum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<object> Enum { get; set; } = null;

        /// <summary>
        /// The schema for items in an array.
        /// </summary>
        [JsonPropertyName("items")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OpenApiSchemaMetadata Items { get; set; } = null;

        /// <summary>
        /// The properties of an object schema.
        /// </summary>
        [JsonPropertyName("properties")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, OpenApiSchemaMetadata> Properties { get; set; } = null;

        /// <summary>
        /// A list of required property names for an object schema.
        /// </summary>
        [JsonPropertyName("required")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string> Required { get; set; } = null;

        /// <summary>
        /// The schema for additional properties in an object.
        /// </summary>
        [JsonPropertyName("additionalProperties")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object AdditionalProperties { get; set; } = null;

        /// <summary>
        /// A reference to another schema.
        /// </summary>
        [JsonPropertyName("$ref")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Ref { get; set; } = null;

        /// <summary>
        /// Minimum value for numeric types.
        /// </summary>
        [JsonPropertyName("minimum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Minimum { get; set; } = null;

        /// <summary>
        /// Maximum value for numeric types.
        /// </summary>
        [JsonPropertyName("maximum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Maximum { get; set; } = null;

        /// <summary>
        /// Minimum length for string types.
        /// </summary>
        [JsonPropertyName("minLength")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MinLength { get; set; } = null;

        /// <summary>
        /// Maximum length for string types.
        /// </summary>
        [JsonPropertyName("maxLength")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxLength { get; set; } = null;

        /// <summary>
        /// Pattern (regular expression) for string types.
        /// </summary>
        [JsonPropertyName("pattern")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Pattern { get; set; } = null;

        /// <summary>
        /// Minimum number of items for array types.
        /// </summary>
        [JsonPropertyName("minItems")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MinItems { get; set; } = null;

        /// <summary>
        /// Maximum number of items for array types.
        /// </summary>
        [JsonPropertyName("maxItems")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxItems { get; set; } = null;

        /// <summary>
        /// Whether items in an array must be unique.
        /// </summary>
        [JsonPropertyName("uniqueItems")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool UniqueItems { get; set; } = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates an empty schema.
        /// </summary>
        public OpenApiSchemaMetadata()
        {
        }

        /// <summary>
        /// Creates a string schema.
        /// </summary>
        /// <param name="format">The format of the string (e.g., "email", "uri", "uuid", "date", "date-time").</param>
        /// <returns>A string schema.</returns>
        public static OpenApiSchemaMetadata String(string format = null)
        {
            return new OpenApiSchemaMetadata { Type = "string", Format = format };
        }

        /// <summary>
        /// Creates an integer schema.
        /// </summary>
        /// <param name="format">The format ("int32" or "int64"). Default is "int32".</param>
        /// <returns>An integer schema.</returns>
        public static OpenApiSchemaMetadata Integer(string format = "int32")
        {
            return new OpenApiSchemaMetadata { Type = "integer", Format = format };
        }

        /// <summary>
        /// Creates a long integer schema.
        /// </summary>
        /// <returns>A long integer schema.</returns>
        public static OpenApiSchemaMetadata Long()
        {
            return new OpenApiSchemaMetadata { Type = "integer", Format = "int64" };
        }

        /// <summary>
        /// Creates a number schema.
        /// </summary>
        /// <param name="format">The format ("float" or "double"). Default is "double".</param>
        /// <returns>A number schema.</returns>
        public static OpenApiSchemaMetadata Number(string format = "double")
        {
            return new OpenApiSchemaMetadata { Type = "number", Format = format };
        }

        /// <summary>
        /// Creates a boolean schema.
        /// </summary>
        /// <returns>A boolean schema.</returns>
        public static OpenApiSchemaMetadata Boolean()
        {
            return new OpenApiSchemaMetadata { Type = "boolean" };
        }

        /// <summary>
        /// Creates an array schema.
        /// </summary>
        /// <param name="items">The schema for items in the array.</param>
        /// <returns>An array schema.</returns>
        public static OpenApiSchemaMetadata Array(OpenApiSchemaMetadata items)
        {
            return new OpenApiSchemaMetadata { Type = "array", Items = items };
        }

        /// <summary>
        /// Creates an object schema.
        /// </summary>
        /// <param name="properties">The properties of the object.</param>
        /// <param name="required">A list of required property names.</param>
        /// <returns>An object schema.</returns>
        public static OpenApiSchemaMetadata Object(Dictionary<string, OpenApiSchemaMetadata> properties = null, List<string> required = null)
        {
            return new OpenApiSchemaMetadata { Type = "object", Properties = properties, Required = required };
        }

        /// <summary>
        /// Creates a reference to another schema.
        /// </summary>
        /// <param name="schemaName">The name of the schema to reference.</param>
        /// <returns>A schema reference.</returns>
        public static OpenApiSchemaMetadata CreateRef(string schemaName)
        {
            return new OpenApiSchemaMetadata { Ref = $"#/components/schemas/{schemaName}" };
        }

        /// <summary>
        /// Creates a schema from a .NET type using reflection.
        /// </summary>
        /// <typeparam name="T">The type to create a schema for.</typeparam>
        /// <returns>A schema representing the type.</returns>
        public static OpenApiSchemaMetadata FromType<T>()
        {
            return FromType(typeof(T));
        }

        /// <summary>
        /// Creates a schema from a .NET type using reflection.
        /// </summary>
        /// <param name="type">The type to create a schema for.</param>
        /// <returns>A schema representing the type.</returns>
        public static OpenApiSchemaMetadata FromType(Type type)
        {
            return FromType(type, new HashSet<Type>());
        }

        #endregion

        #region Private-Methods

        private static OpenApiSchemaMetadata FromType(Type type, HashSet<Type> visitedTypes)
        {
            if (type == null)
                return new OpenApiSchemaMetadata { Type = "object" };

            // Handle nullable types
            Type underlyingType = System.Nullable.GetUnderlyingType(type);
            bool typeIsNullable = underlyingType != null;
            if (typeIsNullable)
                type = underlyingType;

            // Handle primitive types
            if (type == typeof(string))
                return CreateSchema("string", null, typeIsNullable);

            if (type == typeof(int) || type == typeof(short) || type == typeof(byte) || type == typeof(sbyte))
                return CreateSchema("integer", "int32", typeIsNullable);

            if (type == typeof(long))
                return CreateSchema("integer", "int64", typeIsNullable);

            if (type == typeof(float))
                return CreateSchema("number", "float", typeIsNullable);

            if (type == typeof(double))
                return CreateSchema("number", "double", typeIsNullable);

            if (type == typeof(decimal))
                return CreateSchema("number", "decimal", typeIsNullable);

            if (type == typeof(bool))
                return CreateSchema("boolean", null, typeIsNullable);

            if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
                return CreateSchema("string", "date-time", typeIsNullable);

            if (type == typeof(TimeSpan))
                return CreateSchema("string", "time", typeIsNullable);

            if (type == typeof(Guid))
                return CreateSchema("string", "uuid", typeIsNullable);

            if (type == typeof(Uri))
                return CreateSchema("string", "uri", typeIsNullable);

            if (type == typeof(byte[]))
                return CreateSchema("string", "byte", typeIsNullable);

            if (type == typeof(object))
                return CreateSchema("object", null, typeIsNullable);

            // Handle enums
            if (type.IsEnum)
            {
                List<object> enumValues = System.Enum.GetNames(type).Cast<object>().ToList();
                OpenApiSchemaMetadata enumSchema = CreateSchema("string", null, typeIsNullable);
                enumSchema.Enum = enumValues;
                return enumSchema;
            }

            // Handle arrays and collections
            if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                OpenApiSchemaMetadata arraySchema = CreateSchema("array", null, typeIsNullable);
                arraySchema.Items = FromType(elementType, visitedTypes);
                return arraySchema;
            }

            if (type.IsGenericType)
            {
                Type genericDef = type.GetGenericTypeDefinition();
                Type[] genericArgs = type.GetGenericArguments();

                // Handle List<T>, IList<T>, IEnumerable<T>, ICollection<T>
                if (genericDef == typeof(List<>) ||
                    genericDef == typeof(IList<>) ||
                    genericDef == typeof(IEnumerable<>) ||
                    genericDef == typeof(ICollection<>) ||
                    genericDef == typeof(IReadOnlyList<>) ||
                    genericDef == typeof(IReadOnlyCollection<>))
                {
                    OpenApiSchemaMetadata listSchema = CreateSchema("array", null, typeIsNullable);
                    listSchema.Items = FromType(genericArgs[0], visitedTypes);
                    return listSchema;
                }

                // Handle Dictionary<string, T>
                if (genericDef == typeof(Dictionary<,>) ||
                    genericDef == typeof(IDictionary<,>) ||
                    genericDef == typeof(IReadOnlyDictionary<,>))
                {
                    if (genericArgs[0] == typeof(string))
                    {
                        OpenApiSchemaMetadata dictSchema = CreateSchema("object", null, typeIsNullable);
                        dictSchema.AdditionalProperties = FromType(genericArgs[1], visitedTypes);
                        return dictSchema;
                    }
                }
            }

            // Handle complex objects - detect cycles
            if (visitedTypes.Contains(type))
            {
                return new OpenApiSchemaMetadata { Type = "object", Description = $"Reference to {type.Name}" };
            }

            visitedTypes.Add(type);

            // Build object schema from properties
            Dictionary<string, OpenApiSchemaMetadata> properties = new Dictionary<string, OpenApiSchemaMetadata>();
            List<string> required = new List<string>();

            PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in props)
            {
                if (!prop.CanRead) continue;

                // Check for JsonPropertyName attribute
                JsonPropertyNameAttribute jsonProp = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
                string propName = jsonProp?.Name ?? prop.Name;

                // Check for JsonIgnore attribute
                JsonIgnoreAttribute jsonIgnore = prop.GetCustomAttribute<JsonIgnoreAttribute>();
                if (jsonIgnore != null && jsonIgnore.Condition == JsonIgnoreCondition.Always)
                    continue;

                OpenApiSchemaMetadata propSchema = FromType(prop.PropertyType, new HashSet<Type>(visitedTypes));
                properties[propName] = propSchema;

                // Check if property is required (non-nullable value type or has required attribute)
                bool propIsNullable = System.Nullable.GetUnderlyingType(prop.PropertyType) != null ||
                                      !prop.PropertyType.IsValueType;
                if (!propIsNullable)
                    required.Add(propName);
            }

            visitedTypes.Remove(type);

            OpenApiSchemaMetadata objectSchema = CreateSchema("object", null, typeIsNullable);
            objectSchema.Properties = properties.Count > 0 ? properties : null;
            objectSchema.Required = required.Count > 0 ? required : null;
            return objectSchema;
        }

        private static OpenApiSchemaMetadata CreateSchema(string type, string format, bool nullable)
        {
            OpenApiSchemaMetadata schema = new OpenApiSchemaMetadata
            {
                Type = type,
                Format = format
            };
            if (nullable)
            {
                schema.Nullable = true;
            }
            return schema;
        }

        #endregion
    }
}
