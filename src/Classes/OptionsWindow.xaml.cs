namespace Random_Image.Classes;

using System.Windows;
using System.Windows.Controls;

using Random_Image.Classes.Browser;
using Random_Image.Resources;

/// <summary>
/// Interaction logic for the Options window.
/// </summary>
public partial class OptionsWindow : Window
{
    public string WindowTitle { get; } = $"{App.Title} - {Text.OptionsTitle}";

    public ImageBrowserSetOptions Options { get; }

    private OptionsWindow(ImageBrowser browser)
    {
        Owner = browser.ImageCanvas.Window;
        Options = new ImageBrowserSetOptions(browser);
        InitializeComponent();
    }

    public static void Show(ImageBrowser browser) => new OptionsWindow(browser).ShowDialog();
    
    private void ButtonFolder_Click(object sender, RoutedEventArgs e)
    {
        Options.ShowFolderDialog((string)((Button)sender).Tag);
    }

    private void SetAsDefault_Click(object sender, RoutedEventArgs e)
    {
        Options.SetAsDefault();
    }

    private void ButtonBack_Click(object sender, RoutedEventArgs e)
    {
        Options.Apply();
    }
}