import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RolesService } from './roles.service';
import { RoleAddDto } from '../models/identity-mod/role-add-dto.model';
import { RoleUpdateDto } from '../models/identity-mod/role-update-dto.model';

describe('RolesService', () => {
  let service: RolesService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [RolesService]
    });
    service = TestBed.inject(RolesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should get roles with pagination', (done) => {
    const mockResponse = { data: [], total: 0 };
    const name = 'admin';

    service.getRoles(name, 1, 10, null).subscribe(response => {
      expect(response).toEqual(mockResponse);
      done();
    });

    const expectedUrl = `/api/Roles?name=${name}&pageIndex=1&pageSize=10&orderBy=`;
    const req = httpMock.expectOne(expectedUrl);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should create a new role', (done) => {
    const newRole: RoleAddDto = {
      name: 'NewRole',
      description: 'Test role description'
    } as RoleAddDto;
    const mockResponse = { id: '456', name: 'NewRole' };

    service.createRole(newRole).subscribe(response => {
      expect(response).toEqual(mockResponse);
      done();
    });

    const req = httpMock.expectOne('/api/Roles');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(newRole);
    req.flush(mockResponse);
  });

  it('should get role detail by id', (done) => {
    const roleId = '456';
    const mockRole = { id: roleId, name: 'Admin', description: 'Administrator role' };

    service.getDetail(roleId).subscribe(response => {
      expect(response).toEqual(mockRole);
      done();
    });

    const req = httpMock.expectOne(`/api/Roles/${roleId}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockRole);
  });

  it('should update role', (done) => {
    const roleId = '456';
    const updateData: RoleUpdateDto = {
      name: 'UpdatedRole',
      description: 'Updated description'
    } as RoleUpdateDto;
    const mockResponse = { id: roleId, ...updateData };

    service.updateRole(roleId, updateData).subscribe(response => {
      expect(response).toEqual(mockResponse);
      done();
    });

    const req = httpMock.expectOne(`/api/Roles/${roleId}`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(updateData);
    req.flush(mockResponse);
  });

  it('should delete role', (done) => {
    const roleId = '456';

    service.deleteRole(roleId, false).subscribe(response => {
      expect(response).toBeTruthy();
      done();
    });

    const req = httpMock.expectOne(`/api/Roles/${roleId}?hardDelete=false`);
    expect(req.request.method).toBe('DELETE');
    req.flush({ success: true });
  });
});
