/**
 * JSON Web Key
 */
export interface JsonWebKeyDto {
  /** Key type (e.g., "RSA") */
  kty: string;
  /** Public key use (e.g., "sig" for signature) */
  use: string;
  /** Key ID - unique identifier for the key */
  kid: string;
  /** Algorithm intended for use with the key (e.g., "RS256") */
  alg: string;
  /** RSA modulus (base64url encoded) */
  n?: string | null;
  /** RSA public exponent (base64url encoded) */
  e?: string | null;
  /** X.509 certificate chain (array of base64-encoded DER) */
  x5c?: string[] | null;
  /** X.509 certificate SHA-1 thumbprint (base64url encoded) */
  x5t?: string | null;
  /** X.509 certificate SHA-256 thumbprint (base64url encoded) */
  x5tS256?: string | null;
}
