import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideNativeDateAdapter } from '@angular/material/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter, withInMemoryScrolling } from '@angular/router';

import { API_BASE_URL } from './core/config/api-base-url.token';
import { routes } from './app.routes';

function resolveApiBaseUrl(): string {
  const hostname = globalThis.location?.hostname?.toLowerCase();

  if (hostname === 'localhost' || hostname === '127.0.0.1') {
    return 'http://localhost:5090';
  }

  return 'https://ayna-9prk.onrender.com';
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(
      routes,
      withInMemoryScrolling({
        scrollPositionRestoration: 'enabled',
        anchorScrolling: 'enabled'
      })
    ),
    provideHttpClient(withInterceptorsFromDi()),
    provideAnimationsAsync(),
    provideNativeDateAdapter(),
    {
      provide: API_BASE_URL,
      useValue: resolveApiBaseUrl()
    }
  ]
};
