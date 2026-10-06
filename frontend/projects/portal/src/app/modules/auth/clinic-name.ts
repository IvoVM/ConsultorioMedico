import { ClinicService } from 'sdk';
import { firstValueFrom } from 'rxjs';

export async function readClinicName(clinic: ClinicService) {
  try {
    const profile = await firstValueFrom(clinic.clinicProfile());
    return profile.name || 'Consultorio';
  } catch {
    return 'Consultorio';
  }
}
