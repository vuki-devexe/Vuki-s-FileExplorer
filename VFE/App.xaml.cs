using System;
using System.Windows;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows.Media;
using System.Data;
using System.Xml;
using System.Configuration;

namespace VFE
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public class ThemeDefinition
	{
	    public string Name { get; set; }
	    public Dictionary<string, string> Colors { get; set; }
	    // You can add properties for Styles, Templates, etc.
	}
	public partial class App : Application
	{
       public static void ChangeTheme(string themeFileName)
		{
		    Application.Current.Resources.MergedDictionaries.Clear();
		    Application.Current.Resources.MergedDictionaries.Add(
		        new ResourceDictionary() { Source = new Uri("Themes/"+themeFileName+".xaml", UriKind.Relative) }
		    );
		}
       public ResourceDictionary LoadCustomTheme(string filePath)
		{
		    string jsonContent = File.ReadAllText(filePath);
		    ThemeDefinition themeData = JsonSerializer.Deserialize<ThemeDefinition>(jsonContent);
		    ResourceDictionary newDictionary = new ResourceDictionary();
		    foreach (var entry in themeData.Colors)
		    {
		        SolidColorBrush brush = (SolidColorBrush)new BrushConverter().ConvertFromString(entry.Value);
		        newDictionary.Add(entry.Key, brush);
		    }
		    
		    return newDictionary;
		}
	   public void ApplyCustomTheme(string themeFilePath)
		{
		    ResourceDictionary themeToApply = LoadCustomTheme(themeFilePath);
		    Application.Current.Resources.MergedDictionaries.Add(themeToApply);
		}
	   protected override void OnStartup(StartupEventArgs e)
	    {
	        base.OnStartup(e);
	
	        // 1. Retrieve the saved theme name from settings
	        string savedTheme = Settings.Default.LastThemeName;
	
	        // 2. Call the theme change method to load it immediately
	        ChangeTheme(savedTheme);
	        
	        // Note: The MainWindow creation is handled by App.xaml,
	        // but if you create it manually, you'd do it here:
	        // var mainWindow = new MainWindow();
	        // mainWindow.Show();
	    }
	}
}