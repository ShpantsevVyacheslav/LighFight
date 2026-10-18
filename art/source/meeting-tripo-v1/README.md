# Переговорная: подготовка исходника для Tripo

Статус: **исходный GLB Tripo скачан; локальная визуальная проверка и перенос в игру ещё не завершены**.

- Настоящий источник: `assets/references/photo-03.jpg`.
- `furniture-reference-v1.png` — производное изображение, созданное встроенным imagegen по фотографии. Это не фотография готовой 3D-модели и не новый пользовательский референс.
- Задача изображения: выделить длинный стол со светлой древесной фактурой и чёрные сетчатые офисные кресла; убрать комнату, куртку, бумаги, растение и технику.
- В результате видны пять кресел вместо запрошенных шести. Число/расстановка кресел не принимаются как точная реконструкция комнаты. При сборке арены положение и количество мебели сверяются с исходным фото; предпочтительны отдельные экземпляры кресел.
- Tripo-задача `516b8c42-8e19-4297-b133-7dc815c99ec2` завершена; исходный GLB сохранён без изменений (1 910 406 треугольников, 1 002 607 вершин). SHA и счётчики в `original-audit.json`; веб-результат в `result-web.png`.
- Следующий этап: локальная проверка геометрии, затем архитектура по фото. До этого изображение не используется как превью арены в игре.

Промпт: `Use case: precise-object-edit. Asset: reference image for Tripo 3D reconstruction of the actual pictured meeting-room furniture. From this supplied photograph isolate and faithfully reconstruct its long rectangular pale grey-brown woodgrain conference table with dark thin metal legs, and six black ergonomic mesh office swivel chairs with armrests and five-star wheeled bases arranged three each long side. Keep recognisable pictured table proportions, materials, chair mesh and headrests; remove jacket, paper, electronics, plant and all loose clutter. Show entire furniture group including all chair bases, table legs and tabletop from a moderately elevated three-quarter perspective, spaced so components are legible; no cropping. Neutral light grey seamless background, diffuse neutral studio light, realistic materials. No room, walls, windows, floor slab, people, text or additional furniture. This is a clean reference-derived reconstruction of these specific objects, not a redesigned conference table.`
