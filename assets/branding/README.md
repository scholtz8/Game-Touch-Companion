# Iconos de Game Touch Companion

Paquete original aportado por el usuario: `GameTouchCompanion_Icons.zip` en la raíz del repositorio. Su contenido completo se conserva sin modificaciones en `GameTouchCompanion_Icons/`, incluidos README, master, PNG e imágenes WindowsAssets para futuros empaquetados.

El ICO original se copia sin modificar a `src/GameTouchCompanion.App/Assets/GameTouchCompanion.ico`. El proyecto WPF lo usa mediante ApplicationIcon (recurso nativo del EXE) y Resource/Icon para MainWindow. Contiene 16, 20, 24, 32, 40, 48, 64, 96, 128 y 256 px, verificados en su directorio ICO.

No se incorporan todos los PNG al binario ni se crea MSIX/instalador. Al reemplazar el diseño en el futuro, actualizar también la copia del ICO de App. Windows puede conservar iconos antiguos en accesos directos/caché; verificar con el EXE recién compilado, no con el ZIP anterior.
