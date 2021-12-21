using System;
using System.IO;
using System.Reflection;

namespace HeadlessTweaks
{
    public class Configuration //Literally ripped from NeosModLoader
    {
        private static readonly string CONFIG_FILENAME = "HeadlessTweaks.config";

        private static Configuration _configuration;

        internal static Configuration get()
        {
            if (_configuration == null)
            {
                // the config file can just sit next to the dll. Simple.
                string path = Path.Combine(GetAssemblyDirectory(), CONFIG_FILENAME);
                _configuration = new Configuration();

                // .NET's ConfigurationManager is some hot trash to the point where I'm just done with it.
                // Time to reinvent the wheel. This parses simple key=value style properties from a text file.
                try
                {
                    var lines = File.ReadAllLines(path);
                    foreach (var line in lines)
                    {
                        int splitIdx = line.IndexOf('=');
                        if (splitIdx != -1)
                        {
                            string key = line.Substring(0, splitIdx);
                            string value = line.Substring(splitIdx + 1);

                            if ("UseImpulsePass".Equals(key) && "true".Equals(value))
                            {
                                _configuration.UseImpulsePass = true;
                            }
                            if ("UseDiscordWebhook".Equals(key) && "true".Equals(value))
                            {
                                _configuration.UseDiscordWebhook = true;
                            }
                            else if ("DiscordWebhookID".Equals(key) && !string.IsNullOrWhiteSpace(value))
                            {
                                _configuration.DiscordWebhookID = value;
                            }
                            else if ("DiscordWebhookKey".Equals(key) && !string.IsNullOrWhiteSpace(value))
                            {
                                _configuration.DiscordWebhookKey = value;
                            }
                            else if ("DiscordWebhookUsername".Equals(key) && !string.IsNullOrWhiteSpace(value))
                            {
                                _configuration.DiscordWebhookUsername = value;
                            }
                            else if ("DiscordWebhookAvatar".Equals(key) && !string.IsNullOrWhiteSpace(value))
                            {
                                _configuration.DiscordWebhookAvatar = value;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (e is FileNotFoundException)
                    {
                        //Logger.MsgInternal($"{path} is missing! This is probably fine.");
                    }
                    else if (e is DirectoryNotFoundException || e is IOException || e is UnauthorizedAccessException)
                    {
                        //Logger.WarnInternal(e.ToString());
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return _configuration;
        }

        private static string GetAssemblyDirectory()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
        
        public bool UseImpulsePass { get; private set; } = false;
        public bool UseDiscordWebhook { get; private set; } = false;
        public string DiscordWebhookID { get; private set; } = null;
        public string DiscordWebhookKey { get; private set; } = null;
        public string DiscordWebhookUsername { get; private set; } = "New Headless";
        public string DiscordWebhookAvatar { get; private set; } = "https://newweb.page/assets/images/logo.png";
    }
}