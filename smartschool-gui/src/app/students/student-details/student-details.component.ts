import { Component, OnInit } from '@angular/core';
import { StudentsService } from '../students.service';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NotFoundComponent } from '../../shared/not-found/not-found.component';
import { MatDialog } from '@angular/material/dialog';
import { GraphqlRecordFormComponent } from '../../shared/graphql-record-form/graphql-record-form.component';
import { GraphqlService } from '../../shared/services/graphql.service';
import { EnrollmentStatus, StudentModel } from '../../../../graphql/generated';
import { ToastService } from '../../shared/services/toast.service';
import { GET_STUDENT } from '../../shared/queries';
import { AuthService } from '../../auth/auth.service';
import { RecordComponent } from '../../shared/components/record/record.component';
import { GraphqlTypes, GraphqlCollections } from '../../shared/enums';
import { SkeletonComponent } from '../../shared/components/skeleton/skeleton.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-student-details',
  standalone: true,
  templateUrl: './student-details.component.html',
  styleUrl: './student-details.component.scss',
  imports: [
    CommonModule,
    NotFoundComponent,
    RouterLink,
    SkeletonComponent,
    EmptyStateComponent,
  ],
})
export class StudentDetailsComponent extends RecordComponent<StudentModel> implements OnInit {
  EnrollmentStatus = EnrollmentStatus;

  constructor(
    private studentsService: StudentsService,
    private activatedRoute: ActivatedRoute,
    private router: Router,
    private matDialog: MatDialog,
    private graphqlService: GraphqlService,
    private toast: ToastService,
    public authService: AuthService,
  ) {
    super();
  }

  ngOnInit(): void {
    this.id = this.activatedRoute.snapshot.paramMap.get('studentId');
    this.loadData();

    this.activatedRoute.data.subscribe(data => {
      if (data['isEdit']) {
        this.openRecordFormModal();
      }
    });
  }

  loadData(): void {
    if (this.id) {
      this.isLoading = true;
      this.graphqlService.getGqlQueryObservable(GET_STUDENT, { id: +this.id }).subscribe({
        next: res => {
          this.isLoading = false;
          this.record = res.data[GraphqlTypes.STUDENT];
        },
        error: err => {
          this.isLoading = false;
          console.error(err);
        },
      });
    }
  }

  openRecordFormModal(): void {
    const dialogRef = this.matDialog.open(GraphqlRecordFormComponent, {
      width: '1200px',
      data: {
        collection: GraphqlCollections.STUDENTS,
        type: GraphqlTypes.STUDENT,
        inputDefs: {},
        id: this.id,
      },
    });

    dialogRef.afterClosed().subscribe(() => {
      this.router.navigate(['/students', this.id]);
    });
  }

  editRecord(): void {
    this.router.navigate(['/students', this.id, 'edit']);
  }

  deleteRecord(): void {
    this.toast.error('Could not delete', 'Students');
  }

  addSchoolStudentEnrollment(): void {}

  getInitials(name?: string | null): string {
    if (!name) return '?';
    return name.split(' ').slice(0, 2).map((w: string) => w[0]).join('').toUpperCase();
  }
}
