# LighFight — автономная разработка solo

Цель: законченная игра против бота в Game Hub LighChat. Онлайн PvP — следующая версия.
Рабочие правила: `/Users/macbook1/Desktop/LighChat/.agents/skills/autonomous-work/SKILL.md`.
Коммиты в локальный main, push и публикация ресурсов — по явной команде владельца.

| Задача | Код | Проверка | На устройстве |
|---|---|---|---|
| Боевые окна, дистанция, блок, 20 приёмов | Есть | 16 исходных unit + Unity | Нет |
| Native host Android/iOS и Game Hub | Есть | Flutter 8, native compile/manifest | Нет |
| Раунды до 2 побед, таймер, input buffer | Есть | .NET 23; 7 мутаций обнаружены | Нет |
| Сочленённые модели и движения по фазам удара | Есть, временная геометрия | Unity compile прошёл; preview выявил и исправлен знак маха ногой | Нет |
| Офисные 3D-арены и выбор бойцов | Есть: 8 вариантов и 5 арен; временные меши | Unity 24/24 + рендер переговорной | Нет |
| Knockdown, counter hit, recovery | Есть; 20 приёмов | .NET 28/28, Unity 29/29, 5 мутаций | Нет |
| Remote starter content: manifest/catalog/download/scene | Есть; mobile bundles собраны, CDN не опубликован | Unity 46/46; HTTP + offline packed smoke | Нет |
| Полноценные модели сотрудников, rig, материалы, анимации | Требуется; текущие меши не финальные | — | Нет |
| Первый сотрудник Island: меш и локальный скелет | Источник импортирован; Blender draft с 21 костью | 5 файлов проверены по SHA256; автоматические веса без непривязанных вершин; деформации проверяются отдельно | Нет |
| Island: первые боевые анимации | 4 blocking-клипа: Idle, Guard, jab, frontKick; ещё без кулаков | Unity Humanoid; 12 фаз ударов; перемещение кисти 0.206 м/стопы 0.967 м; возврат 0 м | Нет |
| Island: кулаки, корпус, новые удары | Закрытая геометрия кистей, cross/hook/uppercut; 7 blocking-клипов | Unity Humanoid; разворот 22–40°; опорная стопа дрейф ≤1.2 мм после исправления root settings | Нет |
| Обучение, меню, RU/EN, звук, бот с задержкой реакции | Есть | 40 core / 46 Unity; проверки ошибок через мутации | Нет |
| Полный клиент: сборка, launch/exit/relaunch | Требуется | — | Нет |
| Производительность и внутренняя мобильная сборка | Требуется | — | Нет |

## GitHub

Закрытый репозиторий создан 2026-10-10: https://github.com/ShpantsevVyacheslav/LighFight.
Локальный origin подключён. Код ещё не опубликован.

## Самопроверка

Документы прежнего этапа содержали устаревшую запись, что Flutter host не реализован.
Источник истины — код и результаты проверок; эту запись требуется обновить.
Нельзя считать генерацию концепта созданием rigged 3D-модели.

Самопроверка доставки: исправлены ожидание JSON вместо bin, обратный знак маха
ногой, ленивый перебор материалов при замене ссылок и отсутствие выхода из меню.
Все bundles пока с временными моделями; они не являются финальной графикой.

## Итог локальной проверки 2026-10-10

46/46 Unity и 40/40 .NET. Добавлены проверка места, замена старого каталога,
сохранение языка до загрузки ресурсов, отсутствие мерцания шага при 120 Hz,
очистка объектов сцены. Новые проверки проверяются мутациями в отдельных копиях.
Android AAR и iOS UnityFramework пересобраны с OfficeLit и скопированы в
игнорируемые каталоги плагина LighChat. Полный клиент и телефоны не проверены.
Подготовлен отдельный Firebase Hosting site `lighfight-assets-72b24` в конфигурации;
сам сайт не создан и ресурсы не опубликованы. GitHub по-прежнему без push.

