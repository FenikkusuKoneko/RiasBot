using System;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Extensions;

namespace Rias.Core.Services
{
    /// <summary>
    /// Each service that implement this class will be added in the <see cref="ServiceCollection"/>.
    /// </summary>
    public abstract class RiasService
    {
        protected RiasService(IServiceProvider services)
        {
            services.Inject(this);
        }
    }
}