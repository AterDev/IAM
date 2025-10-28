import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ClientsService } from './clients.service';

describe('ClientsService', () => {
  let service: ClientsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ClientsService]
    });
    service = TestBed.inject(ClientsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should get clients with pagination', (done) => {
    const mockResponse = { data: [], total: 0 };

    service.getClients('test-client', null, 1, 10, null).subscribe(response => {
      expect(response).toEqual(mockResponse);
      done();
    });

    const expectedUrl = `/api/Clients?clientId=test-client&displayName=&pageIndex=1&pageSize=10&orderBy=`;
    const req = httpMock.expectOne(expectedUrl);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should create a new client', (done) => {
    const newClient = {
      clientId: 'new-client',
      displayName: 'New Client',
      clientSecret: 'secret123'
    };
    const mockResponse = { id: '789', clientId: 'new-client' };

    service.createClient(newClient).subscribe(response => {
      expect(response).toEqual(mockResponse);
      done();
    });

    const req = httpMock.expectOne('/api/Clients');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(newClient);
    req.flush(mockResponse);
  });

  it('should get client detail by id', (done) => {
    const clientId = '789';
    const mockClient = {
      id: clientId,
      clientId: 'test-client',
      displayName: 'Test Client',
      requirePkce: true
    };

    service.getDetail(clientId).subscribe(response => {
      expect(response).toEqual(mockClient);
      done();
    });

    const req = httpMock.expectOne(`/api/Clients/${clientId}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockClient);
  });

  it('should update client', (done) => {
    const clientId = '789';
    const updateData = {
      displayName: 'Updated Client',
      requirePkce: false
    };
    const mockResponse = { id: clientId, ...updateData };

    service.updateClient(clientId, updateData).subscribe(response => {
      expect(response).toEqual(mockResponse);
      done();
    });

    const req = httpMock.expectOne(`/api/Clients/${clientId}`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(updateData);
    req.flush(mockResponse);
  });

  it('should delete client', (done) => {
    const clientId = '789';

    service.deleteClient(clientId, false).subscribe(response => {
      expect(response).toBeTruthy();
      done();
    });

    const req = httpMock.expectOne(`/api/Clients/${clientId}?hardDelete=false`);
    expect(req.request.method).toBe('DELETE');
    req.flush({ success: true });
  });
});
