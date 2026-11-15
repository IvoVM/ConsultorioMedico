// Where a patient is in today's visit, as front desk sees it: still to come, inside, or done and left to pay.
export enum DeskStage {
  Arriving = 'arriving',
  Inside = 'inside',
  Checkout = 'checkout',
}

export const deskStageLabels: Record<DeskStage, { title: string; empty: string }> = {
  [DeskStage.Arriving]: { title: 'Por llegar', empty: 'No queda nadie por llegar.' },
  [DeskStage.Inside]: { title: 'En sala y en consulta', empty: 'No hay nadie adentro.' },
  [DeskStage.Checkout]: { title: 'Para cobrar', empty: 'No hay nada para cobrar.' },
};
