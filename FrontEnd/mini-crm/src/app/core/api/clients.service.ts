import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { ClientDto } from '../interfaces/clientDto';


@Injectable({ providedIn: 'root' })
export class ClientsService {
  private http = inject(HttpClient);

  // Usa HTTPS si tu backend corre en https; si el navegador se queja del cert,
  // cambia a HTTP (el puerto http que imprime Kestrel).
  private base = 'https://localhost:7213';

  clients = signal<ClientDto[]>([]);
  loading = signal(false);
  total = signal(0);
  page = signal(1);
  pageSize = signal(10);
  search = signal('');

  list(p: { page?: number; pageSize?: number; search?: string } = {}) {
    const params = new HttpParams()
      .set('page', String(p.page ?? this.page()))
      .set('pageSize', String(p.pageSize ?? this.pageSize()))
      .set('search', p.search ?? this.search());

    this.http.get<{ total: number; page: number; pageSize: number; clients: ClientDto[] }>(
      `${this.base}/clients`, { params }
    ).subscribe(res => {
      this.clients.set(res.clients);
      this.total.set(res.total);
      this.page.set(res.page);
      this.pageSize.set(res.pageSize);
    });
  }

  create(payload: ClientDto) {
    return this.http.post<ClientDto>(`${this.base}/clients`, payload);
  }

  update(id: string, payload: ClientDto) {
    return this.http.put(`${this.base}/clients/${id}`, payload);
  }

  remove(id: string) {
    return this.http.delete(`${this.base}/clients/${id}`);
  }
}
