namespace Contracts.Dto;

public class DeepSeekPromptResponseDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string Output { get; set; } = string.Empty;
}
