using App.Api.Accessors;
using App.ServiceInvoker.Attributes;
using App.ServiceInvoker.Interfaces;
using Contracts.Dto;

namespace App.Api.Managers;

public class AIManager : IManagerService
{
    private readonly IDeepSeekAccessor _deepSeekAccessor;

    public AIManager(IDeepSeekAccessor deepSeekAccessor)
    {
        _deepSeekAccessor = deepSeekAccessor;
    }

    [RequireAuthenticated]
    public async Task<DeepSeekPromptResponseDto> PromptDeepSeek(DeepSeekPromptRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return new DeepSeekPromptResponseDto
            {
                Success = false,
                Message = "Prompt is required."
            };
        }

        try
        {
            var output = await _deepSeekAccessor.GenerateText(request.Prompt.Trim());
            return new DeepSeekPromptResponseDto
            {
                Success = true,
                Message = "DeepSeek response generated successfully.",
                Output = output
            };
        }
        catch (Exception ex)
        {
            return new DeepSeekPromptResponseDto
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
}
