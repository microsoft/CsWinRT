using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;


namespace ObjectLifetimeTests
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed class CustomGroupedItemsPage : Page
    {
        public CustomGroupedItemsPage()
        {
            this.DataContext = CustomViewModel.Current;

            CollectionViewSource cvs = (CollectionViewSource)XamlReader.Load(@"
                    <CollectionViewSource
                                xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                                Source='{Binding Groups}'
                                IsSourceGrouped='true'
                                ItemsPath='Items' />");

            this.Resources.Add("groupedItemsViewSource", cvs);
        }
    }

    public sealed class CustomGroupedItemsPage2 : Page
    {
        public CustomGroupedItemsPage2()
        {
            defaultViewModel["Groups"] = CustomViewModel.Current.Groups;
            defaultViewModel["Group"] = CustomViewModel.Current.Groups[0];
            defaultViewModel["Items"] = CustomViewModel.Current.Groups[0].Items;

            this.DataContext = defaultViewModel;

            Grid grid = (Grid)XamlReader.Load(@"
                    <Grid
                        xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <Grid.Resources>
                            <ResourceDictionary>
                                <CollectionViewSource
                                    x:Name='groupViewSource'
                                    IsSourceGrouped='true'
                                    Source='{Binding Groups}'
                                    ItemsPath='Items' />
                                <CollectionViewSource
                                    x:Name='itemsViewSource'
                                    Source='{Binding Items}' />
                            </ResourceDictionary>
                        </Grid.Resources>
                        <GridView
			                ItemsSource='{Binding Source={StaticResource itemsViewSource}}'>
                            <GridView.ItemTemplate>
                                <DataTemplate>
                                    <StackPanel  />
                                </DataTemplate>
                            </GridView.ItemTemplate>
                            <GridView.ItemsPanel>
                                <ItemsPanelTemplate>
                                    <StackPanel />
                                </ItemsPanelTemplate>
                            </GridView.ItemsPanel>
                        </GridView>
                    </Grid>");

            this.Content = grid;
        }

        private ObservableDictionary defaultViewModel = new ObservableDictionary();
    }

    /// <summary>
    /// Generic group data model.
    /// </summary>
    public class CustomDataGroup
    {
        public CustomDataGroup()
        {
            this.Items = new ObservableCollection<CustomClrObject>();
        }

        public ObservableCollection<CustomClrObject> Items { get; private set; }
    }


    public class CustomViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        private readonly ObservableCollection<CustomDataGroup> groups = new ObservableCollection<CustomDataGroup>();
        private static CustomViewModel _current = new CustomViewModel();
        private CustomDataSource _CustomDataSource = null;

        private CustomViewModel()
        {
            _CustomDataSource = new CustomDataSource();
        }

        public static CustomViewModel Current
        {
            get
            {
                return _current;
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }

        public ObservableCollection<CustomDataGroup> Groups
        {
            get { return groups; }
        }

        public void LoadData()
        {
            Groups.Clear();

            foreach (var i in _CustomDataSource.Groups)
            {
                Groups.Add(i);
            }
        }

        internal void CleanUp()
        {
            Groups.Clear();
        }

        internal void ChangeItems()
        {
            _CustomDataSource.ChangeItems();
        }
    }

    public sealed class CustomDataSource
    {
        public CustomDataSource()
        {
            CustomDataGroup group = new CustomDataGroup();
            group.Items.Add(new CustomClrObject());
            this.Groups.Add(group);
        }

        private ObservableCollection<CustomDataGroup> _groups = new ObservableCollection<CustomDataGroup>();

        public ObservableCollection<CustomDataGroup> Groups
        {
            get { return this._groups; }
        }

        public void ChangeItems()
        {
            foreach (var g in this.Groups)
            {
                g.Items.Clear();
                for (int i = 0; i < 10; i++)
                {
                    g.Items.Add(new CustomClrObject());
                }
            }
        }
    }


}