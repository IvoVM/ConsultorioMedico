# Cómo seguir en este repo

Cuando un cambio queda hecho y verificado, commitealo y pushealo en ese momento. No lo dejes pendiente para el próximo turno.

- El mensaje va en inglés, corto y en minúsculas, y explica por qué.
- No commitees secretos, binarios de build ni `node_modules`.
- No hagas force push ni cambies la configuración de git.

Un texto que se repite en varias pantallas, o entre el frontend y el backend, va en un enum con el mismo nombre en los dos lados. No se compara ni se deja escrito suelto. Los roles son `TenantRole`. Las rutas del portal son `AppRoute`. El generador del SDK tiene que emitir esos enums; no los vuelvas a dejar como unión de textos.
