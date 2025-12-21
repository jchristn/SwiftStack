namespace SwiftStack.Rest.OpenApi
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Contact information for the exposed API.
    /// </summary>
    public class OpenApiContact
    {
        #region Public-Members

        /// <summary>
        /// The identifying name of the contact person/organization.
        /// </summary>
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Name { get; set; } = null;

        /// <summary>
        /// The URL pointing to the contact information.
        /// </summary>
        [JsonPropertyName("url")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Url { get; set; } = null;

        /// <summary>
        /// The email address of the contact person/organization.
        /// </summary>
        [JsonPropertyName("email")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Email { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiates an empty contact.
        /// </summary>
        public OpenApiContact()
        {
        }

        /// <summary>
        /// Instantiates a contact with the specified values.
        /// </summary>
        /// <param name="name">The identifying name of the contact person/organization.</param>
        /// <param name="email">The email address of the contact person/organization.</param>
        /// <param name="url">The URL pointing to the contact information.</param>
        public OpenApiContact(string name, string email = null, string url = null)
        {
            Name = name;
            Email = email;
            Url = url;
        }

        #endregion
    }
}
