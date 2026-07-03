using System.ComponentModel.DataAnnotations;

namespace PharmaDocs.Api.DTOs.Knowledge;

/// <summary>Een vraag aan de kennisassistent.</summary>
public record AskRequest(
    [Required, MaxLength(1000)] string Question);
