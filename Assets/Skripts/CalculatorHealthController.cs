using System;
using UnityEngine;
using UnityEngine.UI;

// Owns HP calculator modes, their effects, and rest results.
public sealed class CalculatorHealthController
{
    private enum Mode
    {
        None,
        MaxHp,
        TemporaryHp,
        Damage,
        Heal
    }

    private Mode mode;
    private Color textColor = Color.white;
    private bool hasTextColor;

    public bool IsActive => mode != Mode.None;
    public string ModeLabel { get; private set; } = "";

    public bool TrySelectMode(string label, Func<string, string> localizedText, AppLanguage language)
    {
        switch (label.ToLowerInvariant())
        {
            case "maxhp": mode = Mode.MaxHp; break;
            case "folslive": mode = Mode.TemporaryHp; break;
            case "damage": mode = Mode.Damage; break;
            case "heal": mode = Mode.Heal; break;
            default: return false;
        }

        RefreshLabel(localizedText, language);
        return true;
    }

    public void RefreshLabel(Func<string, string> localizedText, AppLanguage language)
    {
        switch (mode)
        {
            case Mode.MaxHp: ModeLabel = "Max HP:"; break;
            case Mode.TemporaryHp: ModeLabel = localizedText("tempHp") + ":"; break;
            case Mode.Damage: ModeLabel = language == AppLanguage.English ? "Damage:" : "Урон:"; break;
            case Mode.Heal:
                ModeLabel = language == AppLanguage.English ? "Healing:" :
                    language == AppLanguage.Russian ? "Лечение:" : "Зцілення:";
                break;
            default: ModeLabel = ""; break;
        }
    }

    public string Apply(int value, HealthBar healthBar, Func<string, string> localizedText)
    {
        if (healthBar == null)
        {
            Reset();
            return localizedText("hpBarNotFound");
        }

        value = Mathf.Max(0, value);
        string message;
        switch (mode)
        {
            case Mode.MaxHp:
                healthBar.SetMaxHealthAndFill(value);
                message = "Max HP =  " + value;
                break;
            case Mode.TemporaryHp:
                healthBar.SetTemporaryHealth(value);
                message = localizedText("tempHp") + " =  " + value;
                break;
            case Mode.Damage:
                message = localizedText("damageDone") + "  " + healthBar.ApplyDamage(value);
                break;
            case Mode.Heal:
                message = FormatHealed(healthBar.ApplyHeal(value), localizedText);
                break;
            default:
                return "";
        }

        Reset();
        return message;
    }

    public string ApplyShortRest(HealthBar healthBar, Func<string, string> localizedText)
    {
        CharacterRestResult result = CharacterRestService.ApplyShortRest(healthBar);
        if (!result.HasHealthBar)
            return localizedText("shortRest");
        if (!result.HasHitDice)
            return localizedText("hitDiceNotFound");

        return FormatHealed(result.Healed, localizedText) + " (" + result.DiceRolled + "d" +
            result.DiceSides + "=" + result.Roll + ")";
    }

    public string ApplyLongRest(HealthBar healthBar, Func<string, string> localizedText)
    {
        CharacterRestResult result = CharacterRestService.ApplyLongRest(healthBar);
        return result.HasHealthBar ? FormatHealed(result.Healed, localizedText) : localizedText("longRest");
    }

    public void Reset()
    {
        mode = Mode.None;
        ModeLabel = "";
    }

    public void CaptureTextColor(Button button)
    {
        Text buttonText = button != null ? button.GetComponentInChildren<Text>(true) : null;
        if (buttonText == null)
            return;

        textColor = buttonText.color;
        hasTextColor = true;
    }

    public void ApplyTextColor(Text equationText, Text resultText)
    {
        if (!hasTextColor)
            return;
        if (equationText != null)
            equationText.color = textColor;
        if (resultText != null)
            resultText.color = textColor;
    }

    public static string FormatHealed(int value, Func<string, string> localizedText)
    {
        return localizedText("healed") + "  " + value + " HP";
    }

    public static bool IsButtonName(string label)
    {
        switch (label.ToLowerInvariant())
        {
            case "maxhp":
            case "folslive":
            case "damage":
            case "heal":
            case "shortrest":
            case "longrest":
                return true;
            default:
                return false;
        }
    }

    public static HealthBar FindActiveHealthBar()
    {
        HealthBar[] bars = UnityEngine.Object.FindObjectsByType<HealthBar>(FindObjectsInactive.Exclude);
        foreach (HealthBar bar in bars)
            if (bar != null && bar.IsUsableForCalculator)
                return bar;

        return bars.Length > 0 ? bars[0] : null;
    }
}
