# Tripo: проверка модели и рига, 2026-10-12

Проект: 2aa5f79e-109f-45c8-8f48-33c9e806aa5b. Референс employee-tropical-apose-v2.png. Публичная генерация разрешена владельцем. Работа через веб-интерфейс, не API.

## Проверенные факты

- H3.1, максимальное качество. Исходник: 1 995 228 треугольников, 1 020 888 вершин.
- Автоскелет отказал исходнику и версии 150 000 треугольников.
- Ретопология без Smart Mesh: 150 000 (5 кредитов), затем 100 000 (5 кредитов).
- На 100 000 треугольниках / 56 090 вершинах создан Unity Humanoid Compatible риг за 20 кредитов.
- Баланс 200 → 170. Генерация была пробной; дополнительные деньги и подписка не оплачены.
- Визуализированный скелет проходит через кисти и пальцы. Число костей и веса без экспорта неизвестны.
- Проверены пресеты бокс_01 (2.20 с), бокс_02 (2.79 с), бокс_03 (2.54 с). Пресеты не уменьшили отображаемый баланс.
- Контрольные кадры: 01 на 0.8 с, 02 на 1.4 с, 03 на 1.2 с; также просмотрены движущиеся кадры.
- У 01/03 открытые ладони. У 02 на контрольном кадре кулаки закрыты. Нельзя считать все три готовыми боксерскими клипами.
- По выбранным кадрам лицо и верх тела выглядят лучше предыдущего Meshy-рига. Это субъективное визуальное сравнение разных просмотрщиков, не доказательство сохранения качества в Unity.
- У шорт остаются деформации вокруг коленей. Полный покадровый аудит ещё не выполнен.
- Экспорт GLB с 4K открыл требование подписки. Файлы не скачаны. Premium-блокировка не обходилась.

## Следующий этап

1. После доступного штатного экспорта скачать исходник и риг отдельными файлами.
2. Проверить вершины, UV, текстуры, bind pose, иерархию пальцев и веса без собственной перепривязки.
3. Сравнить исходник и риг в одном свете/камере в Blender и Unity.
4. Проверить кулаки, плечи, локти, колени в начале/контакте/возврате; принять только прошедшие клипы.
5. Сохранить исходную поверхность высокого разрешения. Если риг низкой плотности хорош, отдельно проверить перенос его весов на исходную поверхность, сохраняя UV и геометрию; не заменять игровую модель до визуальной проверки.

## Самопроверка

Не утверждаю, что риг экспортирован, импортирован в Unity, либо проверен на iPhone. 8K переключатель был пробно включён при генерации, но фактическая текстура не измерена. Снижение сетки не равно автоматическому сохранению качества: нужны одинаковые кадры после экспорта. Текущий блокер — платный экспорт Tripo.

## Дополнительная проверка пресетов и P2.0

- Mixamo повторно создан на той же сетке 100 000 треугольников за 20 кредитов. Анимация бокс_01 на 0.8 с загружена, но персонаж расположен ниже камеры. Новый просмотрщик и перезагрузка не исправили кадрирование; отдаление показывает всю фигуру, но не позволяет сравнить детали. Причина координат/камеры без выгрузки неизвестна. Улучшение привязки не доказано.
- Баннер P2.0 открывает вкладку генерации Smart Mesh по изображению, а НЕ P2.0 ретопологию текущего ассета. Предыдущее предположение о прямой переработке существующей модели было неточным.
- Одна бесплатная публичная P2.0 генерация по тому же референсу: проект b6558ebb-9bd6-4036-8c7a-8ea9cd3ea3f7, 5 687 Quad-граней, 5 228 вершин, без текстуры. Это новая геометрия, а не контролируемая замена только топологии исходного H3.1.
- Создан Unity-риг P2.0 за 20 кредитов; просмотрены бокс_01 на 0.8 с и бокс_02 на 1.4 с. Контур угловатый, открытая ладонь бокс_01 сохранилась. Детальная производственная замена не принята. Текстурирование не запускалось: оно не проверяет деформации и не вернёт отсутствующую геометрию.
- Баланс 170 → 130. Итого проверка Tripo потратила 70 бесплатных кредитов с исходных 200; подписка не куплена.
- Скриншоты: mixamo-boxing01-0.8.png (проблема кадрирования), p2-wire.png, p2-boxing01-0.8.png, p2-boxing02-1.4.png.

