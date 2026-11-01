# Восстановленные текстуры

Источник: исходный архив «локация и зомби.zip» в папке команды:
https://drive.google.com/file/d/11lAv0TAsYrPGIoUgBK_lLdv4gcdlqAJE/view

Карты были упакованы в `локация.blend`, но не были включены в экспортированный FBX.
Из Blender 5.1 извлечён 21 исходный файл изображения (18 PNG, 2 JPEG, 1 EXR).
В игре используются:

- RockBaseColor.jpg: исходный rock_moss_set_01_diff_2k.jpg без изменений.
- RockNormal.exr: исходный rock_moss_set_01_nor_gl_2k.exr, импорт Unity как Normal Map.
- RockRoughness.jpg: исходный rock_moss_set_01_rough_2k.jpg, импорт как линейные данные.
- RockMetallicSmoothness.png: упаковка для URP — RGB=0 (камень не металл), A=1−roughness.
- BarkBaseColor.png: fir_tree_01_bark_diff, преобразование исходного 16-битного PNG
  в RGB8 средствами Unity. Сохраняются исходное разрешение 2048×2048 и UV.

Назначение карт и упаковка URP: `RecoveredMaterialSetup.Apply()` в Editor через MCP.
Оставшиеся карты исходных деревьев сохранены в локальном Builds/Imports/NamedTextures.
Атлас листьев jacaranda не назначен новым Leaf Oak: у них другая геометрия и UV.
Для земли и ворот подходящих упакованных карт не обнаружено.

У исходного `житель walking.blend` изображения `color.png` и `-color.png`
имеют пустые packedfiles и ссылаются на внешние файлы. В архиве их нет.
У зомби уже используется ранее извлечённый ZombieBaseColor.png.

Crosshair.png — прицел из архива дизайнерки от 01.11.2026.
SoilTile.asset — бесшовная процедурная текстура грунта, созданная средствами Unity;
каменный UV-атлас для земли не используется.
