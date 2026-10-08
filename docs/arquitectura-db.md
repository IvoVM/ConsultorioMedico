# Arquitectura de la base

Una sola base PostgreSQL por consultorio (`clinica` en local). El modelo sale de `TenantDbContext` y del snapshot de migraciones.

Las líneas sólidas del diagrama son claves foráneas de verdad. El resto son columnas `uuid` que apuntan a otra tabla, pero **no** tienen FK en la base: la integridad la mantiene la aplicación.

## Diagrama

```mermaid
erDiagram
    Users {
        uuid Id PK
        string FirstName
        string LastName
        string Email
        string PhoneNumber
        string Role
        bool MustChangePassword
    }
    Employees {
        uuid Id PK
        uuid UserId
        string LicenseNumber
        uuid SpecialtyId
    }
    Clients {
        uuid Id PK
        uuid UserId
        string DocumentNumber
        date BirthDate
        string Phone
    }
    Roles {
        uuid Id PK
        string Name
    }
    UserRoles {
        uuid UserId PK
        uuid RoleId PK
    }
    UserClaims {
        int Id PK
        uuid UserId
        string ClaimType
        string ClaimValue
    }
    UserLogins {
        string LoginProvider PK
        string ProviderKey PK
        string ProviderDisplayName
        uuid UserId
    }
    UserTokens {
        uuid UserId PK
        string LoginProvider PK
        string Name PK
        string Value
    }
    RoleClaims {
        int Id PK
        uuid RoleId
        string ClaimType
        string ClaimValue
    }
    RefreshSessions {
        uuid Id PK
        uuid UserId
        string TokenHash
        timestamptz ExpiresAt
        timestamptz CreatedAt
        timestamptz RevokedAt
        uuid ReplacedById
    }

    Locations {
        uuid Id PK
        string Name
        string Address
        bool IsActive
    }
    MedicalServices {
        uuid Id PK
        uuid LocationId
        string Name
    }
    Specialties {
        uuid Id PK
        string Name
    }
    AppointmentTypes {
        uuid Id PK
        string Name
        int DurationMinutes
        uuid SpecialtyId
    }
    ScheduleBlocks {
        uuid Id PK
        uuid ProfessionalId
        string Day
        time StartTime
        time EndTime
        uuid LocationId
        uuid AppointmentTypeId
    }
    ScheduleBlockouts {
        uuid Id PK
        uuid ProfessionalId
        timestamptz Start
        timestamptz End
        string Reason
    }

    MedicalRecords {
        uuid Id PK
        uuid ClientId
        string BloodType
        string Allergies
        string PersonalHistory
        string FamilyHistory
        string CurrentMedication
        string Habits
        string HealthInsurance
        string MemberNumber
        string EmergencyContact
        string EmergencyPhone
        string Notes
        timestamptz UpdatedAt
        uuid UpdatedBy
    }
    Appointments {
        uuid Id PK
        uuid ClientId
        uuid ProfessionalId
        uuid LocationId
        uuid AppointmentTypeId
        timestamptz Start
        timestamptz End
        string Status
        string VisitReason
    }
    WaitlistEntries {
        uuid Id PK
        uuid ClientId
        uuid ProfessionalId
        uuid LocationId
        uuid SpecialtyId
        string Status
        timestamptz CreatedAt
        string Notes
    }

    Diagnoses {
        uuid Id PK
        string Code
        string Name
    }
    Encounters {
        uuid Id PK
        uuid AppointmentId
        uuid ClientId
        uuid ProfessionalId
        string Note
        string BloodPressure
        int HeartRate
        decimal Temperature
        decimal WeightKg
        bool IsClosed
        timestamptz CreatedAt
    }
    EncounterDiagnoses {
        uuid EncounterId PK
        uuid DiagnosisId PK
    }
    Prescriptions {
        uuid Id PK
        uuid EncounterId
        uuid ClientId
        uuid ProfessionalId
        string Instructions
        timestamptz CreatedAt
    }
    PrescriptionItems {
        uuid Id PK
        uuid PrescriptionId
        string Medication
        string Dose
        string Frequency
        string Duration
    }

    Fees {
        uuid Id PK
        uuid AppointmentTypeId
        decimal Amount
        date EffectiveFrom
    }
    Invoices {
        uuid Id PK
        uuid AppointmentId
        uuid ClientId
        decimal Total
        string Status
        string PaymentMethod
        timestamptz CreatedAt
        timestamptz PaidAt
    }
    InvoiceItems {
        uuid Id PK
        uuid InvoiceId
        string Description
        decimal Amount
    }
    AuditEntries {
        uuid Id PK
        uuid UserId
        string Action
        string Entity
        string EntityId
        string Detail
        timestamptz Timestamp
    }

    Users ||--o| Employees : "UserId FK cascade, unico"
    Users ||--o| Clients : "UserId FK cascade, unico"
    Users ||--o{ RefreshSessions : "UserId FK cascade"
    Users ||--o{ ScheduleBlocks : "ProfessionalId sin FK"
    Users ||--o{ ScheduleBlockouts : "ProfessionalId sin FK"
    Users ||--o{ Appointments : "ProfessionalId sin FK"
    Users ||--o{ Encounters : "ProfessionalId sin FK"
    Users ||--o{ Prescriptions : "ProfessionalId sin FK"
    Employees }o--o| Specialties : "SpecialtyId FK set null"

    Specialties ||--o{ AppointmentTypes : "SpecialtyId sin FK"
    Specialties ||--o{ WaitlistEntries : "SpecialtyId sin FK"

    Locations ||--o{ MedicalServices : "LocationId sin FK"
    Locations ||--o{ ScheduleBlocks : "LocationId sin FK"
    Locations ||--o{ Appointments : "LocationId sin FK"
    Locations ||--o{ WaitlistEntries : "LocationId sin FK"

    AppointmentTypes ||--o{ ScheduleBlocks : "AppointmentTypeId sin FK"
    AppointmentTypes ||--o{ Appointments : "AppointmentTypeId sin FK"
    AppointmentTypes ||--o{ Fees : "AppointmentTypeId sin FK"

    Clients ||--|| MedicalRecords : "ClientId FK cascade, unico"
    Clients ||--o{ Appointments : "ClientId FK cascade"
    Clients ||--o{ WaitlistEntries : "ClientId sin FK"
    Clients ||--o{ Encounters : "ClientId sin FK"
    Clients ||--o{ Prescriptions : "ClientId sin FK"
    Clients ||--o{ Invoices : "ClientId sin FK"

    Appointments ||--o| Encounters : "AppointmentId unico, sin FK"
    Appointments ||--o| Invoices : "AppointmentId unico, sin FK"

    Encounters ||--o{ EncounterDiagnoses : "EncounterId FK cascade"
    Diagnoses ||--o{ EncounterDiagnoses : "DiagnosisId FK cascade"
    Encounters ||--o{ Prescriptions : "EncounterId sin FK"
    Prescriptions ||--o{ PrescriptionItems : "PrescriptionId FK cascade"
    Invoices ||--o{ InvoiceItems : "InvoiceId FK cascade"

    Roles ||--o{ UserRoles : "RoleId FK cascade"
    Users ||--o{ UserRoles : "UserId FK cascade"
    Users ||--o{ UserClaims : "UserId FK cascade"
    Users ||--o{ UserLogins : "UserId FK cascade"
    Users ||--o{ UserTokens : "UserId FK cascade"
    Roles ||--o{ RoleClaims : "RoleId FK cascade"
```

