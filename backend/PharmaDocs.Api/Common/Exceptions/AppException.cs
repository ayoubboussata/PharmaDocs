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

/// <summary>Ongeldige invraag van de client (bv. ontbrekend bestand) → 400.</summary>
public class BadRequestException : AppException
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
    public BadRequestException(string message) : base(message) { }
}

/// <summary>Niet-ondersteund bestandstype (enkel PDF) → 415.</summary>
public class UnsupportedMediaTypeException : AppException
{
    public override int StatusCode => StatusCodes.Status415UnsupportedMediaType;
    public UnsupportedMediaTypeException(string message) : base(message) { }
}

/// <summary>Payload te groot (bv. PDF > 10 MB) → 413.</summary>
public class PayloadTooLargeException : AppException
{
    public override int StatusCode => StatusCodes.Status413PayloadTooLarge;
    public PayloadTooLargeException(string message) : base(message) { }
}

/// <summary>Een afhankelijke dienst (bv. de AI-service) is niet beschikbaar → 503.</summary>
public class ServiceUnavailableException : AppException
{
    public override int StatusCode => StatusCodes.Status503ServiceUnavailable;
    public ServiceUnavailableException(string message) : base(message) { }
}
