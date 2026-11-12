import { Component, effect, signal } from '@angular/core';
import { TenantRole, clinicaSession } from 'sdk';
import { TabItem, UiTabs } from 'ui';
import { MetricsDashboard } from '../metrics/metrics-dashboard';
import { HomeView, homeViewLabels } from './models/home-view';
import { TodayBoard } from './today-board';

const storageKey = 'clinica.admin.home-view';

// The admin opens on how the clinic is doing; front desk opens on today's patient flow, which is its job.
@Component({
  imports: [MetricsDashboard, TodayBoard, UiTabs],
  template: `
    @if (isAdmin) {
      <ui-tabs class="mb-5" label="Vista del inicio" [tabs]="tabs" [active]="view()" (activeChange)="view.set($any($event))" />
      @if (view() === views.Metrics) {
        <metrics-dashboard />
      } @else {
        <today-board />
      }
    } @else {
      <today-board />
    }
  `,
})
export class HomePage {
  readonly isAdmin = clinicaSession.role() === TenantRole.TenantAdmin;
  readonly views = HomeView;
  readonly tabs: TabItem[] = [
    { key: HomeView.Metrics, label: homeViewLabels[HomeView.Metrics], icon: 'monitoring' },
    { key: HomeView.Today, label: homeViewLabels[HomeView.Today], icon: 'today' },
  ];
  readonly view = signal(readView());

  constructor() {
    effect(() => {
      const view = this.view();
      try {
        localStorage.setItem(storageKey, view);
      } catch {
        // Private windows can refuse storage; the tab simply is not remembered.
      }
    });
  }
}

function readView(): HomeView {
  try {
    return localStorage.getItem(storageKey) === HomeView.Today ? HomeView.Today : HomeView.Metrics;
  } catch {
    return HomeView.Metrics;
  }
}
