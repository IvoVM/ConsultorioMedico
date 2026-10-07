import { Component, inject, OnDestroy, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ClinicService } from 'sdk';
import { readClinicName } from './clinic-name';

@Component({
  imports: [],
  templateUrl: './welcome.html',
  styleUrl: './welcome.css',
})
export class WelcomePage implements OnDestroy {
  private readonly clinic = inject(ClinicService);
  private readonly router = inject(Router);
  private timer = 0;
  readonly clinicName = signal('Consultorio');
  readonly chosen = signal<'patient' | 'staff' | null>(null);

  constructor() {
    void readClinicName(this.clinic).then((name) => this.clinicName.set(name));
  }

  open(side: 'patient' | 'staff', event: Event) {
    event.preventDefault();
    if (this.chosen()) return;
    const next = side === 'patient' ? '/reservar' : '/ingreso/empleado';
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduce) {
      void this.router.navigateByUrl(next);
      return;
    }
    this.chosen.set(side);
    this.timer = window.setTimeout(() => void this.router.navigateByUrl(next), 780);
  }

  ngOnDestroy() {
    window.clearTimeout(this.timer);
  }
}
