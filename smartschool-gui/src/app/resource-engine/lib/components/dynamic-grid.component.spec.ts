import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { DynamicGridComponent } from './dynamic-grid.component';
import { ResourceDescriptor } from '../models/resource.models';

const RESOURCE: ResourceDescriptor = {
  key: 'school', plural: 'schools', module: 'academic',
  icon: 'mat:school', label: 'School', isSystem: false, sortOrder: 0,
  fields: [
    { key: 'name',     label: 'Name',     type: 'String', required: true,  unique: false,
      inGrid: true,  inForm: true,  enumValues: [], sortOrder: 1 },
    { key: 'censusNo', label: 'Census No',type: 'String', required: true,  unique: true,
      inGrid: true,  inForm: true,  enumValues: [], sortOrder: 0 },
    { key: 'email',    label: 'Email',    type: 'String', required: false, unique: false,
      inGrid: false, inForm: true,  enumValues: [], sortOrder: 2 },
    { key: 'tags',     label: 'Tags',     type: 'RefCollection', required: false, unique: false,
      inGrid: true,  inForm: false, enumValues: [], sortOrder: 3 },
  ],
  relations: [], actions: [],
};

describe('DynamicGridComponent', () => {
  let fixture: ComponentFixture<DynamicGridComponent>;
  let component: DynamicGridComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DynamicGridComponent, TranslateModule.forRoot()],
    }).compileComponents();

    fixture = TestBed.createComponent(DynamicGridComponent);
    component = fixture.componentInstance;
    component.resource = RESOURCE;
  });

  // ── Column rendering ───────────────────────────────────────────────────────

  it('renders a <th> for each inGrid field', () => {
    fixture.detectChanges();
    const headers = fixture.nativeElement.querySelectorAll('th');
    // inGrid=true: name, censusNo (inGrid=false: email excluded; RefCollection: tags excluded)
    expect(headers.length).toBe(2);
  });

  it('does not render a column for inGrid=false fields', () => {
    fixture.detectChanges();
    const headers: HTMLElement[] = Array.from(fixture.nativeElement.querySelectorAll('th'));
    const labels = headers.map(h => h.textContent?.trim());
    expect(labels).not.toContain('Email');
  });

  it('does not render a column for RefCollection fields even when inGrid=true', () => {
    fixture.detectChanges();
    const headers: HTMLElement[] = Array.from(fixture.nativeElement.querySelectorAll('th'));
    const labels = headers.map(h => h.textContent?.trim());
    expect(labels).not.toContain('Tags');
  });

  it('renders columns sorted by sortOrder ascending', () => {
    fixture.detectChanges();
    const headers: HTMLElement[] = Array.from(fixture.nativeElement.querySelectorAll('th'));
    const labels = headers.map(h => h.textContent?.trim());
    // censusNo sortOrder=0 comes before name sortOrder=1
    expect(labels[0]).toBe('Census No');
    expect(labels[1]).toBe('Name');
  });

  // ── Row rendering ──────────────────────────────────────────────────────────

  it('renders one <tr> per item in the data body', () => {
    component.items = [
      { name: 'School A', censusNo: 'A001' },
      { name: 'School B', censusNo: 'B001' },
    ];
    fixture.detectChanges();
    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
  });

  it('renders cell values from item by field key', () => {
    component.items = [{ name: 'Test School', censusNo: 'T001' }];
    fixture.detectChanges();
    const cells: HTMLElement[] = Array.from(
      fixture.nativeElement.querySelectorAll('tbody tr:first-child td'),
    );
    const texts = cells.map(c => c.textContent?.trim());
    expect(texts).toContain('Test School');
    expect(texts).toContain('T001');
  });

  // ── Empty state ────────────────────────────────────────────────────────────

  it('shows empty state row when items array is empty', () => {
    component.items = [];
    fixture.detectChanges();
    const emptyRow = fixture.nativeElement.querySelector('tbody tr td[colspan]');
    expect(emptyRow).toBeTruthy();
  });

  it('empty state cell spans all visible columns', () => {
    component.items = [];
    fixture.detectChanges();
    const cell: HTMLElement = fixture.nativeElement.querySelector('tbody td[colspan]');
    const colspan = parseInt(cell.getAttribute('colspan') ?? '0', 10);
    expect(colspan).toBe(2); // 2 inGrid non-RefCollection columns
  });

  it('no empty row when items are present', () => {
    component.items = [{ name: 'X', censusNo: 'X1' }];
    fixture.detectChanges();
    const emptyRow = fixture.nativeElement.querySelector('tbody tr td[colspan]');
    expect(emptyRow).toBeNull();
  });
});
