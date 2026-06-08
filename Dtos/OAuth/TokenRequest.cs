using System.ComponentModel.DataAnnotations;

namespace ConcertApi.Dtos.OAuth;

public class TokenRequest : IValidatableObject
{
    [Required(ErrorMessage = "GrantType is required. Use 'password' or 'refresh_token'.")]
    public string GrantType { get; set; } = "";

    // password grant
    public string? Username { get; set; }       // email
    public string? Password { get; set; }

    // refresh_token grant
    public string? RefreshToken { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.Equals(GrantType, "password", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(Username))
                yield return new ValidationResult(
                    "Username is required when GrantType='password'.",
                    new[] { nameof(Username) });

            if (string.IsNullOrWhiteSpace(Password))
                yield return new ValidationResult(
                    "Password is required when GrantType='password'.",
                    new[] { nameof(Password) });
        }
        else if (string.Equals(GrantType, "refresh_token", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(RefreshToken))
                yield return new ValidationResult(
                    "RefreshToken is required when GrantType='refresh_token'.",
                    new[] { nameof(RefreshToken) });
        }
        else
        {
            yield return new ValidationResult(
                "Unsupported GrantType. Use 'password' or 'refresh_token'.",
                new[] { nameof(GrantType) });
        }
    }
}