# Architecture Visualizer

Плагин для Unity, который помогает визуализировать и анализировать зависимости между скриптами в вашем проекте.

## Установка

1. Откройте ваш Unity проект
2. В Package Manager выберите "Add package from git URL"
3. Введите URL: `https://github.com/alexey231090/ArchitectureVisualizer.git`
4. Нажмите "Add"

## Структура плагина

```
Assets/
└── Editor/
    ├── ArchitectureVisualizerWindow.cs
    ├── DependencyGraphView.cs
    ├── ScriptAnalyzer.cs
    ├── DependencyData.cs
    ├── TabView.cs
    ├── package.json
    ├── README.md
    ├── .gitignore
    ├── Resources/
    │   └── styles.uss
    └── Plugins/
```

## Использование

1. В Unity выберите Window > Architecture Visualizer
2. Откроется окно с тремя вкладками:
   - Dependencies: Графическое представление зависимостей
   - Structure: Иерархическая структура зависимостей
   - Analysis: Детальный анализ зависимостей

## Функции

- Визуализация зависимостей между скриптами
- Анализ циклических зависимостей
- Поиск неиспользуемых скриптов
- Группировка скриптов по папкам
- Экспорт данных в различные форматы

## Требования

- Unity 2020.3 или выше
- .NET 4.7.1 или выше

## Лицензия

MIT License

## Автор

Alexey231090
https://github.com/alexey231090 