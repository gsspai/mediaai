import {
  GetSignalThresholdsRequestDto,
  SaveSignalThresholdRequestDto,
  SaveSignalThresholdResultDto,
  SignalThresholdDto,
} from './AppDtos';
import ApiClient from './ApiClient';

const GetSignalThresholds = (request: GetSignalThresholdsRequestDto): Promise<SignalThresholdDto> =>
  ApiClient.invokeMethod<SignalThresholdDto>('Api', 'SettingsManager', 'GetSignalThresholds', request);

const SaveSignalThresholds = (request: SaveSignalThresholdRequestDto): Promise<SaveSignalThresholdResultDto> =>
  ApiClient.invokeMethod<SaveSignalThresholdResultDto>('Api', 'SettingsManager', 'SaveSignalThresholds', request);

export default { GetSignalThresholds, SaveSignalThresholds };
