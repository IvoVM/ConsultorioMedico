import { DayOfWeek, MetricsRange } from 'sdk';

export const metricsRangeLabels: Record<MetricsRange, string> = {
  [MetricsRange.Week]: '7 días',
  [MetricsRange.Month]: '30 días',
  [MetricsRange.Quarter]: '90 días',
};

// Monday first, the way the clinic reads its week; the weekend only shows up when something happened on it.
export const weekDays: { day: DayOfWeek; label: string }[] = [
  { day: DayOfWeek.Monday, label: 'Lun' },
  { day: DayOfWeek.Tuesday, label: 'Mar' },
  { day: DayOfWeek.Wednesday, label: 'Mié' },
  { day: DayOfWeek.Thursday, label: 'Jue' },
  { day: DayOfWeek.Friday, label: 'Vie' },
  { day: DayOfWeek.Saturday, label: 'Sáb' },
  { day: DayOfWeek.Sunday, label: 'Dom' },
];

// Which way is good news for a figure: more consultations is good, more absences is not.
export enum Trend {
  UpIsGood = 'up',
  DownIsGood = 'down',
}

export enum RankingView {
  Professionals = 'professionals',
  Specialties = 'specialties',
}

export const rankingViewLabels: Record<RankingView, string> = {
  [RankingView.Professionals]: 'Profesionales',
  [RankingView.Specialties]: 'Especialidades',
};
