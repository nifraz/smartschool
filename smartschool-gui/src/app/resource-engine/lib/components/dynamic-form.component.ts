import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ResourceDescriptor } from '../models/resource.models';

/**
 * Minimal dynamic form. Renders a single column of inputs from the descriptor.
 * TODO: replace with @ngx-formly/material driven by validators_json.
 */
@Component({
  selector: 'ss-dynamic-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule],
  template: `
    <form [formGroup]="form" (ngSubmit)="onSubmit()" class="ss-form">
      @for (f of resource.fields; track f.key) {
        @if (f.inForm) {
          <label>
            <span>{{ f.label }}{{ f.required ? ' *' : '' }}</span>
            @switch (f.type) {
              @case ('Bool') { <input type="checkbox" [formControlName]="f.key" /> }
              @case ('Date') { <input type="date" [formControlName]="f.key" /> }
              @case ('DateTime') { <input type="datetime-local" [formControlName]="f.key" /> }
              @case ('Int') { <input type="number" [formControlName]="f.key" /> }
              @case ('Long') { <input type="number" [formControlName]="f.key" /> }
              @case ('Decimal') { <input type="number" step="0.01" [formControlName]="f.key" /> }
              @case ('Enum') {
                <select [formControlName]="f.key">
                  <option value=""></option>
                  @for (v of f.enumValues; track v) { <option [value]="v">{{ v }}</option> }
                </select>
              }
              @default { <input type="text" [formControlName]="f.key" /> }
            }
          </label>
        }
      }
      <button type="submit" [disabled]="form.invalid">{{ 'action.save' | translate }}</button>
    </form>
  `,
  styles: [`
    .ss-form { display: flex; flex-direction: column; gap: .75rem; max-width: 32rem; }
    .ss-form label { display: flex; flex-direction: column; gap: .25rem; }
  `],
})
export class DynamicFormComponent implements OnInit {
  @Input({ required: true }) resource!: ResourceDescriptor;
  @Input() value: Record<string, any> = {};
  @Output() readonly save = new EventEmitter<Record<string, any>>();

  private readonly fb = inject(FormBuilder);
  form = this.fb.group({});

  ngOnInit(): void {
    const controls: Record<string, any> = {};
    for (const f of this.resource.fields) {
      if (!f.inForm) continue;
      const validators = f.required ? [Validators.required] : [];
      controls[f.key] = [this.value[f.key] ?? null, validators];
    }
    this.form = this.fb.group(controls);
  }

  onSubmit(): void {
    if (this.form.valid) this.save.emit(this.form.value);
  }
}
