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
    [BepInPlugin("nickklmao.repoconfig", MOD_NAME, "1.1.4")]
    internal sealed class Entry : BaseUnityPlugin
    {
        private const string MOD_NAME = "REPO Config";

        internal static readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(MOD_NAME);
        
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

        private static ConfigEntry<Key> test;
        private static ConfigEntry<float> test3;
        private static ConfigEntry<Key> test2;
        
        private void Awake()
        {
            showInGame = Config.Bind("General", "Show In Game", true, new ConfigDescription(string.Empty, null, "HideREPOConfig"));
            test = Config.Bind("test", "test", Key.A);
            test3 = Config.Bind("test", "test3", 2f);
            test2 = Config.Bind("test", "test 2", Key.B);
            
            if (!showInGame.Value)
                return;
            
            logger.LogDebug("Hooking `MenuPageMain.Start`");
            new Hook(AccessTools.Method(typeof(MenuPageMain), "Start"), MenuPageMain_StartHook);

            ConfigMenu.Initialize();
        }
    }
}