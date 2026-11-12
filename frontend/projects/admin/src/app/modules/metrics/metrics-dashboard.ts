import { Component, computed, effect, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AppointmentStatus, MetricKpiDto, MetricsDto, MetricsRange, MetricsService, clinicaSession, errorMessage } from 'sdk';
import {
  BarListRow,
  ChartKind,
  ChartPoint,
  ChartSeries,
  HeatmapRow,
  TabItem,
  UiBarList,
  UiChart,
  UiEmpty,
  UiHeatmap,
  UiIcon,
  UiSkeleton,
  UiStat,
  UiTabs,
  UiTone,
  greeting,
} from 'ui';
import { appointmentStatusLabels } from '../appointments/models/appointment-status-labels';
import { RankingView, Trend, metricsRangeLabels, rankingViewLabels, weekDays } from './models/metrics-labels';

const storageKey = 'clinica.admin.metrics-range';

type Tile = { label: string; icon: string; value: number | string; suffix: string; note: string; tone: UiTone };

const decimals = new Intl.NumberFormat('es-AR', { maximumFractionDigits: 1 });
const currency = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 });
const compactCurrency = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', notation: 'compact', maximumFractionDigits: 1 });
const shortDate = new Intl.DateTimeFormat('es-AR', { day: 'numeric', month: 'numeric' });
const longDate = new Intl.DateTimeFormat('es-AR', { weekday: 'long', day: 'numeric', month: 'long' });

// Three questions and nothing else: are we seeing patients, are we losing appointments, is the money coming in.
@Component({
  selector: 'metrics-dashboard',
  imports: [UiBarList, UiChart, UiEmpty, UiHeatmap, UiIcon, UiSkeleton, UiStat, UiTabs],
  templateUrl: './metrics-dashboard.html',
})
export class MetricsDashboard {
  private readonly metricsApi = inject(MetricsService);
  readonly hello = greeting(clinicaSession.name());
  readonly kinds = ChartKind;
  readonly tone = UiTone;
  readonly rankingViews = RankingView;
  readonly money = (value: number) => currency.format(value);
  readonly moneyAxis = (value: number) => compactCurrency.format(value);
  readonly ranges: TabItem[] = Object.values(MetricsRange).map((range) => ({ key: range, label: metricsRangeLabels[range] }));
  readonly rankingTabs: TabItem[] = Object.values(RankingView).map((view) => ({ key: view, label: rankingViewLabels[view] }));
  readonly range = signal(readRange());
  readonly ranking = signal(RankingView.Professionals);
  readonly data = signal<MetricsDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  private request = 0;

  readonly period = computed(() => {
    const data = this.data();
    if (!data) return '';
    return `Del ${shortDate.format(day(data.from))} al ${shortDate.format(day(data.to))}, comparado con los ${metricsRangeLabels[data.range]} anteriores`;
  });

  readonly weekly = computed(() => (this.data()?.bucketDays ?? 1) > 1);

  readonly tiles = computed((): Tile[] => {
    const data = this.data();
    if (!data) return [];
    const k = data.kpis;
    const last = data.billing
      ? { label: 'Cobrado', icon: 'payments', ...money(data.billing.collected) }
      : { label: 'Pacientes nuevos', icon: 'person_add', ...count(k.newPatients, Trend.UpIsGood) };
    return [
      { label: 'Atendidos', icon: 'task_alt', ...count(k.completed, Trend.UpIsGood) },
      { label: 'Ausentismo', icon: 'person_off', ...rate(k.noShowRate, Trend.DownIsGood) },
      { label: 'Ocupación de agenda', icon: 'donut_large', ...rate(k.occupancy, Trend.UpIsGood) },
      last,
    ];
  });

  readonly outstanding = computed(() => currency.format(this.data()?.billing?.outstanding ?? 0));

  readonly appointmentSeries: ChartSeries[] = [
    { key: 'completed', label: appointmentStatusLabels[AppointmentStatus.Completed], tone: UiTone.Ok },
    { key: 'pending', label: 'Por atender', tone: UiTone.Sky },
    { key: 'noShow', label: appointmentStatusLabels[AppointmentStatus.NoShow], tone: UiTone.Warn },
    { key: 'cancelled', label: appointmentStatusLabels[AppointmentStatus.Cancelled], tone: UiTone.Bad },
  ];

