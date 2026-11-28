using UnityEngine;

/// <summary>
/// Optional ScriptableObject asset allowing designers to create reusable loot configurations
/// via Assets -> Create -> Gardien Furtif -> Mission -> Loot Item Data.
/// </summary>
[CreateAssetMenu(fileName = "LootItemData", menuName = "Gardien Furtif/Mission/Loot Item Data", order = 1)]
public class LootItemData : ScriptableObject
{
    public LootCollectible.LootPresetType presetType = LootCollectible.LootPresetType.CashBundle;
    public string displayName = "Cash Bundle";
    [TextArea(1, 3)]
    public string description = "Unmarked stack of bills left in the office.";
    public string objectiveText = "Collect the cash bundle";
    public Sprite icon;
    [Min(0)] public int monetaryValue = 250;
    public bool requiredForExit = false;
    public string customGuideCollectedMessage = "";
}
