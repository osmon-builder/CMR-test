import { Routes } from '@angular/router';
import { ClientsPageComponent } from './feature/clients/clients-page.component';

export const routes: Routes = [
    { path: '' , redirectTo: 'clients', pathMatch: 'full'},
    { path: 'clients', component: ClientsPageComponent}
    
];
