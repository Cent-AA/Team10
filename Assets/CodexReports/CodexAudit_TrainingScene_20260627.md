# Аудит TrainingScene

Дата: 2026-06-27

Источники проверки:

- `CodexSceneReport_OpenScenes_20260627_210336.md` — снимок открытой сцены в Unity;
- `Scenes/TrainingScene.unity` — сохранённая версия сцены на диске;
- связанные runtime-скрипты, `EditorBuildSettings.asset` и свежий `Editor.log`.

## Вердикт

Сцена запускаемо собрана, но пока не готова к приёмке. В отчёте нет missing scripts (`MissingScripts: 0`), `TrainingScene` включена в Build Settings, основные ссылки камеры, спавнера, переходов, зомби и турели назначены. Блокирующие проблемы находятся в UI и в рассинхроне между открытой и сохранённой сценой.

Критично: последний report снят с `IsDirty: True`. Он описывает несохранённое состояние редактора, которое уже отличается от `TrainingScene.unity` на диске. Перед любым сохранением нужно определить, какие изменения были намеренными.

## P0 — блокеры

### 1. Четыре кнопки паузы не вызывают свои действия в сохранённой сцене

В `TrainingScene.unity` у `ResumeBtn`, `RestartBtn`, `MenuBtn` и `ExitBtn` сохранены имена методов, но `m_Target: {fileID: 0}`. Нажатия не доходят до `PauseManager`.

Последствия:

- Resume работает только через повторное нажатие Escape;
- Restart, Main Menu и Exit из pause UI не работают;
- scene report эту поломку не показывает, потому что не раскрывает persistent UnityEvent targets.

Исправление: заново привязать `PauseManager` как OnClick target и выбрать соответственно `ResumeGame`, `OnRestartButton`, `OnMainMenuButton`, `OnExitButton`.

### 2. В несохранённом состоянии `ButtonTutorial` потерял компонент Button

В сохранённом YAML у `ButtonTutorial` есть `Button` с рабочим вызовом `TutorialPanelManager.OpenTutorial`. В последнем dirty-report объект содержит только `RectTransform`, `CanvasRenderer` и `Image` (`Components: 3`). Если сохранить текущее открытое состояние как есть, открыть tutorial кликом будет невозможно.

Исправление: до сохранения восстановить `Button` и его OnClick либо отменить случайное удаление компонента. После этого сохранить сцену и снять новый report с `IsDirty: False`.

## P1 — функциональные проблемы

### 3. Вкладки tutorial фактически не реализованы

`ShowControls()`, `ShowCombat()` и `ShowTips()` пустые. Кнопки имеют callbacks, но ничего не меняют; `InfoText` навсегда остаётся с исходным текстом Controls.

Дополнительно `TutorialPanel` и `ButtonTutorial` одновременно активны в dirty-report, хотя `OpenTutorial/CloseTutorial` предполагают взаимоисключающие состояния. `Start()` не нормализует начальное состояние.

Исправление:

- реализовать смену текста/страниц;
- выбрать явное стартовое состояние, обычно `TutorialPanel = false`, `ButtonTutorial = true`;
- удалить дублирующий `TutorialPanelManager` с объекта `PauseManager`: у него все три ссылки `null`, а настроенный компонент уже находится на `Canvas/TutorialPanel`.

### 4. UI рассчитан почти только на 1920×1080

Оба `CanvasScaler` используют `Constant Pixel Size`, а ключевые элементы стоят на фиксированных координатах: tutorial-кнопки около `x = -800`, pause-кнопки около `x = -658`, панели имеют размеры до `1920×1080`. На 1366×768, 1280×720 и особенно 800×600 часть интерфейса будет обрезана или окажется за экраном.

Исправление: перейти на `Scale With Screen Size`, задать осознанную reference resolution (например, 1920×1080), пересобрать anchors и проверить safe area/aspect ratios.

### 5. Medic и Sniper всё ещё спавнят Heavy

В `CharacterSpawner` поля `medicPrefab` и `sniperPrefab` назначены тем же `Player1 1`, что и Heavy. Выбор персонажей с индексами 2 и 3 визуально существует, но не создаёт уникальные классы.

Исправление: назначить реальные prefabs либо временно скрыть/заблокировать недоступные варианты в CharacterSelect.

## P2 — качество и техдолг

### 6. Rope-анимация pause UI не настроена

В массиве `PauseManager.ropeButtons` четыре элемента, но у каждого `button: {fileID: 0}`. Код безопасно пропускает пустые ссылки, поэтому падения не будет, однако задуманная drop/swing/retract-анимация не работает.

Исправление: назначить четыре `RectTransform` pause-кнопок и проверить их конечные позиции после Resume и перехода в меню.

### 7. В сцене сосуществуют две системы турелей

`PrototypeTurret` оставлен неактивным, а legacy `Turret` активен. Legacy-скрипт каждый кадр вызывает `FindGameObjectsWithTag("Zombie")` и при каждом выстреле пишет в Console. Для одной учебной турели это не блокер, но создаёт лишние аллокации/поиск и console spam.

Исправление: выбрать одну систему. Если остаётся legacy-версия, искать цели через `Registry`, убрать постоянный `Debug.Log` и добавить валидацию массивов `directions/firePoints`.

## Что уже в порядке

- `MissingScripts: 0`, в свежем Editor log нет компиляционных ошибок и недавних исключений по сцене.
- `TrainingScene` включена в Build Settings.
- `CharacterSpawner` имеет обе точки спавна, shared/split cameras и основные prefabs.
- Пустые `ArenaCamera.target1/target2` до запуска ожидаемы: `CharacterSpawner.Start()` назначает созданных игроков.
- `ArenaCamera.mapSprite`, обе split-камеры и divider назначены.
- Три зомби активны, имеют tag `Zombie` и layer `Enemy`; ссылки `ZombieAI` на Animator/Rigidbody2D/SpriteRenderer назначены.
- Активная турель имеет 10 fire points и 10 direction sprites в сохранённой сцене.
- Ссылки `ArenaEntryTransition` и `PauseManager.bushTransition` назначены.

## Минимальный чек-лист после исправлений

1. Сохранить сцену и получить новый report с `IsDirty: False`.
2. Запустить сцену напрямую: должны появиться два игрока, камера должна получить обе цели.
3. Открыть/закрыть pause через Escape; проверить Resume, Restart, Main Menu и Exit кнопками.
4. Открыть tutorial, переключить Controls/Combat/Tips, закрыть и открыть повторно.
5. Проверить 1920×1080, 1366×768, 1280×720 и один ультраширокий aspect ratio.
6. Развести игроков дальше `splitThreshold = 13`, затем сблизить до `mergeThreshold = 11`; проверить камеры и divider.
7. Пройти в Training через основной поток CharacterSelect и отдельно проверить выбор Medic/Sniper.