`AuditEntries` no se une con línea: `UserId` y `EntityId` son de auditoría, sin FK. `RefreshSessions.ReplacedById` apunta a otra sesión de la misma tabla, también sin FK.

## Dominio clínico

| Tabla | Clave | Columnas que apuntan a otra tabla | Restricción real |
|---|---|---|---|
| `Locations` | `Id` | — | — |
| `MedicalServices` | `Id` | `LocationId` → `Locations` | ninguna |
| `Specialties` | `Id` | — | `Name` único |
| `AppointmentTypes` | `Id` | `SpecialtyId?` → `Specialties` | ninguna |
| `ScheduleBlocks` | `Id` | `ProfessionalId` → `Users`, `LocationId` → `Locations`, `AppointmentTypeId` → `AppointmentTypes` | índice en `ProfessionalId` |
| `ScheduleBlockouts` | `Id` | `ProfessionalId` → `Users` | ninguna |
| `Employees` | `Id` | `UserId` → `Users`, `SpecialtyId?` → `Specialties` | FK `UserId` cascade, índice único (1:1). FK `SpecialtyId` set null |
| `Clients` | `Id` | `UserId` → `Users` | FK `UserId` cascade, índice único (1:1) |
| `MedicalRecords` | `Id` | `ClientId` → `Clients`, `UpdatedBy?` → `Users` | FK `ClientId` cascade, índice único (1:1) |
| `Appointments` | `Id` | `ClientId` → `Clients`, `ProfessionalId`, `LocationId`, `AppointmentTypeId` | FK `ClientId` cascade. Índice `(ProfessionalId, Start)` |
| `WaitlistEntries` | `Id` | `ClientId`, `ProfessionalId?`, `LocationId`, `SpecialtyId` | ninguna |
| `Diagnoses` | `Id` | — | `Code` único |
| `Encounters` | `Id` | `AppointmentId`, `ClientId`, `ProfessionalId` | `AppointmentId` único (1:1 lógico) |
| `EncounterDiagnoses` | `(EncounterId, DiagnosisId)` | ambas columnas | FK cascade a `Encounters` y a `Diagnoses` |
| `Prescriptions` | `Id` | `EncounterId`, `ClientId`, `ProfessionalId` | ninguna |
| `PrescriptionItems` | `Id` | `PrescriptionId` | FK cascade a `Prescriptions` |
| `Fees` | `Id` | `AppointmentTypeId` | ninguna |
| `Invoices` | `Id` | `AppointmentId`, `ClientId` | `AppointmentId` único (1:1 lógico) |
| `InvoiceItems` | `Id` | `InvoiceId` | FK cascade a `Invoices` |
| `AuditEntries` | `Id` | `UserId?` (sin tabla forzada) | índice en `Timestamp` |

