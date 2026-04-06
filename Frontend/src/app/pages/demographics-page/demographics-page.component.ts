import { DecimalPipe, TitleCasePipe } from '@angular/common';
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
import { MatButtonToggleChange, MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import * as L from 'leaflet';
import type { GeoJsonObject } from 'geojson';

import { DemographicsFeature, DemographicsFeatureCollection } from '../../models/demographics.models';
import type { DemographicsLevel, DemographicsMetric } from '../../models/demographics.models';
import { ApiService } from '../../services/api.service';
import { DashboardStateComponent } from '../../shared/components/dashboard-state/dashboard-state.component';

interface LegendStep {
  color: string;
  from: number;
  to: number;
}

@Component({
  selector: 'app-demographics-page',
  standalone: true,
  imports: [
    DecimalPipe,
    TitleCasePipe,
    MatButtonToggleModule,
    MatCardModule,
    DashboardStateComponent
  ],
  templateUrl: './demographics-page.component.html',
  styleUrl: './demographics-page.component.scss'
})
export class DemographicsPageComponent implements AfterViewInit, OnDestroy {
  @ViewChild('mapContainer', { static: true }) private mapContainer!: ElementRef<HTMLDivElement>;

  readonly levels: Array<{ label: string; value: DemographicsLevel }> = [
    { label: 'Micro', value: 'micro' },
    { label: 'Meso', value: 'meso' },
    { label: 'Macro', value: 'macro' }
  ];

  readonly metrics: Array<{ label: string; value: DemographicsMetric }> = [
    { label: 'Population', value: 'population' },
    { label: 'Jobs', value: 'jobs' }
  ];

  selectedLevel: DemographicsLevel = 'meso';
  selectedMetric: DemographicsMetric = 'population';
  featureCount = 0;
  isLoading = false;
  errorMessage = '';

  private readonly apiService = inject(ApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly colorRamp = ['#ecfeff', '#a5f3fc', '#67e8f9', '#06b6d4', '#0f766e'];

  private map?: L.Map;
  private baseTileLayer?: L.TileLayer;
  private demographicsLayer?: L.GeoJSON;
  private legendControl?: L.Control;
  private legendContainer?: HTMLDivElement;
  private legendSteps: LegendStep[] = [];

  ngAfterViewInit(): void {
    this.initializeMap();
    this.loadDemographics();
  }

  ngOnDestroy(): void {
    this.demographicsLayer?.remove();
    this.legendControl?.remove();
    this.map?.remove();
  }

  onLevelChange(event: MatButtonToggleChange): void {
    if (!event.value || event.value === this.selectedLevel) {
      return;
    }

    this.selectedLevel = event.value as DemographicsLevel;
    this.loadDemographics();
  }

  onMetricChange(event: MatButtonToggleChange): void {
    if (!event.value || event.value === this.selectedMetric) {
      return;
    }

    this.selectedMetric = event.value as DemographicsMetric;
    this.loadDemographics();
  }

  private initializeMap(): void {
    this.map = L.map(this.mapContainer.nativeElement, {
      zoomControl: true,
      preferCanvas: true
    }).setView([40.4093, 49.8671], 10);

    this.baseTileLayer = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors',
      maxZoom: 18
    }).addTo(this.map);

    this.createLegend();
  }

  private loadDemographics(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.apiService
      .getDemographics({
        level: this.selectedLevel,
        metric: this.selectedMetric
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (featureCollection) => {
          this.featureCount = featureCollection.features.length;
          this.renderLayer(featureCollection);
          this.isLoading = false;
        },
        error: (error: HttpErrorResponse) => {
          this.featureCount = 0;
          this.clearDemographicsLayer();
          this.legendSteps = [];
          this.updateLegend();
          this.errorMessage = error.error?.title ?? 'Failed to load demographics data.';
          this.isLoading = false;
        }
      });
  }

  private renderLayer(featureCollection: DemographicsFeatureCollection): void {
    this.clearDemographicsLayer();

    const values = featureCollection.features
      .map((feature) => feature.properties?.value ?? 0)
      .filter((value) => Number.isFinite(value))
      .sort((a, b) => a - b);

    this.legendSteps = this.buildLegendSteps(values);
    this.updateLegend();

    if (featureCollection.features.length === 0) {
      return;
    }

    this.demographicsLayer = L.geoJSON(featureCollection as GeoJsonObject, {
      style: (feature) => this.getFeatureStyle(feature as DemographicsFeature),
      onEachFeature: (feature, layer) =>
        this.enhanceFeature(feature as DemographicsFeature, layer as L.Path)
    }).addTo(this.map!);

    const bounds = this.demographicsLayer.getBounds();
    if (bounds.isValid()) {
      this.map!.fitBounds(bounds, {
        padding: [24, 24]
      });
    }

    queueMicrotask(() => this.map?.invalidateSize());
  }

  private clearDemographicsLayer(): void {
    this.demographicsLayer?.remove();
    this.demographicsLayer = undefined;
  }

  private getFeatureStyle(feature: DemographicsFeature): L.PathOptions {
    const value = feature.properties?.value ?? 0;

    return {
      color: '#164e63',
      weight: 1,
      opacity: 0.9,
      fillColor: this.getColorForValue(value),
      fillOpacity: 0.78
    };
  }

  private enhanceFeature(feature: DemographicsFeature, layer: L.Path): void {
    const regionName = feature.properties?.regionName ?? 'Unknown region';
    const value = feature.properties?.value ?? 0;
    const metricLabel = this.selectedMetric === 'jobs' ? 'Jobs' : 'Population';
    const popupContent = `
      <div class="map-popup">
        <strong>${this.escapeHtml(regionName)}</strong><br />
        ${metricLabel}: ${this.formatValue(value)}
      </div>
    `;

    layer.bindTooltip(popupContent, {
      direction: 'top',
      sticky: true,
      opacity: 0.92
    });

    layer.bindPopup(popupContent, {
      maxWidth: 240
    });

    layer.on({
      mouseover: () => {
        layer.setStyle({
          weight: 2,
          color: '#082f49',
          fillOpacity: 0.92
        });

        if ('bringToFront' in layer) {
          layer.bringToFront();
        }
      },
      mouseout: () => {
        this.demographicsLayer?.resetStyle(layer);
      },
      click: () => {
        if ('getBounds' in layer) {
          this.map?.fitBounds((layer as L.Polygon).getBounds(), {
            maxZoom: 12,
            padding: [24, 24]
          });
        }

        layer.openPopup();
      }
    });
  }

  private buildLegendSteps(values: number[]): LegendStep[] {
    if (values.length === 0) {
      return [];
    }

    const min = values[0];
    const max = values[values.length - 1];

    if (min === max) {
      return [
        {
          color: this.colorRamp[this.colorRamp.length - 1],
          from: min,
          to: max
        }
      ];
    }

    const stepSize = (max - min) / this.colorRamp.length;

    return this.colorRamp.map((color, index) => ({
      color,
      from: min + stepSize * index,
      to: index === this.colorRamp.length - 1 ? max : min + stepSize * (index + 1)
    }));
  }

  private getColorForValue(value: number): string {
    if (this.legendSteps.length === 0) {
      return '#cbd5e1';
    }

    const match = this.legendSteps.find((step, index) => {
      if (index === this.legendSteps.length - 1) {
        return value >= step.from && value <= step.to;
      }

      return value >= step.from && value < step.to;
    });

    return match?.color ?? this.legendSteps[this.legendSteps.length - 1].color;
  }

  private createLegend(): void {
    if (!this.map) {
      return;
    }

    const legendControl = new L.Control({ position: 'bottomright' });

    legendControl.onAdd = () => {
      this.legendContainer = L.DomUtil.create('div', 'leaflet-demographics-legend');
      L.DomEvent.disableClickPropagation(this.legendContainer);
      this.updateLegend();
      return this.legendContainer;
    };

    legendControl.addTo(this.map);
    this.legendControl = legendControl;
  }

  private updateLegend(): void {
    if (!this.legendContainer) {
      return;
    }

    const metricLabel = this.selectedMetric === 'jobs' ? 'Jobs' : 'Population';
    const levelLabel = this.selectedLevel.charAt(0).toUpperCase() + this.selectedLevel.slice(1);

    const rows =
      this.legendSteps.length === 0
        ? '<p class="legend-empty">No data loaded</p>'
        : this.legendSteps
            .map(
              (step) => `
                <div class="legend-row">
                  <span class="legend-swatch" style="background:${step.color}"></span>
                  <span>${this.formatRange(step.from, step.to)}</span>
                </div>
              `
            )
            .join('');

    this.legendContainer.innerHTML = `
      <div class="legend-title">${metricLabel} by ${levelLabel}</div>
      ${rows}
    `;
  }

  private formatRange(from: number, to: number): string {
    if (Math.abs(from - to) < Number.EPSILON) {
      return this.formatValue(from);
    }

    return `${this.formatValue(from)} - ${this.formatValue(to)}`;
  }

  private formatValue(value: number): string {
    return new Intl.NumberFormat('en-US', {
      maximumFractionDigits: 0
    }).format(value);
  }

  private escapeHtml(value: string): string {
    return value
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#39;');
  }
}
