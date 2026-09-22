/**
 * Returns whether a user's lockout window is still active.
 *
 * API date values may be runtime strings even though generated models type them
 * as Date, so parse both representations and ignore invalid timestamps.
 */
export function isLockoutActive(
  lockoutEnd: Date | string | null | undefined,
  now = Date.now()
): boolean {
  if (!lockoutEnd) {
    return false;
  }

  const end = lockoutEnd instanceof Date ? lockoutEnd.getTime() : Date.parse(lockoutEnd);
  return Number.isFinite(end) && end > now;
}
