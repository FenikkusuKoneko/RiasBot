using System;

namespace Rias.Commons
{
    public class CommandNoPermissionsException : Exception
    {
        public CommandNoPermissionsException(string? message)
            : base(message)
        {
        }
    }
}