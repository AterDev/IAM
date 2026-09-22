import { of } from 'rxjs';
import { UserListComponent } from './list';
import { UserItemDto } from 'src/app/services/api/models/iammod/user-item-dto.model';

describe('UserListComponent lockout actions', () => {
  it('locks an account when lockoutEnd is missing, regardless of lockoutEnabled', () => {
    const updateStatus = jest.fn(() => of(null));
    const api = {
      users: {
        updateStatus,
        getUsers: jest.fn(() => of({ data: [], count: 0 }))
      }
    };
    const component = new UserListComponent(
      api as never,
      {} as never,
      {} as never,
      { open: jest.fn() } as never,
      { instant: jest.fn((key: string) => key) } as never
    );

    component.toggleUserStatus({ id: 'user-id', lockoutEnabled: true, lockoutEnd: null } as UserItemDto);

    expect(updateStatus).toHaveBeenCalledWith('user-id', expect.any(Date));
  });

  it('unlocks only when lockoutEnd is still in the future', () => {
    const updateStatus = jest.fn(() => of(null));
    const api = {
      users: {
        updateStatus,
        getUsers: jest.fn(() => of({ data: [], count: 0 }))
      }
    };
    const component = new UserListComponent(
      api as never,
      {} as never,
      {} as never,
      { open: jest.fn() } as never,
      { instant: jest.fn((key: string) => key) } as never
    );

    component.toggleUserStatus({
      id: 'user-id',
      lockoutEnabled: false,
      lockoutEnd: new Date(Date.now() + 60_000)
    } as UserItemDto);

    expect(updateStatus).toHaveBeenCalledWith('user-id', null);
  });
});
