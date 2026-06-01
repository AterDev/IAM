/**
 * Decision payload for a device authorization interaction.
 */
export interface DeviceAuthorizationDecisionDto {
  /** Submitted user code. */
  userCode: string;
  /** Whether the device request is approved. */
  approve: boolean;
}
