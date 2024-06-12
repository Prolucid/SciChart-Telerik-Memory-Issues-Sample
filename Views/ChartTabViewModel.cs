using System.Collections.ObjectModel;

namespace Views;

public class ChartTabViewModel
{
    public ObservableCollection<ChartView> Tabs { get; } = new();

    public void AddTab()
    {
        Tabs.Add(new ChartView());
    }
}