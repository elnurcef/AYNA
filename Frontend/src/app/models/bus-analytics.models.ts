export type SortDirection = 'asc' | 'desc';

export type BusAnalyticsSortBy =
  | 'date'
  | 'hour'
  | 'route'
  | 'totalCount'
  | 'operator';

export interface BusAnalyticsQuery {
  page: number;
  pageSize: number;
  route?: string;
  operatorName?: string;
  date?: string;
  sortBy: BusAnalyticsSortBy;
  sortDir: SortDirection;
}

export interface BusAnalyticsItem {
  date: string;
  hour: number;
  route: string;
  totalCount: number;
  bySmartCard: number;
  byQr: number;
  numberOfBusses: number;
  operator: string;
}

export interface PagedResponse<T> {
  total: number;
  items: T[];
}

export type BusAnalyticsResponse = PagedResponse<BusAnalyticsItem>;
