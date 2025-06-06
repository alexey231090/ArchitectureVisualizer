# Architecture Visualizer
Версия: 0.1.0 (Alpha)

## Описание
Плагин для Unity, который визуализирует архитектуру проекта, показывая связи между скриптами, префабами и сценами.

## Текущий Прогресс

### Реализованная Функциональность
1. ✅ Создано базовое окно редактора Unity
2. ✅ Реализован анализатор скриптов
3. ✅ Создан базовый граф зависимостей
4. ✅ Добавлена возможность масштабирования и перемещения по графу
5. ✅ Реализовано создание узлов для скриптов
6. ✅ Добавлено определение зависимостей между скриптами
7. ✅ Реализовано базовое позиционирование узлов

### Известные Проблемы

1. **Визуализация Графа**
   - Узлы не отображаются после анализа
   - Тестовый узел виден, но исчезает при анализе
   - Проблема с центрированием графа

2. **Анализ Зависимостей**
   - Нужно улучшить определение наследования
   - Добавить анализ использования типов в коде
   - Исключить системные зависимости

3. **Производительность**
   - Ограничение на количество отображаемых узлов (MAX_NODES = 100)
   - Нужна оптимизация для больших проектов

4. **UI/UX**
   - Добавить фильтрацию узлов
   - Реализовать группировку связанных узлов
   - Добавить поиск по узлам
   - Улучшить стилизацию узлов и связей

## Следующие Шаги

1. **Исправление Визуализации**
   - Проверить инициализацию GraphView
   - Исправить проблему с отображением узлов
   - Улучшить алгоритм позиционирования

2. **Улучшение Анализа**
   - Расширить анализ зависимостей
   - Добавить анализ использования типов
   - Улучшить фильтрацию системных зависимостей

3. **Оптимизация**
   - Реализовать ленивую загрузку узлов
   - Добавить кэширование результатов анализа
   - Оптимизировать алгоритм размещения узлов

4. **UI Улучшения**
   - Добавить панель инструментов
   - Реализовать контекстное меню для узлов
   - Добавить настройки отображения

## Технические Детали

### Структура Проекта
```
Assets/
  Editor/
    ArchitectureVisualizer/
      ArchitectureVisualizerWindow.cs  - Основное окно редактора
      DependencyGraphView.cs          - Визуализация графа
      DependencyNode.cs               - Узел графа
      ScriptAnalyzer.cs               - Анализатор скриптов
      Resources/
        styles.uss                    - Стили для графа
```

### Ключевые Компоненты
1. **ArchitectureVisualizerWindow**
   - Точка входа в инструмент
   - Создает и управляет GraphView
   - Обрабатывает пользовательский ввод

2. **DependencyGraphView**
   - Отвечает за отображение графа
   - Управляет манипуляторами (масштаб, перемещение)
   - Содержит логику размещения узлов

3. **DependencyNode**
   - Представляет узел в графе
   - Содержит порты для связей
   - Управляет визуальным представлением

4. **ScriptAnalyzer**
   - Анализирует скрипты проекта
   - Определяет зависимости
   - Создает узлы и связи

## Зависимости
- Unity Editor
- Unity UI Elements
- Unity GraphView (Experimental)

## Примечания
- Текущая версия использует экспериментальный GraphView
- Требуется Unity 2020.3 или новее
- Рекомендуется использовать в небольших проектах 

## 2024-03-21
### Улучшения в отображении структуры
- Добавлено подробное логирование для отслеживания процесса загрузки префабов
- Улучшена обработка случая отсутствия префабов
- Добавлено информативное сообщение при отсутствии префабов
- Исправлены ошибки компиляции:
  - Заменено свойство `padding` на отдельные свойства `paddingTop`, `paddingRight`, `paddingBottom`, `paddingLeft`
  - Исправлено значение `WhiteSpace.Pre` на `WhiteSpace.NoWrap`
- Улучшена фильтрация префабов (исключение папок Packages и Library)
- Добавлена поддержка отображения компонентов на объектах
- Реализовано древовидное отображение иерархии объектов с отступами и символами

### Текущие задачи
- [ ] Добавить возможность фильтрации префабов по имени
- [ ] Реализовать поиск по компонентам
- [ ] Добавить возможность сворачивания/разворачивания веток иерархии
- [ ] Улучшить визуальное оформление дерева объектов 