using System;

namespace Exceptions
{
    public class InvalidTypeException : Exception
    {
        public InvalidTypeException(object value, Type expectedType) : base($"Value of type {value.GetType()} is invalid. Expected type {expectedType}")
        {

        }
    }
}
