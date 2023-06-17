namespace Random_Image.Classes.Wpf;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

/// <summary>
/// Provides a class supporting WPF bindings to internal properties.
/// </summary>
public class BindingClass : Window, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Notifies the UI that the specified property has changed value.
    /// </summary>
    protected virtual void Notify([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}