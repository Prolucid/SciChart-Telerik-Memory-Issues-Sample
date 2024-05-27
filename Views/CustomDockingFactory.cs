using Views.Common;
using Telerik.Windows.Controls.Docking;
using Telerik.Windows.Controls;

namespace Views
{
    public class CustomDockingFactory : DockingPanesFactory
    {
        protected override void RemovePane(RadPane pane)
        {
            base.RemovePane(pane);
            pane.Content = null;
            pane.Header = null;
            pane.DataContext = null;
            pane.ClearValue(RadDocking.SerializationTagProperty);
        }

        protected override RadPane CreatePaneForItem(object item)
        {
            CustomDocumentPane pane = new CustomDocumentPane
            {
                Content = new ChartView(),
                DataContext = item,
                ContextMenuTemplate = null,
            };

            pane.SetBinding(CustomDocumentPane.DockedIdProperty, "DockedId");

            return pane;
        }
    }
}