Starter-4 подготовлен: Android 151628 байт, iOS 147791 байт bundles.
Пакет содержит временные модели; объём финальной графики будет больше.
Проверки ловят 29 намеренных дефектов: 24 core и 5 Unity runtime/catalog.

## Публикация 2026-10-10

По явному разрешению владельца main отправлен в закрытый GitHub.
Отдельный Hosting site создан, starter-4 опубликован. Android/iOS manifests,
каталоги и bundles отвечают HTTP 200; bundles совпадают с локальными файлами.
Первый deploy выявил запрет Firebase на public вне каталога конфигурации;
конфигурация перенесена в корень как firebase.lighfight.json.
Полный клиент с Bootstrap и запуск на телефонах всё ещё не проверены.


## Island movement review — 2026-10-10

12 local draft clips, including locomotion, crouch, recoil, fall and recovery.
Separate keyboard review scene; no Meshy quota used. Art audit covers mesh/floor
penetration and Fall/GetUp continuity. Main combat prefab still awaits all move
states and animation polish. Device verification: no.


## Wrist/jab correction — 2026-10-10

Local mesh bridge on both wrists; zero open wrist edges. Smooth wrist weights;
jab timing aligned with 10/3/15 combat frames. Close-up sequence for review.
Overall character quality remains below target. Meshy MCP researched, requires
API key and available API balance; not connected. Phone verification: no.

## Meshy Premium pipeline — 2026-10-10

Official Meshy MCP 0.6.1 connected through its SDK. API key stays outside the
repository in a private local file. Premium Island candidate generated from the
approved reference: Meshy 7.1, 4K geometry and PBR textures. Original mesh:
305,044 triangles. Remesh target: 60,000 triangles; actual count awaits import.
Provider automatic rig and preset Left_Jab_from_Guard (191), 60 fps, completed.
Total confirmed spend: 48 credits (35 generation + 5 remesh + 5 rig + 3 motion).
Downloaded GLBs validated by header length and SHA256. Mobile mesh: 61,801
triangles; animated mesh: 61,881. Reviewed 24 frames of the full 1.733-second jab:
wrist separation not observed, but open palms remain. Provider rig has 24 bones,
zero finger bones. Hair clumps remain visible. Candidate is not promoted to the
final roster. Preview: `art/previews/island-premium/jab.gif`.
Device verification: no. No push or resource publication during this stage.

Самопроверка: это рабочий кандидат, не профессиональная финальная модель.
Следующая задача — боевые кисти с сохранением непрерывной геометрии запястий,
затем волосы и проверка локтей на других ударах. Генерация остальных персонажей
ждёт проверки этого шаблона, чтобы не повторять дефекты и расходовать кредиты.

Замечания владельца: деформация локтей и левой икры. Требуется аудит топологии
и весов в согнутых позах; отсутствие отрыва кистей не доказывает корректность
всех суставов. iPhone 17 Pro Max доступен через devicectl, но новый Meshy-кандидат
ещё не импортирован в Unity и не входит в опубликованный starter bundle.
Первую проверку кандидата на телефоне делать в отдельной Unity art-review сборке
с поворотом камеры, паузой и замедлением; затем проверять внутри LighChat.

## Unity Meshy review — 2026-10-10

Imported provider FBX, Generic rig and jab into an isolated review scene. Touch
camera orbit, joint close-up presets, pause, quarter speed, frame stepping and
timeline scrub implemented. Unity sampled 25 poses: 24 bones, 1.75-second clip,
0.986 m maximum vertex displacement. Existing EditMode tests: 46/46 passed.
Initial import had a missing Animator; fixed and playback audit passed.
Source animation scale tracks remain constant for both calves and forearms.
This does not establish correct weights/topology or resolve the reported defects.

Phone build/install belongs to agent «Нативная лента IOS», per owner. No device
build or install executed here. Separate export recipe and findings in
`docs/handoffs/MESHY_IPHONE_REVIEW.md`; output is `Builds/ArtReviewiOS`, not
LighChat's Unity export. Самопроверка: touch controls and iOS export are not
device-verified; elbows, left calf, palms and hair remain open art issues.
