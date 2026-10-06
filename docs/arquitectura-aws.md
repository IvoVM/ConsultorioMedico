# Arquitectura prevista en AWS

El sistema corre hoy en local. Esta nota describe el despliegue típico en AWS, sin Terraform ni recursos creados.

## Desarrollo local

1. `docker compose up -d` en `clinica-saas` levanta PostgreSQL 18. El catálogo queda en `clinica_catalog` y cada consultorio recibe su propia base `tenant_{slug}`.
2. `dotnet run --project backend/src/ClinicaSaaS.Api` publica la API en `http://localhost:5080`. Al arrancar migra el catálogo y crea el superadmin `admin@clinica.local` / `Admin123!`. Cada consultorio nuevo recibe un admin inicial `admin@{slug}.local` con la misma clave.
3. `dotnet run --project backend/src/ClinicaSaaS.Api -- migrate-tenants` reaplica las migraciones de todas las bases de consultorio.
4. En `frontend`, `npm run start:admin` (puerto 4200) y `npm run start:portal` (puerto 4201).
5. El portal manda el slug en `X-Tenant-Slug`. También acepta un subdominio `{slug}.localhost`.

## AWS

- Route 53 resuelve `admin.ejemplo.com` y `*.ejemplo.com`.
- Dos distribuciones de CloudFront sirven los estáticos de admin y portal desde buckets S3.
- Un Application Load Balancer entrega la API a un servicio ECS Fargate.
- RDS PostgreSQL aloja el catálogo y las bases de cada tenant en la misma instancia.
- Secrets Manager reemplaza el cifrado local de Data Protection para las connection strings.
- Un bucket S3 aparte queda reservado para adjuntos clínicos, todavía no implementados.
- Los SPA no hablan con la base: solo con la API, por HTTPS, con el mismo JWT y el encabezado de tenant.
