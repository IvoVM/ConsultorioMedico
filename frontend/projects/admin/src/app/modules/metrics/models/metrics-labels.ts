import { MetricsRange } from 'sdk';

export const metricsRangeLabels: Record<MetricsRange, string> = {
  [MetricsRange.Week]: '7 días',
  [MetricsRange.Month]: '30 días',
  [MetricsRange.Quarter]: '90 días',
};
