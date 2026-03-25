/**
 * Result returned after processing an authorize interaction decision.
 */
export interface AuthorizeInteractionDecisionResponseDto {
  /** Outcome status. */
  status: string;
  /** Redirect target for the calling client. */
  redirectUrl: string;
  /** Optional message for the caller. */
  message?: string | null;
}
