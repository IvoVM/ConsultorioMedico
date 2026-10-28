import { AfterViewInit, Component, ElementRef, OnDestroy, inject } from '@angular/core';
import { ToastService } from './ui-toasts.service';

@Component({
  selector: 'ui-toasts',
  templateUrl: './ui-toasts.component.html',
  styleUrl: './ui-toasts.component.css',
})
export class UiToasts implements AfterViewInit, OnDestroy {
  private readonly host = inject(ElementRef<HTMLElement>);
  readonly toasts = inject(ToastService);

  ngAfterViewInit() {
    document.body.appendChild(this.host.nativeElement);
  }

  ngOnDestroy() {
    this.host.nativeElement.remove();
  }
}
