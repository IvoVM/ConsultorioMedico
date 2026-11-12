export enum HomeView {
  Metrics = 'metrics',
  Today = 'today',
}

export const homeViewLabels: Record<HomeView, string> = {
  [HomeView.Metrics]: 'Métricas',
  [HomeView.Today]: 'Hoy',
};
