import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, catchError, iif, map, Observable, of, switchMap, tap, throwError } from 'rxjs';
import { AuthenticateResponse, UserLoginRequest, UserRegisterRequest, VerifyRequest, UserResponse } from '../shared/models';
import { UserModel } from '../../../graphql/generated';
import { GraphqlService } from '../shared/services/graphql.service';
import { GET_USER } from '../shared/queries';
import { GraphqlTypes } from '../shared/enums';

const API_BASE = `http://${window.location.hostname}:5000`;

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private userLoggedSubject: BehaviorSubject<number>;
  public userLogged: Observable<number>;
  public loggedInUser?: UserModel | null;

  constructor(
    private http: HttpClient,
    private graphqlService: GraphqlService,
    private router: Router
  ) {
    this.userLoggedSubject = new BehaviorSubject<number>(this.getUserId());
    this.userLogged = this.userLoggedSubject.asObservable();
  }

  register(model: UserRegisterRequest): Observable<UserResponse> {
    return iif(
      () => !!(model.password == model.password2),
      this.http.post<UserResponse>(`${API_BASE}/api/auth/register`, model),
      throwError(() => ({
        error: {
          message: "Passwords do not match.",
        }
      }))
    )
  }

  verify(model: VerifyRequest): Observable<UserResponse> {
    return this.http.put<UserResponse>(`${API_BASE}/api/auth/verify`, model);
  }
  
  login(model: UserLoginRequest): Observable<AuthenticateResponse> {
    return this.http.post<AuthenticateResponse>(`${API_BASE}/api/auth/login`, model).pipe(
      tap(res => {
        if ((model.email && res.isEmailVerified) || (model.mobileNo && res.isMobileNoVerified)) {
          this.setSession(res);
          this.userLoggedSubject.next(res.userId);
          this.router.navigate(['/']);
        }
        else {
          this.router.navigate(['/auth', 'verify'], { queryParams: { email: model.email, mobileNo: model.mobileNo }});
        }
      })
    );
  }
  
  logout() {
    this.userLoggedSubject.next(0);
    this.loggedInUser = null;

      localStorage.removeItem('userId');
      localStorage.removeItem("token");
      localStorage.removeItem("expires");
  }
  
  getUser(id: number): Observable<UserModel> {
    const variables = {
      id,
    };

    return this.graphqlService.getGqlQueryObservable(GET_USER, variables).pipe(
      map(res => {
        return res.data[GraphqlTypes.USER] as UserModel;
      }),
      tap(res => {
        this.loggedInUser = res;
      }),
      catchError(err => {
        this.loggedInUser = null;
        return throwError(() => err);
      })
    );
  }

  private setSession(model: AuthenticateResponse) {
    localStorage.setItem('userId', model.userId.toString());
    localStorage.setItem('token', model.token);
    localStorage.setItem('expires', String(new Date(model.expires).getTime()));
  }

  getUserId(): number {
    return +(localStorage.getItem('userId') ?? "0");
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  getExpiration(): Date | null {
    const expiration = localStorage.getItem('expires');
    if (!expiration) return null;
    return new Date(Number(expiration));
  }

  getLoggedInUser(): UserModel | null {
    return this.isLoggedIn() ? JSON.parse(localStorage.getItem('userId') ?? "null") : null;
  }

  public isLoggedIn(): boolean {
    const expiration = this.getExpiration();
    return !!expiration && Date.now() < expiration.getTime();
  }

  isLoggedOut(): boolean {
      return !this.isLoggedIn();
  }
}
