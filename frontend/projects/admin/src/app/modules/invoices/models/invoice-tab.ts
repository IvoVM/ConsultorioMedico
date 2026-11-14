// The invoice list keeps the voided ones apart from the ones still in force.
export enum InvoiceTab {
  Vigentes = 'vigentes',
  Anulados = 'anulados',
}

export const invoiceTabLabels: Record<InvoiceTab, string> = {
  [InvoiceTab.Vigentes]: 'Comprobantes',
  [InvoiceTab.Anulados]: 'Anulados',
};
