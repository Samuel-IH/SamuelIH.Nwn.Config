using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Anvil.API;
using Anvil.Services;
using Castle.DynamicProxy;
using NLog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SamuelIH.Nwn.Config;

    [ServiceBinding(typeof(ConfigManager))]
    public class ConfigManager
    {
        public const string ConfigDir = "/nwn/home/config/";
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        private readonly ISerializer serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        private readonly Dictionary<object, string?> configs = new();
        
        private readonly ProxyGenerator proxyGenerator = new();
        private readonly ConfigScriptAccessor configScriptAccessor = new();

        public ConfigManager()
        {
            if (!Directory.Exists(ConfigDir))
            {
                Log.Info("Config directory does not exist. Creating...");
                Directory.CreateDirectory(ConfigDir);
            }
        }

        public T BuildConfig<T>(string name) where T: class, new()
        {
            T config;

            // make sure that the name doesn't contain characters that are illegal in filenames
            var regex = new Regex("^[a-zA-Z0-9_-]+$");
            if (!regex.IsMatch(name))
                throw new Exception(
                    $"Config name {name} contains illegal characters. Only a-z, A-Z, 0-9, _ and - are allowed ([a-zA-Z0-9_-]).");

            var path = Path.Combine(ConfigDir, name + ".yaml");

            if (File.Exists(path))
            {
                var rawYaml = File.ReadAllText(path);
                config = deserializer.Deserialize<T>(rawYaml);
                configs.Add(config!, name);
            }
            else
            {
                config = new T();
                Log.Info($"Config {name} not found. Creating new one at {path}...");
                configs.Add(config!, name);
                SaveConfig(config);
            }
            
            var proxiedConfig = proxyGenerator.CreateClassProxyWithTarget(config, new ConfigInterceptor(this, config));
            
            configScriptAccessor.RegisterConfig(proxiedConfig, name);

            return proxiedConfig;
        }

        internal void SaveConfig<T>(T config)
        {
            if (config is null) return;
            if (!configs.TryGetValue(config, out var name))
            {
                Log.Error($"Config {config} is not registered!");
            }
            
            var path = Path.Combine(ConfigDir, name + ".yaml");
            var yaml = serializer.Serialize(config);
            File.WriteAllText(path, yaml);
        }

        [ScriptHandler("config_getset")]
        public void NwScriptGetSet()
        {
            var taskVar = NwModule.Instance.GetObjectVariable<LocalVariableInt>("config_getset_task");
            var typeVar = NwModule.Instance.GetObjectVariable<LocalVariableInt>("config_getset_type");
            var configNameVar = NwModule.Instance.GetObjectVariable<LocalVariableString>("config_getset_config");
            var propNameVar = NwModule.Instance.GetObjectVariable<LocalVariableString>("config_getset_prop");
            
            var returnIntVar = NwModule.Instance.GetObjectVariable<LocalVariableInt>("config_getset_return_int");
            var returnFloatVar = NwModule.Instance.GetObjectVariable<LocalVariableFloat>("config_getset_return_float");
            var returnStringVar = NwModule.Instance.GetObjectVariable<LocalVariableString>("config_getset_return_string");

            if (taskVar.Value == 0) // get
            {
                switch (typeVar.Value)
                {
                    case 0: // int
                        returnIntVar.Value = configScriptAccessor.GetInt(configNameVar.Value ?? "", propNameVar.Value ?? "");
                        break;
                    case 1: // float
                        returnFloatVar.Value = configScriptAccessor.GetFloat(configNameVar.Value ?? "", propNameVar.Value ?? "");
                        break;
                    case 2: // string
                        returnStringVar.Value = configScriptAccessor.GetString(configNameVar.Value ?? "", propNameVar.Value ?? "");
                        break;
                }
            }
            else // set
            {
                switch (returnIntVar.Value)
                {
                    case 0: // int
                        configScriptAccessor.SetInt(configNameVar.Value ?? "", propNameVar.Value ?? "", returnIntVar.Value);
                        break;
                    case 1: // float
                        configScriptAccessor.SetFloat(configNameVar.Value ?? "", propNameVar.Value ?? "", returnFloatVar.Value);
                        break;
                    case 2: // string
                        configScriptAccessor.SetString(configNameVar.Value ?? "", propNameVar.Value ?? "", returnStringVar.Value ?? "");
                        break;
                }
            }
            
            // Wipe all vars but return vars
            taskVar.Delete();
            typeVar.Delete();
            configNameVar.Delete();
            propNameVar.Delete();
        }
    }