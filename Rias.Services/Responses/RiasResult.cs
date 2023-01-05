using JetBrains.Annotations;
using Qmmands;
using Qommon;

namespace Rias.Services.Responses;

public class RiasResult
{
    public bool IsSuccessful { get; private init; }
    public string FailureReason { get; private init; } = string.Empty;
    
    public static RiasResult FromSuccess()
        => new() { IsSuccessful = true };

    [StringFormatMethod("message")]
    public static RiasResult FromFailure(string reason) => new()
    {
        IsSuccessful = false,
        FailureReason = reason
    };
    
    [StringFormatMethod("message")]
    public static RiasResult FromFailure(string reason, object arg0) => new()
    {
        IsSuccessful = false,
        FailureReason = string.Format(reason, arg0)
    };
    
    [StringFormatMethod("message")]
    public static RiasResult FromFailure(string reason, object arg0, object arg1) => new()
    {
        IsSuccessful = false,
        FailureReason = string.Format(reason, arg0, arg1)
    };
    
    [StringFormatMethod("message")]
    public static RiasResult FromFailure(string reason, object arg0, object arg1, object arg2) => new()
    {
        IsSuccessful = false,
        FailureReason = string.Format(reason, arg0, arg1, arg2)
    };
    
    [StringFormatMethod("message")]
    public static RiasResult FromFailure(string reason, params object[] args) => new()
    {
        IsSuccessful = false,
        FailureReason = string.Format(reason, args)
    };
}

public class RiasResult<T> : IResult
{
    public bool IsSuccessful { get; private init; }
    public string? FailureReason { get; private init; }
    
    private Optional<T> _value;

    public T Value => _value.Value;

    public static RiasResult<T> FromSuccess(T value)
        => new() { IsSuccessful = true, _value = value };

    [StringFormatMethod("message")]
    public static RiasResult<T> FromFailure(string reason) => new()
    {
        IsSuccessful = false,
        FailureReason = reason
    };
    
    [StringFormatMethod("message")]
    public static RiasResult<T> FromFailure(string reason, object arg0) => new()
    {
        IsSuccessful = false,
        FailureReason = string.Format(reason, arg0)
    };
    
    [StringFormatMethod("message")]
    public static RiasResult<T> FromFailure(string reason, object arg0, object arg1) => new()
    {
        IsSuccessful = false,
        FailureReason = string.Format(reason, arg0, arg1)
    };
    
    [StringFormatMethod("message")]
    public static RiasResult<T> FromFailure(string reason, object arg0, object arg1, object arg2) => new()
    {
        IsSuccessful = false,
        FailureReason = string.Format(reason, arg0, arg1, arg2)
    };
    
    [StringFormatMethod("message")]
    public static RiasResult<T> FromFailure(string reason, params object[] args) => new()
    {
        IsSuccessful = false,
        FailureReason = string.Format(reason, args)
    };
    
    public static implicit operator RiasResult<T>(T value) => FromSuccess(value);
}