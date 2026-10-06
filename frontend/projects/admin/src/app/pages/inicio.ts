import { Component } from '@angular/core';
import { clinicaSession } from 'sdk';

@Component({
  template: `
    <section class="max-w-2xl">
      <p class="text-lg leading-relaxed text-ink/80">
        @if (session.rol() === 'SuperAdmin') {
          Desde Consultorios podés dar de alta un hospital o consultorio. Eso crea su base PostgreSQL.
        } @else {
          Estás en {{ session.tenant() }}. Cargá la organización, los empleados y la agenda semanal.
        }
      </p>
    </section>
  `,
})
export class InicioPage {
  readonly session = clinicaSession;
}
