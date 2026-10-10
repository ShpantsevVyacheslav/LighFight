# Финальные 3D-ассеты LighFight

Сочленённые модели в `Content/` — технические временные меши. Их качество не
соответствует финальному запросу на профессиональную графику и узнаваемые лица.
Фотографии и ранее созданные PNG-концепты в `assets/` — референсы; PNG не содержит
меш, UV, skeleton или animation clips. Генератор изображений их не заменяет.

## Контракт для финальных бойцов

- Prefab: `Assets/LighFight/Art/Fighters/fighter-0.prefab` … `fighter-7.prefab`.
- Масштаб: Unity units = метры; корень на уровне пола, локальная передняя ось +Z.
- Humanoid rig, валидный Avatar, Animator Controller; root motion выключен.
- Компонент FighterView в корне; его поле Animator указывает на Animator модели.
- Первый слой называется Base Layer. Состояния: Idle, Walk, Guard, Crouch, Hit,
  Fall, GetUp; атаки: jab, cross, hook, body, frontKick, lowKick, sweep, uppercut,
  knee, elbow, spinKick, jumpKick, backfist, axeKick, sideKick, roundhouseKick,
  hammer, palm, heelKick, switchKick.
- Симуляция задаёт нормализованную фазу атаки/падения; Animation Events не наносят урон.
- Нужны собственные или лицензированные клипы. Ассеты Tekken не используются.
- Source FBX/Blend, текстуры и права на ассеты сохраняются отдельно от bundles.
- LOD и бюджеты уточняются после первого измерения на устройствах.

Порядок персонажей соответствует браузерному прототипу:
0 bone, 1 witch, 2 master, 3 island (photo-06), 4 rose (photo-07),
5 ranger (photo-08), 6 gala (photo-09), 7 cobalt (правый человек photo-10).
Одежда ranger — вымышленная форма без реальных знаков различия.

## Арены

`Assets/LighFight/Art/Arenas/arena-0.prefab` … `arena-4.prefab`:
meeting, kitchen, open-space, washroom, yard. Корневой компонент OfficeArena.
Зона боя x=-5…5, z=0; декорации вынесены из неё. Временные арены расширяют комнаты
до игрового пространства и не являются точной реконструкцией размеров офиса.

ContentBuild использует художественный prefab, если он существует; иначе создаёт
временный. Художественные файлы не перезаписываются при сборке другой платформы.
Generated Content редактировать не нужно: сборщик обновляет его из рецепта.

## Первый меш сотрудника — Island

2026-10-10 владелец экспортировал текстурированный FBX из Meshy 6 Lite.
Исходники сохранены в `Assets/LighFight/Art/Source/Island/`, контрольные суммы —
`art/source/island-import.json`. Не заменять эти файлы результатами риггинга.
Бесплатный workflow требует атрибуции CC BY 4.0; перед релизом добавить credit
Meshy и ссылку на лицензию в экран авторства.

`LighFight.Editor.IslandArtAudit.Run` импортирует источник и рендерит три ракурса
в `art/previews/island-unity-*.png`. Измерение: 38 294 вершины, 66 364 треугольника,
исходный bounds 0.94 × 1.90 × 0.46 м. Preview нормализован до роста 1.7 м.
Костей и клипов в исходном FBX нет. Это пока не игровой prefab.

Meshy потребовал подписку для повторного авто-риггинга; платный тариф не включён.
Локальная подготовка скелета выполняется в Blender; скрипт
`scripts/art/rig_island.py` создаёт черновой скелет по ориентирам A-позы и
проверяет наличие весов. Наличие весов не заменяет визуальную проверку сгибов,
плеч, таза и одежды. До неё черновой результат не подключать к roster.

Первый локальный проход: 21 кость, 0 вершин без весов. Unity подтвердил
`Avatar.isValid && Avatar.isHuman`. `IslandArtAudit.RunRig` рендерит сгибы локтей
CPU skinning, чтобы не зависеть от обновления GPU между кадрами batchmode.
Черновой FBX — `Art/Drafts/Island/island-rig-draft.fbx`; Blender source — локально
`art/source/island-rig-draft.blend`. Локти/кисти требуют доработки;
боевых клипов ещё нет, проверка на устройстве не выполнена.

## Первый проход анимаций Island

`scripts/art/animate_island.py` создаёт собственные черновые движения на локальном
скелете: Idle, Guard (1.483 с), jab (0.517 с), frontKick (0.85 с), 60 fps.
Двухзвенное решение позы ограничивает вытягивание руки/ноги длиной костей.
Исходник `art/source/island-motion-draft.blend`; экспорт
`Assets/LighFight/Art/Drafts/Island/island-motion-draft.fbx`. Source mesh не изменён.

