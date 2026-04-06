import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { Subscription, debounceTime } from 'rxjs';

import { ApiService } from '../../services/api.service';
import {
  BusAnalyticsItem,
  BusAnalyticsQuery,
  BusAnalyticsSortBy
} from '../../models/bus-analytics.models';
import { DashboardStateComponent } from '../../shared/components/dashboard-state/dashboard-state.component';

@Component({
  selector: 'app-bus-analytics-page',
  standalone: true,
  imports: [
    DecimalPipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSortModule,
    MatTableModule,
    DashboardStateComponent
  ],
  templateUrl: './bus-analytics-page.component.html',
  styleUrl: './bus-analytics-page.component.scss'
})
export class BusAnalyticsPageComponent implements OnInit, OnDestroy {
  @ViewChild(MatPaginator)
  set paginator(value: MatPaginator | undefined) {
    this.paginatorSubscription?.unsubscribe();
    this.paginatorRef = value;

    if (!value) {
      return;
    }

    value.pageIndex = this.pageIndex;
    value.pageSize = this.pageSize;

    this.paginatorSubscription = value.page.subscribe((event) => {
      this.pageIndex = event.pageIndex;
      this.pageSize = event.pageSize;
      this.loadBusAnalytics();
    });
  }

  @ViewChild(MatSort)
  set sort(value: MatSort | undefined) {
    this.sortSubscription?.unsubscribe();
    this.sortRef = value;

    if (!value) {
      return;
    }

    value.active = this.sortActive;
    value.direction = this.sortDirection;

    this.sortSubscription = value.sortChange.subscribe((sort) => {
      this.sortActive = sort.active || 'date';
      this.sortDirection = (sort.direction || 'desc') as 'asc' | 'desc';
      this.resetToFirstPageOrLoad();
    });
  }

  readonly displayedColumns = [
    'date',
    'hour',
    'route',
    'operator',
    'totalCount',
    'bySmartCard',
    'byQr',
    'numberOfBusses'
  ];

  readonly pageSizeOptions = [10, 25, 50, 100];
  readonly filterForm = inject(FormBuilder).group({
    route: [''],
    operatorName: [''],
    date: [null as Date | null]
  });

  items: BusAnalyticsItem[] = [];
  total = 0;
  pageIndex = 0;
  pageSize = 25;
  sortActive = 'date';
  sortDirection: 'asc' | 'desc' = 'desc';
  isLoading = false;
  errorMessage = '';

  private readonly apiService = inject(ApiService);
  private readonly destroyRef = inject(DestroyRef);
  private paginatorRef?: MatPaginator;
  private sortRef?: MatSort;
  private paginatorSubscription?: Subscription;
  private sortSubscription?: Subscription;

  ngOnInit(): void {
    this.filterForm.valueChanges
      .pipe(debounceTime(250), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.resetToFirstPageOrLoad();
      });

    this.loadBusAnalytics();
  }

  ngOnDestroy(): void {
    this.paginatorSubscription?.unsubscribe();
    this.sortSubscription?.unsubscribe();
  }

  resetFilters(): void {
    this.filterForm.reset(
      {
        route: '',
        operatorName: '',
        date: null
      },
      {
        emitEvent: false
      }
    );

    this.pageIndex = 0;

    if (this.paginatorRef) {
      this.paginatorRef.pageIndex = 0;
    }

    this.loadBusAnalytics();
  }

  trackByRouteAndHour(index: number, item: BusAnalyticsItem): string {
    return `${item.date}-${item.hour}-${item.route}-${item.operator}-${index}`;
  }

  private resetToFirstPageOrLoad(): void {
    if (this.pageIndex > 0) {
      this.pageIndex = 0;

      if (this.paginatorRef) {
        this.paginatorRef.firstPage();
        return;
      }

      this.loadBusAnalytics();
      return;
    }

    this.loadBusAnalytics();
  }

  private loadBusAnalytics(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.apiService
      .getBusAnalytics(this.buildQuery())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.items = response.items;
          this.total = response.total;
          this.isLoading = false;
        },
        error: (error: HttpErrorResponse) => {
          this.items = [];
          this.total = 0;
          this.isLoading = false;
          this.errorMessage = error.error?.title ?? 'Failed to load bus analytics.';
        }
      });
  }

  private buildQuery(): BusAnalyticsQuery {
    const filters = this.filterForm.getRawValue();

    return {
      page: this.pageIndex + 1,
      pageSize: this.pageSize,
      route: filters.route?.trim() || undefined,
      operatorName: filters.operatorName?.trim() || undefined,
      date: filters.date ? this.toDateOnlyString(filters.date) : undefined,
      sortBy: this.mapSortField(this.sortActive),
      sortDir: this.sortDirection
    };
  }

  private mapSortField(active: string | undefined): BusAnalyticsSortBy {
    switch (active) {
      case 'hour':
        return 'hour';
      case 'route':
        return 'route';
      case 'operator':
        return 'operator';
      case 'totalCount':
        return 'totalCount';
      case 'date':
      default:
        return 'date';
    }
  }

  private toDateOnlyString(date: Date): string {
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');

    return `${year}-${month}-${day}`;
  }
}
