namespace PharmaDocs.Api.Common.Exceptions;

/// <summary>
/// Basis voor verwachte, functionele fouten die op een specifieke HTTP-status mappen.
/// De <see cref="ExceptionHandlingMiddleware"/> vertaalt ze naar nette ProblemDetails.
/// </summary>
public abstract class AppException : Exception
{
    public abstract int StatusCode { get; }

    protected AppException(string message) : base(message) { }
}

/// <summary>Een conflict, bv. een e-mailadres dat al bestaat → 409.</summary>
public class ConflictException : AppException
{
    public override int StatusCode => StatusCodes.Status409Conflict;
    public ConflictException(string message) : base(message) { }
}

/// <summary>Authenticatie mislukt (ongeldige inloggegevens) → 401.</summary>
public class UnauthorizedException : AppException
{
    public override int StatusCode => StatusCodes.Status401Unauthorized;
    public UnauthorizedException(string message) : base(message) { }
}
