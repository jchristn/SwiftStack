namespace SwiftStack.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Serializer interface.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Deserialize JSON to an object instance.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="json">JSON.</param>
        /// <returns>Object instance.</returns>
        T DeserializeJson<T>(string json);

        /// <summary>
        /// Deserialize bytes containing JSON to an object instance.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="bytes">Bytes containing JSON.</param>
        /// <returns>Object instance.</returns>
        T DeserializeJson<T>(byte[] bytes);

        /// <summary>
        /// Serialize object instance to JSON.
        /// </summary>
        /// <param name="obj">Object instance.</param>
        /// <param name="pretty">True to enable pretty-print.</param>
        /// <returns>JSON string.</returns>
        string SerializeJson(object obj, bool pretty = false);
    }
}
