using System;
using System.IO;
using System.Text.RegularExpressions;
using Anvil.Services;
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

        public ConfigManager()
        {
            if (!Directory.Exists(ConfigDir))
            {
                Log.Info("Config directory does not exist. Creating...");
                Directory.CreateDirectory(ConfigDir);
            }
        }

        public T BuildConfig<T>(string name) where T : Configuration, new()
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
                config.Name = name;
            }
            else
            {
                config = new T();
                config.Name = name;
                Log.Info($"Config {config.Name} not found. Creating new one at {path}...");
                SaveConfig(config);
            }

            config.Manager = this;

            return config;
        }

        internal void SaveConfig(Configuration config)
        {
            var path = Path.Combine(ConfigDir, config.Name + ".yaml");
            var yaml = serializer.Serialize(config);
            File.WriteAllText(path, yaml);
        }
    }