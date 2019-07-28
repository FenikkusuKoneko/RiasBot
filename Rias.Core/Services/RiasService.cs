using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Extensions;
using Serilog;

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

        /// <summary>
        /// Run a task in an async way.
        /// </summary>
        /// <param name="func"></param>
        protected void RunAsyncTask(Task func)
        {
            Task.Run(async () =>
            {
                try
                {
                    await func;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });
        }
    }
}