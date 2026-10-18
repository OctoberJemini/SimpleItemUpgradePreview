using System;
using System.Collections.Generic;
using SimpleUpgradePreview;

internal static class ComparisonTextTests
{
    private static int passed;
    private static void Equal(string name, string expected, string actual)
    {
        if (expected != actual) throw new Exception(name + "\nExpected: " + expected + "\nActual: " + actual);
        passed++;
    }
    private static string Arrow(string label, string oldValue, string newValue)
    {
        return label + ": <color=#BBBBBB>" + oldValue + "</color> \u2192 <color=#FFD966>" + newValue + "</color>";
    }
    public static void Main()
    {
        var labels = new HashSet<string> { "Slash", "Armor", "Durability", "Block armor", "Tranchant" };
        string before = "A blade.\nSlash: <color=orange>53</color> <color=yellow>(20-30)</color>\nDurability: <color=orange>350</color>";
        string after = "A blade.\nSlash: <color=orange>59</color> <color=yellow>(23-34)</color>\nDurability: <color=orange>400</color>";
        string expected = "A blade.\n" + Arrow("Slash", "53", "59") + " (23-34)\n" + Arrow("Durability", "350", "400");
        Equal("Weapon damage, target skill range and max durability", expected, ComparisonText.Apply(after, before, after, labels));
        Equal("Does not duplicate arrows", expected, ComparisonText.Apply(expected, before, after, labels));
        Equal("Unchanged stats preserve formatting", after, ComparisonText.Apply(after, after, after, labels));
        Equal("Armor upgrade", Arrow("Armor", "6", "8"), ComparisonText.Apply("Armor: 8", "Armor: 6", "Armor: 8", labels));
        Equal("Block skill value preserved", Arrow("Block armor", "10", "12") + " (13)", ComparisonText.Apply("Block armor: 12 (13)", "Block armor: 10 (11)", "Block armor: 12 (13)", labels));
        Equal("Localized decimals", Arrow("Tranchant", "53,5", "59,5"), ComparisonText.Apply("Tranchant: 59,5", "Tranchant: 53,5", "Tranchant: 59,5", labels));
        Equal("Mod text remains intact", Arrow("Slash", "53", "59") + "\n<color=purple>Fire imbue: 25%</color>", ComparisonText.Apply("Slash: 59\n<color=purple>Fire imbue: 25%</color>", "Slash: 53", "Slash: 59", labels));
        Equal("Actual enchanted item stats replace prefab preview", Arrow("Slash", "79.5", "88.5"), ComparisonText.Apply("Slash: 59", "Slash: 79.5", "Slash: 88.5", labels));
        Equal("Missing baseline left alone", "Armor: 8", ComparisonText.Apply("Armor: 8", "Slash: 53", "Armor: 8", labels));
        Equal("Ambiguous duplicate stat left alone", "Slash: 59", ComparisonText.Apply("Slash: 59", "Slash: 53\nSlash: 10", "Slash: 59", labels));
        Equal("Duplicate display labels left alone", "Slash: 59\nSlash: 20", ComparisonText.Apply("Slash: 59\nSlash: 20", "Slash: 53", "Slash: 59", labels));
        Equal("Non-numeric effect left alone", "Armor: Strong", ComparisonText.Apply("Armor: Strong", "Armor: Weak", "Armor: Strong", labels));
        Equal("Quality line not altered", "Quality: 5", ComparisonText.Apply("Quality: 5", "Quality: 4", "Quality: 5", labels));
        Equal("No selection text", "", ComparisonText.Apply("", before, after, labels));
        Console.WriteLine("PASS: " + passed + " upgrade display checks.");
        Console.WriteLine(expected);
    }
}
