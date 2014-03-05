using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace OuiGui.WPF.Views
{
    /// <summary>
    /// Interaction logic for PackageDetailsView.xaml
    /// </summary>
    public partial class PackageDetailsView : UserControl
    {
        public PackageDetailsView()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var listView = sender as ListView;
            var gridView = listView.View as GridView;

            var workingWidth = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth;
            for (int i = 1; i < gridView.Columns.Count; ++i)
                workingWidth -= gridView.Columns[i].ActualWidth;

            gridView.Columns[0].Width = Math.Max(0, workingWidth - 10);
        }
    }
}
