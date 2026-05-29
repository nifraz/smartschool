import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GridHolderComponent } from './grid.component';

describe('GridComponent', () => {
  let component: GridHolderComponent;
  let fixture: ComponentFixture<GridHolderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GridHolderComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(GridHolderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
