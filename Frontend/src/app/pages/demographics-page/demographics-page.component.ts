import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-demographics-page',
  standalone: true,
  imports: [MatCardModule],
  templateUrl: './demographics-page.component.html',
  styleUrl: './demographics-page.component.scss'
})
export class DemographicsPageComponent {}
