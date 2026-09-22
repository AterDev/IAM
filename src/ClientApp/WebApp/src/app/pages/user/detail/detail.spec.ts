import { of } from 'rxjs';
import { UserDetailComponent } from './detail';
import { UserDetailDto } from 'src/app/services/api/models/iammod/user-detail-dto.model';

describe('UserDetailComponent lockout action', () => {
  it('unlocks a currently locked account based on lockoutEnd', () => {
    const user = {
      id: 'user-id',
      lockoutEnabled: false,
      lockoutEnd: new Date(Date.now() + 60_000)
    } as UserDetailDto;
    const updateStatus = jest.fn(() => of(null));
    const api = {
      users: {
        updateStatus,
        getDetail: jest.fn(() => of(user))
      }
    };
    const component = new UserDetailComponent(
      {} as never,
      {} as never,
      api as never,
      {} as never,
      { open: jest.fn() } as never,
      { instant: jest.fn((key: string) => key) } as never
    );
    component.userId = 'user-id';
    component.user.set(user);

    component.toggleUserStatus();

    expect(updateStatus).toHaveBeenCalledWith('user-id', null);
  });
});
