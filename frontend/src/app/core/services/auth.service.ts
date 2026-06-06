import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AuthTokenResponse, LoginRequest, RegisterRequest, User } from '../../models/user.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private token: string | null = null;
  private currentUserId: string | null = null;
  private currentUsername: string | null = null;

  constructor(private http: HttpClient) {}

  register(request: RegisterRequest): Observable<User> {
    return this.http.post<User>(`${environment.apiBaseUrl}/auth/register`, request);
  }

  login(request: LoginRequest): Observable<AuthTokenResponse> {
    return this.http.post<AuthTokenResponse>(`${environment.apiBaseUrl}/auth/login`, request).pipe(
      tap(response => {
        this.token = response.token;
        this.currentUserId = response.userId;
        this.currentUsername = response.username;
      })
    );
  }

  logout(): void {
    this.token = null;
    this.currentUserId = null;
    this.currentUsername = null;
  }

  getToken(): string | null {
    return this.token;
  }

  isLoggedIn(): boolean {
    return this.token !== null;
  }

  getUserId(): string | null {
    return this.currentUserId;
  }

  getUsername(): string | null {
    return this.currentUsername;
  }

  getMe(): Observable<User> {
    return this.http.get<User>(`${environment.apiBaseUrl}/auth/me`);
  }
}
