import { Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

export type DashboardStateMode = 'loading' | 'empty' | 'error';

@Component({
  selector: 'app-dashboard-state',
  standalone: true,
  imports: [MatIconModule, MatProgressSpinnerModule],
  templateUrl: './dashboard-state.component.html',
  styleUrl: './dashboard-state.component.scss'
})
export class DashboardStateComponent {
  @Input() mode: DashboardStateMode = 'empty';
  @Input({ required: true }) title = '';
  @Input() message = '';
  @Input() icon?: string;

  get iconName(): string {
    if (this.icon) {
      return this.icon;
    }

    if (this.mode === 'error') {
      return 'error_outline';
    }

    return 'inbox';
  }
}
