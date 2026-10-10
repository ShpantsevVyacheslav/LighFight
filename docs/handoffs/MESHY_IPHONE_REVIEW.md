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
