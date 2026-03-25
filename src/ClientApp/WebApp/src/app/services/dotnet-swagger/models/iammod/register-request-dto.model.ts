/**
 * Public self-service registration request.
 */
export interface RegisterRequestDto {
  /** userName */
  userName: string;
  /** email */
  email: string;
  /** phoneNumber */
  phoneNumber?: string | null;
  /** password */
  password: string;
}
