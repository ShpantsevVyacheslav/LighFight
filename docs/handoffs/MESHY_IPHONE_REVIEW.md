# LighFight — передача агенту «Нативная лента IOS»

Владелец закрепил сборку и установку на iPhone за этим агентом. Агент LighFight
готовит сцену и ресурсы, не запускает мобильную сборку и не заменяет артефакты LighChat.

## Подготовленные ресурсы

Unity project: `/Users/macbook1/Documents/ChatGPT/LighFight/unity`.
Editor: `/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity`.
Scene: `Assets/LighFight/Art/Drafts/IslandPremium/MeshyArtReview.unity`.
Model: `island-meshy-jab.fbx` в той же папке. Generic rig сохраняет веса Meshy;
Humanoid retarget и Foot IK отключены, чтобы видеть исходные деформации.
Это отдельный графический стенд с одним джэбом, не полноценный бой.

## Сборка — выполняет владелец мобильной сборки

Вызвать `LighFight.Editor.MeshyReviewSetup.ExportIosReview` через Unity batchmode
с `-buildTarget iOS`. Экспорт окажется в `unity/Builds/ArtReviewiOS`, отдельно
от `Builds/iOS` и UnityFramework LighChat. Это рецепт, экспорт пока не проверен.
Команда экспорта (запускает агент мобильной сборки):

```sh
/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -buildTarget iOS -projectPath /Users/macbook1/Documents/ChatGPT/LighFight/unity -executeMethod LighFight.Editor.MeshyReviewSetup.ExportIosReview -logFile /Users/macbook1/Documents/ChatGPT/LighFight/.tools/reports/art-review-ios-export.log
```

Открыть полученный Xcode-проект, выбрать существующую команду подписи, устройство
и собрать development app. Bundle ID: `com.lighchat.lighfight.artreview`.
Не заменять текущий Runner/LighChat; это отдельное приложение.
Метод восстанавливает настройки идентификатора, имени и ориентации Unity после экспорта.

## Проверка владельцем на iPhone

- Landscape, кнопки внутри safe area; поворот камеры свайпом над панелью.
- Пауза, скорость ¼, покадровый шаг и ползунок времени (перемотка включает паузу).
- Кнопки «Локти / кисти» и «Икры / колени» приближают нужную область.
- Осмотреть оба локтя спереди/сбоку/сзади при сгибании и выпрямлении.
- Осмотреть левую икру и сравнить с правой; проверить пересечение колена/шорт.
- Проверить отсутствие разрыва запястий. Открытые ладони — известный дефект.
- Проверить возврат джэба, читаемость кнопок, плавность и нагрев.
- Сделать скриншот дефекта и указать положение ползунка/ракурс.

Самопроверка: волосы, ладони, локти и левая икра требуют доработки. На телефоне
не проверено. Стенд ещё не подключён к Game Hub и не опубликован в CDN.
Офисные арены и остальные персонажи здесь не проверяются.

Локальная проверка: Unity Generic rig, 24 кости, клип 1.75 секунды после FBX-экспорта,
25 поз; максимальное смещение вершины 0.986 м. Это проверка воспроизведения,
не доказательство корректной анатомии. Масштаб костей голени и предплечий
в исходном GLB постоянен (отклонение от 1 менее 0.000001).

## Обновление: сравнение весов

В сцене теперь два варианта. Кнопка «Вариант: исходник Meshy / правка весов»
переключает их, сохраняя время клипа и камеру. Исходник выбран по умолчанию.
У правки ограничено влияние бедра на голень и плеча на предплечье;
совпадающие вершины на UV-швах получают одинаковые веса. Геометрия не изменена.
Unity сравнил 25 поз: максимальная разница положения вершины 6.55 см. Это
кандидат, а не подтверждение правильной формы локтей и икры. Проверять оба
варианта в паузе; фиксировать, какой вариант показан на скриншоте.

## Обновление: коррекции формы суставов

Вариант правки теперь содержит четыре blend shapes. Локти и левое колено
корректируются по углу сгибания, контур левой икры — мягкой постоянной коррекцией.
В кнопке написано «правка суставов». Исходный вариант сохранён. В Unity
максимальное влияние коррекции в 25 позах — 3.49 мм. Проверки выявляют
отключение коррекции и возврат исходных весов. Проверять прежние дефекты,
особенно на максимальном сгибании. Другие удары этой коррекцией не проверены.

## Separate finger geometry candidate — 2026-10-10

New isolated scene: `Assets/LighFight/Art/Drafts/IslandFingers/FingerArtReview.unity`.
Regenerate/import/audit with `LighFight.Editor.FingerHandAudit.Verify`.
52 deform bones: original 24 + 28 digit bones (14 per hand), five distinct digits
per hand; closed pose captured as ClosedFists shape. The optional shape is driven
by JointCorrectives; older four-shape model remains supported.

This scene is an art draft, not combat/CDN promotion. Source/default view remains
original; candidate toggle shows rebuilt hands. Palm skin is a flat draft
material; skin details, nails, palmar creases and final wrist silhouette require
further work. Frame through jab, compare thumb position/finger collisions and
wrist join from both sides. Phone build/install belongs to «Нативная лента IOS».
The existing ExportIosReview exports the older scene; do not assume it exports
FingerArtReview. Select the new scene explicitly in a separate review build.
No phone verification is claimed here.

### Anatomy V2 study (owner rejected first hands)

Optional comparison scene:
`Assets/LighFight/Art/Drafts/IslandFingersV2/FingerArtReview.unity`.
Recreate and audit via `LighFight.Editor.FingerHandAudit.VerifyAnatomyV2`.
This revision has tapered phalanges, flatter palm and isolated digit weight fields.
It still has angular thumb-base folds and gaps at tight closure; no final art
acceptance or combat promotion. Preserve both earlier scenes for comparison.


## MakeHuman anatomical hand candidate — 2026-10-10

Isolated scene: `unity/Assets/LighFight/Art/Drafts/IslandMakeHumanHands/FingerArtReview.unity`.
62 bones, 38 added finger/palm bones; authored CC0 anatomy and weights.
25 Unity jab samples pass; zeroed fist morph mutation detected. Both wrist seams
closed; deleted bridge face mutation detected. Scoped thumb/finger base-face
overlaps reduced from 288 to zero on both hands (excludes palm/web surfaces).
Skin transition and visual/device acceptance remain pending. No phone build or
installation performed: owner is «Нативная лента IOS». Select this scene explicitly;
existing ExportIosReview uses the older scene. Combat/CDN asset unchanged.

## Closed Meshy fist candidate — 2026-10-11

Current optional review scene: `Assets/LighFight/Art/Drafts/IslandMeshyStaticFists/FingerArtReview.unity`.
Run `LighFight.Editor.StaticFistAudit.Verify` to recreate scene/materials and audit
rigid fists. New character is still inactive by default; enable candidate comparison
in the review UI. Existing ExportIosReview targets the original scene, so select
this scene explicitly for a separate review build. Do not replace LighChat assets.

24 original bones; closed static fists (no opening), neutral left wrist at jab impact,
source body skin maps, rebuilt rigid watch. Check both wrist seams, thumb placement,
skin detail scale, silhouette and shadows at actual phone resolution.
Build/install remains owned by «Нативная лента IOS»; no device verification performed.
