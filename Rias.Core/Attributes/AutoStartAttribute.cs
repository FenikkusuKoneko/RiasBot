using System;

namespace Rias.Core.Attributes
{
    /// <summary>
    /// Services with this class will be started at the bot initialization/startup
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoStartAttribute : Attribute
    {
        public int Priority { get; set; }
    }
}