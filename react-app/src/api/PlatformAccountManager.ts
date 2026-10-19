import {
  AccountIdRequestDto,
  ListAccountsRequestDto,
  OperationResultDto,
  PlatformAccountListDto,
  SaveAccountRequestDto,
  SaveAccountResultDto,
} from './AppDtos';
import ApiClient from './ApiClient';

const ListAccounts = (request: ListAccountsRequestDto): Promise<PlatformAccountListDto> =>
  ApiClient.invokeMethod<PlatformAccountListDto>('Api', 'PlatformAccountManager', 'ListAccounts', request);

const SaveAccount = (request: SaveAccountRequestDto): Promise<SaveAccountResultDto> =>
  ApiClient.invokeMethod<SaveAccountResultDto>('Api', 'PlatformAccountManager', 'SaveAccount', request);

const DisableAccount = (request: AccountIdRequestDto): Promise<OperationResultDto> =>
  ApiClient.invokeMethod<OperationResultDto>('Api', 'PlatformAccountManager', 'DisableAccount', request);

export default { ListAccounts, SaveAccount, DisableAccount };
