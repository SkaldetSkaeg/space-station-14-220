# Настройка миникарты культа

`CultMiniMap` находится на **владельце способности**. Поле `trackedComponents`
задаёт список компонентов, носителей которых он видит, и оформление их маркеров.
Настройки можно задать в YAML-прототипе владельца или изменить на его компоненте
через серверный View Variables. Открытая карта обновляется раз в секунду.

```yaml
components:
- type: CultMiniMap
  selfIcon: /Textures/Interface/NavMap/beveled_star.png
  selfColor: Cyan
  selfScale: 1.2
  trackedComponents:
  - component: MiGo
    label: cult-mini-map-migo
    icon: /Textures/Interface/NavMap/beveled_triangle.png
    color: Gold
    scale: 1.0
  - component: CultYogg
    label: cult-mini-map-cultist
    icon: /Textures/Interface/NavMap/beveled_diamond.png
    color: Violet
    scale: 0.8
```

Значение `component` — зарегистрированное имя компонента без суффикса `Component`.
Это может быть и другой компонент, например `MobState`. Правила объединяются по
«ИЛИ». Если у сущности несколько подходящих компонентов, используется **первое**
правило; дубликатов на карте не будет. В меню подходящие сущности разделяются на
секции по этим правилам.

Владелец карты отображается всегда, отдельно от секций компонентов. Его оформление
задают поля `selfIcon`, `selfColor` и `selfScale`; по умолчанию это голубая звезда.
Даже если владелец подходит под одно из `trackedComponents`, второй раз в этой
секции он не появится.

| Поле правила | Назначение | По умолчанию |
| --- | --- | --- |
| `component` | Компонент отслеживаемой сущности | Обязательно |
| `label` | Ключ локализации типа в списке и поиске | Имя компонента |
| `icon` | Путь к PNG или RSI со `state` | `beveled_circle.png` |
| `color` | Цвет, умножаемый на цвета текстуры | `White` |
| `scale` | Множитель размера значка на карте | `1` |

Для более сложного значка можно указать RSI:

```yaml
- type: CultMiniMap
  trackedComponents:
  - component: MiGo
    label: cult-mini-map-migo
    icon:
      sprite: SS220/Interface/Actions/cult_yogg.rsi
      state: migo_teleport
    color: White
    scale: 0.75
```

Для PNG используется путь от `/Textures/`, для `sprite` — путь внутри
`Resources/Textures`. RSI отображается первым кадром, без анимации.
`White` сохраняет исходные цвета изображения. При выборе участника его значок
мигает, остальные затемняются. Значки в списке вписываются в 16×16,
а `scale` влияет только на карту. Неположительный или нечисловой `scale`
заменяется на `1`.

Готовые фигуры находятся в `Resources/Textures/Interface/NavMap/`: например,
`beveled_circle.png`, `beveled_triangle.png`, `beveled_square.png`,
`beveled_diamond.png`, `beveled_star.png`, `beveled_hexagon.png`.

Без `trackedComponents` сохраняются стандартные правила: `MiGo` — золотой силуэт,
`CultYogg` — фиолетовый круг, с приоритетом `MiGo`. Явный список заменяет эти
настройки целиком; `trackedComponents: []` отображает только владельца. Изменение настроек
одного владельца не влияет на карты остальных. Стандартные правила для всех
автоматически получающих способность участников заданы в
`CultMiniMapComponent.TrackedComponents`.

Добавление сущности в список отслеживания само по себе не выдаёт ей способность.
Автоматическая выдача при получении `CultYogg` или `MiGo` сохранена. `CultMiniMap`
также можно явно добавить другому владельцу. Состояние здоровья продолжает
отображаться отдельно от настраиваемого маркера; при отсутствии медицинских
компонентов оно обозначается как неизвестное. Сущности на других картах остаются
в списке, но их координаты не отображаются на текущем гриде.