  readonly billingSeries: ChartSeries[] = [
    { key: 'invoiced', label: 'Facturado', tone: UiTone.Sky },
    { key: 'collected', label: 'Cobrado', tone: UiTone.Ok },
  ];

  readonly appointmentPoints = computed((): ChartPoint[] =>
    (this.data()?.series ?? []).map((bucket) => ({
      ...this.slot(bucket.date),
      values: { completed: bucket.completed, pending: bucket.pending, noShow: bucket.noShow, cancelled: bucket.cancelled },
    })),
  );

  readonly billingPoints = computed((): ChartPoint[] =>
    (this.data()?.billing?.series ?? []).map((bucket) => ({
      ...this.slot(bucket.date),
      values: { invoiced: bucket.invoiced, collected: bucket.collected },
    })),
  );

  readonly rankingRows = computed((): BarListRow[] => {
    const data = this.data();
    const rows = (this.ranking() === RankingView.Professionals ? data?.professionals : data?.specialties) ?? [];
    return rows.map((row) => ({
      label: row.label,
      value: row.completed,
      note: row.noShow ? `${row.noShow} ${row.noShow === 1 ? 'ausente' : 'ausentes'}` : '',
    }));
  });

  readonly heatmap = computed(() => {
    const cells = this.data()?.heatmap ?? [];
    if (!cells.length) return { rows: [] as HeatmapRow[], columns: [] as string[] };
    const hours = cells.map((cell) => cell.hour);
    const first = Math.min(...hours);
    const span = Array.from({ length: Math.max(...hours) - first + 1 }, (_, index) => first + index);
    const used = new Set(cells.map((cell) => cell.day));
    const rows = weekDays
      .filter((item, index) => index < 5 || used.has(item.day))
      .map((item) => ({
        label: item.label,
        values: span.map((hour) => cells.find((cell) => cell.day === item.day && cell.hour === hour)?.count ?? 0),
      }));
    return { rows, columns: span.map((hour) => `${hour}h`) };
  });

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

  pickRanking(key: string) {
    const view = Object.values(RankingView).find((item) => item === key);
    if (view) this.ranking.set(view);
  }

  private slot(date: string) {
    const value = day(date);
    return { label: shortDate.format(value), hint: this.weekly() ? `Semana del ${longDate.format(value)}` : longDate.format(value) };
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

// The API sends calendar days as yyyy-mm-dd; read them as local days, not UTC midnight.
function day(value: string) {
  const [year, month, date] = value.split('-').map(Number);
  return new Date(year, month - 1, date);
}

function trendTone(change: number, trend: Trend) {
  if (!change) return UiTone.Muted;
  return change > 0 === (trend === Trend.UpIsGood) ? UiTone.Ok : UiTone.Bad;
}

// The header already says what the comparison is against, so each tile only carries the arrow and the size.
function delta(kpi: MetricKpiDto, trend: Trend): Pick<Tile, 'note' | 'tone'> {
  const value = kpi.value ?? 0;
  const previous = kpi.previous;
  if (previous == null || previous === 0) return { note: '', tone: UiTone.Muted };
  const percent = ((value - previous) / previous) * 100;
  if (Math.abs(percent) < 0.05) return { note: '= igual', tone: UiTone.Muted };
  return { note: `${percent > 0 ? '▲' : '▼'} ${decimals.format(Math.abs(percent))} %`, tone: trendTone(percent, trend) };
}

function count(kpi: MetricKpiDto, trend: Trend) {
  return { value: kpi.value ?? 0, suffix: '', ...delta(kpi, trend) };
}

function money(kpi: MetricKpiDto) {
  return { value: currency.format(kpi.value ?? 0), suffix: '', ...delta(kpi, Trend.UpIsGood) };
}

// Rates compare in percentage points, since a percent of a percent reads wrong.
function rate(kpi: MetricKpiDto, trend: Trend) {
  if (kpi.value == null) return { value: '–', suffix: '', note: '', tone: UiTone.Muted };
  const base = { value: kpi.value, suffix: ' %' };
  if (kpi.previous == null) return { ...base, note: '', tone: UiTone.Muted };
  const points = Math.round((kpi.value - kpi.previous) * 10) / 10;
  if (!points) return { ...base, note: '= igual', tone: UiTone.Muted };
  return { ...base, note: `${points > 0 ? '▲' : '▼'} ${decimals.format(Math.abs(points))} pts`, tone: trendTone(points, trend) };
}
