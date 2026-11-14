import { TaxCondition } from 'sdk';

export const taxConditionLabels: Record<TaxCondition, string> = {
  [TaxCondition.ResponsableInscripto]: 'Responsable inscripto (emite factura B)',
  [TaxCondition.Monotributista]: 'Monotributista (emite factura C)',
  [TaxCondition.Exento]: 'Exento (emite factura C)',
};
