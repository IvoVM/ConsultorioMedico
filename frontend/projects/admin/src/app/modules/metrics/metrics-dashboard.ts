import { Component, computed, effect, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AppointmentStatus, MetricKpiDto, MetricsDto, MetricsRange, MetricsService, PaymentMethod, clinicaSession, errorMessage } from 'sdk';
import {
  BarListRow,
  ChartKind,
  ChartPoint,
  ChartSeries,
  ChartSlice,
  HeatmapRow,
  TabItem,
  UiBarList,
  UiChart,
  UiDonut,
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
import { paymentMethodLabels } from '../invoices/models/invoice-labels';
import { Trend, metricsRangeLabels, weekDays } from './models/metrics-labels';

const storageKey = 'clinica.admin.metrics-range';

type Tile = { label: string; icon: string; value: number | string; suffix: string; note: string; tone: UiTone };

const decimals = new Intl.NumberFormat('es-AR', { maximumFractionDigits: 1 });
const currency = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 });
const compactCurrency = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', notation: 'compact', maximumFractionDigits: 1 });
const shortDate = new Intl.DateTimeFormat('es-AR', { day: 'numeric', month: 'numeric' });
const longDate = new Intl.DateTimeFormat('es-AR', { weekday: 'long', day: 'numeric', month: 'long' });

@Component({
  selector: 'metrics-dashboard',
  imports: [UiBarList, UiChart, UiDonut, UiEmpty, UiHeatmap, UiIcon, UiSkeleton, UiStat, UiTabs],
  templateUrl: './metrics-dashboard.html',
})
export class MetricsDashboard {
  private readonly metricsApi = inject(MetricsService);
  readonly hello = greeting(clinicaSession.name());
  readonly kinds = ChartKind;
  readonly tone = UiTone;
  readonly money = (value: number) => currency.format(value);
  readonly moneyAxis = (value: number) => compactCurrency.format(value);
  readonly ranges: TabItem[] = Object.values(MetricsRange).map((range) => ({ key: range, label: metricsRangeLabels[range] }));
  readonly range = signal(readRange());
  readonly data = signal<MetricsDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  private request = 0;

  readonly period = computed(() => {
    const data = this.data();
    return data ? `Del ${shortDate.format(day(data.from))} al ${shortDate.format(day(data.to))}` : '';
  });

  readonly weekly = computed(() => (this.data()?.bucketDays ?? 1) > 1);

  readonly tiles = computed((): Tile[] => {
    const data = this.data();
    if (!data) return [];
    const k = data.kpis;
    const requests = k.requestApproval.value;
    return [
      tile('Turnos', 'event', k.appointments, Trend.UpIsGood),
      tile('Atendidos', 'task_alt', k.completed, Trend.UpIsGood),
      rate('Ausentismo', 'person_off', k.noShowRate, Trend.DownIsGood),
      rate('Cancelación', 'event_busy', k.cancellationRate, Trend.DownIsGood),
      tile('Pacientes atendidos', 'group', k.patients, Trend.UpIsGood),
      tile('Pacientes nuevos', 'person_add', k.newPatients, Trend.UpIsGood),
      rate('Ocupación de agenda', 'donut_large', k.occupancy, Trend.UpIsGood),
      {
        label: 'Solicitudes web',
        icon: 'inbox',
        value: k.requests.value ?? 0,
        suffix: '',
        note: requests == null ? 'sin resolver' : `${decimals.format(requests)} % aprobadas`,
        tone: UiTone.Muted,
      },
    ];
  });

  readonly billingTiles = computed((): Tile[] => {
    const billing = this.data()?.billing;
    if (!billing) return [];
    return [
      money('Facturado', 'receipt_long', billing.invoiced),
      money('Cobrado', 'payments', billing.collected),
      money('Ticket promedio', 'sell', billing.averageTicket),
      {
        label: 'Pendiente de cobro',
        icon: 'hourglass_top',
        value: currency.format(billing.outstanding),
        suffix: '',
        note: 'a hoy, de todos los períodos',
        tone: billing.outstanding > 0 ? UiTone.Warn : UiTone.Ok,
      },
    ];
  });

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

  readonly professionals = computed(() => rankRows(this.data()?.professionals ?? []));
  readonly specialties = computed(() => rankRows(this.data()?.specialties ?? []));
  readonly insurers = computed((): BarListRow[] => (this.data()?.coverage.insurers ?? []).map((row) => ({ label: row.label, value: row.count })));

