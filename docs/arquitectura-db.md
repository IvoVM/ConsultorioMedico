# Arquitectura de la base

Una sola base PostgreSQL por consultorio (`clinica` en local). El modelo sale de `TenantDbContext` y del snapshot de migraciones.

Las líneas del diagrama son claves foráneas. Las relaciones clínicas nuevas usan `ON DELETE RESTRICT`, así borrar una sede, un profesional o un tipo de turno no arrastra turnos, encuentros ni facturas. `AuditEntries.UserId`, `MedicalRecords.UpdatedBy` y `RefreshSessions.ReplacedById` siguen sin FK. En el dominio cada FK es una propiedad `virtual`.

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
    Users ||--o{ ScheduleBlocks : "ProfessionalId FK restrict"
    Users ||--o{ ScheduleBlockouts : "ProfessionalId FK restrict"
    Users ||--o{ Appointments : "ProfessionalId FK restrict"
    Users ||--o{ Encounters : "ProfessionalId FK restrict"
    Users ||--o{ Prescriptions : "ProfessionalId FK restrict"
    Users ||--o{ WaitlistEntries : "ProfessionalId FK restrict"
    Employees }o--o| Specialties : "SpecialtyId FK set null"

    Specialties ||--o{ AppointmentTypes : "SpecialtyId FK set null"
    Specialties ||--o{ WaitlistEntries : "SpecialtyId FK restrict"

    Locations ||--o{ MedicalServices : "LocationId FK restrict"
    Locations ||--o{ ScheduleBlocks : "LocationId FK restrict"
    Locations ||--o{ Appointments : "LocationId FK restrict"
    Locations ||--o{ WaitlistEntries : "LocationId FK restrict"

    AppointmentTypes ||--o{ ScheduleBlocks : "AppointmentTypeId FK restrict"
    AppointmentTypes ||--o{ Appointments : "AppointmentTypeId FK restrict"
    AppointmentTypes ||--o{ Fees : "AppointmentTypeId FK restrict"

    Clients ||--|| MedicalRecords : "ClientId FK cascade, unico"
    Clients ||--o{ Appointments : "ClientId FK cascade"
    Clients ||--o{ WaitlistEntries : "ClientId FK restrict"
    Clients ||--o{ Encounters : "ClientId FK restrict"
    Clients ||--o{ Prescriptions : "ClientId FK restrict"
    Clients ||--o{ Invoices : "ClientId FK restrict"

    Appointments ||--o| Encounters : "AppointmentId FK restrict, unico"
    Appointments ||--o| Invoices : "AppointmentId FK restrict, unico"

    Encounters ||--o{ EncounterDiagnoses : "EncounterId FK cascade"
    Diagnoses ||--o{ EncounterDiagnoses : "DiagnosisId FK cascade"
    Encounters ||--o{ Prescriptions : "EncounterId FK restrict"
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
| `MedicalServices` | `Id` | `LocationId` → `Locations` | FK restrict |
| `Specialties` | `Id` | — | `Name` único |
| `AppointmentTypes` | `Id` | `SpecialtyId?` → `Specialties` | FK set null |
| `ScheduleBlocks` | `Id` | `ProfessionalId` → `Users`, `LocationId` → `Locations`, `AppointmentTypeId` → `AppointmentTypes` | FK restrict. Índice en `ProfessionalId` |
| `ScheduleBlockouts` | `Id` | `ProfessionalId` → `Users` | FK restrict |
| `Employees` | `Id` | `UserId` → `Users`, `SpecialtyId?` → `Specialties` | FK `UserId` cascade, índice único (1:1). FK `SpecialtyId` set null |
| `Clients` | `Id` | `UserId` → `Users` | FK `UserId` cascade, índice único (1:1) |
| `MedicalRecords` | `Id` | `ClientId` → `Clients`, `UpdatedBy?` → `Users` | FK `ClientId` cascade, índice único (1:1) |
| `Appointments` | `Id` | `ClientId` → `Clients`, `ProfessionalId` → `Users`, `LocationId` → `Locations`, `AppointmentTypeId` → `AppointmentTypes` | FK `ClientId` cascade. El resto restrict. Índice `(ProfessionalId, Start)` |
| `WaitlistEntries` | `Id` | `ClientId`, `ProfessionalId?`, `LocationId`, `SpecialtyId` | FK restrict |
| `Diagnoses` | `Id` | — | `Code` único |
| `Encounters` | `Id` | `AppointmentId`, `ClientId`, `ProfessionalId` | FK restrict. `AppointmentId` único (1:1) |
| `EncounterDiagnoses` | `(EncounterId, DiagnosisId)` | ambas columnas | FK cascade a `Encounters` y a `Diagnoses` |
| `Prescriptions` | `Id` | `EncounterId`, `ClientId`, `ProfessionalId` | FK restrict |
| `PrescriptionItems` | `Id` | `PrescriptionId` | FK cascade a `Prescriptions` |
| `Fees` | `Id` | `AppointmentTypeId` | FK restrict |
| `Invoices` | `Id` | `AppointmentId`, `ClientId` | FK restrict. `AppointmentId` único (1:1) |
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
- Un turno tiene como máximo un encuentro y como máximo una factura (índice único y FK restrict).
- Un encuentro tiene muchos diagnósticos a través de `EncounterDiagnoses`.
- Una receta tiene muchos ítems; una factura tiene muchos ítems. Esas dos sí borran en cascada.
