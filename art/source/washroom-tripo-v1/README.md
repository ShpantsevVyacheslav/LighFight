# Санузел: исходные модели Tripo

`fixtures-reference-v1.png` подготовлен встроенным imagegen по настоящему `assets/references/photo-04.jpg`: выделены конкретные раковина на пьедестале и унитаз, удалена комната и мелкие предметы. Это производный референс, не снимок готовой арены.

Промпт подготовки: `Reference-derived modular reconstruction: isolate the photo's compact white ceramic pedestal washbasin with chrome single-lever tap and close-coupled toilet with rectangular rounded cistern, top flush button and closed lid. Preserve pictured forms/proportions. Full silhouettes, separated, front three-quarter view on neutral grey studio backdrop; remove bottles, bucket, pipes, mirror, brush, tiles, walls, floor slab, door, people and text.`

Tripo H3.1 HD geometry+texture, private, 4K export, без рига/ремеша. Задача и расход в `generation.json`. Веб-кадр `result-web.png` показывает оба предмета. Скачан неизменённый `original-v1.glb`: 1 894 538 треугольников, 980 455 вершин; SHA в `original-audit.json`.

Локальная визуальная проверка, сборка стен/пола по фото, мобильный бюджет, коллизии и перенос в Unity ещё **не завершены**.
