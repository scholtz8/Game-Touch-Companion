# Plan de ampliaciones

Las fases 1–7 conservan su cierre. Las ampliaciones se entregan incrementalmente, con pruebas y sin sustituir el portable validado hasta publicar explícitamente otro.

## 1. Preferencia de detección al abrir — validada por el usuario

PASS manual global: «funciona perfecto». Sin nueva timeline ni medición de foco inferida.

- Ajuste persistente, desactivado por defecto para instalaciones existentes.
- Aplicarlo una sola vez tras cargar configuración, perfiles y navegador correctamente, con runtime disponible y selección válida.
- Guardarlo no inicia/pausa la sesión actual. Cerrar Companion sigue pausando detección sin borrar la preferencia.
- Verificar persistencia, fallos de escritura, refresco de monitores y aplicación de perfiles.

## 2. Inicio con Windows y bandeja — validada por el usuario

Implementación y decisión: ADR-0009. Pruebas normales y escritorio PASS. Cierre manual global el 2026-09-07: «funciono todo como deberia». Sin desglose por escenario ni nueva timeline de foco; checklist conservado para regresiones.

- Registro por usuario, reversible y sin administrador; usar ruta real del EXE publicado, no fijar una ruta de desarrollo.
- Icono de bandeja con Configuración, activar/pausar detección y Salir.
- Ajustes separados: iniciar con Windows, iniciar oculto y cerrar Configuración a bandeja. Salir debe cerrar Companion y liberar recursos.
- Ocultar requiere icono de bandeja disponible; si falla, mantener ventana accesible. Windows controla el área de iconos ocultos.
- Probar ruta movida, inicio de sesión, salida completa y que abrir Configuración sea una acción explícita del usuario.

## 3. Idiomas — implementada, pendiente validación manual final

ADR-0010 conserva selector inicial/persistencia. ADR-0011 sustituye reinicio manual por aplicación en vivo al guardar, según solicitud del usuario. Guía completa/Next, preview/roles, diálogos, diagnóstico, navegador/perfiles/detección y bandeja localizados; mensajes conservan identidad y argumentos. No se recrean ventanas ni cambia la sesión. Regresión normal y escritorio PASS; ver LOCALIZATION.md. Pendiente confirmación manual final, sin heredar el PASS de etapas anteriores.

- Español e inglés para UI, estados, validación y errores propios; no traducir páginas web.
- Primera ejecución: elección de idioma antes de detección/ocultación; preferencia persistente y editable en Ajustes.
- Centralizar recursos y comprobar claves/fallback. No exportar contenido privado para traducirlo.

## 4. Personalización y mejora visual — en curso, barra implementada

Usuario autorizó continuar con esta etapa sin aportar nueva medición/confirmación manual de idiomas. Primer bloque (ADR-0012): pestaña Personalización, cuatro paletas, tres densidades, texto opcional junto a iconos, título, preview y restablecimiento de borrador. La barra fija navegación a la izquierda y las acciones de favorito a la derecha, oculta Inicio y usa iconos vectoriales. Guardar aplica a Companion abierto sin recrearlo. Pendientes temas completos claro/oscuro/sistema de Configuration y rediseño general, además de validación manual de la barra.

- Tema claro/oscuro/sistema, acento y estilos consistentes.
- Barra: colores, tipografía/tamaños, texto opcional con tooltip, distribución fija accesible y diseños compacto/cómodo/táctil.
- Vista previa y restablecimiento; mantener cerrar/recuperar barra accesibles, contraste y mínimos táctiles.
- Reutilizar paquete gráfico del usuario; no añadir animaciones o cambios que comprometan NoActivate.

## 5. Entrega integrada — pendiente

- Regresión automatizada, escritorio y protocolo manual por etapa.
- Publicar nuevo portable con iconos y ampliaciones, conservar ZIP anterior y distinguir sus evidencias.
- Validación de foco con FocusProbe/juego/control; la matriz Beta continúa aparte.

No se implementan hooks globales, restauración forzada, ejecución de juegos, instalador ni cambios automáticos en Windows por defecto. Las opciones que afectan al sistema solo se registran cuando el usuario las habilita.
