# Arquitectura prevista en AWS

Cada consultorio es un despliegue propio del mismo código. No hay una app compartida donde se elige el consultorio: la URL, la configuración y la base llegan armadas con esa instalación.

## Desarrollo local

1. `docker compose up -d` en `clinica-saas` levanta PostgreSQL 18 con la base `clinica`. Si el volumen ya existía con el catálogo anterior, hay que recrearlo (`docker compose down -v`) para que cree esa base.
2. `dotnet run --project backend/src/ClinicaSaaS.Api` publica la API en `http://localhost:5080`. Al arrancar migra la base de este consultorio y, si no hay usuarios, crea el admin `admin@demo.local` / `Admin123!`.
3. La identidad sale de `appsettings.json`: `Clinic:Slug`, `Clinic:Name`, `ConnectionStrings:Clinic`, `Cors:Origins` y `TimeZone`. El local de ejemplo es el consultorio `demo`.
4. En `frontend`, `npm run start:admin` (puerto 4200) y `npm run start:portal` (puerto 4201). Esas dos URL están en `Cors:Origins`.

## Un stack por consultorio

- Route 53 apunta el dominio de ese consultorio (panel y portal) a sus distribuciones de CloudFront. Los estáticos salen de buckets S3 de esa instalación.
- Un Application Load Balancer entrega la API a un servicio ECS Fargate con la configuración de ese consultorio: nombre, slug, zona horaria, orígenes CORS y clave JWT.
- RDS PostgreSQL es la base de ese consultorio. AWS Backup guarda los respaldos de esa instancia, aparte de las demás.
- Secrets Manager guarda la connection string y la clave JWT.
- Un bucket S3 aparte queda reservado para adjuntos clínicos, todavía no implementados.
- Los SPA hablan solo con la API de su consultorio, por HTTPS. El access token dura quince minutos y vive en memoria. El refresh token dura catorce días y viaja en una cookie `HttpOnly`, `Secure` y `SameSite=Lax`, solo hacia `/api/acceso`. No envían un consultorio elegido por el usuario: el proceso ya está atado a su base.
