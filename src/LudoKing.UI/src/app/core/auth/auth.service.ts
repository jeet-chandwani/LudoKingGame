import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { AuthResult, LoginRequest, RegisterRequest, ResetPasswordRequest } from '../../shared/models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly base = '/api/v1/auth';

  // Access token lives in memory only — never in localStorage
  private _accessToken: string | null = null;
  private _currentUser$ = new BehaviorSubject<AuthResult | null>(null);

  readonly currentUser$ = this._currentUser$.asObservable();

  constructor(private http: HttpClient) {}

  get accessToken(): string | null { return this._accessToken; }
  get currentUser(): AuthResult | null { return this._currentUser$.value; }
  get isLoggedIn(): boolean { return !!this._accessToken; }

  register(req: RegisterRequest): Observable<AuthResult> {
    return this.http.post<AuthResult>(`${this.base}/register`, req);
  }

  login(req: LoginRequest): Observable<AuthResult> {
    return this.http.post<AuthResult>(`${this.base}/login`, { identifier: req.identifier, password: req.password }, { withCredentials: true }).pipe(
      tap(res => this.storeAuth(res))
    );
  }

  refresh(): Observable<AuthResult> {
    return this.http.post<AuthResult>(`${this.base}/refresh`, {}, { withCredentials: true }).pipe(
      tap(res => this.storeAuth(res))
    );
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.base}/logout`, {}, { withCredentials: true }).pipe(
      tap(() => this.clearAuth())
    );
  }

  forgotPassword(email: string): Observable<void> {
    return this.http.post<void>(`${this.base}/forgot-password`, { email });
  }

  resetPassword(req: ResetPasswordRequest): Observable<boolean> {
    return this.http.post<boolean>(`${this.base}/reset-password`, req);
  }

  confirmEmail(token: string): Observable<boolean> {
    return this.http.get<boolean>(`${this.base}/confirm-email`, { params: { token } });
  }

  private storeAuth(res: AuthResult): void {
    this._accessToken = res.accessToken;
    this._currentUser$.next(res);
  }

  clearAuth(): void {
    this._accessToken = null;
    this._currentUser$.next(null);
  }
}
