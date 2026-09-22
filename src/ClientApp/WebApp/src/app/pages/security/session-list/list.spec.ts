import { SessionListComponent } from './list';
import { LoginSessionItemDto } from 'src/app/services/api/models/iammod/login-session-item-dto.model';

describe('SessionListComponent', () => {
  it('uses the business sessionId when opening a session view', () => {
    const router = { navigate: jest.fn() };
    const component = new SessionListComponent(
      {} as never,
      {} as never,
      router as never,
      {} as never,
      {} as never,
      {} as never,
    );
    const session = {
      id: 'entity-id',
      sessionId: 'sid-value',
    } as LoginSessionItemDto;

    component.viewDetail(session);

    expect(router.navigate).toHaveBeenCalledWith(['/security/session-list', 'sid-value']);
  });
});
