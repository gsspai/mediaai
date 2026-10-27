import {
  ImportMetricsCsvRequestDto,
  ImportMetricsCsvResultDto,
  IngestManualMetricsRequestDto,
  ListRecentMetricsRequestDto,
  MetricEntryListDto,
  MetricIngestResultDto,
} from './AppDtos';
import ApiClient from './ApiClient';

const IngestManual = (request: IngestManualMetricsRequestDto): Promise<MetricIngestResultDto> =>
  ApiClient.invokeMethod<MetricIngestResultDto>('Api', 'MetricManager', 'IngestManual', request);

const ListRecentMetrics = (request: ListRecentMetricsRequestDto): Promise<MetricEntryListDto> =>
  ApiClient.invokeMethod<MetricEntryListDto>('Api', 'MetricManager', 'ListRecentMetrics', request);

const ImportCsv = (request: ImportMetricsCsvRequestDto): Promise<ImportMetricsCsvResultDto> =>
  ApiClient.invokeMethod<ImportMetricsCsvResultDto>('Api', 'MetricManager', 'ImportCsv', request);

export default { IngestManual, ListRecentMetrics, ImportCsv };
