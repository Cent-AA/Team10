# Deep Audit: MainMenu, CharacterSelect, TestArena_PrototypeMVP, TrainingScene

Дата аудита: 2026-06-20

Источник: `CodexReports/CodexSceneReport_AllScenes_20260620_212518.md` плюс текущие файлы сцен и скриптов на диске.

Важно: последний scene report немного устарел относительно текущих `.unity`. Например, в report у `CharacterSelect` указаны `wasdPressed/arrowsPressed = null`, а в текущем `Scenes/CharacterSelect.unity` эти спрайты уже назначены. Поэтому report использовался как снимок и карта сцен, но финальные выводы сверены с текущими YAML/скриптами.

## Общий вердикт

Текущий основной поток должен быть:

`MainMenu -> CharacterSelect -> TestArena_PrototypeMVP -> MainMenu`

Этот поток уже почти собран. Самая сильная сцена сейчас - `TestArena_PrototypeMVP`: там настроены волны, костер, revive, prototype cards, enemy variants и переход в меню. Самые проблемные зоны - не одна конкретная ссылка, а дисциплина проекта: старые тестовые сцены включены в билд, Training не включен, часть UI завязана на автопоиск по именам, а prototype/runtime UI и старые тестовые панели сосуществуют рядом.

## Высокий приоритет

1. `TrainingScene` не включена в Build Settings, а `TestArena.unity` и `SampleScene.unity` включены.
   - `EditorBuildSettings.asset` содержит `MainMenu`, `CharacterSelect`, `TestArena`, `TestArena_PrototypeMVP`, `SampleScene`.
   - `TrainingScene` отсутствует.
   - Риск: в билде будет старый/тестовый мусор, а Training нельзя загрузить штатно.

2. В `TrainingScene` кнопки паузы имеют потерянные callback target.
   - В текущем YAML у кнопок стоят `m_Target: {fileID: 0}` при `m_TargetAssemblyTypeName: PauseManager`.
   - Затронуты `OnMainMenuButton`, `OnExitButton`, `OnRestartButton`, `ResumeGame`.
   - В `TestArena_PrototypeMVP` эти же кнопки ссылаются на реальный `PauseManager`, поэтому MVP-арена выглядит здоровее.

3. `CharacterSelect` показывает 4 карточки, но `CharacterSpawner` фактически поддерживает только 2 уникальных персонажа.
   - `CharacterSpawner.GetPrefab(2)` и `GetPrefab(3)` возвращают `medicPrefab/sniperPrefab`, но в обеих целевых аренах эти поля назначены на Heavy.
   - Риск: игрок выбирает Medic/Sniper, но получает Heavy.

4. `CharacterSelectTransition` и `MenuTransition` слишком доверяют сцене.
   - `CharacterSelectTransition.Start()` напрямую обращается к `forestOverlay`, `moon`, `characterCards.Length`, `characterCards[i]`.
   - `MenuTransition` напрямую использует `logo`, `buttons[i]`, `moon`, `forestOverlay`.
   - Сейчас ссылки есть, но один удаленный/переименованный UI объект превращает переход в `NullReferenceException`.

5. В проекте есть несколько владельцев `Time.timeScale`.
   - `PauseManager`, `PrototypeCardRewardManager`, `PrototypeRunStats`, старые `Cards/CardsTestArena`, hit-stop в Player/Engineer меняют `Time.timeScale`.
   - Риск: пауза, награда после волны, game over и hit-stop могут конфликтовать. Нужен один `GameState/PauseService`.

## MainMenu

Сильные стороны:

- Сцена есть в build settings и является первым пунктом.
- `MenuTransition.nextSceneName = CharacterSelect`.
- Основные ссылки `MainMenuController` на панели, slider/toggle и exit confirmation назначены.
- В текущей сцене `soundOnSprite/soundOffSprite` назначены.
- Есть EventSystem, CanvasScaler, GraphicRaycaster.

Проблемы:

