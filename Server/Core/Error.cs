namespace Core;

public enum ErrorType {
    NotFound,
    InvalidInput,
    InternalError,
}

public record Error(ErrorType Type, string Message) {
    public string Id => Type.ToString();
    public override string ToString() => $"{Id}: {Message}";

    public static Error NotFound(string message) => new Error(ErrorType.NotFound, message);
    public static Error InvalidInput(string message) => new Error(ErrorType.InvalidInput, message);
    public static Error InternalError(string message) => new Error(ErrorType.InternalError, message);

    public static implicit operator string(Error error) => error.ToString();
}