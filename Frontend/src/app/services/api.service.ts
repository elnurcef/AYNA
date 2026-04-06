import { HttpClient, HttpParams } from '@angular/common/http';
import { Inject, Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../core/config/api-base-url.token';
import { BusAnalyticsQuery, BusAnalyticsResponse } from '../models/bus-analytics.models';
import { DemographicsFeatureCollection, DemographicsQuery } from '../models/demographics.models';
import { LiveRouteSnapshot } from '../models/live-routes.models';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly http = inject(HttpClient);

  constructor(@Inject(API_BASE_URL) private readonly apiBaseUrl: string) {}

  getBusAnalytics(query: BusAnalyticsQuery): Observable<BusAnalyticsResponse> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize)
      .set('sortBy', query.sortBy)
      .set('sortDir', query.sortDir);

    if (query.route) {
      params = params.set('route', query.route);
    }

    if (query.operatorName) {
      params = params.set('operatorName', query.operatorName);
    }

    if (query.date) {
      params = params.set('date', query.date);
    }

    return this.http.get<BusAnalyticsResponse>(`${this.apiBaseUrl}/api/bus-analytics`, { params });
  }

  getDemographics(query: DemographicsQuery): Observable<DemographicsFeatureCollection> {
    const params = new HttpParams()
      .set('level', query.level)
      .set('metric', query.metric);

    return this.http.get<DemographicsFeatureCollection>(`${this.apiBaseUrl}/api/demographics`, {
      params
    });
  }

  getLiveRoutes(): Observable<LiveRouteSnapshot> {
    return this.http.get<LiveRouteSnapshot>(`${this.apiBaseUrl}/api/routes/live`);
  }
}
