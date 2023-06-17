namespace Random_Image.Classes;

using System.Windows;

using Random_Image.Classes.Utilities;
using Random_Image.Resources;

/// <summary>
/// Interaction logic for the help window.
/// </summary>
public partial class HelpWindow : Window
{
    public string WindowTitle { get; } = $"{App.Title} - {Text.HelpTitle}";

    private HelpWindow(Window parent)
    {
        Owner = parent;
        InitializeComponent();
        WebContent.NavigateToString(ResourceHelper.LoadTextResource(ResourceHelper.HelpHtml));
    }

    public static void Show(Window parent) => new HelpWindow(parent).ShowDialog();
}