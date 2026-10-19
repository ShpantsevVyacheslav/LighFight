# Персонажи: исходники, скелеты и коррекции

Обновлено 2026-10-19. Приоритет владельца: сначала original-density rig и полный проверенный набор исправлений, затем окончательные анимированные превью.

## Источники истины

- Состав/порядок: `art/drafts/full-roster-v1/inventory.json`: bone, witch, master, island, rose, ranger, gala, cobalt, retro, sailor, summer.
- Фото личности: `art/references/identity-originals/` для bone/witch/rose. A/T-pose концепты задают одежду, не заменяют исходное лицо.
- Original mesh: путь original в inventory. Обычно 1,86–1,99 млн треугольников без скелета. Native animation donor: около 100 тысяч треугольников, 67 костей. Его нельзя называть полной исходной детализацией.
- Для island полный original локально пока не найден. Для трёх героинь сходство старого 3D-лица не принято, даже если портрет обновлён.

## Состав переносимого набора

| Этап | Что переносим / проверяем | Статус |
|---|---|---|
| Полная поверхность | Исходные вершины, треугольники, UV, карты; контрольные суммы | Обязательный контракт без упрощения |
| Привязка | Native67 true bind, согласование source/donor, перенос по замкнутым ближайшим треугольникам, сохранение положительных влияний | Моряк guard-v3 — full bary; spatial-варианты Мастера — отдельные кандидаты |
| Плечевой пояс | Каноническое соответствие источника/цели при retarget, положение ключицы и оси плеча | Проверять вместе с весами; не заменяется одним параметром twist |
| Осевой твист плеча | Разделение swing/twist, 0% axial twist как принятый визуальный ориентир; компенсация ориентации предплечья, контроль локтя/кисти | Моряк: пользователю лучше 0%; перенос на других требует проверки |
| Переходы | Плавное включение/выключение коррекции у границ клипа; возвращение в guard | У Моряка export: fade 0,25 c; UI blend 0,35 c |
| Стойка | Авторский guard, перенос на native67, замыкание цикла без обрыва | Моряк guard-v3: 24 ключа overlap при 96 Hz; original guard FBX сохранён |
| Боевые движения | Танец, удар рукой/ногой, реакции, блок, присед; полные клипы и переходы | Ни один отдельный кадр не закрывает этот этап |
| Превью / игра / телефон | Экспорт всех влияний, визуальная проверка, runtime-контакты, нагрузка | Раздельные критерии приёмки |

Проверяемый эталон Моряка: `prototypes/roster-portraits/twist-review/guard-v3/manifest.json`; экспорт `unity/Assets/Native67SpatialReview/Editor/Native67WebPreviewExport.cs`, retarget/correction `Native67AlternativeDanceReview.cs`, замыкание `art/drafts/sailor-native67-dense-v1/tools/close_guard_loop.py`.

**Не включать автоматически в эталон:** экспериментальный подъём ключиц до16°, clavicle gain0.65, source-matched clavicle twist, delta mush, DQS, top4 weights. Это отдельные A/B-эксперименты, наличие кода не означает принятие. Дополнительную подтверждённую правку фиксировать отдельной версией recipe и доказательством.

## Текущая матрица

| Боец | Dense rig | Полный набор / движения | В игре новая версия |
|---|---|---|---|
| Моряк | Есть отдельный full-density web-кандидат | 0% визуально одобрено владельцем; полная игровая приёмка не завершена | Не считать web-кандидат автоматически установленной сборкой |
| Мастер стихий | Full-bary кандидат: 1 992 276 tris, 0 unweighted, до6 влияний; отдельный spatial A/B до8 | Unity retarget/twist gate:1155 отсчётов PASS;16кадров, визуальный кандидат; loop/контакты ещё открыты | Нет |
| Рейнджер | Full-bary candidate:1 986 463tris,0unweighted,до7влияний; export/reimport PASS | Import/GPU/loop/surface gates выполнены; raw-лист просмотрен; боевые клипы/PBR/runtime открыты | Нет |
| Ретро | Original FBX1 921 472tris, full-bary67,0unweighted,max7; полный набор diagnostic gates завершён | Raw Float32/loop/return PASS; A/B плеч в работе; web/runtime ещё старые | Нет |
| Гала, Кобальт, Дачник | Original найден; перенос нового полного набора не завершён | Открыто | Нет |
| Костяная леди, Ведьма, Пантера | Original есть; лицо требует пересмотра по фото | Открыто | Нет |
| Островной | Нужен original из провайдера | Открыто | Нет |

## Воспроизводимость и продвижение

