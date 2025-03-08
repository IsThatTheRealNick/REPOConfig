using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace REPOConfig.MonoBehaviors;

internal sealed class CustomMenuKeybind : MonoBehaviour
{
    internal Action<Key> onValueChanged;
    
    private MenuPage menuPage;
    private MenuBigButton menuBigButton;
    private TextMeshProUGUI headerTMP, buttonTMP;
    private Button button;

    private Key currentKey;

    private InputActionRebindingExtensions.RebindingOperation currentRebindingOperation;
    private float timeSinceRebind;
    
    private static Key ParseKeyName(string keyName)
    {
        if (Enum.TryParse<Key>(keyName.Replace(" ", string.Empty), out var key))
            return key;

        return keyName switch
        {
            "-" => Key.Minus,
            "+" => Key.NumpadPlus,
            "," => Key.Comma,
            "." => Key.Period,
            "=" => Key.Equals,
            _ => Key.None
        };
    }
    
    internal void Initialize(Key startingValue)
    {
        menuPage = GetComponentInParent<MenuPage>();
        menuBigButton = GetComponent<MenuBigButton>();
        headerTMP = GetComponentInChildren<TextMeshProUGUI>();
        buttonTMP = AccessTools.Field(typeof(MenuButton), "buttonText").GetValue(menuBigButton.menuButton) as TextMeshProUGUI;
        button = GetComponentInChildren<Button>();

        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(OnClick);

        currentKey = startingValue;
        UpdateKeybindLabel();
    }
    
    internal void SetHeader(string label) => headerTMP.text = label;
    
    internal void UpdateKeybindLabel() => buttonTMP.text = menuBigButton.buttonName = currentKey.ToString();

    private void Update()
    {
        if (currentRebindingOperation == null)
            return;
        
        timeSinceRebind += Time.deltaTime;

        if (!Mouse.current.leftButton.wasPressedThisFrame || timeSinceRebind <= .1f)
            return;
        
        currentRebindingOperation.Cancel();
        currentRebindingOperation = null;
    }

    private void OnClick()
    {
        currentRebindingOperation?.Cancel();
        
        menuBigButton.state = MenuBigButton.State.Edit;
        
        var templateAction = new InputAction("Template Action", InputActionType.Button, "<Keyboard>/anyKey");
        templateAction.Disable();
        
        currentRebindingOperation = templateAction.PerformInteractiveRebinding()
            .WithCancelingThrough("<Keyboard>/escape")
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(.1f)
            .OnComplete(operation =>
            {
                currentKey = ParseKeyName(InputControlPath.ToHumanReadableString(templateAction.bindings[0].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice));
                
                onValueChanged?.Invoke(currentKey);
                
                menuBigButton.state = MenuBigButton.State.Main;
                MenuManager.instance.MenuEffectClick(MenuManager.MenuClickEffectType.Confirm, menuPage, 0.2f);
                UpdateKeybindLabel();
                
                operation.Dispose();
                templateAction.Dispose();
            })
            .OnCancel(operation =>
            {
                menuBigButton.state = MenuBigButton.State.Main;
                MenuManager.instance.MenuEffectClick(MenuManager.MenuClickEffectType.Deny, menuPage, 0.2f);
                UpdateKeybindLabel();
                
                operation.Dispose();
                templateAction.Disable();
            }).Start();
        timeSinceRebind = 0;
    }
}