Самопроверка: победитель сравнения ригов не установлен. Не называем Mixamo лучше по неполному кадру; не называем P2.0 более детальным из-за Quad. Основной кандидат остаётся H3.1 + Unity до штатного экспорта и одинаковой камеры в локальном стенде.

## После покупки Pro: локальная проверка

- Подписка активна. Studio и API используют отдельную оплату; здесь используется Studio через штатный интерфейс.
- Сравнены скачанные Mixamo, ActorCore, UE5 Mannequin и VRM на выбранной фазе box_01. Явного улучшения поверхности относительно Unity не найдено; полного покадрового сравнения всех движений пока нет.
- Выбран Unity Humanoid Compatible для дальнейшей интеграции. Родной FBX: 100000 треугольников, 50000 вершин, 67 костей, максимум 4 влияния, 0 непривязанных вершин.
- Родной пакет содержит 7 клипов: box_01, box_02, front_kick_01, walk, greet_01, dance_01, defeat_02. Последний является эмоциональной реакцией, а не падением. box_01 — с открытой ладонью; box_02 требует выделения отдельных ударов и перехода обратно в стойку.
- Проверено в отдельном Unity 6000.3.26f1 тем же PrepareImporter: Generic, без resample/compression/weld, текстура без компрессии. Exit 0, 100000 треугольников, цвет 8192, 7 клипов, 28 CPU-skinned кадров. Тест не подтверждает FPS или вид на iPhone.
- Лицо и торс нового кандидата на выбранных кадрах приемлемы; прежнего сильного сжатия талии не видно. Кандидат ещё не заменяет производственный prefab.
- Розовая пантера H3.1 создана приватно за 55; исходный GLB 1865580 треугольников сохранён отдельно. Рабочая сетка 100000 за 5, Unity-риг за 20.
- Для новых генераций Pro доступно 4K; 8K открывает Max upgrade. Дополнительная подписка не покупалась. 8K Островного сохранились от пробной генерации.
- Рейнджер, Костяная леди, Ночная ведьма и Мастер стихий запущены по отдельным подготовленным референсам, каждый H3.1 maximum/private/4K за 55.

## 13 October: controlled Unity parity and close-up camera correction

Independent Blender reference for untouched native Unity FBX box_01: 37 samples, 67 bones, 1,613 surface vertices per frame, 100,000 triangles. Unity without resampling: max bone position deviation 0.9305 mm and max surface deviation 0.9416 mm; pass below 1 mm. Resampling increases surface deviation to 4.2109 mm; keep resampling disabled. Report: `art/previews/island-tripo-v1/tripo-unity-pose-parity.json`.

The apparently sliced native hand in our diagnostic close-up was camera clipping, not evidence of collapsed topology: the camera was approximately 34 cm from the wrist with Unity default near plane 30 cm. Setting near plane to 1 cm restored the full fingers visually. Review helpers additionally initialize/disable Animator, use Unlimited skin weights, and compensate BakeMesh local scale. These changes are review infrastructure, not a claim that production or device quality is finished. Native-open image now displays intact fingers.

Current isolated runtime suite: 113/143 pass, 30 fail. Failures reference missing source models, shaders, scenes, or editor importer classes in the reduced QA fixture. All 8 NativeRigBones tests pass, with old-implementation mutation previously rejecting 6/8. The full application suite is not reported green.

Full main-project EditMode verification after fixture recovery: 143/143 pass. The lone first-run failure selected the first Camera, which can be the cullingMask=0 backdrop camera. The test now selects the Prototype-owned fighter-rendering camera and retains assertions for target texture, transparent background, and battle viewport restoration. No production condition was relaxed. Result: `.tools/main-runtime-suite.xml`.

Untouched source close-ups rendered for Ranger (1,986,463 triangles), Bone (1,888,146), and Master (1,992,276), each with original 4096 texture maps. They are reference evidence, not proof of runtime integration.

Arena promoted to main GameContent arena index 4 after source-preserving FBX import and 1,922,912-triangle clearance audit. Before/after catalog diff changes only the courtyard reference; other asset references unchanged. Main prefab GUID 50ab0d0e3fb0147758bf28947c9fb420. Imported 4096 albedo; no shadows. Combat fixture rendered at production minimum camera distance 3.6 m / FOV 42. Background visual polish and device performance remain pending.
