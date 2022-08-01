using Rias.Database;

namespace Rias.Services;

public abstract class RiasCommandService
{
    protected readonly RiasDbContext Db;

    protected RiasCommandService(RiasDbContext db)
    {
        Db = db;
    }
}