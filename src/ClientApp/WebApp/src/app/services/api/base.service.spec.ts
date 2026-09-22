import { HttpClient, HttpHeaders, HttpResponse } from '@angular/common/http';
import { firstValueFrom, of } from 'rxjs';
import { UsersService } from './services/users.service';

describe('BaseService response handling', () => {
  function createService(response: HttpResponse<Blob>): UsersService {
    const http = { request: jest.fn().mockReturnValue(of(response)) } as unknown as HttpClient;
    return new UsersService(http, '/api');
  }

  it('completes a user status update successfully for HTTP 204', async () => {
    const service = createService(new HttpResponse<Blob>({ status: 204, body: null }));

    await expect(firstValueFrom(service.updateStatus('user-id', new Date())))
      .resolves.toBeUndefined();
  });

  it('accepts an empty response body even if JSON content type is present', async () => {
    const service = createService(new HttpResponse<Blob>({
      status: 200,
      body: { text: () => Promise.resolve('') } as Blob,
      headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
    }));

    await expect(firstValueFrom(service.deleteUser('user-id', false)))
      .resolves.toBeUndefined();
  });

  it('continues to parse nonempty JSON responses', async () => {
    const service = createService(new HttpResponse<Blob>({
      body: { text: () => Promise.resolve('{"id":"user-id","userName":"alice"}') } as Blob,
      headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
    }));

    await expect(firstValueFrom(service.getDetail('user-id')))
      .resolves.toEqual({ id: 'user-id', userName: 'alice' });
  });
});