1. Зафиксировать original/donor SHA, bone names/parents/bind matrices, maps, число треугольников.
2. Проверить rest alignment до переноса. Не подгонять плохую исходную позу скрытым масштабированием.
3. Построить производные веса без top-N отсечения; проверить unweighted=0, суммы≈1, геометрия/UV/maps неизменны.
4. Применить версионированный recipe целиком, но с измеренными каноническими осями конкретного бойца. Не копировать абсолютные повороты Моряка.
5. Проверить **сам факт действия** коррекции: разные позы костей/деформированной поверхности, а не одинаковые кадры из-за повторной выборки анимации при рендере.
6. Проверить локоть/кисть, плечевой силуэт и ткань с нескольких сторон; затем клипы целиком, переходы и контактные позы.
7. Только после этого назначать модель в окончательный preview и игровой prefab. Пользовательская/телефонная приёмка отмечается отдельно.

Мастер: `art/drafts/full-roster-v1/master-dense-positive/manifest.json`, `twist-review.json`. Новый transfer: `reconstruct_dense_preserving_weights.py`; диагностический renderer: `review_dense.py`. Это пока кандидаты, не общий production recipe.

Обновлять эту матрицу, backlog и evidence manifest при каждом продвижении. Дата, recipe version, точный артефакт, что проверено, что осталось — обязательны.

## Уточнение состава guard-v3 по фактическим зависимостям

Машиночитаемый recipe с SHA: `art/drafts/full-roster-v1/sailor-guard-v3-recipe.json`. Текущий exporter использует `.tools/native67-bary-weight-packet-v1`, то есть **полный barycentric перенос без spatial averaging**. Это заменяет предположение, что spatial-кандидат был финальной весовой основой. Поле scope текущего web manifest само по себе не доказывает версию весов — проверять код builder и packet SHA.

В этом exporter коррекция0% применяется к retargeted танцу, guard сохраняет gain1, а native box/kick экспортируются без twist-коррекции. Поэтому фразу «все движения уже исправлены у Моряка» использовать нельзя. Перенос полного набора на остальных должен сохранить эти границы и отдельно проверить новые расширения.

### Мастер: обмен полным rig с Unity

`art/drafts/full-roster-v1/master-native67-bary/fbx-export.json`: FBX140MB,1 026 465 original vertices,1 992 276tris, все положительные именованные веса после Blender reimport совпали точно, max6влияний. Производный дополнительный UV-канал кодирует source vertex ID; исходный UV сохраняется. `FullRosterNative67Gate.cs` импортирует без mesh compression, custom255/minWeight0 и проверяет skin before retarget. Итог Unity записывается в `master-unity-recipe/report.json`; до PASS и просмотра кадров продвижение запрещено.

### 2026-10-19 — измеренный Unity retarget Мастера

`master-unity-recipe/report.json`: original1 992 276tris,1 031 866Unity seam vertices,max6influences, суммарная ошибка весов≤5.22e−8.1155отсчётов guard/action67при96Hz; max endpoint shift4.35e−7m. Кадры с двух сторон:16, просмотрен contact-front.jpg и центральный кадр со спины. Статус **кандидат, не production**.

Рендер выполняется независимым CPU LBS по всем импортированным положительным весам, в нейтральном материале. Это устраняет неоднозначность масштаба BakeMesh, но не доказывает GPU/телефонную эквивалентность. Нужно ещё проверить coefficient correspondence по source IDs, циклическую стойку/переходы, боевые клипы/присед/блок, слои одежды у талии.

Для Рейнджера read-only inventory выявил отсутствие front_kick_01 в nativepack: набор включает fall и отдельный boxing idle. Старый универсальный список8take неприменим; перенос остановлен до явного per-character motion mapping. `native-clip-inventory.json` хранит реальные клипы всех11бойцов. Не подставлять другой клип под именем отсутствующего.

### 2026-10-19 — скрытые потери стандартного Unity FBX skinning

Полная проверка по source-ID выявила: стандартный importer изменил2 653 849коэффициентов и состав влияний у20 734Unity-вершин, максимальная ошибка коэффициента≈0.0019986. Настройки meshCompression Off/custom255/minWeight0 этого не предотвратили. Поэтому предыдущая проверка «число костей/сумма весов» недостаточна. Стандартный imported skin **не принят как сохранение source weights**.

`master-float32-gpu.json`: на AppleM1/Metal все2 801 001положительных Float32влияний побитно сохранены, минимум4.144e−12,max6. В5позах GPU/CPUdoubleрасхождение мировых позиций≤1.88e−7m. Это numeric-position gate, не полноценный renderer/GPU-normal/phone gate. Для визуального raw-прохода `FullRosterNative67Gate` теперь получает веса из неизменного FBX-oracle, а не из `Mesh.GetAllBoneWeights`.

