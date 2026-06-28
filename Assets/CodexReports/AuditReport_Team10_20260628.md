# Аудит проекта Team10 — Unity 2D

**Дата:** 2026-06-28  
**Скриптов проверено:** 64 (.cs), ~13 400 строк  
**Сцены:** MainMenu, CharacterSelect, TestArena_PrototypeMVP, TrainingScene, TestEnemy

---

## Общая картина

Проект — co-op 2D-файтер/выживач с двумя классами (Heavy/PlayerController и Engineer/EngineerController), зомби-волнами и боссом. Ядро механик реализовано аккуратно: есть Object Pool для зомби, Registry-паттерн для глобального поиска игроков, отдельная папка `PrototypeArenaMechanics` для WIP-фич. Код читается, комментарии есть. Тем не менее есть несколько системных проблем, которые нужно устранить до финала.

---

## P0 — Критично (может сломать сборку или создать баг в рантайме)

### 1. `WaveManager.cs` — мёртвый код за пределами класса

После закрывающей `}` класса `WaveManager` в файле находятся ~25 строк текста: незакоммиченный "патч" с комментариями и заготовками методов. Это **синтаксически невалидно** и вызовет ошибку компиляции при следующей сборке.

```
// ============================================================
// ПАТЧ ДЛЯ WaveManager.cs
// ...
// ============================================================
```

**Исправление:** удалить всё после последней `}` класса (строки ~385–401).

---

### 2. `Time.timeScale` изменяют 8 независимых систем без координации

`Cards`, `CardsTestArena`, `PauseManager`, `PrototypeCardRewardManager`, `PrototypeRunStats`, `EngineerController`, `PlayerController` — все напрямую пишут `Time.timeScale = 0f / 1f`. Если два события наложатся (например, карта появляется в тот момент, когда срабатывает hit-stop инженера), одна система сбросит `timeScale = 1f`, пока другая ещё ждёт паузы.

**Исправление:** ввести единственный `TimeManager` (или метод в `PauseManager`) со счётчиком пауз:
```csharp
// TimeManager.Pause() → pauseCount++; if(pauseCount == 1) timeScale = 0
// TimeManager.Resume() → pauseCount--; if(pauseCount == 0) timeScale = 1
```

---

### 3. `BossController` — босс не наносит урон

В `BossController.Update()` реализовано только движение к игроку и смена состояний (Idle/Walk/Scream). Метода атаки нет совсем — ни `DealDamage`, ни collision-триггера. Босс подходит вплотную и стоит.

**Исправление:** добавить Attack-состояние по аналогии с `ZombieAI.PerformAttack()`.

---

### 4. `Spawn.cs` — устаревший тестовый скрипт в продакшен-папке

`Spawn.cs` (23 строки) — ранний прототип, не связанный с `WaveManager`. Пустой `Update()`, хардкод `Random.Range(-15, 15)`, нет пула, нет связи с кемпфайром. Если объект с этим компонентом случайно останется в сцене, он будет спавнить зомби параллельно с `WaveManager`.

**Исправление:** удалить файл или перенести в папку `_Recovery`/архив.

---

## P1 — Важно (влияет на стабильность и качество игры)

### 5. `GameObject.Find()` в Update-цепочке

`ZombieAI.EnsureCampfireTarget()` вызывается из `Update → MoveToCampfire → EnsureCampfireTarget`. Внутри — `GameObject.Find("CampFire")`, который обходит всю иерархию сцены каждый раз. При 20+ зомби это заметная нагрузка.

Та же логика продублирована в `WaveManager` и `PrototypeCampfireHealth` — три отдельных `Find("CampFire")`.

**Исправление:** кешировать campfire один раз в `Start()` через `[SerializeField]` или через `WaveManager`, который уже знает ссылку и передаёт её через `zombie.SetCampfireTarget()`.

---

### 6. `Turret.cs` — `FindGameObjectsWithTag` каждый кадр

Старая турель делает `GameObject.FindGameObjectsWithTag("Zombie")` в `Update`. Это аллоцирует массив каждый кадр. `PrototypeTurret` уже использует `Registry.Zombies` — правильный подход.

**Исправление:** заменить `FindGameObjectsWithTag` на `Registry.Zombies`, как это сделано в `PrototypeTurret`.

---

### 7. Нет интерфейса `IDamageable` — дублирование в каждой атаке

В `ZombieAI.DealDamage()`, `PlayerController.DealAttackDamage()`, `PlayerController.DealBarrageDamage()` одинаковый паттерн: `GetComponent<PlayerController>()` + отдельно `GetComponent<EngineerController>()`. Каждый новый класс врага/игрока потребует правок во всех местах нанесения урона.

**Исправление:**
```csharp
public interface IDamageable {
    void TakeDamage(float damage, Vector2 knockbackDir);
}
```
`PlayerController` и `EngineerController` реализуют интерфейс, атакующий делает один `GetComponent<IDamageable>()`.

---

### 8. `PlayerController.currentHealth` — публичное поле без валидации

`currentHealth` открыт как `public float`, что позволяет внешнему коду произвольно записать любое значение, минуя логику смерти и события `OnHealthChanged`. В `ZombieAI.DealDamage()` есть строка `float previousHealth = player.currentHealth` — прямое чтение поля нормально, но запись снаружи опасна.

**Исправление:** сделать `public float CurrentHealth { get; private set; }` и убрать прямые присваивания снаружи класса.

