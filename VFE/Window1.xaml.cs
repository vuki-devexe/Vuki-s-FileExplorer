/*
 * Created by SharpDevelop.
 * User: vukiYT2011
 * Date: 11/17/2025
 * Time: 07:39
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace VFE
{
	public enum ItemType
    {
        Drive,
        Folder,
        File
    }

    public class FileSystemItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
        
        public string FullPath { get; set; }
        public string Name { get; set; }
        public ItemType Type { get; set; }
        
        public ObservableCollection<FileSystemItem> Children { get; set; }
        public bool HasChildren { get { return this.Type != ItemType.File; } }
        
        private bool _isExpanded;
        public bool IsExpanded 
        { 
            get { return _isExpanded; } 
            private set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged("IsExpanded");
                }
            }
        }
        
        private bool _isLoading = false; 

        public FileSystemItem(string fullPath, ItemType type)
        {
            this.FullPath = fullPath;
            this.Type = type;
            
            if (fullPath != null)
            {
                this.Name = type == ItemType.Drive ? new DirectoryInfo(fullPath).Name : Path.GetFileName(fullPath);
            }
            else
            {
                this.Name = "Loading...";
            }
            
            this.Children = new ObservableCollection<FileSystemItem>();
            if (this.HasChildren && fullPath != null) 
            {
                this.Children.Add(new FileSystemItem(null, ItemType.File)); 
            }
        }
        
        public async Task LoadChildrenAsync() 
        {
            if (this.Type == ItemType.File || this.IsExpanded || _isLoading)
            {
                return;
            }
            
            _isLoading = true;

            try
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    this.Children.Clear();
                    this.Children.Add(new FileSystemItem("Loading...", ItemType.File)); 
                });

                await Task.Run(() =>
                {
                    
                    try
                    {
                        var directories = Directory.GetDirectories(this.FullPath);
                        var files = Directory.GetFiles(this.FullPath);
                        
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (string dirPath in directories)
                            {
                                this.Children.Add(new FileSystemItem(dirPath, ItemType.Folder));
                            }
                            foreach (string filePath in files)
                            {
                                this.Children.Add(new FileSystemItem(filePath, ItemType.File));
                            }
                        });
                    }
                    catch (UnauthorizedAccessException)
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            this.Children.Clear(); 
                            this.Children.Add(new FileSystemItem("Access Denied", ItemType.File));
                        });
                    }
                    catch (Exception ex)
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            this.Children.Clear(); 
                            this.Children.Add(new FileSystemItem("Error: " + ex.Message, ItemType.File));
                        });
                    }
                }); 

                App.Current.Dispatcher.Invoke(() =>
                {
                    var loadingItem = this.Children.FirstOrDefault(c => c.Name == "Loading...");
                    if (loadingItem != null)
                    {
                        this.Children.Remove(loadingItem);
                    }
                    this.IsExpanded = true; 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Async Load Error: " + ex.Message);
            }
            finally
            {
                _isLoading = false;
            }
        }
    }
	public partial class Window1 : Window, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
        
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        private ObservableCollection<FileSystemItem> _rootFolders;
        public ObservableCollection<FileSystemItem> RootFolders
        {
            get { return _rootFolders; }
            set
            {
                if (_rootFolders != value)
                {
                    _rootFolders = value;
                    OnPropertyChanged("RootFolders"); 
                }
            }
        }
		public Window1()
		{
			InitializeComponent();
			RootFolders = new ObservableCollection<FileSystemItem>(); 
			this.DataContext = this;

            Loaded += async (s, e) => await LoadRootDrivesAsync();
		}
		private async Task LoadRootDrivesAsync()
        {
            this.RootFolders.Clear();
            
            string[] drives = await Task.Run(() => Directory.GetLogicalDrives());

            foreach (string drive in drives)
            {
                try
                {
                    var driveItem = new FileSystemItem(drive, ItemType.Drive);
                    this.RootFolders.Add(driveItem); 
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error loading drive: " + drive + " - " + ex.Message);
                }
            }
        }

        private async void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem uiItem = e.OriginalSource as TreeViewItem;
            
            if (uiItem != null)
            {
                FileSystemItem fileSystemItem = uiItem.DataContext as FileSystemItem;

                if (fileSystemItem != null)
                {
                    if (fileSystemItem.IsExpanded)
                    {
                        e.Handled = true;
                        return;
                    }
                    await fileSystemItem.LoadChildrenAsync();
                }
            }
            e.Handled = true;
        }
    }
}