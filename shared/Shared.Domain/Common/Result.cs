
using System.Net.Http.Headers;

namespace Shared.Domain.Common;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if(isSuccess && error != Error.None)
            throw new InvalidOperationException("Success result cannot have an error");
            if(!isSuccess && error == Error.None)
            throw new InvalidOperationException("Failure result must have an error");

        IsSuccess = isSuccess;
        Error = error;

    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success()=>new (true, Error.None);

    public static Result Failure(Error error) => new (false, error);

     public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value  => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access value of a failure result");
}

public record Error(string Code,string Description)
{
    public static readonly Error None = new(string.Empty,string.Empty);
    public static readonly Error NullValue = new("Error.NullValue","A null value was provided");

    public static Error NotFound(string entity,object id)
    =>new ($"{entity}.NotFound",$"{entity} with id {id} was not found");

    public static Error Unauthorized()
    =>new ($"Error.Unauthorized","You are not authorized to perform this action");

    public static Error Validation(string field,string message)
    =>new($"Validation.{field}",message);

    public static Error Conflict(string message)
    =>new("Error.Conflict",message);
}