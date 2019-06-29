using System;

namespace Rias.Core.Attributes
{
    /// <summary>
    /// Fields marked with this attribute will have their value automatically assigned from the Dependency Injection
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class InjectAttribute : Attribute
    {
        
    }
}