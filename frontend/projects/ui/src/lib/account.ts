const area3 = new Set([
  '220', '221', '223', '230', '236', '237', '249',
  '260', '261', '263', '264', '266', '280', '291', '294', '297', '298', '299',
  '336', '341', '342', '343', '345', '348',
  '351', '353', '358',
  '362', '364', '370', '376', '379', '380', '381', '383', '385', '387', '388',
]);

export function formatPhone(value: string): string | null {
  const national = nationalDigits(value);
  if (!national) return null;
  const area = national.startsWith('11') ? 2 : area3.has(national.slice(0, 3)) ? 3 : 4;
  return `+54 ${national.slice(0, area)} ${national.slice(area)}`;
}

export function passwordIssue(value: string): string {
  if (value.length < 8) return 'La contraseña tiene que tener al menos 8 caracteres.';
  if (!/[A-Z]/.test(value)) return 'La contraseña tiene que incluir una mayúscula.';
  if (!/[a-z]/.test(value)) return 'La contraseña tiene que incluir una minúscula.';
  if (!/[0-9]/.test(value)) return 'La contraseña tiene que incluir un número.';
  return '';
}

function nationalDigits(value: string): string | null {
  let digits = value.replace(/\D/g, '');
  if (digits.startsWith('00')) digits = digits.slice(2);
  if (digits.startsWith('54')) digits = digits.slice(2);
  if (digits.startsWith('0')) digits = digits.slice(1);
  if (digits.startsWith('9') && digits.length > 10) digits = digits.slice(1);
  if (digits.startsWith('15') && digits.length === 12) digits = digits.slice(2);
  if (digits.length === 12) digits = withoutMobileMarker(digits);
  return digits.length === 10 ? digits : null;
}

function withoutMobileMarker(digits: string): string {
  if (digits.startsWith('11') && digits.slice(2, 4) === '15') return `11${digits.slice(4)}`;
  if (area3.has(digits.slice(0, 3)) && digits.slice(3, 5) === '15') return `${digits.slice(0, 3)}${digits.slice(5)}`;
  if (digits.slice(4, 6) === '15') return `${digits.slice(0, 4)}${digits.slice(6)}`;
  return digits;
}
