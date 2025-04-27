using System;
using System.Reflection;
using System.Threading.Tasks;
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
        else GD.PrintErr($"Method '{methodName}' not found.");
        return default;
    }

    public static async Task<object?> CallMethodAsync(object target, string methodName, object?[]? parameters)
    {
        // Get the method info based on the method name
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (method != null)
        {
            // Check if the method is asynchronous
            if (method.ReturnType == typeof(Task))
            {
                // Invoke the method and await the result
                await (Task)method.Invoke(target, parameters);
                return null; // Since the method returns void, we return null
            }
            else if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                // Invoke the method and await the result
                return await (Task<object?>)method.Invoke(target, parameters);
            }
            else
            {
                // If the method is synchronous, invoke it normally
                return method.Invoke(target, parameters);
            }
        }
        else
        {
            GD.PrintErr($"Method '{methodName}' not found.");
            return default;
        }
    }
}