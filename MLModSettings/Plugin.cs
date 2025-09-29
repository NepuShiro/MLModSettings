using BepInEx;
using BepInEx.Logging;
using BepInEx.NET.Common;
using BepInExResoniteShim;
using BepisModSettings.DataFeeds;
using BepisResoniteWrapper;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using MonkeyLoader.Resonite.DataFeeds;
using MonkeyLoader.Resonite.DataFeeds.Settings;

namespace MLModSettings;

[ResonitePlugin(PluginMetadata.GUID, PluginMetadata.NAME, PluginMetadata.VERSION, PluginMetadata.AUTHORS, PluginMetadata.REPOSITORY_URL)]
[BepInDependency(BepInExResoniteShim.PluginMetadata.GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(BepisModSettings.PluginMetadata.GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("Nytra.MonkeyLoaderLoader", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("NepuShiro.RMLModSettings", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log = null!;

    public override void Load()
    {
        Log = base.Log;

        ResoniteHooks.OnEngineReady += () =>
        {
            if (AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name?.Contains("MonkeyLoader") == true))
            {
                BepisPluginsPage.CustomPluginsPages += PatchClass.CreateMlCategory;
                BepisConfigsPage.CustomPluginConfigsPages += PatchClass.GotoMlCategory;

                HarmonyInstance.Patch(
                    AccessTools.Method(typeof(FeedItemInterface), nameof(FeedItemInterface.Set), new Type[] { typeof(IDataFeedView), typeof(DataFeedItem) }),
                    postfix: new HarmonyMethod(typeof(PatchClass), nameof(PatchClass.HideMlSettingsCategory))
                );

                HarmonyInstance.Patch(
                    AccessTools.Method(typeof(MonkeyLoaderRootCategorySettingsItems), nameof(MonkeyLoaderRootCategorySettingsItems.Apply)),
                    prefix: new HarmonyMethod(typeof(PatchClass), nameof(PatchClass.LogMlPath))
                );

                Log.LogInfo($"Plugin {PluginMetadata.GUID} is fully loaded!");
            }
            else
            {
                Log.LogFatal("MonkeyLoader is not loaded! You cannot use this plugin without it.");
            }
        };

        Log.LogInfo($"Plugin {PluginMetadata.GUID} is partially loaded!");
    }

    private class PatchClass
    {
        public static async IAsyncEnumerable<DataFeedItem> CreateMlCategory(IReadOnlyList<string> path)
        {
            await Task.CompletedTask;

            DataFeedGroup pluginsGroup = new DataFeedGroup();
            pluginsGroup.InitBase("MLSettingsGroup", path, null!, "Settings.Category.MonkeyLoader".AsLocaleKey());
            yield return pluginsGroup;

            DataFeedCategory mlSettingsCategory = new DataFeedCategory();
            mlSettingsCategory.InitBase("MonkeyLoaderMLSettings", path, ["MLSettingsGroup"], "MonkeyLoader.GamePacks.Resonite.OpenMonkeyLoader.Name".AsLocaleKey(), "MonkeyLoader.GamePacks.Resonite.OpenMonkeyLoader.Description".AsLocaleKey());
            yield return mlSettingsCategory;
        }

        public static async IAsyncEnumerable<DataFeedItem> GotoMlCategory(IReadOnlyList<string> path)
        {
            await Task.CompletedTask;
            
            if (path[1] == "MonkeyLoaderMLSettings") DataFeedHelpers.GoToSettingPath("MonkeyLoader");

            yield break;
        }

        public static void HideMlSettingsCategory(FeedItemInterface __instance)
        {
            SetDataFeedCategory setDfc = __instance.Slot.GetComponent<SetDataFeedCategory>();
            if (setDfc?.CategoryPath?.Contains("MonkeyLoader") == true)
            {
                setDfc.RunSynchronously(() => __instance.Slot.ActiveSelf = false);
            }
        }

        public static void LogMlPath(EnumerateDataFeedParameters<SettingsDataFeed> parameters)
        {
            if (parameters.Path.Contains("MonkeyLoader"))
            {
                Log.LogDebug($"Current Path: {string.Join(" -> ", parameters.Path)}");
            }
        }
    }
}