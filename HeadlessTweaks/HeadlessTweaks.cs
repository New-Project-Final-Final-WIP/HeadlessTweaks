using HarmonyLib;
using NeosModLoader;

namespace HeadlessTweaks
{
    public class HeadlessTweaks : NeosMod
    {
        public override string Name => "HeadlessTweaks";
        public override string Author => "New-Project-Final-Final-WIP";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/New-Project-Final-Final-WIP/HeadlessTweaks";


        public static Configuration config = Configuration.get();
        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("me.New-Project-Final-Final-WIP.HeadlessTweaks");
            if (config.UseDiscordWebhook) DiscordIntegration.Init(harmony);
            if (config.UseImpulsePass) ImpulsePass.Init(harmony);
        }
    }
}