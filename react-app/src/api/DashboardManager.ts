import { DashboardSummaryDto, GetDashboardSummaryRequestDto } from './AppDtos';
import ApiClient from './ApiClient';

const GetSummary = (request: GetDashboardSummaryRequestDto): Promise<DashboardSummaryDto> =>
  ApiClient.invokeMethod<DashboardSummaryDto>('Api', 'DashboardManager', 'GetSummary', request);

export default { GetSummary };
