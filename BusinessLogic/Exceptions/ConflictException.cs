using System;

namespace BusinessLogic.Exceptions
{
    /// <summary>
    /// Exception thrown when a conflict occurs (e.g., duplicate registration)
    /// </summary>
    public class ConflictException : DomainException
    {
        public ConflictException(string message) : base(message)
        {
        }

        public ConflictException(string message, System.Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
