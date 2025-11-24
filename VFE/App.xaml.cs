using System;
using System.Windows;
using System.Data;
using System.Xml;
using System.Configuration;

namespace VFE
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
       public static void ChangeTheme(string themeFileName)
		{
		    Application.Current.Resources.MergedDictionaries.Clear();
		    Application.Current.Resources.MergedDictionaries.Add(
		        new ResourceDictionary() { Source = new Uri($"Themes/{themeFileName}.xaml", UriKind.Relative) }
		    );
		}
	}
}