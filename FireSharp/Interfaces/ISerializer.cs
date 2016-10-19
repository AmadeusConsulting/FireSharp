namespace FireSharp.Interfaces
{
    public interface ISerializer
    {
        /// <summary>
        /// Deserializes the specified json string.
        /// </summary>
        /// <typeparam name="T">The value Type</typeparam>
        /// <param name="json">The serialized JSON value.</param>
        /// <returns>The deserialized value of type <see cref="T"/></returns>
        T Deserialize<T>(string json);
        
        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <typeparam name="T">The value Type</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="formatted">if set to <c>true</c> serialized value will be formatted (i.e. indented).</param>
        /// <returns>The JSON serialized value</returns>
        string Serialize<T>(T value, bool formatted = false);
    }
}