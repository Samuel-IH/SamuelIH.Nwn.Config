using System;
using Castle.DynamicProxy;
using NLog;

namespace SamuelIH.Nwn.Config;

internal class ConfigInterceptor : IInterceptor
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    private readonly ConfigManager configManager;
    private readonly object config;

    internal ConfigInterceptor(ConfigManager configManager, object config)
    {
        this.configManager = configManager;
        this.config = config;
    }
    
    public void Intercept(IInvocation invocation)
    {
        Log.Info($"invocation: {invocation.Method.Name}");
        
        // Check if the method is a property setter
        if (invocation.Method.IsSpecialName && invocation.Method.Name.StartsWith("set_"))
        {
            // Proceed with the original setter call
            invocation.Proceed();

            // Extract property name from the setter name (removes "set_")
            var propertyName = invocation.Method.Name.Substring(4);
            Log.Info($"Property '{propertyName}' was modified, saving configuration.");

            // Call your config manager's save logic
            configManager.SaveConfig(config);
        }
        else
        {
            // Proceed normally for all other calls
            invocation.Proceed();
        }
    }
}