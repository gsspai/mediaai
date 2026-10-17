namespace App.Api.Accessors;

public interface IDeepSeekAccessor
{
    Task<string> GenerateText(string prompt);
}
