import { Inject, Injectable, NgZone, inject } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel
} from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';

import { API_BASE_URL } from '../core/config/api-base-url.token';
import { LiveRouteSnapshot, LiveRoutesConnectionState } from '../models/live-routes.models';

@Injectable({
  providedIn: 'root'
})
export class LiveRoutesRealtimeService {
  private readonly zone = inject(NgZone);

  private readonly connectionStateSubject =
    new BehaviorSubject<LiveRoutesConnectionState>('disconnected');
  private readonly updatesSubject = new BehaviorSubject<LiveRouteSnapshot | null>(null);

  readonly connectionState$ = this.connectionStateSubject.asObservable();
  readonly updates$ = this.updatesSubject.asObservable();

  private connection: HubConnection | null = null;
  private startPromise: Promise<void> | null = null;

  constructor(@Inject(API_BASE_URL) private readonly apiBaseUrl: string) {}

  async start(): Promise<void> {
    const connection = this.getOrCreateConnection();

    if (
      connection.state === HubConnectionState.Connected ||
      connection.state === HubConnectionState.Connecting ||
      connection.state === HubConnectionState.Reconnecting
    ) {
      return this.startPromise ?? Promise.resolve();
    }

    if (this.startPromise) {
      return this.startPromise;
    }

    this.connectionStateSubject.next('connecting');

    this.startPromise = connection
      .start()
      .then(() => {
        this.zone.run(() => this.connectionStateSubject.next('connected'));
      })
      .catch((error) => {
        this.zone.run(() => this.connectionStateSubject.next('disconnected'));
        throw error;
      })
      .finally(() => {
        this.startPromise = null;
      });

    return this.startPromise;
  }

  async stop(): Promise<void> {
    if (!this.connection) {
      this.connectionStateSubject.next('disconnected');
      return;
    }

    if (this.startPromise) {
      try {
        await this.startPromise;
      } catch {
        // The connection state is already updated in start().
      }
    }

    if (this.connection.state !== HubConnectionState.Disconnected) {
      await this.connection.stop();
    }

    this.connectionStateSubject.next('disconnected');
  }

  private getOrCreateConnection(): HubConnection {
    if (this.connection) {
      return this.connection;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(`${this.apiBaseUrl}/hubs/routes`, {
        withCredentials: true
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('routesUpdated', (snapshot: LiveRouteSnapshot) => {
      this.zone.run(() => {
        this.updatesSubject.next(snapshot);
      });
    });

    connection.onreconnecting(() => {
      this.zone.run(() => this.connectionStateSubject.next('reconnecting'));
    });

    connection.onreconnected(() => {
      this.zone.run(() => this.connectionStateSubject.next('connected'));
    });

    connection.onclose(() => {
      this.zone.run(() => this.connectionStateSubject.next('disconnected'));
    });

    this.connection = connection;
    return connection;
  }
}
