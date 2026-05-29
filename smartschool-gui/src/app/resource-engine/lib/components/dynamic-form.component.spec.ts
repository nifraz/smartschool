import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { DynamicFormComponent } from './dynamic-form.component';
import { ResourceDescriptor } from '../models/resource.models';

function makeResource(overrides: Partial<ResourceDescriptor> = {}): ResourceDescriptor {
  return {
    key: 'widget', plural: 'widgets', module: 'test',
    icon: 'mat:widgets', label: 'Widget', isSystem: false, sortOrder: 0,
    fields: [
      { key: 'name',    label: 'Name',    type: 'String',  required: true,  unique: false,
        inGrid: true, inForm: true,  enumValues: [], sortOrder: 1 },
      { key: 'count',   label: 'Count',   type: 'Int',     required: false, unique: false,
        inGrid: true, inForm: true,  enumValues: [], sortOrder: 2 },
      { key: 'active',  label: 'Active',  type: 'Bool',    required: false, unique: false,
        inGrid: true, inForm: true,  enumValues: [], sortOrder: 3 },
      { key: 'madeOn',  label: 'Made On', type: 'Date',    required: false, unique: false,
        inGrid: false, inForm: true, enumValues: [], sortOrder: 4 },
      { key: 'color',   label: 'Color',   type: 'Enum',    required: true,  unique: false,
        inGrid: true, inForm: true,  enumValues: ['Red', 'Green', 'Blue'], sortOrder: 5 },
      { key: 'notes',   label: 'Notes',   type: 'String',  required: false, unique: false,
        inGrid: false, inForm: false, enumValues: [], sortOrder: 6 },  // inForm=false → excluded
    ],
    relations: [], actions: [],
    ...overrides,
  };
}

describe('DynamicFormComponent', () => {
  let fixture: ComponentFixture<DynamicFormComponent>;
  let component: DynamicFormComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DynamicFormComponent, TranslateModule.forRoot()],
    }).compileComponents();

    fixture = TestBed.createComponent(DynamicFormComponent);
    component = fixture.componentInstance;
    component.resource = makeResource();
  });

  // ── Form control creation ──────────────────────────────────────────────────

  it('creates controls only for inForm=true fields', () => {
    fixture.detectChanges();
    const controlKeys = Object.keys(component.form.controls);
    expect(controlKeys).toContain('name');
    expect(controlKeys).toContain('count');
    expect(controlKeys).not.toContain('notes'); // inForm=false
  });

  it('control count matches number of inForm fields', () => {
    fixture.detectChanges();
    // name, count, active, madeOn, color → 5 controls; notes excluded
    expect(Object.keys(component.form.controls).length).toBe(5);
  });

  it('required fields have Validators.required', () => {
    fixture.detectChanges();
    const nameControl = component.form.get('name')!;
    nameControl.setValue('');
    expect(nameControl.valid).toBeFalse();
    expect(nameControl.errors?.['required']).toBeTruthy();
  });

  it('optional fields do not have required validator', () => {
    fixture.detectChanges();
    const countControl = component.form.get('count')!;
    countControl.setValue(null);
    expect(countControl.valid).toBeTrue();
  });

  // ── Value initialization ───────────────────────────────────────────────────

  it('initializes controls with provided value', () => {
    component.value = { name: 'Pre-filled', count: 42 };
    fixture.detectChanges();
    expect(component.form.get('name')?.value).toBe('Pre-filled');
    expect(component.form.get('count')?.value).toBe(42);
  });

  it('initializes missing keys to null', () => {
    component.value = { name: 'Only Name' };
    fixture.detectChanges();
    expect(component.form.get('count')?.value).toBeNull();
  });

  // ── Input type rendering ───────────────────────────────────────────────────

  it('renders text input for String type', () => {
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('input[formControlName="name"]');
    expect(input?.type).toBe('text');
  });

  it('renders number input for Int type', () => {
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('input[formControlName="count"]');
    expect(input?.type).toBe('number');
  });

  it('renders checkbox for Bool type', () => {
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('input[formControlName="active"]');
    expect(input?.type).toBe('checkbox');
  });

  it('renders date input for Date type', () => {
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('input[formControlName="madeOn"]');
    expect(input?.type).toBe('date');
  });

  it('renders select with options for Enum type', () => {
    fixture.detectChanges();
    const select = fixture.nativeElement.querySelector('select[formControlName="color"]');
    expect(select).toBeTruthy();
    const options: HTMLOptionElement[] = Array.from(select.querySelectorAll('option'));
    const values = options.map((o: HTMLOptionElement) => o.value).filter((v: string) => v !== '');
    expect(values).toEqual(['Red', 'Green', 'Blue']);
  });

  // ── Submit behaviour ───────────────────────────────────────────────────────

  it('emits save event with form value when form is valid', () => {
    fixture.detectChanges();
    const saves: any[] = [];
    component.save.subscribe((v: any) => saves.push(v));

    component.form.setValue({ name: 'Widget A', count: 5, active: true, madeOn: null, color: 'Red' });
    component.onSubmit();

    expect(saves.length).toBe(1);
    expect(saves[0].name).toBe('Widget A');
  });

  it('does not emit save when required field is missing', () => {
    fixture.detectChanges();
    const saves: any[] = [];
    component.save.subscribe((v: any) => saves.push(v));

    // name is required but left empty
    component.form.setValue({ name: '', count: 0, active: false, madeOn: null, color: 'Red' });
    component.onSubmit();

    expect(saves.length).toBe(0);
  });

  it('form is invalid when required fields are empty', () => {
    fixture.detectChanges();
    component.form.get('name')?.setValue('');
    expect(component.form.invalid).toBeTrue();
  });

  it('form is valid when all required fields are filled', () => {
    fixture.detectChanges();
    component.form.get('name')?.setValue('Valid Name');
    component.form.get('color')?.setValue('Green');
    expect(component.form.valid).toBeTrue();
  });
});
