using System;
using DSharpPlus;

namespace Rias.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter)]
    public class ChannelAttribute : Attribute
    {
        public readonly ChannelType ChannelType;

        public ChannelAttribute(ChannelType channelType)
        {
            ChannelType = channelType;
        }
    }
}