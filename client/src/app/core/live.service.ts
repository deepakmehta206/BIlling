import { Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class LiveService {
  readonly tick = signal(0);
  private hub: HubConnection | null = null;
  private houseId: string | null = null;

  connect(restaurantId: string) {
    if (this.houseId === restaurantId && this.hub) {
      return;
    }

    void this.hub?.stop();
    this.houseId = restaurantId;
    const hub = new HubConnectionBuilder()
      .withUrl(environment.hubUrl, { withCredentials: false })
      .withAutomaticReconnect()
      .build();
    hub.on('changed', () => this.tick.update((value) => value + 1));
    this.hub = hub;
    void hub.start().then(() => hub.invoke('JoinHouse', restaurantId));
  }
}
