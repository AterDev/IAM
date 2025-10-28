import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { OAuthService } from './oauth.service';
import { TokenResponseDto } from '../models/identity-mod/token-response-dto.model';

describe('OAuthService', () => {
  let service: OAuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [OAuthService]
    });
    service = TestBed.inject(OAuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should call token endpoint with credentials', () => {
    const tokenRequest = {
      grant_type: 'password',
      username: 'testuser',
      password: 'password123',
      client_id: 'admin-portal',
      scope: 'openid profile'
    };

    const mockTokenResponse: TokenResponseDto = {
      accessToken: 'mock-access-token',
      tokenType: 'Bearer',
      expiresIn: 3600,
      refreshToken: 'mock-refresh-token',
      idToken: 'mock-id-token'
    } as TokenResponseDto;

    service.token(tokenRequest).subscribe(response => {
      expect(response).toEqual(mockTokenResponse);
      expect(response.accessToken).toBe('mock-access-token');
      expect(response.tokenType).toBe('Bearer');
    });

    const req = httpMock.expectOne('/connect/token');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(tokenRequest);
    req.flush(mockTokenResponse);
  });

  it('should call token endpoint with authorization code', () => {
    const tokenRequest = {
      grant_type: 'authorization_code',
      code: 'auth-code-123',
      redirect_uri: 'http://localhost:4200/callback',
      client_id: 'admin-portal',
      code_verifier: 'verifier-123'
    };

    const mockTokenResponse: TokenResponseDto = {
      accessToken: 'mock-access-token',
      tokenType: 'Bearer',
      expiresIn: 3600
    } as TokenResponseDto;

    service.token(tokenRequest).subscribe(response => {
      expect(response).toEqual(mockTokenResponse);
    });

    const req = httpMock.expectOne('/connect/token');
    expect(req.request.method).toBe('POST');
    req.flush(mockTokenResponse);
  });

  it('should call device authorization endpoint', () => {
    const deviceRequest = {
      client_id: 'device-client'
    };

    const mockDeviceResponse = {
      device_code: 'device-code-123',
      user_code: 'USER-CODE',
      verification_uri: 'http://localhost:4200/device',
      expires_in: 900
    };

    service.deviceAuthorization(deviceRequest).subscribe(response => {
      expect(response).toEqual(mockDeviceResponse);
      expect(response.user_code).toBe('USER-CODE');
    });

    const req = httpMock.expectOne('/connect/device');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(deviceRequest);
    req.flush(mockDeviceResponse);
  });
});

