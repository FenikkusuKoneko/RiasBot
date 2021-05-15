using System;
using System.Collections.Generic;
using DSharpPlus;

namespace Rias.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class IgnoreChannelTypesAttribute : Attribute
    {
        public readonly HashSet<ChannelType> ChannelTypes;
        
        public IgnoreChannelTypesAttribute(params ChannelType[] channelTypes)
        {
            ChannelTypes = new HashSet<ChannelType>(channelTypes);
        }
    }
}