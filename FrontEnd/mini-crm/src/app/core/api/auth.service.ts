import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private base = 'https://localhost:7213';
  private key = 'mini_crm_token';

  get token() { return localStorage.getItem(this.key) ?? ''; }
  set token(v: string) { localStorage.setItem(this.key, v); }

  async registerTenant(tenantName: string, adminEmail: string, password: string) {
    const res = await this.http.post<{ accessToken: string }>(`${this.base}/auth/register-tenant`,
      { tenantName, adminEmail, password }).toPromise();
    if (res?.accessToken) this.token = res.accessToken;
  }

  async login(email: string, password: string) {
    const res = await this.http.post<{ accessToken: string }>(`${this.base}/auth/login`,
      { email, password }).toPromise();
    if (res?.accessToken) this.token = res.accessToken;
  }
}
