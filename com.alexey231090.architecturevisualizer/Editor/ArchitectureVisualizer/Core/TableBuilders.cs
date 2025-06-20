using UnityEngine;
using UnityEngine.UIElements;

namespace ArchitectureVisualizer
{
    public static class TableBuilders
    {
        public static void CreateEventsTable(ScrollView tablesContainer, DependencyData dependencyData)
        {
            var container = new VisualElement();
            container.style.marginBottom = 20;
            var header = new Label("1. События (Events / UnityEvent)");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 16;
            header.style.marginBottom = 10;
            container.Add(header);
            var table = new VisualElement();
            table.style.flexDirection = FlexDirection.Column;
            table.style.borderLeftWidth = 1;
            table.style.borderRightWidth = 1;
            table.style.borderTopWidth = 1;
            table.style.borderBottomWidth = 1;
            table.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.width = new Length(100, LengthUnit.Percent);
            table.style.minWidth = 800;
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            headerRow.style.paddingTop = 5;
            headerRow.style.paddingRight = 5;
            headerRow.style.paddingBottom = 5;
            headerRow.style.paddingLeft = 5;
            headerRow.style.width = new Length(100, LengthUnit.Percent);
            string[] headers = { "Генератор", "Подписчик", "Метод подписчика", "Описание" };
            float[] columnWidths = { 20, 20, 20, 40 };
            for (int i = 0; i < headers.Length; i++)
            {
                var headerCell = new Label(headers[i]);
                headerCell.style.width = new Length(columnWidths[i], LengthUnit.Percent);
                headerCell.style.minWidth = 150;
                headerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
                headerCell.style.paddingTop = 5;
                headerCell.style.paddingRight = 5;
                headerCell.style.paddingBottom = 5;
                headerCell.style.paddingLeft = 5;
                headerCell.style.whiteSpace = WhiteSpace.Normal;
                headerCell.style.overflow = Overflow.Hidden;
                headerCell.style.textOverflow = TextOverflow.Ellipsis;
                headerRow.Add(headerCell);
            }
            table.Add(headerRow);
            foreach (var evt in dependencyData.Events)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.paddingTop = 5;
                row.style.paddingRight = 5;
                row.style.paddingBottom = 5;
                row.style.paddingLeft = 5;
                row.style.borderBottomWidth = 1;
                row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                row.style.width = new Length(100, LengthUnit.Percent);
                AddCell(row, evt.Generator, columnWidths[0]);
                AddCell(row, evt.Subscriber, columnWidths[1]);
                AddCell(row, evt.SubscriberMethod, columnWidths[2]);
                AddCell(row, evt.Description, columnWidths[3]);
                table.Add(row);
            }
            container.Add(table);
            tablesContainer.Add(container);
        }
        public static void CreateHardDependenciesTable(ScrollView tablesContainer, DependencyData dependencyData)
        {
            var container = new VisualElement();
            container.style.marginBottom = 20;
            var header = new Label("2. Жёсткие зависимости (GetComponent, FindObjectOfType)");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 16;
            header.style.marginBottom = 10;
            container.Add(header);
            var table = new VisualElement();
            table.style.flexDirection = FlexDirection.Column;
            table.style.borderLeftWidth = 1;
            table.style.borderRightWidth = 1;
            table.style.borderTopWidth = 1;
            table.style.borderBottomWidth = 1;
            table.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.width = new Length(100, LengthUnit.Percent);
            table.style.minWidth = 800;
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            headerRow.style.paddingTop = 5;
            headerRow.style.paddingRight = 5;
            headerRow.style.paddingBottom = 5;
            headerRow.style.paddingLeft = 5;
            headerRow.style.width = new Length(100, LengthUnit.Percent);
            string[] headers = { "Потребитель", "Зависимость", "Метод доступа", "Риски" };
            float[] columnWidths = { 25, 25, 25, 25 };
            for (int i = 0; i < headers.Length; i++)
            {
                var headerCell = new Label(headers[i]);
                headerCell.style.width = new Length(columnWidths[i], LengthUnit.Percent);
                headerCell.style.minWidth = 150;
                headerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
                headerCell.style.paddingTop = 5;
                headerCell.style.paddingRight = 5;
                headerCell.style.paddingBottom = 5;
                headerCell.style.paddingLeft = 5;
                headerCell.style.whiteSpace = WhiteSpace.Normal;
                headerCell.style.overflow = Overflow.Hidden;
                headerCell.style.textOverflow = TextOverflow.Ellipsis;
                headerRow.Add(headerCell);
            }
            table.Add(headerRow);
            foreach (var dep in dependencyData.HardDependencies)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.paddingTop = 5;
                row.style.paddingRight = 5;
                row.style.paddingBottom = 5;
                row.style.paddingLeft = 5;
                row.style.borderBottomWidth = 1;
                row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                row.style.width = new Length(100, LengthUnit.Percent);
                AddCell(row, dep.Consumer, columnWidths[0]);
                AddCell(row, dep.Dependency, columnWidths[1]);
                AddCell(row, dep.AccessMethod, columnWidths[2]);
                AddCell(row, dep.Risks, columnWidths[3]);
                table.Add(row);
            }
            container.Add(table);
            tablesContainer.Add(container);
        }
        public static void CreateDITable(ScrollView tablesContainer, DependencyData dependencyData)
        {
            var container = new VisualElement();
            container.style.marginBottom = 20;
            var header = new Label("3. DI зависимости");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 16;
            header.style.marginBottom = 10;
            container.Add(header);
            var table = new VisualElement();
            table.style.flexDirection = FlexDirection.Column;
            table.style.borderLeftWidth = 1;
            table.style.borderRightWidth = 1;
            table.style.borderTopWidth = 1;
            table.style.borderBottomWidth = 1;
            table.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.width = new Length(100, LengthUnit.Percent);
            table.style.minWidth = 800;
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            headerRow.style.paddingTop = 5;
            headerRow.style.paddingRight = 5;
            headerRow.style.paddingBottom = 5;
            headerRow.style.paddingLeft = 5;
            headerRow.style.width = new Length(100, LengthUnit.Percent);
            string[] headers = { "Класс", "Интерфейс", "Метод внедрения", "Заметки" };
            float[] columnWidths = { 25, 25, 25, 25 };
            for (int i = 0; i < headers.Length; i++)
            {
                var headerCell = new Label(headers[i]);
                headerCell.style.width = new Length(columnWidths[i], LengthUnit.Percent);
                headerCell.style.minWidth = 150;
                headerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
                headerCell.style.paddingTop = 5;
                headerCell.style.paddingRight = 5;
                headerCell.style.paddingBottom = 5;
                headerCell.style.paddingLeft = 5;
                headerCell.style.whiteSpace = WhiteSpace.Normal;
                headerCell.style.overflow = Overflow.Hidden;
                headerCell.style.textOverflow = TextOverflow.Ellipsis;
                headerRow.Add(headerCell);
            }
            table.Add(headerRow);
            foreach (var di in dependencyData.Dependencies)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.paddingTop = 5;
                row.style.paddingRight = 5;
                row.style.paddingBottom = 5;
                row.style.paddingLeft = 5;
                row.style.borderBottomWidth = 1;
                row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                row.style.width = new Length(100, LengthUnit.Percent);
                AddCell(row, di.Class, columnWidths[0]);
                AddCell(row, di.Interface, columnWidths[1]);
                AddCell(row, di.InjectionMethod, columnWidths[2]);
                AddCell(row, di.Notes, columnWidths[3]);
                table.Add(row);
            }
            container.Add(table);
            tablesContainer.Add(container);
        }
        public static void CreateScriptableObjectTable(ScrollView tablesContainer, DependencyData dependencyData)
        {
            var container = new VisualElement();
            container.style.marginBottom = 20;
            var header = new Label("4. ScriptableObject зависимости");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 16;
            header.style.marginBottom = 10;
            container.Add(header);
            var table = new VisualElement();
            table.style.flexDirection = FlexDirection.Column;
            table.style.borderLeftWidth = 1;
            table.style.borderRightWidth = 1;
            table.style.borderTopWidth = 1;
            table.style.borderBottomWidth = 1;
            table.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.width = new Length(100, LengthUnit.Percent);
            table.style.minWidth = 800;
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            headerRow.style.paddingTop = 5;
            headerRow.style.paddingRight = 5;
            headerRow.style.paddingBottom = 5;
            headerRow.style.paddingLeft = 5;
            headerRow.style.width = new Length(100, LengthUnit.Percent);
            string[] headers = { "ScriptableObject", "Пользователь", "Метод/Свойство", "Заметки" };
            float[] columnWidths = { 25, 25, 25, 25 };
            for (int i = 0; i < headers.Length; i++)
            {
                var headerCell = new Label(headers[i]);
                headerCell.style.width = new Length(columnWidths[i], LengthUnit.Percent);
                headerCell.style.minWidth = 150;
                headerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
                headerCell.style.paddingTop = 5;
                headerCell.style.paddingRight = 5;
                headerCell.style.paddingBottom = 5;
                headerCell.style.paddingLeft = 5;
                headerCell.style.whiteSpace = WhiteSpace.Normal;
                headerCell.style.overflow = Overflow.Hidden;
                headerCell.style.textOverflow = TextOverflow.Ellipsis;
                headerRow.Add(headerCell);
            }
            table.Add(headerRow);
            foreach (var so in dependencyData.ScriptableObjects)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.paddingTop = 5;
                row.style.paddingRight = 5;
                row.style.paddingBottom = 5;
                row.style.paddingLeft = 5;
                row.style.borderBottomWidth = 1;
                row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                row.style.width = new Length(100, LengthUnit.Percent);
                AddCell(row, so.SOObject, columnWidths[0]);
                AddCell(row, so.User, columnWidths[1]);
                AddCell(row, so.MethodOrProperty, columnWidths[2]);
                AddCell(row, so.Notes, columnWidths[3]);
                table.Add(row);
            }
            container.Add(table);
            tablesContainer.Add(container);
        }
        public static void CreateSingletonTable(ScrollView tablesContainer, DependencyData dependencyData)
        {
            var container = new VisualElement();
            container.style.marginBottom = 20;
            var header = new Label("5. Singleton зависимости");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 16;
            header.style.marginBottom = 10;
            container.Add(header);
            var table = new VisualElement();
            table.style.flexDirection = FlexDirection.Column;
            table.style.borderLeftWidth = 1;
            table.style.borderRightWidth = 1;
            table.style.borderTopWidth = 1;
            table.style.borderBottomWidth = 1;
            table.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.width = new Length(100, LengthUnit.Percent);
            table.style.minWidth = 800;
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            headerRow.style.paddingTop = 5;
            headerRow.style.paddingRight = 5;
            headerRow.style.paddingBottom = 5;
            headerRow.style.paddingLeft = 5;
            headerRow.style.width = new Length(100, LengthUnit.Percent);
            string[] headers = { "Singleton", "Пользователь", "Метод доступа", "Проблемы" };
            float[] columnWidths = { 25, 25, 25, 25 };
            for (int i = 0; i < headers.Length; i++)
            {
                var headerCell = new Label(headers[i]);
                headerCell.style.width = new Length(columnWidths[i], LengthUnit.Percent);
                headerCell.style.minWidth = 150;
                headerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
                headerCell.style.paddingTop = 5;
                headerCell.style.paddingRight = 5;
                headerCell.style.paddingBottom = 5;
                headerCell.style.paddingLeft = 5;
                headerCell.style.whiteSpace = WhiteSpace.Normal;
                headerCell.style.overflow = Overflow.Hidden;
                headerCell.style.textOverflow = TextOverflow.Ellipsis;
                headerRow.Add(headerCell);
            }
            table.Add(headerRow);
            foreach (var singleton in dependencyData.Singletons)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.paddingTop = 5;
                row.style.paddingRight = 5;
                row.style.paddingBottom = 5;
                row.style.paddingLeft = 5;
                row.style.borderBottomWidth = 1;
                row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                row.style.width = new Length(100, LengthUnit.Percent);
                AddCell(row, singleton.SingletonClass, columnWidths[0]);
                AddCell(row, singleton.User, columnWidths[1]);
                AddCell(row, singleton.AccessMethod, columnWidths[2]);
                AddCell(row, singleton.Problems, columnWidths[3]);
                table.Add(row);
            }
            container.Add(table);
            tablesContainer.Add(container);
        }
        public static void CreateMessageBusTable(ScrollView tablesContainer, DependencyData dependencyData)
        {
            var container = new VisualElement();
            container.style.marginBottom = 20;
            var header = new Label("6. MessageBus зависимости");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 16;
            header.style.marginBottom = 10;
            container.Add(header);
            var table = new VisualElement();
            table.style.flexDirection = FlexDirection.Column;
            table.style.borderLeftWidth = 1;
            table.style.borderRightWidth = 1;
            table.style.borderTopWidth = 1;
            table.style.borderBottomWidth = 1;
            table.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.width = new Length(100, LengthUnit.Percent);
            table.style.minWidth = 800;
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            headerRow.style.paddingTop = 5;
            headerRow.style.paddingRight = 5;
            headerRow.style.paddingBottom = 5;
            headerRow.style.paddingLeft = 5;
            headerRow.style.width = new Length(100, LengthUnit.Percent);
            string[] headers = { "Отправитель", "Получатель", "Тип сообщения", "Заметки" };
            float[] columnWidths = { 25, 25, 25, 25 };
            for (int i = 0; i < headers.Length; i++)
            {
                var headerCell = new Label(headers[i]);
                headerCell.style.width = new Length(columnWidths[i], LengthUnit.Percent);
                headerCell.style.minWidth = 150;
                headerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
                headerCell.style.paddingTop = 5;
                headerCell.style.paddingRight = 5;
                headerCell.style.paddingBottom = 5;
                headerCell.style.paddingLeft = 5;
                headerCell.style.whiteSpace = WhiteSpace.Normal;
                headerCell.style.overflow = Overflow.Hidden;
                headerCell.style.textOverflow = TextOverflow.Ellipsis;
                headerRow.Add(headerCell);
            }
            table.Add(headerRow);
            foreach (var message in dependencyData.Messages)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.paddingTop = 5;
                row.style.paddingRight = 5;
                row.style.paddingBottom = 5;
                row.style.paddingLeft = 5;
                row.style.borderBottomWidth = 1;
                row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                row.style.width = new Length(100, LengthUnit.Percent);
                AddCell(row, message.Sender, columnWidths[0]);
                AddCell(row, message.Receiver, columnWidths[1]);
                AddCell(row, message.MessageType, columnWidths[2]);
                AddCell(row, message.Notes, columnWidths[3]);
                table.Add(row);
            }
            container.Add(table);
            tablesContainer.Add(container);
        }
        public static void AddCell(VisualElement row, string text, float widthPercent)
        {
            var cell = new Label(text);
            cell.style.width = new Length(widthPercent, LengthUnit.Percent);
            cell.style.minWidth = 150;
            cell.style.paddingTop = 5;
            cell.style.paddingRight = 5;
            cell.style.paddingBottom = 5;
            cell.style.paddingLeft = 5;
            cell.style.whiteSpace = WhiteSpace.Normal;
            cell.style.overflow = Overflow.Hidden;
            cell.style.textOverflow = TextOverflow.Ellipsis;
            row.Add(cell);
        }
    }
} 