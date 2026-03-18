import { ClientAddDto } from 'src/app/services/api/models/iammod/client-add-dto.model';
import { ClientDetailDto } from 'src/app/services/api/models/iammod/client-detail-dto.model';
import { ClientUpdateDto } from 'src/app/services/api/models/iammod/client-update-dto.model';

export interface ClientPasswordGrantPolicyFields {
  allowPasswordGrant?: boolean | null;
  passwordGrantRestrictionReason?: string | null;
}

export type ClientDetailViewModel = ClientDetailDto & ClientPasswordGrantPolicyFields;
export type ClientAddPayload = ClientAddDto & ClientPasswordGrantPolicyFields;
export type ClientUpdatePayload = ClientUpdateDto & ClientPasswordGrantPolicyFields;
