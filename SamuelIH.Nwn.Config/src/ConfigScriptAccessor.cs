using System;
using System.Collections.Generic;
using System.Reflection;
using YamlDotNet.Serialization;

namespace SamuelIH.Nwn.Config;

internal class ConfigScriptAccessor
{
    class ScriptingAccessibleConfigVar<T>
    {
        internal Func<T> getValue;
        internal Action<T> setValue;

        internal ScriptingAccessibleConfigVar(Func<T> getValue, Action<T> setValue)
        {
            this.getValue = getValue;
            this.setValue = setValue;
        }
    }

    private Dictionary<string, Dictionary<string, ScriptingAccessibleConfigVar<int>>> scriptingIntsByNameByConfig =
        new();
    private Dictionary<string, Dictionary<string, ScriptingAccessibleConfigVar<float>>> scriptingFloatsByNameByConfig =
        new();
    private Dictionary<string, Dictionary<string, ScriptingAccessibleConfigVar<string>>> scriptingStringsByNameByConfig =
        new();
    
    internal void RegisterConfig<T>(T config, string configName)
    { 
        scriptingIntsByNameByConfig.Add(configName, new Dictionary<string, ScriptingAccessibleConfigVar<int>>());
        scriptingFloatsByNameByConfig.Add(configName, new Dictionary<string, ScriptingAccessibleConfigVar<float>>());
        scriptingStringsByNameByConfig.Add(configName, new Dictionary<string, ScriptingAccessibleConfigVar<string>>());
        
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            // Check for allowed types.
            if (prop.PropertyType == typeof(int) ||
                prop.PropertyType == typeof(float) ||
                prop.PropertyType == typeof(string))
            {
                // Default to the property name.
                string propName = prop.Name;

                // Depending on the property type, create the appropriate accessor.
                if (prop.PropertyType == typeof(int))
                {
                    var accessor = new ScriptingAccessibleConfigVar<int>(
                        getValue: () => (int)prop.GetValue(config)!,
                        setValue: value => prop.SetValue(config, value)
                    );
                    
                    scriptingIntsByNameByConfig[configName].Add(propName, accessor);
                }
                else if (prop.PropertyType == typeof(float))
                {
                    var accessor = new ScriptingAccessibleConfigVar<float>(
                        getValue: () => (float)prop.GetValue(config)!,
                        setValue: value => prop.SetValue(config, value)
                    );
                    
                    scriptingFloatsByNameByConfig[configName].Add(propName, accessor);
                }
                else if (prop.PropertyType == typeof(string))
                {
                    var accessor = new ScriptingAccessibleConfigVar<string>(
                        getValue: () => (string)prop.GetValue(config)!,
                        setValue: value => prop.SetValue(config, value)
                    );
                    
                    scriptingStringsByNameByConfig[configName].Add(propName, accessor);
                }
            }
        }
    }
    
    internal int GetInt(string configName, string propName)
    {
        if (scriptingIntsByNameByConfig.TryGetValue(configName, out var configInts))
        {
            if (configInts.TryGetValue(propName, out var accessor))
            {
                return accessor.getValue();
            }
        }
        
        return 0;
    }
    
    internal void SetInt(string configName, string propName, int value)
    {
        if (scriptingIntsByNameByConfig.TryGetValue(configName, out var configInts))
        {
            if (configInts.TryGetValue(propName, out var accessor))
            {
                accessor.setValue(value);
            }
        }
    }
    
    internal float GetFloat(string configName, string propName)
    {
        if (scriptingFloatsByNameByConfig.TryGetValue(configName, out var configFloats))
        {
            if (configFloats.TryGetValue(propName, out var accessor))
            {
                return accessor.getValue();
            }
        }
        
        return 0.0f;
    }
    
    internal void SetFloat(string configName, string propName, float value)
    {
        if (scriptingFloatsByNameByConfig.TryGetValue(configName, out var configFloats))
        {
            if (configFloats.TryGetValue(propName, out var accessor))
            {
                accessor.setValue(value);
            }
        }
    }
    
    internal string GetString(string configName, string propName)
    {
        if (scriptingStringsByNameByConfig.TryGetValue(configName, out var configStrings))
        {
            if (configStrings.TryGetValue(propName, out var accessor))
            {
                return accessor.getValue();
            }
        }
        
        return "";
    }
    
    internal void SetString(string configName, string propName, string value)
    {
        if (scriptingStringsByNameByConfig.TryGetValue(configName, out var configStrings))
        {
            if (configStrings.TryGetValue(propName, out var accessor))
            {
                accessor.setValue(value);
            }
        }
    }
}

