import { TestBed } from '@angular/core/testing';
import { Apollo } from 'apollo-angular';
import { of } from 'rxjs';
import { ResourceRegistryService } from './resource-registry.service';
import { ResourceDescriptor, NavNode } from '../models/resource.models';

const SCHOOL_DESCRIPTOR: ResourceDescriptor = {
  key: 'school', plural: 'schools', module: 'academic',
  icon: 'mat:school', label: 'School', isSystem: false, sortOrder: 10,
  fields: [
    { key: 'name', label: 'Name', type: 'String', required: true, unique: false,
      inGrid: true, inForm: true, enumValues: [], sortOrder: 1 },
    { key: 'censusNo', label: 'Census No', type: 'String', required: true, unique: true,
      inGrid: true, inForm: true, enumValues: [], sortOrder: 0 },
  ],
  relations: [],
  actions: [],
};

const STUDENT_DESCRIPTOR: ResourceDescriptor = {
  key: 'student', plural: 'students', module: 'academic',
  icon: 'mat:person', label: 'Student', isSystem: false, sortOrder: 20,
  fields: [], relations: [], actions: [],
};

const MOCK_NAV: NavNode[] = [{
  key: 'academic', label: 'Academic', sortOrder: 0, children: [
    { key: 'school', label: 'School', icon: 'mat:school',
      resourceKey: 'school', path: null, permissionCode: 'school:read',
      sortOrder: 0, children: [] },
  ],
}];

function apolloQueryOf(resources: ResourceDescriptor[], navigation: NavNode[]) {
  return { data: { resources, navigation }, loading: false, networkStatus: 7 };
}

describe('ResourceRegistryService', () => {
  let service: ResourceRegistryService;
  let apolloSpy: jasmine.SpyObj<Apollo>;

  beforeEach(() => {
    apolloSpy = jasmine.createSpyObj<Apollo>('Apollo', ['query']);

    TestBed.configureTestingModule({
      providers: [
        ResourceRegistryService,
        { provide: Apollo, useValue: apolloSpy },
      ],
    });

    service = TestBed.inject(ResourceRegistryService);
  });

  // ── Initial state ──────────────────────────────────────────────────────────

  it('starts with empty resources signal', () => {
    expect(service.resources()).toEqual([]);
  });

  it('starts with empty navigation signal', () => {
    expect(service.navigation()).toEqual([]);
  });

  // ── load() ─────────────────────────────────────────────────────────────────

  it('load() – sets resources from Apollo response', async () => {
    apolloSpy.query.and.returnValue(
      of(apolloQueryOf([SCHOOL_DESCRIPTOR], MOCK_NAV)) as any,
    );

    await service.load();

    expect(service.resources()).toEqual([SCHOOL_DESCRIPTOR]);
  });

  it('load() – sets navigation from Apollo response', async () => {
    apolloSpy.query.and.returnValue(
      of(apolloQueryOf([SCHOOL_DESCRIPTOR], MOCK_NAV)) as any,
    );

    await service.load();

    expect(service.navigation()).toEqual(MOCK_NAV);
  });

  it('load() – handles null resources in response gracefully', async () => {
    apolloSpy.query.and.returnValue(
      of({ data: { resources: null, navigation: null } }) as any,
    );

    await service.load();

    expect(service.resources()).toEqual([]);
    expect(service.navigation()).toEqual([]);
  });

  it('load() – sets multiple resources', async () => {
    apolloSpy.query.and.returnValue(
      of(apolloQueryOf([SCHOOL_DESCRIPTOR, STUDENT_DESCRIPTOR], [])) as any,
    );

    await service.load();

    expect(service.resources().length).toBe(2);
  });

  it('load() – replaces previous resources on second call', async () => {
    apolloSpy.query.and.returnValues(
      of(apolloQueryOf([SCHOOL_DESCRIPTOR], [])) as any,
      of(apolloQueryOf([STUDENT_DESCRIPTOR], [])) as any,
    );

    await service.load();
    await service.load();

    expect(service.resources()).toEqual([STUDENT_DESCRIPTOR]);
  });

  // ── get() ──────────────────────────────────────────────────────────────────

  it('get() – returns descriptor by key', async () => {
    apolloSpy.query.and.returnValue(
      of(apolloQueryOf([SCHOOL_DESCRIPTOR], [])) as any,
    );
    await service.load();

    expect(service.get('school')).toEqual(SCHOOL_DESCRIPTOR);
  });

  it('get() – returns undefined for unknown key', async () => {
    apolloSpy.query.and.returnValue(
      of(apolloQueryOf([SCHOOL_DESCRIPTOR], [])) as any,
    );
    await service.load();

    expect(service.get('nonexistent')).toBeUndefined();
  });

  it('get() – returns undefined before load()', () => {
    expect(service.get('school')).toBeUndefined();
  });

  // ── getByPlural() ──────────────────────────────────────────────────────────

  it('getByPlural() – matches by plural field', async () => {
    apolloSpy.query.and.returnValue(
      of(apolloQueryOf([SCHOOL_DESCRIPTOR], [])) as any,
    );
    await service.load();

    expect(service.getByPlural('schools')).toEqual(SCHOOL_DESCRIPTOR);
  });

  it('getByPlural() – falls back to key match when plural does not match', async () => {
    const custom = { ...SCHOOL_DESCRIPTOR, plural: 'schoolz' };
    apolloSpy.query.and.returnValue(
      of(apolloQueryOf([custom], [])) as any,
    );
    await service.load();

    // 'school' matches r.key === plural arg
    expect(service.getByPlural('school')).toEqual(custom);
  });

  it('getByPlural() – returns undefined for no match', async () => {
    apolloSpy.query.and.returnValue(
      of(apolloQueryOf([SCHOOL_DESCRIPTOR], [])) as any,
    );
    await service.load();

    expect(service.getByPlural('unknown')).toBeUndefined();
  });

  // ── resourceMap computed ───────────────────────────────────────────────────

  it('resourceMap – is keyed by resource key', async () => {
    apolloSpy.query.and.returnValue(
      of(apolloQueryOf([SCHOOL_DESCRIPTOR, STUDENT_DESCRIPTOR], [])) as any,
    );
    await service.load();

    const map = service.resourceMap();
    expect(map.get('school')).toEqual(SCHOOL_DESCRIPTOR);
    expect(map.get('student')).toEqual(STUDENT_DESCRIPTOR);
    expect(map.size).toBe(2);
  });

  it('resourceMap – is empty before load()', () => {
    expect(service.resourceMap().size).toBe(0);
  });
});
