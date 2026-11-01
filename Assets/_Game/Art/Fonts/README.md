# Шрифт камеры

VCR_OSD_MONO.ttf — файл VCR_OSD_MONO_1.001.ttf, предоставленный пользователем 01.11.2026.
CameraOsd.asset — SDF-атлас латиницы, цифр и знаков для HUD камеры.
В исходном cmap нет кириллицы. CameraCyrillic.asset использует уже имеющийся
в проекте LiberationSans.ttf как явный fallback для русских букв.
Главное меню и пауза сохраняют свой прежний шрифт.
Создание: CameraPresentationSetup.Apply() через Unity MCP.
