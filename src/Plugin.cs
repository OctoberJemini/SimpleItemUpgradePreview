using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace SimpleUpgradePreview
{
    [BepInPlugin(Id, "Simple Item Upgrade Preview", "1.0.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Id = "local.che.simpleupgradepreview";
        private Harmony harmony;
        internal static Plugin Instance;
        internal static bool Failed;

        private void Awake()
        {
            Instance = this;
            try
            {
                PreviewPatch.ValidateApi();
                harmony = new Harmony(Id);
                harmony.PatchAll(typeof(Plugin).Assembly);
                Logger.LogInfo("Simple Item Upgrade Preview ready: current -> next stats in the Upgrade panel.");
            }
            catch (Exception error) { StopPreview(error); }
        }

        internal static void StopPreview(Exception error)
        {
            if (Failed) return;
            Failed = true;
            Instance.Logger.LogWarning("Upgrade preview disabled; normal crafting remains available. " + error);
        }

        private void OnDestroy()
        {
            if (harmony != null) harmony.UnpatchSelf();
            Instance = null;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "UpdateRecipe")]
    internal static class PreviewPatch
    {
        private static readonly FieldInfo Selected = AccessTools.Field(typeof(InventoryGui), "m_selectedRecipe");
        private static MethodInfo getItem;
        private static MethodInfo getRecipe;
        private static ItemDrop.ItemData cachedItem;
        private static int cachedQuality;
        private static string cachedInput, cachedOutput;
        private static float refreshAt;
        private static readonly string[] Tokens = {
            "$item_durability", "$item_armor", "$item_blockarmor", "$item_blockforce",
            "$item_parrybonus", "$item_parryadrenaline", "$item_knockback",
            "$inventory_damage", "$inventory_blunt", "$inventory_slash", "$inventory_pierce",
            "$inventory_fire", "$inventory_frost", "$inventory_lightning",
            "$inventory_poison", "$inventory_spirit"
        };

        internal struct Context
        {
            public ItemDrop.ItemData Item;
            public int Quality;
        }

        internal static void ValidateApi()
        {
            if (Selected == null) throw new MissingFieldException("InventoryGui.m_selectedRecipe");
            getItem = AccessTools.PropertyGetter(Selected.FieldType, "ItemData");
            getRecipe = AccessTools.PropertyGetter(Selected.FieldType, "Recipe");
            if (getItem == null || getRecipe == null) throw new MissingMemberException("RecipeDataPair accessors");
        }

        private static void Prefix(InventoryGui __instance, out Context __state)
        {
            __state = new Context();
            if (Plugin.Failed) return;
            try
            {
                object selected = Selected.GetValue(__instance);
                var item = getItem.Invoke(selected, null) as ItemDrop.ItemData;
                var recipe = getRecipe.Invoke(selected, null) as Recipe;
                if (item == null || recipe == null || item.m_shared == null) return;
                var player = Player.m_localPlayer;
                if (player == null) return;
                var station = player.GetCurrentCraftingStation();
                if (item.m_quality >= item.m_shared.m_maxQuality &&
                    (station == null || !station.m_upgrader)) return;
                __state.Item = item;
                __state.Quality = item.m_quality;
            }
            catch (Exception error) { Plugin.StopPreview(error); }
        }

        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("randyknapp.mods.epicloot")]
        private static void Postfix(InventoryGui __instance, Context __state)
        {
            if (Plugin.Failed || __state.Item == null) return;
            try
            {
                // A completed craft can replace or advance the item during UpdateRecipe.
                if (__state.Item.m_quality != __state.Quality) return;
                object selected = Selected.GetValue(__instance);
                if (!System.Object.ReferenceEquals(getItem.Invoke(selected, null), __state.Item)) return;
                var description = __instance.m_recipeDecription;
                if (description == null || !description.enabled) return;
                string input = description.text;
                if (System.Object.ReferenceEquals(cachedItem, __state.Item) && cachedQuality == __state.Quality &&
                    Time.unscaledTime < refreshAt && (input == cachedInput || input == cachedOutput))
                {
                    description.text = cachedOutput;
                    return;
                }
                var labels = new HashSet<string>();
                foreach (string token in Tokens) labels.Add(Localization.instance.Localize(token));
                // The actual selected item retains its mod data. The crafting flag gives MAX
                // durability rather than remaining durability and excludes worn-item counters.
                string before = Localization.instance.Localize(ItemDrop.ItemData.GetTooltip(
                    __state.Item, __state.Quality, true, Game.m_worldLevel, -1, false));
                string after = Localization.instance.Localize(ItemDrop.ItemData.GetTooltip(
                    __state.Item, __state.Quality + 1, true, Game.m_worldLevel, -1, false));
                string output = ComparisonText.Apply(input, before, after, labels);
                cachedItem = __state.Item;
                cachedQuality = __state.Quality;
                cachedInput = input;
                cachedOutput = output;
                refreshAt = Time.unscaledTime + 0.5f;
                description.text = output;
            }
            catch (Exception error) { Plugin.StopPreview(error); }
        }
    }
}