- `MenuTransition.blackScreen = null`. Возврат из паузы через `MenuTransition.SetPauseEntry()` будет без черного fade-слоя. Код это переживет, но визуал будет беднее и менее контролируемый.
- `MainMenuController` оставляет `soundSettingsPage`, `controlsSettingsPage`, `controlsSettingsUI`, radio buttons пустыми и ищет их по именам. Это удобно сейчас, но хрупко: переименование `ControlSettings`, `Audio`, `SideBTN`, `Controls` ломает настройки без ошибки компиляции.
- В сцене есть два `ControlsSettingsUI`: один отключенный/пустой ранний компонент, второй рабочий на `ControlSettings`. Это не crash, но мусорит диагностику и может путать инспектор.
- Несколько `ButtonGlow.glowImage = null`. Скрипт безопасный, но glow-эффект фактически не работает на этих кнопках.
- В меню нет маршрута в Training. Если Training должен быть частью продукта, нужен отдельный пункт или режим выбора.

Рекомендация:

- Оставить автопоиск как fallback, но явно назначить страницы настроек и radio buttons в инспекторе.
- Удалить/отключить лишний `ControlsSettingsUI`, если он не нужен.
- Назначить `blackScreen` или убрать pause-entry fade из дизайна осознанно.
- Добавить кнопку Training только после включения TrainingScene в Build Settings.

## CharacterSelect

Сильные стороны:

- `arenaSceneName = TestArena_PrototypeMVP`, то есть выбор ведет в новую MVP-арену.
- `menuSceneName = MainMenu`.
- Текущий `InputJoinManager` имеет назначенные `wasdPressed` и `arrowsPressed`.
- После правки `CharacterSelector` стрелки больше не должны улетать из-за смешивания координат разных `RectTransform` родителей.
- `CharacterSelector` теперь клампит индексы, проверяет пустой массив карточек и сохраняет выбор в одном месте.

Проблемы:

- `CharacterSelectTransition` все еще хрупкий: нет системной валидации обязательных ссылок и нет graceful disable, если массив карточек пустой/содержит null.
- Визуально доступно 4 персонажа, но арена подставляет Heavy для 3 и 4 выбора.
- `InputJoinManager` хранит join-состояние в static полях. Это нормально для простого перехода, но при рестартах/прямом запуске сцен это надо держать под контролем.
- Сцена сильно завязана на анимационные magic numbers: `-2700`, `-1800`, `800`, `1200`. Это не срочно, но делает адаптацию под другие разрешения более рискованной.

Рекомендация:

- Добавить `OnValidate`/`ValidateReferences()` в `CharacterSelectTransition`.
- Либо скрыть Medic/Sniper карточки до готовности, либо сделать настоящие префабы.
- Вынести данные персонажей в `CharacterDefinition`/ScriptableObject: id, prefab, portrait, name, role, stats.
- Сохранять не только индекс, а стабильный id персонажа.

## TestArena_PrototypeMVP

Сильные стороны:

- Это сейчас главная рабочая арена.
- `WaveManager` настроен: zombie prefab, 5 spawn points, campfire target, `initialZombiePoolSize = 32`, `maxZombiePoolSize = 64`, UI wave/zombie count назначены.
- `PauseManager` настроен лучше, чем в Training: rope buttons и callbacks живые, `bushTransition` назначен.
- `CharacterSpawner` назначает `ArenaCamera`, split cameras, spawn points.
- Есть единый `PrototypeManagers` блок: run stats, campfire health, card rewards, enemy variants, class role tuner, revive manager.
- `PrototypeArenaMechanics/README.md` честно фиксирует, что эти механики пока отдельный prototype слой.

Проблемы:

- В сцене остался старый `CardsTestArena`, он disabled, но его UI/панель/кнопки все еще рядом с новым `PrototypeCardRewardManager`.
- `PrototypeCardRewardManager` генерирует HUD/карты кодом. Это быстро для MVP, но плохо для финальной верстки, локализации, анимаций и настройки через инспектор.
- `WaveManager` бесконечный. Если игра должна иметь забег/цель/босса/победу, нужен `RunDirector`, а не бесконечные волны как единственная структура.
- `PrototypeRunStats` завершает run, если все игроки мертвы, а revive-система может требовать окно, где один игрок мертв, но run еще продолжается. Сейчас логика "all players dead" нормальная, но ее надо тестировать с downed/revive состояниями.
- Новые поля ammo/drop/weapon после недавних правок еще не пересохранены в prefab YAML. В рантайме дефолты должны примениться, но префабы нужно открыть/сохранить в Unity и проверить инспектором.
- `PlayerController` все еще содержит старые `bulletPrefab/firePoint/Shoot()`, при этом реальная стрельба уходит в `AutoWeapon`. Это технический долг и источник путаницы.

