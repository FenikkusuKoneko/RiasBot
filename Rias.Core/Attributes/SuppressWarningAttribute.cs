using System;

namespace Rias.Core.Attributes
{
    /// <summary>
    /// Marks the <see cref="T:Qmmands.Parameter" /> to ignore the TypeParser and use the default value."/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class SuppressWarningAttribute : Attribute
    {
    }
}