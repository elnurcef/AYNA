import type { Feature, FeatureCollection, MultiPolygon, Polygon } from 'geojson';

export type DemographicsLevel = 'micro' | 'meso' | 'macro';
export type DemographicsMetric = 'population' | 'jobs';

export interface DemographicsProperties {
  regionName: string;
  value: number;
}

export interface DemographicsQuery {
  level: DemographicsLevel;
  metric: DemographicsMetric;
}

export type DemographicsFeature = Feature<Polygon | MultiPolygon, DemographicsProperties>;
export type DemographicsFeatureCollection = FeatureCollection<
  Polygon | MultiPolygon,
  DemographicsProperties
>;
