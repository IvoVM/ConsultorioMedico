import { AppointmentStatus, MetricsRange } from 'sdk';
import { ChartHue } from 'ui';

export const metricsRangeLabels: Record<MetricsRange, string> = {
  [MetricsRange.Today]: 'Hoy',
  [MetricsRange.Week]: '7 días',
  [MetricsRange.Month]: '30 días',
};

// Slices go around the donut in this order; the colorblind check passed for these neighbours, so keep them together.
export const statusSlices: { status: AppointmentStatus; hue: ChartHue }[] = [
  { status: AppointmentStatus.Booked, hue: ChartHue.Sky },
  { status: AppointmentStatus.CheckedIn, hue: ChartHue.Orange },
  { status: AppointmentStatus.InProgress, hue: ChartHue.Violet },
  { status: AppointmentStatus.Completed, hue: ChartHue.Ok },
  { status: AppointmentStatus.Rescheduled, hue: ChartHue.Pink },
  { status: AppointmentStatus.NoShow, hue: ChartHue.Warn },
  { status: AppointmentStatus.Cancelled, hue: ChartHue.Bad },
];