Рекомендация:

- Считать `TestArena_PrototypeMVP` единственной основной ареной и временно убрать `TestArena` из build settings.
- Удалить или вынести `CardsTestArena` в отдельную test scene.
- Сделать `RunDirector`: wave progression, rewards, pause/gameover, victory/defeat, transitions.
- Перевести prototype runtime HUD в prefab-driven UI, когда механики утверждены.
- Добавить явный ammo UI и проверить баланс drop chance.

## TrainingScene

Сильные стороны:

- Есть `CharacterSpawner`, `ArenaCamera`, split cameras, pause UI, tutorial panel и `TrainingDummy_0`.
- Есть `DebugInputSetup`, чтобы запускать Training напрямую без CharacterSelect.
- Есть EventSystem/Canvas.

Проблемы:

- Сцены нет в Build Settings.
- Кнопки паузы потеряли target на `PauseManager`.
- `PauseManager.ropeButtons` все четыре пустые. После текущей правки кода это не краш, но анимации rope buttons не будет.
- `TutorialPanelManager.ShowControls`, `ShowCombat`, `ShowTips` пустые, хотя кнопки на них назначены.
- `TutorialPanelManager.OpenTutorial/CloseTutorial` без null-guard.
- `TrainingDummy_0` содержит физику/коллайдер, но не является боевой учебной целью с health/enemy интерфейсом. Для тренировки стрельбы, revive и ammo drop этого мало.
- Нет маршрута из MainMenu в Training.

Рекомендация:

- Если Training нужен игроку: включить в Build Settings, добавить кнопку в MainMenu, починить pause callbacks.
- Если Training нужен только разработчикам: пометить как DevOnly, не включать в build, оставить `DebugInputSetup`, но не показывать игроку.
- Сделать `TrainingDirector`, который ведет шаги: движение, атака, стрельба, подбор патронов, revive, пауза/выход.
- Сделать `TrainingTarget` с health, reset, hit feedback и совместимостью с текущим targeting/bullet pipeline.

## Что делать дальше

Рекомендуемый путь:

1. Зафиксировать сценовый поток.
   - Build Settings: оставить `MainMenu`, `CharacterSelect`, `TestArena_PrototypeMVP`.
   - Добавить `TrainingScene`, если это player-facing режим.
   - Убрать `SampleScene` и старый `TestArena` из production build.

2. Починить Training как отдельный режим.
   - Восстановить callbacks кнопок паузы на `PauseManager`.
   - Назначить rope buttons или отключить rope-анимацию в этой сцене.
   - Реализовать `TutorialPanelManager` и `TrainingDirector`.

3. Завершить честный выбор персонажей.
   - Либо оставить 2 персонажа в UI, либо добавить реальные Medic/Sniper prefabs.
   - Вынести персонажей в `CharacterDefinition`.

4. Укрепить переходы.
   - Добавить validation в `MenuTransition` и `CharacterSelectTransition`.
   - Убрать зависимость от случайных null и magic numbers.
   - Сделать единый `SceneFlow`/`TransitionService`.

5. Привести MVP-арену к единой архитектуре.
   - `RunDirector` владеет волнами, наградами, game over, victory и pause state.
   - `WaveManager` только спавнит волны.
   - `PrototypeCardRewardManager` только показывает/применяет награды.
   - `PauseManager` не спорит с reward/gameover за `Time.timeScale`.

6. Закрыть недавнюю механику стрельбы.
   - Открыть/сохранить Player/Engineer/Zombie prefabs в Unity, чтобы новые serialized fields появились явно.
   - Добавить ammo UI.
   - Удалить или пометить legacy `PlayerController.Shoot()`.
   - Протестировать: bullets ignore scenery, target downed ally, revive, ammo pickup from zombies.

7. Чистка проекта.
   - Вынести `_Recovery`, `SampleScene`, старые тестовые сцены из production-пути.
   - Оставить один главный MVP путь и одну dev/test зону.
   - После этого новый scene report станет намного полезнее, потому что noise исчезнет.