`IslandMotionAudit.Run` проверяет Humanoid Avatar, воспроизводит клипы через
PlayableGraph и снимает 12 фаз каждого удара. AnimatorCullingMode.AlwaysAnimate
обязателен: после выключения исходного SkinnedMeshRenderer для CPU-рендера
обычный culling останавливал скелет и давал одинаковые кадры.
Замеры: LeftHand в jab — 0.205921 м; RightFoot в frontKick — 0.967374 м;
ошибка возврата обоих клипов — 0 м. Проверка отвергает неподвижные клипы и
возврат с ошибкой больше 3 см. GIF в `art/previews/island-motion` показывает
снятые кадры; джеб в GIF замедлен для осмотра.

Самопроверка: открытые кисти не являются кулаками; пальцы пока не имеют костей.
Нужны перенос веса/работа корпуса, доводка плеч и суставов, постановка стоп,
оставшиеся клипы по контракту и проверка на телефоне. Эти blocking-клипы не
подключены к финальному roster и не считаются уровнем профессионального файтинга.

## Боевые кулаки и корпус — второй проход

Источник Meshy плохо деформирует пальцы при сжатии: попытка добавить 30 костей
дала заломы. Применён отдельный вариант закрытых кистей через
`scripts/art/fists_island.py`: непрерывная сглаженная геометрия кулаков,
привязанная к LeftHand/RightHand исходного 21-костного скелета. Открытые кисти
остаются в оригинальном FBX и `island-rig-draft.blend`. Это не анимация пальцев;
в боевом варианте руки всегда сжаты. Материал кулаков пока упрощённый, без UV
кожи; стык с запястьем и анатомическая детализация требуют художественной доводки.

Добавлены cross (0.617 с), hook и uppercut (оба 0.717 с); всего 7 клипов.
Исходники закрытых кистей — `art/source/island-combat-hands.blend`, анимаций —
обновлённый `island-motion-draft.blend`. Полный разворот корпуса: jab/cross ~28°,
hook ~40°, uppercut ~22°. Поворот головы частично компенсирует разворот груди.

Unity import фиксирует исходную ориентацию и запекает root translation/rotation
в pose; preview включает Foot IK и root motion=false. При прежних настройках
аудит обнаружил сдвиг опорной стопы 0.06695 м; после исправления максимум
0.00119 м. Это измерение кости стопы, а не физическая проверка контакта подошвы.
Аудит отвергает дрейф >5 мм, пропавший разворот корпуса <15°, неподвижный удар
и ошибку возврата >3 см. Humanoid bones сопоставляются явно; skeleton description
обновляется при изменении состава костей, чтобы не использовать старый список.

Самопроверка: это всё ещё blocking-анимации. Кулаки упрощены; реакции на удар,
падение, перемещение и остальные приёмы ещё не перенесены на этого бойца.
Финальный prefab/AnimatorController и мобильная производительность не проверены.

Следующие гейты: проверить деформации, Humanoid Avatar, клипы по контракту,
LOD/материалы и производительность на телефонах. Нельзя закрывать эту задачу
скриншотом концепта, статическим мешем или примитивами.


### Island: movement and recovery review (2026-10-10)

Added local Walk, Crouch, Hit, Fall and GetUp blocking clips (12 clips total).
Fall and GetUp share the same ground pose. A per-frame deformed-mesh floor
constraint lifts the pelvis where needed. Unity rejects mesh penetration deeper
than 2 cm, stationary walk/reaction clips, and a Fall/GetUp pelvis seam >2 cm.
The first floor check failed at -0.216 m, then -0.111 m after lowering the hands;
these failures demonstrated that the geometry check detects the defect.

Open `Assets/LighFight/Art/Drafts/Island/IslandMotionReview.unity` and press Play:
left/right arrows move, down crouches; J/K/L/U/I select jab/cross/hook/uppercut/kick;
H shows recoil, F falls, R gets up after falling. Clip changes blend over 120 ms.
This isolated art review uses a PlayableGraph; it is not the combat simulation
and does not award damage. The incomplete fighter is not promoted to the roster.

Самопроверка: recovery is still blocking (reversed fall), not a polished kneeling
get-up. Walk needs foot-contact timing and backward locomotion polish. The scene
and graph are checked in batch Unity; interactive input and phones remain unchecked.

Final mesh minimum height in Unity: Fall 0.01735 m, GetUp 0.01748 m;
Walk 0.00377 m. All 11 selectable motions switched through a live PlayableGraph
without exceptions in the review scene. This is not a keyboard/device test.
Unity EditMode suite: 46/46 passed after this stage.
