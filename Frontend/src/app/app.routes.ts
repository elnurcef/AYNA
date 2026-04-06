import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'demographics'
  },
  {
    path: 'demographics',
    title: 'Demographics',
    loadComponent: () =>
      import('./pages/demographics-page/demographics-page.component').then(
        (m) => m.DemographicsPageComponent
      )
  },
  {
    path: 'bus-analytics',
    title: 'Bus Analytics',
    loadComponent: () =>
      import('./pages/bus-analytics-page/bus-analytics-page.component').then(
        (m) => m.BusAnalyticsPageComponent
      )
  },
  {
    path: 'live-routes',
    title: 'Live Routes',
    loadComponent: () =>
      import('./pages/live-routes-page/live-routes-page.component').then(
        (m) => m.LiveRoutesPageComponent
      )
  },
  {
    path: '**',
    redirectTo: 'demographics'
  }
];
