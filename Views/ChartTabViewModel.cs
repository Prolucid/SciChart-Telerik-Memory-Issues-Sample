using System.Collections.ObjectModel;

namespace Views;

public class ChartTabViewModel
{
    public ObservableCollection<ChartView> Tabs { get; set; }

    public ChartTabViewModel()
    {
        Tabs = new ObservableCollection<ChartView>();
    }
    
    public void AddTab()
    {
        Tabs.Add(new ChartView());
    }
}