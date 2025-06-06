using UnityEngine;
using UnityEngine.UIElements;

public class TabView : VisualElement
{
    private VisualElement tabContainer;
    private new VisualElement contentContainer;
    private Tab selectedTab;

    public TabView()
    {
        // Создаем контейнер для вкладок
        tabContainer = new VisualElement();
        tabContainer.style.flexDirection = FlexDirection.Row;
        tabContainer.style.borderBottomWidth = 1;
        tabContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
        Add(tabContainer);

        // Создаем контейнер для содержимого
        contentContainer = new VisualElement();
        contentContainer.style.flexGrow = 1;
        Add(contentContainer);
    }

    public void AddTab(Tab tab)
    {
        tabContainer.Add(tab);
        tab.clicked += () => SelectTab(tab);
        
        // Если это первая вкладка, выбираем её
        if (selectedTab == null)
        {
            SelectTab(tab);
        }
    }

    private void SelectTab(Tab tab)
    {
        if (selectedTab != null)
        {
            selectedTab.RemoveFromClassList("selected");
            if (selectedTab.content != null)
            {
                contentContainer.Remove(selectedTab.content);
            }
        }

        selectedTab = tab;
        selectedTab.AddToClassList("selected");
        if (selectedTab.content != null)
        {
            contentContainer.Add(selectedTab.content);
        }
    }
}

public class Tab : Button
{
    public VisualElement content { get; private set; }

    public Tab(string title)
    {
        text = title;
        style.width = 100;
        style.height = 30;
        style.marginRight = 2;
        style.borderTopLeftRadius = 5;
        style.borderTopRightRadius = 5;
        style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        style.color = Color.white;
    }

    public void SetContent(VisualElement content)
    {
        this.content = content;
    }
} 