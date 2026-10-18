import { AfterViewInit, Component, DestroyRef, ElementRef, inject, signal, viewChild } from '@angular/core';
import { UiIcon } from './ui-icon.component';

@Component({
  selector: 'ui-table',
  imports: [UiIcon],
  host: { '[class.is-scrolled]': 'scrolled()' },
  templateUrl: './ui-table.component.html',
  styleUrl: './ui-table.component.css',
})
export class UiTable implements AfterViewInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly scroller = viewChild.required<ElementRef<HTMLElement>>('scroller');
  readonly overflow = signal(false);
  readonly canLeft = signal(false);
  readonly canRight = signal(false);
  readonly scrolled = signal(false);

  ngAfterViewInit() {
    const el = this.scroller().nativeElement;
    const measure = () => this.measure();
    const observer = new ResizeObserver(measure);
    observer.observe(el);
    const table = el.querySelector('table');
    if (table) observer.observe(table);
    const mutations = new MutationObserver(measure);
    mutations.observe(el, { childList: true, subtree: true, characterData: true });
    el.addEventListener('scroll', measure, { passive: true });
    measure();
    this.destroyRef.onDestroy(() => {
      observer.disconnect();
      mutations.disconnect();
      el.removeEventListener('scroll', measure);
    });
  }

  nudge(direction: -1 | 1) {
    const el = this.scroller().nativeElement;
    el.scrollBy({ left: direction * Math.max(180, Math.round(el.clientWidth * 0.7)), behavior: 'smooth' });
  }

  private measure() {
    const el = this.scroller().nativeElement;
    const max = el.scrollWidth - el.clientWidth;
    this.overflow.set(max > 2);
    this.canLeft.set(el.scrollLeft > 2);
    this.canRight.set(el.scrollLeft < max - 2);
    this.scrolled.set(el.scrollLeft > 2);
  }
}
