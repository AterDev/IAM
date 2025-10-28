import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { UsersService } from './users.service';

describe('UsersService', () => {
  let service: UsersService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        UsersService,
        { provide: 'API_BASE_URL', useValue: 'http://localhost:5000' }
      ]
    });
    service = TestBed.inject(UsersService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should have getUsers method', () => {
    expect(service.getUsers).toBeDefined();
    expect(typeof service.getUsers).toBe('function');
  });

  it('should have createUser method', () => {
    expect(service.createUser).toBeDefined();
    expect(typeof service.createUser).toBe('function');
  });

  it('should have getDetail method', () => {
    expect(service.getDetail).toBeDefined();
    expect(typeof service.getDetail).toBe('function');
  });

  it('should have updateUser method', () => {
    expect(service.updateUser).toBeDefined();
    expect(typeof service.updateUser).toBe('function');
  });
});
