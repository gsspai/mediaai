import {
  ContentPlanDto,
  ContentPlanListDto,
  PlanByIdRequestDto,
  PlanListRequestDto,
  SavePlanRequestDto,
  SavePlanResultDto,
  UpdatePlanStatusRequestDto,
  UpdatePlanStatusResultDto,
} from './AppDtos';
import ApiClient from './ApiClient';

const ListPlans = (request: PlanListRequestDto): Promise<ContentPlanListDto> =>
  ApiClient.invokeMethod<ContentPlanListDto>('Api', 'ContentPlanManager', 'ListPlans', request);

const GetPlan = (request: PlanByIdRequestDto): Promise<ContentPlanDto | null> =>
  ApiClient.invokeMethod<ContentPlanDto | null>('Api', 'ContentPlanManager', 'GetPlan', request);

const SavePlan = (request: SavePlanRequestDto): Promise<SavePlanResultDto> =>
  ApiClient.invokeMethod<SavePlanResultDto>('Api', 'ContentPlanManager', 'SavePlan', request);

const UpdatePlanStatus = (request: UpdatePlanStatusRequestDto): Promise<UpdatePlanStatusResultDto> =>
  ApiClient.invokeMethod<UpdatePlanStatusResultDto>('Api', 'ContentPlanManager', 'UpdatePlanStatus', request);

export default { ListPlans, GetPlan, SavePlan, UpdatePlanStatus };
