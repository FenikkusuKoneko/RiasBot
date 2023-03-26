using Disqord;
using Rias.Database;
using Rias.Services.Responses;

namespace Rias.Services;

public abstract class RiasCommandService
{
    protected readonly RiasDbContext Db;
    protected readonly LocalisationService Localisation;

    protected RiasCommandService(RiasDbContext db, LocalisationService localisation)
    {
        Db = db;
        Localisation = localisation;
    }

    public RiasResult SuccessResult()
        => RiasResult.FromSuccess();

    public RiasResult<T> SuccessResult<T>(T value)
        => RiasResult.FromSuccess(value);

    public RiasResult ErrorResult(Snowflake? guildId, string key)
        => RiasResult.FromError(Localisation.GetText(guildId, key));

    public RiasResult ErrorResult(Snowflake? guildId, string key, object arg0)
        => RiasResult.FromError(Localisation.GetText(guildId, key, arg0));

    public RiasResult ErrorResult(Snowflake? guildId, string key, object arg0, object arg1)
        => RiasResult.FromError(Localisation.GetText(guildId, key, arg0, arg1));

    public RiasResult ErrorResult(Snowflake? guildId, string key, object arg0, object arg1, object arg2)
        => RiasResult.FromError(Localisation.GetText(guildId, key, arg0, arg1, arg2));

    public RiasResult ErrorResult(Snowflake? guildId, string key, params object[] args)
        => RiasResult.FromError(Localisation.GetText(guildId, key, args));

    public RiasResult<T> ErrorResult<T>(Snowflake? guildId, string key)
        => RiasResult.FromError<T>(Localisation.GetText(guildId, key));

    public RiasResult<T> ErrorResult<T>(Snowflake? guildId, string key, object arg0)
        => RiasResult.FromError<T>(Localisation.GetText(guildId, key, arg0));

    public RiasResult<T> ErrorResult<T>(Snowflake? guildId, string key, object arg0, object arg1)
        => RiasResult.FromError<T>(Localisation.GetText(guildId, key, arg0, arg1));

    public RiasResult<T> ErrorResult<T>(Snowflake? guildId, string key, object arg0, object arg1, object arg2)
        => RiasResult.FromError<T>(Localisation.GetText(guildId, key, arg0, arg1, arg2));

    public RiasResult<T> ErrorResult<T>(Snowflake? guildId, string key, params object[] args)
        => RiasResult.FromError<T>(Localisation.GetText(guildId, key, args));
}