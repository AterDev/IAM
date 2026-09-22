import { isLockoutActive } from './lockout';

describe('isLockoutActive', () => {
  const now = Date.parse('2026-09-22T12:00:00.000Z');

  it('recognizes future Date and ISO string values', () => {
    expect(isLockoutActive(new Date(now + 1), now)).toBe(true);
    expect(isLockoutActive('2026-09-22T12:00:01.000Z', now)).toBe(true);
  });

  it('treats missing, invalid, expired, and exact-boundary values as unlocked', () => {
    expect(isLockoutActive(null, now)).toBe(false);
    expect(isLockoutActive(undefined, now)).toBe(false);
    expect(isLockoutActive('not-a-date', now)).toBe(false);
    expect(isLockoutActive('2026-09-22T11:59:59.000Z', now)).toBe(false);
    expect(isLockoutActive('2026-09-22T12:00:00.000Z', now)).toBe(false);
  });
});
