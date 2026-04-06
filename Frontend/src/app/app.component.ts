import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatToolbarModule, MatButtonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  protected readonly navItems = [
    {
      label: 'Demographics',
      route: '/demographics',
      exact: true
    },
    {
      label: 'Bus Analytics',
      route: '/bus-analytics',
      exact: true
    },
    {
      label: 'Live Routes',
      route: '/live-routes',
      exact: true
    }
  ];
}
