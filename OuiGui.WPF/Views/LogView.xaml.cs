using System;
using System.Windows;
using System.Windows.Controls;

namespace OuiGui.WPF.Views
{
    /// <summary>
    /// Interaction logic for LogView.xaml
    /// </summary>
    public partial class LogView : UserControl
    {
        public LogView()
        {
            InitializeComponent();
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var listView = sender as ListView;
            var gridView = listView.View as GridView;

            var workingWidth = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth;
            for (int i = 0; i < gridView.Columns.Count - 1; ++i)
                    workingWidth -= gridView.Columns[i].ActualWidth;

            gridView.Columns[gridView.Columns.Count - 1].Width = Math.Max(0, workingWidth - 10);
        }
    }
}
