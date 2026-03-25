import { JsonWebKeyDto } from '../iammod/json-web-key-dto.model';

/**
 * JSON Web Key Set
 */
export interface JwksDto {
  /** Array of JSON Web Key values */
  keys: JsonWebKeyDto[];
}
