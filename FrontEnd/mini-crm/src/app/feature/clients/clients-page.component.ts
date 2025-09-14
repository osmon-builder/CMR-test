import { Component, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { ClientDto } from '../../core/interfaces/clientDto';
import { ClientsService } from '../../core/api/clients.service';
import { AuthService } from '../../core/api/auth.service';


@Component({
  selector: 'app-clients-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './clients-page.component.html',
  styleUrl: './clients-page.component.scss'
})
export class ClientsPageComponent {
  // Login temporal para test
  private auth=  inject(AuthService)

  async ngOnInit() {
    await this.auth.registerTenant('Mi Empresa', 'admin@mi.com', '123456');
  }

  svc = inject(ClientsService);

  model : ClientDto = { name: '', email: '', phone: '' };
  search ='';
  saving = signal(false);

  constructor(){
    effect(() =>{
      this.reload()
    })
  }

  reload() { this.svc.list(); }

  onSearchChange(v: string) {
    this.svc.search.set(v);
    this.svc.page.set(1);
    this.svc.list({ page: 1, search: v });
  }

  add() {
    this.svc.create(this.model).subscribe(() => {
      this.model = { name: '', email: '', phone: '' };
      this.svc.list();
    });
  }

  delete(id: string) {
    this.svc.remove(id).subscribe(() => this.svc.list());
  }

  totalPages() {
    const total = this.svc.total(), size = this.svc.pageSize();
    return Math.max(1, Math.ceil(total / size));
  }
  prev() { this.svc.list({ page: this.svc.page() - 1 }); }
  next() { this.svc.list({ page: this.svc.page() + 1 }); }
}