import { Component, computed, effect, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { MetricsDto, MetricsRange, MetricsService, StatusShareDto, clinicaSession, errorMessage } from 'sdk';
import {
  BarListRow,
  ChartHue,
  ChartKind,
  ChartPoint,
  ChartSeries,
  ChartSlice,
  TabItem,
  UiBarList,
  UiChart,
  UiDonut,
  UiEmpty,
  UiIcon,
  UiSkeleton,
  UiStat,
  UiTabs,
  UiTone,
  greeting, UiHelp } from 'ui';
import { appointmentStatusLabels } from '../appointments/models/appointment-status-labels';
import { metricsRangeLabels, statusSlices } from './models/metrics-labels';

const storageKey = 'clinica.admin.metrics-range';

type Tile = { label: string; icon: string; value: number | string; suffix: string; note: string; tone: UiTone };

const currency = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 });
const compactCurrency = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', notation: 'compact', maximumFractionDigits: 1 });
const shortDate = new Intl.DateTimeFormat('es-AR', { day: 'numeric', month: 'numeric' });
const longDate = new Intl.DateTimeFormat('es-AR', { weekday: 'long', day: 'numeric', month: 'long' });

// What happened to the period's appointments, who saw the patients, and how much was invoiced and collected.
@Component({
  selector: 'metrics-dashboard',
  imports: [UiHelp, UiBarList, UiChart, UiDonut, UiEmpty, UiIcon, UiSkeleton, UiStat, UiTabs],
  templateUrl: './metrics-dashboard.html',
})
export class MetricsDashboard {
  private readonly metricsApi = inject(MetricsService);
  readonly hello = greeting(clinicaSession.name());
  readonly kinds = ChartKind;
  readonly hue = ChartHue;
  readonly money = (value: number) => amount(value);
  readonly moneyAxis = (value: number) => compactCurrency.format(value);
  readonly ranges: TabItem[] = Object.values(MetricsRange).map((range) => ({ key: range, label: metricsRangeLabels[range] }));
  readonly range = signal(readRange());
  readonly data = signal<MetricsDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  private request = 0;

  readonly period = computed(() => {
    const data = this.data();
    if (!data) return '';
    if (data.from === data.to) return `Hoy, ${longDate.format(day(data.to))}`;
    return `Del ${shortDate.format(day(data.from))} al ${shortDate.format(day(data.to))}`;
  });

  readonly tiles = computed((): Tile[] => {
    const summary = this.data()?.appointments;
    if (!summary) return [];
    const tile = (label: string, icon: string, share: StatusShareDto, tone: UiTone): Tile => ({
      label,
      icon,
      value: share.percent ?? '–',
      suffix: share.percent == null ? '' : ' %',
      note: `${share.count} de ${summary.total}`,
      tone: share.count ? tone : UiTone.Muted,
    });
    return [
      tile('Atendidos', 'task_alt', summary.completed, UiTone.Ok),
      tile('Ausentes', 'person_off', summary.noShow, UiTone.Warn),
      tile('Cancelados', 'event_busy', summary.cancelled, UiTone.Bad),
      tile('Cambiados de horario', 'update', summary.rescheduled, UiTone.Muted),
    ];
  });

  readonly statuses = computed((): ChartSlice[] => {
    const counts = this.data()?.appointments.byStatus ?? [];
    return statusSlices.map(({ status, hue }) => ({
      label: appointmentStatusLabels[status],
      value: counts.find((row) => row.status === status)?.count ?? 0,
      tone: hue,
    }));
  });

  readonly professionals = computed((): BarListRow[] =>
    (this.data()?.professionals ?? []).map((row) => ({ label: row.label, value: row.completed })),
  );

  readonly billing = computed(() => {
    const billing = this.data()?.billing;
    if (!billing) return null;
    return {
      invoiced: amount(billing.invoiced),
      collected: amount(billing.collected),
      outstanding: amount(billing.outstanding),
      weekly: billing.bucketDays > 1,
      points: billing.series.map((bucket): ChartPoint => {
        const value = day(bucket.date);
        return {
          label: shortDate.format(value),
          hint: billing.bucketDays > 1 ? `Semana del ${longDate.format(value)}` : longDate.format(value),
          values: { invoiced: bucket.invoiced, collected: bucket.collected },
        };
      }),
    };
  });

  readonly billingSeries: ChartSeries[] = [
    { key: 'invoiced', label: 'Facturado', tone: ChartHue.Sky },
    { key: 'collected', label: 'Cobrado', tone: ChartHue.Ok },
  ];

  constructor() {
    effect(() => {
      const range = this.range();
      try {
        localStorage.setItem(storageKey, range);
      } catch {
        // Private windows can refuse storage; the range simply is not remembered.
      }
      void this.load(range);
    });
  }

  pickRange(key: string) {
    const range = Object.values(MetricsRange).find((item) => item === key);
    if (range) this.range.set(range);
  }

  private async load(range: MetricsRange) {
    const request = ++this.request;
    this.loading.set(true);
    this.error.set('');
    try {
      const data = await firstValueFrom(this.metricsApi.metrics({ range }));
      if (request === this.request) this.data.set(data);
    } catch (error) {
      if (request === this.request) this.error.set(errorMessage(error));
    } finally {
      if (request === this.request) this.loading.set(false);
    }
  }
}

function readRange(): MetricsRange {
  try {
    const saved = localStorage.getItem(storageKey);
    return Object.values(MetricsRange).find((range) => range === saved) ?? MetricsRange.Month;
  } catch {
    return MetricsRange.Month;
  }
}

// A missing amount reads as $ 0, never as $ NaN.
function amount(value: number | null | undefined) {
  return currency.format(Number.isFinite(value) ? (value as number) : 0);
}

// The API sends calendar days as yyyy-mm-dd; read them as local days, not UTC midnight.
function day(value: string) {
  const [year, month, date] = value.split('-').map(Number);
  return new Date(year, month - 1, date);
}
