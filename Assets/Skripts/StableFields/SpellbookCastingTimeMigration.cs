using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

internal static class SpellbookCastingTimeMigration
{
    private const string ActionIdPrefix = "dnd.spelbook.dropdown.dropdowndia.";
    private const string TimeIdPrefix = "dnd.spelbook.dropdown.dropdowntime.";
    private const string SceneName = "spelBook";

    internal static void Apply(CharacterSceneData data, IEnumerable<Dropdown> dropdowns)
    {
        if (data.sceneName != SceneName && !data.sceneName.StartsWith(SceneName + " ", StringComparison.Ordinal)) return;
        var cards = dropdowns.Where(dropdown => dropdown != null && dropdown.gameObject.scene.name == SceneName)
            .Select(dropdown => (control: dropdown, field: PersistentFieldId.For(dropdown)))
            .Where(binding => binding.field != null)
            .GroupBy(binding => binding.control.transform.parent);

        foreach (var card in cards)
        {
            var actions = card.Where(binding => binding.field.Id.StartsWith(ActionIdPrefix, StringComparison.Ordinal)).ToArray();
            var times = card.Where(binding => binding.field.Id.StartsWith(TimeIdPrefix, StringComparison.Ordinal)).ToArray();
            if (actions.Length != 1 || times.Length != 1) continue;

            int oldAction = StableFieldStorage.ReadInt(data, actions[0].control, 0);
            if (oldAction != 4 && oldAction != 5) continue;

            // Previous wizard saves stored the two duration options in the action field.
            StableFieldStorage.WriteDropdown(data, times[0].field, oldAction == 4 ? 3 : 5);
            StableFieldStorage.WriteDropdown(data, actions[0].field, 0);
        }
    }
}
