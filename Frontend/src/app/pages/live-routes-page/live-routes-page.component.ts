import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-live-routes-page',
  standalone: true,
  imports: [MatCardModule],
  templateUrl: './live-routes-page.component.html',
  styleUrl: './live-routes-page.component.scss'
})
export class LiveRoutesPageComponent {}
