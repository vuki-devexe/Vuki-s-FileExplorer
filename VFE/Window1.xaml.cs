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

namespace VFE
{
	public enum ItemType
    {
        Drive,
        Folder,
        File
    }
	// --- FILE SYSTEM ITEM MODEL ---
    public class FileSystemItem : INotifyPropertyChanged
    {
        // INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
        
        // Properties
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

        // Constructor
        public FileSystemItem(string fullPath, ItemType type)
        {
            this.FullPath = fullPath;
            this.Type = type;
            this.Name = type == ItemType.Drive ? new DirectoryInfo(fullPath).Name : Path.GetFileName(fullPath);
            this.Children = new ObservableCollection<FileSystemItem>();

            // For drives and folders, add a dummy item to enable the expander (+)
            if (this.HasChildren)
            {
                // This is safe here because the constructor is initially called on the UI thread.
                this.Children.Add(new FileSystemItem(null, ItemType.File));
            }
        }
        
        // Lazy Loading Logic (Renamed to async Task for best practice)
        public async Task LoadChildrenAsync()
        {
            // Only load if it's a folder/drive and hasn't been expanded yet
            if (this.Type == ItemType.File || this.IsExpanded)
            {
                return;
            }

            try
            {
                // 1. Clear dummy item and show "Loading..." (Safe on UI thread, since this method is called on the UI thread)
                this.Children.Clear();
                this.Children.Add(new FileSystemItem("Loading...", ItemType.File)); 

                // 2. Wrap the slow I/O calls in Task.Run()
                await Task.Run(() =>
                {
                    // *** CODE INSIDE THIS BLOCK IS ON A BACKGROUND THREAD ***
                    
                    try
                    {
                        var directories = Directory.GetDirectories(this.FullPath);
                        var files = Directory.GetFiles(this.FullPath);
                        
                        // 3. EVERY UI/Collection MODIFICATION MUST USE THE DISPATCHER
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            // Add sub-directories
                            foreach (string dirPath in directories)
                            {
                                this.Children.Add(new FileSystemItem(dirPath, ItemType.Folder));
                            }
                            // Add files
                            foreach (string filePath in files)
                            {
                                this.Children.Add(new FileSystemItem(filePath, ItemType.File));
                            }
                        });
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Add error message on UI thread
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            this.Children.Add(new FileSystemItem("Access Denied", ItemType.File));
                        });
                    }
                    catch (Exception ex)
                    {
                        // Add error message on UI thread
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            this.Children.Add(new FileSystemItem("Error: " + ex.Message, ItemType.File));
                        });
                    }
                }); 
                
                // *** CODE RESUMES ON THE UI THREAD HERE (after the I/O is finished) ***

                // 4. Remove "Loading..." message (Safe on UI thread after await)
                var loadingItem = this.Children.FirstOrDefault(c => c.Name == "Loading...");
                if (loadingItem != null)
                {
                    this.Children.Remove(loadingItem);
                }
                
                // 5. Mark as loaded (Safe on UI thread after await)
                this.IsExpanded = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Async Load Error: " + ex.Message);
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

        // 1. ADD THE MISSING PROPERTY
        private ObservableCollection<FileSystemItem> _rootFolders = new ObservableCollection<FileSystemItem>();
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
			this.DataContext = this; // Set the context for bindings

            // Initialize the TreeView with the root elements (Drives)
            Loaded += async (s, e) => await LoadRootDrivesAsync();
		}
		private async Task LoadRootDrivesAsync()
        {
            // CHANGE: Clear the data model property, not the UI control's Items
            this.RootFolders.Clear();
            
            string[] drives = await Task.Run(() => Directory.GetLogicalDrives());

            foreach (string drive in drives)
            {
                try
                {
                    var driveItem = new FileSystemItem(drive, ItemType.Drive);
                    // CHANGE: Add the item to the RootFolders collection
                    this.RootFolders.Add(driveItem); 
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error loading drive: " + drive + " - " + ex.Message);
                }
            }
        }

        // Event handler for when a TreeViewItem is expanded (Must be async void)
        private async void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
		{
		    // 1. Get the item that was expanded from the event source
		    TreeViewItem uiItem = e.OriginalSource as TreeViewItem;
		    
		    if (uiItem != null)
		    {
		        // 2. Get the data model bound to the UI item
		        FileSystemItem fileSystemItem = uiItem.DataContext as FileSystemItem;
		
		        if (fileSystemItem != null)
		        {
		            // --- CRITICAL FIX FOR LAG ---
		            if (fileSystemItem.IsExpanded)
		            {
		                // This condition checks if the item's children are ALREADY loaded.
		                // If they are, it means this event is a spurious re-fire (the bubble-up).
		                
		                // We set e.Handled = true to stop the event from continuing to propagate
		                // to the item's parents, which prevents unnecessary re-loads.
		                e.Handled = true;
		                return; // Exit the method immediately.
		            }
		            // --- END OF FIX ---
		            
		            // 3. If it's not already expanded, proceed with loading
		            await fileSystemItem.LoadChildrenAsync();
		        }
		    }
		    // 4. Ensure the event is handled in all cases
		    e.Handled = true;
		}
	}
}