using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagement.Domain.Common
{
    public class Result
    {
        public bool IsSuccess { get; }
        public string Error { get; private set; } = string.Empty;
        public bool IsFailure => !IsSuccess;

        protected Result(bool success, string error)
        {
            if (success && !string.IsNullOrEmpty(error))
                throw new InvalidOperationException("A successful result cannot have an error message.");

            if (!success && string.IsNullOrEmpty(error))
                throw new InvalidOperationException("A failed result must have an error message.");
            IsSuccess = success;
            Error = error;
        }

        public static Result Success() => new Result(true, string.Empty);
        public static Result<T> Success<T>(T value) => new Result<T>(value, true, string.Empty);
        public static Result Failure(string error) => new Result(false, error);
        public static Result<T> Failure<T>(string error) => new Result<T>(default!, false, error);

    }

    public class Result<T> : Result
    {
        private readonly T? _value;


        public T Value => IsSuccess 
            ? _value! 
            : throw new InvalidOperationException("Cannot access the value of a failed result.");
        protected internal Result(T value, bool success, string error) : base(success, error)
        {
            _value = value;
        }
    }
}
