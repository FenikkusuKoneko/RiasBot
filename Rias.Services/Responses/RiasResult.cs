using Qommon;

namespace Rias.Services.Responses;

public class RiasResult
{
    public bool IsSuccessful { get; private init; }
    public string ErrorReason { get; private init; } = string.Empty;

    public static RiasResult FromSuccess() => new() { IsSuccessful = true };
    public static RiasResult<T> FromSuccess<T>(T value) => new(value);
    
    public static RiasResult FromError(string reason) => new()
    {
        IsSuccessful = false,
        ErrorReason = reason
    };
    
    public static RiasResult<T> FromError<T>(string reason) => new(reason);
}

public class RiasResult<T>
{
    public readonly bool IsSuccessful;
    public readonly string ErrorReason = string.Empty;
    
    private readonly Optional<T> _value;

    public T Value => _value.Value;
    
    public RiasResult(T value)
    {
        IsSuccessful = true;
        _value = value;
    }
    
    public RiasResult(string errorReason)
    {
        ErrorReason = errorReason;
    }
}