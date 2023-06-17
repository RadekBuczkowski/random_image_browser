namespace Random_Image.Classes.Extensions;

using System.Reflection;
using System.Windows;
using System.Windows.Media;

using Random_Image.Properties;

/// <summary>
/// Provides extension methods for general objects and WPF dependency objects.
/// </summary>
public static class ObjectExtensionMethods
{
    /// <summary>
    /// Returns property value by property name.
    /// </summary>
    public static T GetPropertyValue<T>(this object obj, string propertyName)
    {
        return (T)obj.GetType().GetProperty(propertyName).GetValue(obj);
    }

    /// <summary>
    /// Sets property value by property name.
    /// </summary>
    public static void SetPropertyValue<T>(this object obj, string propertyName, T value)
    {
        obj.GetType().GetProperty(propertyName).SetValue(obj, value, null);
    }

    /// <summary>
    /// Sets all properties in this object without changing their value. Only readable and writable properties
    /// excluding indexers are refreshed. Can be used with <see cref="Settings.Default"/>
    /// before save the user copy of the configuration file.
    /// </summary>
    public static void RefreshProperties(this object obj)
    {
        foreach (PropertyInfo property in obj.GetType().GetProperties())
        {
            if (property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
                obj.SetPropertyValue(property.Name, obj.GetPropertyValue<object>(property.Name));
        }
    }

    /// <summary>
    /// Finds a visual parent by type <typeparamref name="T"/> in this WPF <paramref name="child"/> object.
    /// </summary>
    public static T FindVisualParent<T>(this DependencyObject child) where T : DependencyObject
    {
        if (child == null)
            return null;
        DependencyObject parentObject = VisualTreeHelper.GetParent(child) ?? (child as FrameworkElement)?.Parent;
        if (parentObject is T parent)
            return parent;
        return parentObject.FindVisualParent<T>();
    }

    /// <summary>
    /// Finds a visual child by type <typeparamref name="T"/> and optionally by <paramref name="childName"/>
    /// in this WPF <paramref name="parent"/> object.
    /// </summary>
    public static T FindVisualChild<T>(this DependencyObject parent, string childName = null) where T : DependencyObject
    {
        if (parent == null)
            return null;
        int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childrenCount; i++)
        {
            DependencyObject element = VisualTreeHelper.GetChild(parent, i);
            if (element is T child && (childName == null || (child as FrameworkElement)?.Name == childName))
                return child;
        }
        return null;
    }
}