---

### 9. `Cards.cs` — заглушки вместо данных карт

`cardDescriptions` содержит 21 строку вида `"TEST1"…"TEST21"` с комментарием `// TEMPORARY WE DONT HAVE CARD DESIGNS`. `PrototypeCardRewardManager` — отдельная система карт с реальными эффектами. Две системы карт сосуществуют, назначение `Cards.cs` не ясно.

**Исправление:** определить, нужен ли `Cards.cs` вообще, или вся логика переходит в `PrototypeCardRewardManager`. Если нет — удалить.

---

### 10. Encoding-артефакт в `TargetingSystem.cs`

`[Header("в•ђв•ђв•ђ РќР°СЃС‚СЂРѕР№РєРё в•ђв•ђв•ђ")]` — кириллица сохранена в неправильной кодировке. В редакторе Inspector-заголовки отображаются как мусор.

**Исправление:** открыть файл в VS Code с кодировкой UTF-8, пересохранить.

---

## P2 — Техдолг (не срочно, но накапливается)

### 11. `PlayerController` и `EngineerController` — ~400 строк дублированного кода

Оба класса содержат: аудио-инициализацию, hit-flash корутину, knockback корутину, invulnerability логику, подключение к `Registry`, работу с `spriteRenderers[]`. Разница только в анимационной системе (`PuppetAnimator` vs ручная кость `wrenchPivot`).

**Рекомендация:** выделить `BasePlayerController : MonoBehaviour` с общей логикой здоровья, звука, вспышки, registry. Оба класса наследуются от него.

---

### 12. `Registry` — статические изменяемые списки без защиты от null в Unity

`Registry.Players`, `PlayerControllers`, `Zombies` — публичные `static List<>`. `[RuntimeInitializeOnLoadMethod]` очищает их при перезапуске домена, но не при hot-reload скриптов в режиме Play. После горячей перекомпиляции в списках могут остаться невалидные ссылки до следующего `CleanupPlayers()`.

**Рекомендация:** использовать `readonly` на коллекциях и добавить `CleanupZombies()` вызов в `WaveManager.Update()` аналогично тому, как `CleanupPlayers()` вызывается в `ZombieAI`.

---

### 13. `Debug.Log` в продакшен-коде

Оставлены `Debug.Log` в: `Cards.cs (EditorLog)`, `CardsTestArena.cs`, `UIBarsManager.cs ("Игрок погиб!")`, `Players.cs`, `CharacterSpawner.cs`. При большом количестве событий (смерть зомби, нажатие карты) это создаёт Console spam и минимальные аллокации.

**Рекомендация:** обернуть в `#if UNITY_EDITOR` или удалить перед релизом.

---

### 14. `HitStopRoutine` — смешение двух разных техник паузы

В `PlayerController` hit-stop реализован через изменение `Time.fixedDeltaTime = 0.02f * 0.2f` (замедление физики), в `EngineerController` — через `Time.timeScale = 0.02f`. Поведение разное и непредсказуемое при одновременном срабатывании (игрок и инженер бьют одновременно).

**Рекомендация:** выбрать один подход и вынести в `TimeManager.HitStop(duration)`.

---

### 15. Prefab `Sniper` существует, но класс не реализован

В `CharacterSpawner` с индексом 2 и 3 (`medicPrefab`, `sniperPrefab`) спавнится тот же `Player1 1` (Heavy) с `Debug.LogWarning`. В папке `Prefabs` есть `Sniper.prefab`. Выбор персонажа в CharacterSelect визуально работает, но не имеет эффекта.

**Рекомендация:** либо реализовать классы, либо заблокировать слоты в UI до готовности.

---

## Хорошее в проекте (не трогать)

- **Object Pool для зомби** в `WaveManager` — грамотно реализован с `Queue<ZombieAI>`, `ResetForSpawn()` и ограничением `maxZombiePoolSize`.
- **Registry-паттерн** — централизованный поиск игроков без `FindObjectOfType` в горячем пути.
- **`PrototypeArenaMechanics`** правильно изолирован с README — WIP-механики не засоряют основные сцены.
- **`PlayerController.DealAttackDamage()`** использует pre-allocated буфер `Collider2D[32]` — нет аллокаций в цикле атаки.
- **`BG3PortraitHealthBar`** — продуманная система с fallback при ошибке рендера.
- **`TargetingSystem`** — приоритет на союзника (ревайв) над зомби — умное дизайн-решение.

---

## Приоритетный план действий

| # | Действие | Файл | Усилие |
|---|----------|------|--------|
| 1 | Удалить мёртвый код | `WaveManager.cs` строки ~385+ | 5 мин |
| 2 | Добавить атаку боссу | `BossController.cs` | 1–2 ч |
| 3 | Ввести `TimeManager` / счётчик пауз | новый файл + 8 мест | 2–3 ч |
| 4 | Удалить `Spawn.cs` | `Spawn.cs` | 5 мин |
| 5 | Заменить `FindGameObjectsWithTag` в Turret | `Turret.cs` | 15 мин |
| 6 | Убрать `Debug.Log` из релизного кода | 5 файлов | 30 мин |
| 7 | Исправить кодировку в TargetingSystem | `TargetingSystem.cs` | 5 мин |
| 8 | Ввести `IDamageable` | новый файл + рефактор | 3–4 ч |
| 9 | Вынести общую логику игроков | новый `BasePlayerController` | 4–6 ч |
