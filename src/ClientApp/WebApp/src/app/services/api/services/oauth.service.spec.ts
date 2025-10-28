import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { OAuthService } from './oauth.service';

describe('OAuthService', () => {
  let service: OAuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        OAuthService,
        { provide: 'API_BASE_URL', useValue: 'http://localhost:5000' }
      ]
    });
    service = TestBed.inject(OAuthService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should have token method', () => {
    expect(service.token).toBeDefined();
    expect(typeof service.token).toBe('function');
  });

  it('should have deviceAuthorization method', () => {
    expect(service.deviceAuthorization).toBeDefined();
    expect(typeof service.deviceAuthorization).toBe('function');
  });

  it('should have introspect method', () => {
    expect(service.introspect).toBeDefined();
    expect(typeof service.introspect).toBe('function');
  });

  it('should have revoke method', () => {
    expect(service.revoke).toBeDefined();
    expect(typeof service.revoke).toBe('function');
  });
});
