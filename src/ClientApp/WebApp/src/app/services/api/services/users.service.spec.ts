import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { UsersService } from './users.service';
import { UserAddDto } from '../models/identity-mod/user-add-dto.model';
import { UserUpdateDto } from '../models/identity-mod/user-update-dto.model';

describe('UsersService', () => {
  let service: UsersService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [UsersService]
    });
    service = TestBed.inject(UsersService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should get users with filters', (done) => {
    const mockResponse = { data: [], total: 0 };
    const userName = 'testuser';
    const email = 'test@example.com';

    service.getUsers(userName, email, null, null, null, null, 1, 10, null).subscribe(response => {
      expect(response).toEqual(mockResponse);
      done();
    });

    const expectedUrl = `/api/Users?userName=${userName}&email=${email}&phoneNumber=&lockoutEnabled=&startDate=&endDate=&pageIndex=1&pageSize=10&orderBy=`;
    const req = httpMock.expectOne(expectedUrl);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should create a new user', (done) => {
    const newUser: UserAddDto = {
      userName: 'newuser',
      email: 'newuser@example.com',
      password: 'Password123!'
    } as UserAddDto;
    const mockResponse = { id: '123', userName: 'newuser' };

    service.createUser(newUser).subscribe(response => {
      expect(response).toEqual(mockResponse);
      done();
    });

    const req = httpMock.expectOne('/api/Users');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(newUser);
    req.flush(mockResponse);
  });

  it('should get user detail by id', (done) => {
    const userId = '123';
    const mockUser = { id: userId, userName: 'testuser', email: 'test@example.com' };

    service.getDetail(userId).subscribe(response => {
      expect(response).toEqual(mockUser);
      done();
    });

    const req = httpMock.expectOne(`/api/Users/${userId}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockUser);
  });

  it('should update user', (done) => {
    const userId = '123';
    const updateData: UserUpdateDto = {
      userName: 'updateduser',
      email: 'updated@example.com'
    } as UserUpdateDto;
    const mockResponse = { id: userId, ...updateData };

    service.updateUser(userId, updateData).subscribe(response => {
      expect(response).toEqual(mockResponse);
      done();
    });

    const req = httpMock.expectOne(`/api/Users/${userId}`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(updateData);
    req.flush(mockResponse);
  });
});
