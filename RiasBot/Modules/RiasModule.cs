using Discord.Commands;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiasBot.Modules
{
    public abstract class RiasModule : ModuleBase
    {
        public readonly string ModuleTypeName;
        public readonly string LowerModuleTypeName;

        protected RiasModule(bool isTopLevelModule = true)
        {
            //if it's top level module
            ModuleTypeName = isTopLevelModule ? this.GetType().Name : this.GetType().DeclaringType.Name;
            LowerModuleTypeName = ModuleTypeName.ToLowerInvariant();
        }
    }

    public abstract class RiasModule<TService> : RiasModule where TService : IKService
    {
        public TService _service { get; set; }

        public RiasModule(bool isTopLevel = true) : base(isTopLevel)
        {
        }
    }

    public abstract class RiasSubmodule : RiasModule
    {
        protected RiasSubmodule() : base(false) { }
    }

    public abstract class RiasSubmodule<TService> : RiasModule<TService> where TService : IKService
    {
        protected RiasSubmodule() : base(false)
        {
        }
    }
}
