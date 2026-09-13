GAME TOUCH COMPANION - ICON PACK

Archivos principales:

ICO/GameTouchCompanion.ico
- Icono multiresolución recomendado para el .exe, accesos directos,
  escritorio y barra de tareas de Windows.

PNG/
- Versiones PNG individuales para UI, instaladores, assets y empaquetado.

Tamaños recomendados:
16x16   - listas compactas / UI pequeña
20x20   - barra de tareas con determinados escalados DPI
24x24   - UI / taskbar en algunos factores de escala
32x32   - barra de tareas / iconos medianos
40x40   - Windows con escalado DPI
48x48   - escritorio / Explorer
64x64   - UI grande
96x96   - escalado 200%
128x128 - accesos directos grandes
150x150 - assets de Windows
256x256 - tamaño máximo clásico dentro de ICO / Explorer
310x310 - assets grandes de Windows
512x512 - stores, launchers, documentación
1024x1024 - fuente de alta resolución

Para WPF:
- Añade ICO/GameTouchCompanion.ico al proyecto.
- Build Action: Resource
- En el .csproj:
  <ApplicationIcon>Assets\GameTouchCompanion.ico</ApplicationIcon>

Para una Window concreta:
  Icon="pack://application:,,,/Assets/GameTouchCompanion.ico"

Nota:
Windows selecciona automáticamente la resolución adecuada del .ico
según DPI, zoom del escritorio y contexto de uso.
