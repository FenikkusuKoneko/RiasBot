using Rias.Database;

namespace Rias.Services.Commands;

public class UtilityService : RiasCommandService
{
    public UtilityService(RiasDbContext db) 
        : base(db)
    {
    }
}