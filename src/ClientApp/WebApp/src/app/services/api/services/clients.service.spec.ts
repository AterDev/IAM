import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ClientsService } from './clients.service';

describe('ClientsService', () => {
  let service: ClientsService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        ClientsService,
        { provide: 'API_BASE_URL', useValue: 'http://localhost:5000' }
      ]
    });
    service = TestBed.inject(ClientsService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should have getClients method', () => {
    expect(service.getClients).toBeDefined();
    expect(typeof service.getClients).toBe('function');
  });

  it('should have createClient method', () => {
    expect(service.createClient).toBeDefined();
    expect(typeof service.createClient).toBe('function');
  });

  it('should have getDetail method', () => {
    expect(service.getDetail).toBeDefined();
    expect(typeof service.getDetail).toBe('function');
  });

  it('should have updateClient method', () => {
    expect(service.updateClient).toBeDefined();
    expect(typeof service.updateClient).toBe('function');
  });

  it('should have deleteClient method', () => {
    expect(service.deleteClient).toBeDefined();
    expect(typeof service.deleteClient).toBe('function');
  });
});