`ProfessionalId` es siempre el `Id` de un usuario con rol médico (`Users`), no una tabla de profesionales.

## Identidad

`Users` es la cuenta. Guarda nombre, apellido, el rol de la clínica (`TenantAdmin`, `Doctor`, `Secretary`, `Patient`) y las columnas de acceso: contraseña, bloqueo, confirmación de email y teléfono.

El nombre visible no es una columna. `TenantUser.Name` lo proyecta como `FirstName` + `LastName`. Identity sigue guardando `UserName` y `NormalizedUserName` con el email, porque el login lo exige; no es un dato del dominio.

Un usuario es empleado o cliente, nunca las dos cosas. `Employees` tiene la matrícula y la especialidad. `Clients` tiene documento, nacimiento y teléfono. El rol distingue admin, médico y secretaria dentro de los empleados.

`Roles` y `UserRoles` son el catálogo de Identity. Conviven con la columna `Users.Role`: esa columna es la que usa la aplicación.

| Tabla | Relación |
|---|---|
| `RefreshSessions` | N sesiones por usuario. FK `UserId` cascade. `ReplacedById` encadena la rotación del refresh token, sin FK. |
| `Roles` / `UserRoles` | qué roles de Identity tiene cada usuario |
| `UserClaims`, `UserLogins`, `UserTokens`, `RoleClaims` | claims, logins externos y tokens, todas con FK cascade |

## Cardinalidades que importan

- Un usuario tiene como máximo un empleado o un cliente (`Employees.UserId` y `Clients.UserId` únicos).
- La especialidad del médico está en `Employees.SpecialtyId`.
- Un cliente tiene exactamente una historia clínica (`MedicalRecords.ClientId` único + FK).
- Los turnos apuntan al cliente (`Appointments.ClientId`).
- Un turno tiene como máximo un encuentro y como máximo una factura (índices únicos, sin FK).
- Un encuentro tiene muchos diagnósticos a través de `EncounterDiagnoses`.
- Una receta tiene muchos ítems; una factura tiene muchos ítems. Esas dos sí borran en cascada.
