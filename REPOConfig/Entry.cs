using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using UnityEngine;
using UnityEngine.InputSystem;

namespace REPOConfig
{
    [BepInPlugin("nickklmao.repoconfig", MOD_NAME, "1.1.5"), BepInDependency("nickklmao.menulib")]
    internal sealed class Entry : BaseUnityPlugin
    {
        private const string MOD_NAME = "REPO Config";

        internal static readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(MOD_NAME);
        
        internal static ConfigEntry<float> descriptionScrollSpeed;
        
        private static ConfigEntry<bool> showInGame;
        
        private static void MenuPageMain_StartHook(Action<MenuPageMain> orig, MenuPageMain self)
        {
            var childrenInOrder = self.transform.Cast<Transform>()
                .Where(transform => transform.name.Contains("Menu Button"))
                .OrderByDescending(transform => transform.localPosition.y);
            
            var yPosition = 224f;
            foreach (var child in childrenInOrder)
            {
                if (child.name.Contains("Quit"))
                    yPosition -= 34;
                
                child.localPosition = child.localPosition with { y = yPosition};
                yPosition -= 34;
            }
            
            orig.Invoke(self);
        }

        private static ConfigEntry<Key> a;
        private static ConfigEntry<float> a1;
        private static ConfigEntry<float> a2;
        private static ConfigEntry<Key> b;
        private static ConfigEntry<float> a3;
        private static ConfigEntry<Key> c;
        private static ConfigEntry<Key> d;
        
        private void Awake()
        {
            a = Config.Bind("test", "test1", Key.A);
            a1 = Config.Bind("test", "help1", 1f);
            b = Config.Bind("test", "test2", Key.A);
            c = Config.Bind("test", "test3", Key.A);
            a2 = Config.Bind("test", "help2", 1f);
            a3 = Config.Bind("test", "help3", 1f);
            d = Config.Bind("test", "test4", Key.A);
            
            descriptionScrollSpeed = Config.Bind("General", "Description Scroll Speed", .15f, new ConfigDescription("How fast descriptions scroll. (Seconds per character)", new AcceptableValueRange<float>(0.1f, 2f)));
            showInGame = Config.Bind("General", "Show In Game", true, new ConfigDescription(string.Empty, null, "HideREPOConfig"));

            if (!showInGame.Value)
                return;
            
            logger.LogDebug("Hooking `MenuPageMain.Start`");
            new Hook(AccessTools.Method(typeof(MenuPageMain), "Start"), MenuPageMain_StartHook);

            ConfigMenu.Initialize();
        }
    }
}