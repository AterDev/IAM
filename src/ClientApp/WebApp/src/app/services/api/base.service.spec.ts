import { HttpHeaders, HttpResponse } from '@angular/common/http';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Injectable } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { BaseService } from './base.service';
import { AuthService } from '../auth.service';

@Injectable()
class TestBaseService extends BaseService {
  send<T>(method: string, path: string, body?: unknown) {
    return this.request<T>(method, path, body);
  }
}

function createBlobMock(text: string): Blob {
  return {
    size: text.length,
    type: 'application/json',
    text: () => Promise.resolve(text)
  } as Blob;
}

describe('BaseService', () => {
  let service: TestBaseService;
  let httpMock: HttpTestingController;
  const authServiceMock = {
    getAccessToken: jest.fn(() => 'test-token')
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        TestBaseService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: 'API_BASE_URL', useValue: 'https://api.example.com/' },
        { provide: AuthService, useValue: authServiceMock }
      ]
    });

    service = TestBed.inject(TestBaseService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    jest.clearAllMocks();
  });

  it('should treat 204 responses as successful empty results', async () => {
    const resultPromise = firstValueFrom(service.send<void>('DELETE', '/resources/1'));

    const req = httpMock.expectOne('https://api.example.com/resources/1');
    expect(req.request.method).toBe('DELETE');
    expect(req.request.headers.get('Authorization')).toBe('Bearer test-token');

    req.flush(null, {
      status: 204,
      statusText: 'No Content'
    });

    await expect(resultPromise).resolves.toBeUndefined();
  });

  it('should not parse empty 200 json bodies', async () => {
    const resultPromise = firstValueFrom(service.send('GET', '/empty-json'));

    const req = httpMock.expectOne('https://api.example.com/empty-json');
    req.event(new HttpResponse<Blob>({
      body: createBlobMock(''),
      status: 200,
      statusText: 'OK',
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      })
    }));

    await expect(resultPromise).resolves.toBeUndefined();
  });

  it('should continue parsing json payloads', async () => {
    const resultPromise = firstValueFrom(service.send<{ success: boolean }>('GET', '/payload'));

    const req = httpMock.expectOne('https://api.example.com/payload');
    req.event(new HttpResponse<Blob>({
      body: createBlobMock(JSON.stringify({ success: true })),
      status: 200,
      statusText: 'OK',
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      })
    }));

    await expect(resultPromise).resolves.toEqual({ success: true });
  });
});