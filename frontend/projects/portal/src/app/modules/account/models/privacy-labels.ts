import { ConsentChannel, ConsentKind } from 'sdk';

export const consentKindLabels: Record<ConsentKind, string> = {
  [ConsentKind.PrivacyNotice]: 'Aceptaste el aviso de privacidad',
  [ConsentKind.EmergencyContact]: 'Confirmaste que tu contacto de emergencia aceptó',
};

export const consentChannelLabels: Record<ConsentChannel, string> = {
  [ConsentChannel.OnlineRequest]: 'al pedir un turno',
  [ConsentChannel.SignUp]: 'al crear la cuenta',
  [ConsentChannel.FrontDesk]: 'en la recepción',
  [ConsentChannel.Account]: 'desde tu cuenta',
};
