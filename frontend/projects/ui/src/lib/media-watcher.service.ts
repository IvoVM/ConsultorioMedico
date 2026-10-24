import { Injectable, signal } from '@angular/core';

/** Same cutoff as the card layout and the collapsible sidebar. */
const mobileQuery = '(max-width: 1023px)';

@Injectable({ providedIn: 'root' })
export class MediaWatcherService {
  private readonly media = typeof matchMedia === 'function' ? matchMedia(mobileQuery) : null;
  readonly mobile = signal(this.media?.matches ?? false);

  constructor() {
    this.media?.addEventListener('change', (event) => this.mobile.set(event.matches));
  }
}
