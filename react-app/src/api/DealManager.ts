import {
  DealByIdRequestDto,
  DealDto,
  DealListDto,
  DealListRequestDto,
  SaveDealRequestDto,
  SaveDealResultDto,
  UpdateDealStageRequestDto,
  UpdateDealStageResultDto,
} from './AppDtos';
import ApiClient from './ApiClient';

const ListDeals = (request: DealListRequestDto): Promise<DealListDto> =>
  ApiClient.invokeMethod<DealListDto>('Api', 'DealManager', 'ListDeals', request);

const GetDeal = (request: DealByIdRequestDto): Promise<DealDto | null> =>
  ApiClient.invokeMethod<DealDto | null>('Api', 'DealManager', 'GetDeal', request);

const SaveDeal = (request: SaveDealRequestDto): Promise<SaveDealResultDto> =>
  ApiClient.invokeMethod<SaveDealResultDto>('Api', 'DealManager', 'SaveDeal', request);

const UpdateDealStage = (request: UpdateDealStageRequestDto): Promise<UpdateDealStageResultDto> =>
  ApiClient.invokeMethod<UpdateDealStageResultDto>('Api', 'DealManager', 'UpdateDealStage', request);

export default { ListDeals, GetDeal, SaveDeal, UpdateDealStage };
