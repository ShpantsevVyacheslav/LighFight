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

## Открытое решение

Владелец выбирает источник финальных моделей: внешний 3D-генератор с rig или
ассеты команды. Доступного инструмента генерации 3D-мешей в этой сессии нет.
Нельзя закрывать эту задачу скриншотом концепта или примитивами.
