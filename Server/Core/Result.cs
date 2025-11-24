namespace Core;

public class Result {
    public bool IsSuccess { get; }
    public Error? Error { 
        get {
            if (IsSuccess) {
                throw new InvalidOperationException("Cannot access Error when the result is a success.");
            }
            return field;
        }
        set;
    }
    private Result(bool isSuccess, Error? error) {
        IsSuccess = isSuccess;
        Error = error;
    }
    public static Result Success() {
        return new Result(true, default(Error));
    }
    public static Result Failure(Error error) {
        return new Result(false, error);
    }
}

public class Result<T> {
    public bool IsSuccess { get; }
    public T? Value { 
        get {
            if (!IsSuccess) {
                throw new InvalidOperationException("Cannot access Value when the result is a failure.");
            }
            return field;
        }
        set;
    }
    public Error? Error { 
        get {
            if (IsSuccess) {
                throw new InvalidOperationException("Cannot access Error when the result is a success.");
            }
            return field;
        }
        set;
    }

    private Result(bool isSuccess, T? value, Error? error) {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) {
        return new Result<T>(true, value, default(Error));
    }

    public static Result<T> Failure(Error error) {
        return new Result<T>(false, default(T), error);
    }
}