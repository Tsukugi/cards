using System;
using System.Reflection;
using Godot;

public static class ClassUtils
{
    public static void SetPropertyValue(this object obj, string propertyName, object value)
    {
        // Get the type of the object
        Type type = obj.GetType();

        // Get the property info
        PropertyInfo propertyInfo = type.GetProperty(propertyName);

        if (propertyInfo != null && propertyInfo.CanWrite)
        {
            // Set the property value
            // GD.Print($"[SetPropertyValue] Property '{propertyInfo.Name}' value is '{value}'");
            propertyInfo.SetValue(obj, value);
        }
        else
        {
            GD.PrintErr($"[SetPropertyValue] Property '{propertyName}' not found or is not writable.");
        }
    }
    public static T GetPropertyValue<T>(this object obj, string propertyName)
    {
        // Get the type of the object
        Type type = obj.GetType();

        // Get the property info
        PropertyInfo propertyInfo = type.GetProperty(propertyName);

        if (propertyInfo != null && propertyInfo.CanRead)
        {
            // Get the property value and cast it to the specified type
            // GD.Print($"[GetPropertyValue] Property '{propertyInfo.Name}' value is '{propertyInfo.GetValue(obj)}'");
            return (T)propertyInfo.GetValue(obj);
        }
        else
        {
            GD.PrintErr($"[GetPropertyValue] Property '{propertyName}' not found or is not readable. '{propertyInfo}' ");
            return default; // Return default value for the type
        }
    }

    public static object? CallMethod(object target, string methodName, object?[]? parameters)
    {
        // Get the method info based on the effect name
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (method != null) return method.Invoke(target, parameters);
        else GD.Print($"Method '{methodName}' not found.");
        return default;
    }
}