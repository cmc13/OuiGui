using OuiGui.Lib.Model;
using System;
using System.Windows;
using System.Windows.Controls;

namespace OuiGui.WPF.Views
{
    /// <summary>
    /// Interaction logic for PackageListView.xaml
    /// </summary>
    public partial class PackageListView : UserControl
    {
        public PackageListView()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        public Package SelectedPackage
        {
            get { return (Package)GetValue(SelectedPackageProperty); }
            set { SetValue(SelectedPackageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedPackage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedPackageProperty =
            DependencyProperty.Register("SelectedPackage", typeof(Package), typeof(PackageListView), new PropertyMetadata(null));

        #endregion

        private void lstPackages_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var listView = sender as ListView;
            var gridView = listView.View as GridView;

            var workingWidth = listView.ActualWidth - 6;
            for (int i = 1; i < gridView.Columns.Count; ++i)
                    workingWidth -= gridView.Columns[i].ActualWidth;

            gridView.Columns[0].Width = Math.Max(0, workingWidth - 10);
        }
    }
}
