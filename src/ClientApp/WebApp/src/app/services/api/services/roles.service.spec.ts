import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RolesService } from './roles.service';

describe('RolesService', () => {
  let service: RolesService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        RolesService,
        { provide: 'API_BASE_URL', useValue: 'http://localhost:5000' }
      ]
    });
    service = TestBed.inject(RolesService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should have getRoles method', () => {
    expect(service.getRoles).toBeDefined();
    expect(typeof service.getRoles).toBe('function');
  });

  it('should have createRole method', () => {
    expect(service.createRole).toBeDefined();
    expect(typeof service.createRole).toBe('function');
  });

  it('should have getDetail method', () => {
    expect(service.getDetail).toBeDefined();
    expect(typeof service.getDetail).toBe('function');
  });

  it('should have updateRole method', () => {
    expect(service.updateRole).toBeDefined();
    expect(typeof service.updateRole).toBe('function');
  });

  it('should have deleteRole method', () => {
    expect(service.deleteRole).toBeDefined();
    expect(typeof service.deleteRole).toBe('function');
  });
});
