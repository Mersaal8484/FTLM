namespace FuelTrack.Application.Common;

public interface ISecretsValidator
{
    SecretsValidationResult Validate();
}

public class SecretsValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
