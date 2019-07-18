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

        protected void RunTask(Func<Task> func)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await func();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });
        }
    }
}