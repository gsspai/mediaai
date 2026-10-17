import { DeepSeekPromptRequestDto, DeepSeekPromptResponseDto } from "./AppDtos";
import ApiClient, { ApiClientRequestOptions } from "./ApiClient";

const PromptDeepSeek = (request: DeepSeekPromptRequestDto, options?: ApiClientRequestOptions): Promise<DeepSeekPromptResponseDto> =>
  ApiClient.invokeMethod<DeepSeekPromptResponseDto>("Api", "AIManager", "PromptDeepSeek", request, options);

export default {
  PromptDeepSeek
};