  readonly coverage = computed((): ChartSlice[] => {
    const coverage = this.data()?.coverage;
    if (!coverage) return [];
    return [
      { label: 'Obra social', value: coverage.insured, tone: UiTone.Sky },
      { label: 'Particular', value: coverage.private, tone: UiTone.Ok },
    ];
  });

  readonly coverageTotal = computed(() => this.coverage().reduce((sum, slice) => sum + slice.value, 0));

  readonly payments = computed((): ChartSlice[] => {
    const tones: Record<PaymentMethod, UiTone> = {
      [PaymentMethod.Cash]: UiTone.Ok,
      [PaymentMethod.Transfer]: UiTone.Sky,
      [PaymentMethod.Card]: UiTone.Warn,
    };
    return (this.data()?.billing?.payments ?? []).map((row) => ({ label: paymentMethodLabels[row.method], value: row.amount, tone: tones[row.method] }));
  });

  readonly paymentsTotal = computed(() => currency.format(this.payments().reduce((sum, slice) => sum + slice.value, 0)));

  readonly heatmap = computed(() => {
    const cells = this.data()?.heatmap ?? [];
    if (!cells.length) return { rows: [] as HeatmapRow[], columns: [] as string[] };
    const hours = cells.map((cell) => cell.hour);
    const first = Math.min(...hours);
    const last = Math.max(...hours);
    const span = Array.from({ length: last - first + 1 }, (_, index) => first + index);
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
    if (Object.values(MetricsRange).includes(key as MetricsRange)) this.range.set(key as MetricsRange);
  }

  private slot(date: string) {
    const value = day(date);
    return this.weekly()
      ? { label: shortDate.format(value), hint: `Semana del ${longDate.format(value)}` }
      : { label: shortDate.format(value), hint: longDate.format(value) };
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

function rankRows(rows: { label: string; completed: number; noShow: number }[]): BarListRow[] {
  return rows.map((row) => ({
    label: row.label,
    value: row.completed,
    note: row.noShow ? `${row.noShow} ${row.noShow === 1 ? 'ausente' : 'ausentes'}` : '',
  }));
}

function trendTone(change: number, trend: Trend) {
  if (!change) return UiTone.Muted;
  return change > 0 === (trend === Trend.UpIsGood) ? UiTone.Ok : UiTone.Bad;
}

function arrow(change: number) {
  return change > 0 ? '▲' : '▼';
}

// Counts and amounts compare as a percent change against the previous period.
function change(kpi: MetricKpiDto, trend: Trend): Pick<Tile, 'note' | 'tone'> {
  const value = kpi.value ?? 0;
  const previous = kpi.previous;
  if (previous == null) return { note: 'sin período previo', tone: UiTone.Muted };
  if (value === previous) return { note: 'igual que antes', tone: UiTone.Muted };
  if (previous === 0) return { note: 'antes no había', tone: trendTone(value, trend) };
  const percent = ((value - previous) / previous) * 100;
  return { note: `${arrow(percent)} ${decimals.format(Math.abs(percent))} % vs. anterior`, tone: trendTone(percent, trend) };
}

function tile(label: string, icon: string, kpi: MetricKpiDto, trend: Trend): Tile {
  return { label, icon, value: kpi.value ?? 0, suffix: '', ...change(kpi, trend) };
}

function money(label: string, icon: string, kpi: MetricKpiDto): Tile {
  return { label, icon, value: kpi.value == null ? '–' : currency.format(kpi.value), suffix: '', ...change(kpi, Trend.UpIsGood) };
}

// Rates compare in percentage points, since a percent of a percent reads wrong.
function rate(label: string, icon: string, kpi: MetricKpiDto, trend: Trend): Tile {
  if (kpi.value == null) return { label, icon, value: '–', suffix: '', note: 'sin datos', tone: UiTone.Muted };
  const base = { label, icon, value: kpi.value, suffix: ' %' };
  if (kpi.previous == null) return { ...base, note: 'sin período previo', tone: UiTone.Muted };
  const points = Math.round((kpi.value - kpi.previous) * 10) / 10;
  if (!points) return { ...base, note: 'igual que antes', tone: UiTone.Muted };
  return { ...base, note: `${arrow(points)} ${decimals.format(Math.abs(points))} pts vs. anterior`, tone: trendTone(points, trend) };
}
