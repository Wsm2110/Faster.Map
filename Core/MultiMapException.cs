using System;

namespace Faster.Map.Core
{
    /// <summary>
    /// Generic exception thrown by multimap
    /// </summary>
    /// <seealso cref="System.Exception" />
#pragma warning disable S3925 // "ISerializable" should be implemented correctly
    public class MultiMapException : Exception
#pragma warning restore S3925 // "ISerializable" should be implemented correctly
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiMapException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MultiMapException(string message) : base(message)
        {
            
        }
    }
}
