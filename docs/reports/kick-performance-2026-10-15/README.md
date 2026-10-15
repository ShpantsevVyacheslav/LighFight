# P0: рывки при ударах ногами и нагрев за ~5 минут

Новый пользовательский дефект 15 октября: тормоза особенно заметны при обычных и прыжковых ударах ногами, сильный нагрев примерно через5мин. THERMAL-01 остаётся открытой; локальная проверка не является thermal-приёмкой iPhone.

Гипотеза1: повторные полные обходы weighted support points при падениях/подъёме. У Островного260точек/623влияния, у Ретро58198/165519; сильные jumpKick/axeKick/heelKick/sweep вызывают падение. NativeSupportOverlay вычисляет минимум пола дляcap, затем CandidateFootGrounding повторяет вычисление длякоррекции. Контактная выборка и LateUpdate добавляютповторы. Это доказанные source counts, доactualпрофиля не миллисекунды и не установленная единственная причина нагрева.

Изолированный actualActor/Core Metal player сравнит idle/frontKick/jumpKick/Fall→GetUp на Island/Rose/Retro по200кадров. Геометрия/текстуры/Avatar/исходные клипы не меняются. Markers: LighFight.Grounding.Apply, .Cap, LighFight.Actor.Pose, LighFight.Contact.Sample; фиксируются wall/main/GC и действующий наборfloorpoints.

Отдельно остаются вопросы безусловной contact-выборки, catch-up coreticks, аллокаций, hit-spark и намеренного contactFreeze. Их не следует одновременно менять до изолированного измерения первого механизма.

## Результат последовательных изменений

Все три Metal Development player используют одни и те же shipping-prefab, Avatar, контроллеры, текстуры и материалы: artist SHA списки baseline/current идентичны. Измерено2400кадров на вариант: Island/Rose/Retro × idle/frontKick/jumpKick/Fall→GetUp ×200. Это actualActor/Core диагностическая сцена с автоматической камерой, а не полный интерфейс Prototype, не iPhone и не измерение энергии.

| Вариант | Retro Fall/GetUp: p95 кадра | Средний кадр только при body support | Что изменено |
|---|---:|---:|---|
| Baseline |83,24мс|79,05мс|Прежний расчёт + только profiler markers|
| Только минимум пола с cache/Y |51,22мс|45,52мс|Тот же набор weighted-вершин, точный matrix-cache и расчёт только Y; cap/коррекция разделяют raw minimum|
| Итог |16,93мс|16,70мс|Дополнительно не вычисляются контактные позы, когда ни у кого нет нужного окна атаки|

Постоянная схема с повторными тяжёлыми обходами запускала до5догоняющих coreticks за кадр. В итоговом Fall/GetUp-проходе — максимум1. Поэтому21body-modeкадр baseline против93итоговых не означает разные анимации: тот же цикл падения и подъёма представлен большим числом плавных кадров, а не урезан. Не суммировать вложенные profiler-маркеры Contact/Pose/Grounding как независимое CPU-время.

Пропуск contact pose не меняет симуляцию: Match.Step продолжает60Hz, физическая выборка обязательно выполняется за тик ДО первого активного кадра и на всех активных кадрах обоих ударов. Между окнами предыдущие sweep-точки сбрасываются и очищается same-frame native contact cache. Восстановление, реакция и падение продолжают обычную render-анимацию. Никакие удары/контакты или full-body floor points не удалены; intentional contactFreeze не изменён.

## Корректность

- GroundingPerformanceTests: RED4/4 до cache, GREEN5/5 после;80случайных взвешенных преобразований совпали с оригинальным XYZ расчётом. Точный Matrix4x4.Equals учитывает весь world transform; смена support array, положения/масштаба/поворота костей/бойца приводит к пересчёту. Изменение maxCorrection повторного скана не требует. При in-place authoring-правках immutable runtime support-массивов предусмотрен InvalidateMinimumCache.
- Actual held-pose gate:11shipping-актёров ×6состояний ×5фаз =330случаев; максимум ошибки legacyXYZ→Y —1,1920929e-7м. Проверены коррекция, повторное использование и инвалидация при изменении/возврате кости. Исходные Avatar/meshes/assets не изменились.
- PhysicalContactSamplingTests: исходный RED2/3, после изменения3/3; отдельная stale-pose регрессия RED1/4→GREEN4/4. Сравнены все25ударов на0,65/3метрах с always-sampled reference: одинаковые здоровье, HitSerial, DownTicks на каждом шаге.
- NativeContactSamplingTests: реальные модели Retro/Island, frontKick/jumpKick на0,65/3метрах, по105реальных EOF-кадров на случай; те же исходы, что при постоянной выборке. Сокращение sample pairs подтверждено.
- Итоговая проверка:258/258 EditMode,8/8 Selected PlayMode,101/101 Core. Оба actual-player прогона completed/errors0; листы кадров baseline/current просмотрены. GC thread-allocation counter в этом Mono-прогоне возвращал0 и не принят как доказательство отсутствия аллокаций.

Диагностические MonoBehaviour, Editor-build helpers и сцена вынесены из Assets в `.tools/kick-performance-review/AcceptedStaging` перед передачей production-сборки. Копии исходников QA и CSV сохранены здесь; app/rawPNG остаются в `.tools/kick-performance-review/Builds`.

## Открыто по THERMAL-01

Нужен длительный (не менее10мин) профиль новой сборки внутри LighChat на iPhone17ProMax: бой с ударами ногами/падениями, CPU/GPU/pacing/память/thermalState до и после5мин, плюс реальная арена, тени, UI, hit-feedback. Владелец в это время тестирует другое направление приложения; устройство не перезапускалось нами и активная конференция не прерывалась. Этот код устраняет измеренный CPU hot path, но не доказывает исправление общего нагрева или других рывков. Задача P0 остаётся «В работе».
