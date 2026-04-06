export interface LiveRoutePoint {
  lat: number;
  lng: number;
}

export interface LiveRouteDirection {
  directionTypeId: number;
  directionName: string;
  stopCount: number;
  path: LiveRoutePoint[];
}

export interface LiveRoute {
  routeId: number;
  routeNumber: string;
  carrier: string;
  firstPoint: string;
  lastPoint: string;
  routeLengthKm: number;
  tariff: string | null;
  durationMinutes: number;
  regionName: string | null;
  workingZoneType: string | null;
  directions: LiveRouteDirection[];
}

export interface LiveRouteSnapshot {
  lastUpdatedUtc: string;
  provider: string;
  totalRoutes: number;
  routes: LiveRoute[];
}

export type LiveRoutesConnectionState =
  | 'disconnected'
  | 'connecting'
  | 'connected'
  | 'reconnecting';