`master-motion-continuity/report.json`: guard382keys → loop358keys, период3.7168s; dance/return773keys. Guard-endpoints position error0, quaterniondot1, blended transition boundary error0. Статус POSE_LOOP_PASS_IMPORTED_WEIGHT_LOSS_REQUIRES_RAW_FLOAT32 намеренно сохраняет проблему импортированных весов. Это численная проверка поз; визуальная непрерывность и native combat ещё открыты.

### Версионированный маршрут без потери весов

`native67-full-density-recipe-v2.json` наследует замороженный Sailor guard-v3 recipe и явно добавляет raw Float32 transport в Unity. v1 не переписан задним числом. `prepare_fbx_weight_oracle.py` читает фактический FBX напрямую; `FullRosterFloatGpuGate` проверяет каждый GPU coefficient и позиции; `FullRosterMotionContinuityGate` сохраняет и отдельно показывает потери стандартного импортера, не ослабляя исходный контракт.

`FullRosterMotionPacketReplay` воспроизводит производные GuardLoop/GuardDanceReturn по именованной иерархии; несовпадение длины, имён или родителей останавливает проверку. `FullRosterNative67Gate` снимает эти позы по raw-весам, не по потерявшему часть влияний imported skin. Просмотрен лист `master-unity-recipe/transitions-raw-contact.jpg`; численная позовая граница замкнута, полный визуальный playback и runtime/phone приёмка остаются открыты.

### Ограничение review FBX материалов

Экспорт Рейнджера показал предупреждения Blender о копировании packed image с пустым файловым путём (`IsADirectoryError: .` внутри FBX texture-copy). Экспорт geometry/rig и полный named-weight reimport прошли, но **материалы этого FBX не приняты**. Original GLB, исходные карты и packed images в blend сохранены. Эти review gates намеренно используют нейтральный материал; PBR нужно отдельно подключить из original maps и проверить перед окончательным превью/игрой. Успех geometry/weight gate не скрывает этот отдельный незакрытый этап.

### Рейнджер: результат общей последовательности

`ranger-recipe-run.json`:6этапов завершены без ошибок gate. `ranger-float32-gpu.json`: все2 772 074положительных веса побитно сохранены, GPU/CPUdoublemax1.93e−7m в5позах. Стандартный Unityimport потерял поддержку у21 938seamvertices, maxcoefficienterror0.001913; этот путь не принят.

Loop/return boundary positionerror0, quaterniondot1. Сняты36raw-weightизображений с двух сторон, просмотрены центральный передний кадр и `ranger-unity-recipe/raw-contact.jpg`. Для сопоставимого stress-test использован action67; это **не назначение одного танца всем бойцам**. Уникальные танцы требуют отдельного подбора/проверки.

Статус обоих бойцов: проверочный full-density rig/recipe, не production prefab. Требуются PBR, полный playback/боевая выборка, source-geometry/UV roundtrip proof, GPU surface normals/tangents и реальная мобильная проверка.

### Проверка Моряка и Ретро по отдельному запросу владельца

Подробный результат: `docs/reports/shoulder-rollout-audit-2026-10-19.md`, машинные evidence — `sailor-retro-correction-audit.json`. Полный набор **не находится в игровых prefab ни одного из этих двух бойцов**. У Моряка он частично подключён только в web guard-v3; Ретро web остаётся native100k без Dance_Zero. Старые C1/clavicle-support варианты не считаются принятыми: поздний controlled reference report их отклоняет/отмечает недостаточными. Перенос принятого эталона Ретро теперь выделен следующим этапом.

### Native motion carrier Мастера

`master-native-motion/report.json`: производный96HzFBX сохраняет исходные positions/oriented topology/UV и все положительные именованные веса, содержит фактические9take текущегоnativepack. Проверены9внеключевых полных поверхностей для box_02/hit_to_head/front_kick_01, maxошибка≈10.55µm после Blender roundtrip. В отличие от неверного первого REST-witness, исправленный проход явно переводит armature в POSE и требует различающиеся поверхности в разных фазах. Первый статичный отчёт помечен invalid, не используется как acceptance.

Следующая версия скрипта дополнительно подготовлена с source-IDUV и проверкой take durations для Unity carrier; эти новые возможности ещё требуют отдельного запуска. Материалы и игровой native playback этим Blender-проходом не закрыты.

### Ретро: fixed-camera gain sweep не закрыл деформацию

`retro-shoulder-ab/visual-review.json`:4вариантаgain1/.5/.25/0,40кадров,3092отсчёта, одинаковые камеры. Численный endpointgatePASS; визуальная приёмкаNOT ACCEPTED: область подмышки/плеча требует разделения вклада weights/geometry/normals. Нельзя переносить пользовательское одобрение Sailor0% на Retro. Нет выбранного productiongain, веб/игра не заменены.
