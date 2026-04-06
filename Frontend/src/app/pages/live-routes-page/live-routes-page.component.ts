import { DatePipe, DecimalPipe, TitleCasePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  AfterViewInit,
  Component,
  DestroyRef,
  ElementRef,
  OnDestroy,
  ViewChild,
  inject
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import * as L from 'leaflet';
import { debounceTime, distinctUntilChanged, filter } from 'rxjs';

import {
  LiveRoute,
  LiveRouteDirection,
  LiveRouteSnapshot,
  LiveRoutesConnectionState
} from '../../models/live-routes.models';
import { ApiService } from '../../services/api.service';
import { LiveRoutesRealtimeService } from '../../services/live-routes-realtime.service';
import { DashboardStateComponent } from '../../shared/components/dashboard-state/dashboard-state.component';

@Component({
  selector: 'app-live-routes-page',
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    TitleCasePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    DashboardStateComponent
  ],
  templateUrl: './live-routes-page.component.html',
  styleUrl: './live-routes-page.component.scss'
})
export class LiveRoutesPageComponent implements AfterViewInit, OnDestroy {
  @ViewChild('mapContainer', { static: true }) private mapContainer!: ElementRef<HTMLDivElement>;

  readonly filterControl = new FormControl('', { nonNullable: true });
  readonly routePalette = ['#0f766e', '#2563eb', '#d97706', '#dc2626', '#7c3aed', '#0891b2'];

  latestSnapshot: LiveRouteSnapshot | null = null;
  visibleRoutes: LiveRoute[] = [];
  isLoading = true;
  errorMessage = '';
  connectionState: LiveRoutesConnectionState = 'disconnected';

  private readonly apiService = inject(ApiService);
  private readonly realtimeService = inject(LiveRoutesRealtimeService);
  private readonly destroyRef = inject(DestroyRef);

  private map?: L.Map;
  private routeLayer = L.layerGroup();
  private hasFitBounds = false;

  ngAfterViewInit(): void {
    this.initializeMap();
    this.watchRealtimeUpdates();
    this.loadInitialSnapshot();
    void this.startRealtimeConnection();
  }

  ngOnDestroy(): void {
    this.routeLayer.remove();
    this.map?.remove();
    void this.realtimeService.stop();
  }

  reconnect(): void {
    this.errorMessage = '';
    void this.startRealtimeConnection();
  }

  clearFilter(): void {
    this.filterControl.setValue('');
  }

  trackByRouteId(index: number, route: LiveRoute): number {
    return route.routeId;
  }

  private initializeMap(): void {
    this.map = L.map(this.mapContainer.nativeElement, {
      zoomControl: true,
      preferCanvas: true
    }).setView([40.4093, 49.8671], 11);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors',
      maxZoom: 18
    }).addTo(this.map);

    this.routeLayer.addTo(this.map);
    queueMicrotask(() => this.map?.invalidateSize());
  }

  private loadInitialSnapshot(): void {
    this.apiService
      .getLiveRoutes()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (snapshot) => {
          this.isLoading = false;
          this.errorMessage = '';
          this.applySnapshot(snapshot, true);
        },
        error: (error: HttpErrorResponse) => {
          this.isLoading = false;
          this.errorMessage = error.error?.title ?? 'Failed to load live routes.';
        }
      });
  }

  private watchRealtimeUpdates(): void {
    this.filterControl.valueChanges
      .pipe(debounceTime(120), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.updateVisibleRoutes(true);
      });

    this.realtimeService.connectionState$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((state) => {
        this.connectionState = state;
      });

    this.realtimeService.updates$
      .pipe(
        filter((snapshot): snapshot is LiveRouteSnapshot => snapshot !== null),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((snapshot) => {
        this.errorMessage = '';
        this.applySnapshot(snapshot, false);
      });

    this.realtimeService.connectionState$
      .pipe(
        distinctUntilChanged(),
        filter((state) => state === 'connected'),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        if (this.latestSnapshot) {
          return;
        }

        this.loadInitialSnapshot();
      });
  }

  private async startRealtimeConnection(): Promise<void> {
    try {
      await this.realtimeService.start();
    } catch (error) {
      console.error('SignalR connection failed.', error);
    }
  }

  private applySnapshot(snapshot: LiveRouteSnapshot, fitBounds: boolean): void {
    this.latestSnapshot = snapshot;
    this.updateVisibleRoutes(fitBounds && !this.hasFitBounds);
  }

  private updateVisibleRoutes(fitBounds: boolean): void {
    const snapshot = this.latestSnapshot;

    if (!snapshot) {
      this.visibleRoutes = [];
      this.routeLayer.clearLayers();
      return;
    }

    const query = this.filterControl.value.trim().toLowerCase();
    this.visibleRoutes = query
      ? snapshot.routes.filter((route) => this.matchesRoute(route, query))
      : snapshot.routes;

    this.redrawRoutes(this.visibleRoutes, fitBounds);
  }

  private redrawRoutes(routes: readonly LiveRoute[], fitBounds: boolean): void {
    this.routeLayer.clearLayers();

    const bounds = L.latLngBounds([]);

    for (const route of routes) {
      const color = this.getRouteColor(route.routeNumber);

      for (const direction of route.directions) {
        const path = direction.path.map((point) => [point.lat, point.lng] as L.LatLngTuple);

        if (path.length < 2) {
          continue;
        }

        const style: L.PolylineOptions = {
          color,
          weight: 4,
          opacity: 0.88,
          dashArray: direction.directionTypeId === 2 ? '10 8' : undefined
        };

        const polyline = L.polyline(path, style);
        polyline.bindPopup(this.buildPopupContent(route, direction));
        polyline.on('mouseover', () => polyline.setStyle({ weight: 6, opacity: 1 }));
        polyline.on('mouseout', () => polyline.setStyle(style));

        polyline.addTo(this.routeLayer);
        bounds.extend(polyline.getBounds());
      }
    }

    if (fitBounds && bounds.isValid()) {
      this.map?.fitBounds(bounds, { padding: [24, 24] });
      this.hasFitBounds = true;
    }
  }

  private matchesRoute(route: LiveRoute, query: string): boolean {
    const haystack = [
      route.routeNumber,
      route.firstPoint,
      route.lastPoint,
      route.carrier,
      route.regionName ?? '',
      route.workingZoneType ?? ''
    ]
      .join(' ')
      .toLowerCase();

    return haystack.includes(query);
  }

  private buildPopupContent(route: LiveRoute, direction: LiveRouteDirection): string {
    const directionLabel = this.escapeHtml(direction.directionName);
    const routeNumber = this.escapeHtml(route.routeNumber);
    const firstPoint = this.escapeHtml(route.firstPoint);
    const lastPoint = this.escapeHtml(route.lastPoint);
    const carrier = this.escapeHtml(route.carrier);

    return `
      <div class="live-route-popup">
        <strong>Route ${routeNumber}</strong><br />
        ${firstPoint} to ${lastPoint}<br />
        Direction: ${directionLabel}<br />
        Carrier: ${carrier}<br />
        Stops: ${new Intl.NumberFormat('en-US').format(direction.stopCount)}
      </div>
    `;
  }

  getRouteColor(routeNumber: string): string {
    let hash = 0;

    for (const character of routeNumber) {
      hash = (hash * 31 + character.charCodeAt(0)) >>> 0;
    }

    return this.routePalette[hash % this.routePalette.length];
  }

  private escapeHtml(value: string | null | undefined): string {
    return (value ?? '')
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#39;');
  }
}
