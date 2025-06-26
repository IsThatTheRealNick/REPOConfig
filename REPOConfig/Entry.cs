using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MenuLib;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace REPOConfig;

[BepInPlugin("nickklmao.repoconfig", MOD_NAME, "1.2.3"), BepInDependency("nickklmao.menulib", "2.5.0")]
internal sealed class Entry : BaseUnityPlugin
{
    private const string MOD_NAME = "REPOConfig";

    internal static readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(MOD_NAME);
        
    //internal static ConfigEntry<bool> showDescriptions;
        
    //internal static ConfigEntry<float> descriptionScrollSpeed;
        
    private static ConfigEntry<bool> showInGame;
        
    private static void MenuPageMain_StartHook(Action<MenuPageMain> orig, MenuPageMain self)
    {
        var modButton = MenuAPI.CreateREPOButton("Mods", ConfigMenu.CreateModMenu, self.transform, new Vector2(48.3f, 0f));
        
        var childrenInOrder = self.transform.Cast<Transform>()
            .Where(transform => transform.name.Contains("Menu Button"))
            .OrderByDescending(transform => transform.localPosition.y).ToList();

        childrenInOrder.Insert(childrenInOrder.Count - 1, modButton.transform);
        
        var yPosition = 224f;
        foreach (var child in childrenInOrder)
        {
            child.localPosition = child.localPosition with { y = yPosition};
            yPosition -= 30;
        }
            
        orig.Invoke(self);
    }
        
    private void Awake()
    {
        //showDescriptions = Config.Bind("General", "Show Descriptions", true);

        /*showDescriptions.SettingChanged += (_, _) => {
            var modButton = ConfigMenu.lastClickedModButton;
                
            if (!modButton)
                return;

            ConfigMenu.lastClickedModButton = null;
            modButton.onClick.Invoke();
        };*/
            
        /*descriptionScrollSpeed = Config.Bind("General", "Description Scroll Speed", .15f, new ConfigDescription("How fast descriptions scroll. (Seconds per character)", new AcceptableValueRange<float>(0.1f, 2f)));*/
        showInGame = Config.Bind("General", "Show In Game", true, new ConfigDescription(string.Empty, null, "HideFromREPOConfig"));

        if (!showInGame.Value)
            return;
            
        logger.LogDebug("Hooking `MenuPageMain.Start`");
        new Hook(AccessTools.Method(typeof(MenuPageMain), "Start"), MenuPageMain_StartHook);

        ConfigMenu.Initialize();
    }
}