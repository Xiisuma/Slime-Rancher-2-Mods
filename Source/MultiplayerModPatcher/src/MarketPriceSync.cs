using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.Economy;
using Il2CppMonomiPark.SlimeRancher.UI;

namespace MultiplayerModPatcher;

/// <summary>
/// Makes the market prices land on the plort they belong to.
///
/// Ranching Together sends the board as a bare array of (current, previous) pairs and applies it by
/// position: it walks the host's <c>PlortEconomyDirector._currValueMap</c> and the client's in
/// parallel, pairing entry 0 with entry 0. Nothing in the packet says which plort a price is for.
///
/// That map is a hash table. Its layout depends on what was inserted and in which order — and a
/// modded plort is inserted at load time, so the two machines lay their tables out differently. The
/// prices then arrive shifted: every board shows the right numbers on the wrong plorts.
///
/// The fix keeps the wire format and changes the order both sides read it in: prices are packed and
/// unpacked sorted by reference id, which is the same sequence on every machine running the same
/// mods. Both players need this patcher for it to hold, so a length mismatch — the shape of a
/// different mod list on the other side — is reported and the update dropped rather than applied to
/// the wrong plorts.
/// </summary>
internal static class MarketPriceSync
{
    private static PropertyInfo _marketUi;
    private static MethodInfo _econUpdate;

    public static bool Install(HarmonyLib.Harmony harmony)
    {
        if (!SR2MPBridge.Available) return false;

        Type globals = SR2MPBridge.Type("SR2MP.GlobalVariables");
        MethodInfo prices = globals?.GetProperty("MarketPricesArray", SR2MPBridge.Any)?.GetGetMethod(true);

        // The handler inherits an abstract Handle and overrides it, so asking by name alone is
        // ambiguous: the one to patch is the override declared on the handler itself.
        MethodInfo handler = SR2MPBridge.Type("SR2MP.Handlers.Currency.MarketPriceHandler")
            ?.GetMethods(SR2MPBridge.Any)
            .FirstOrDefault(method => method.Name == "Handle" && method.GetParameters().Length == 2
                                      && !method.IsAbstract);

        if (prices == null || handler == null)
        {
            Main.Log.Warning("Market prices left as they are: Ranching Together's economy code moved.");
            return false;
        }

        _marketUi = globals.GetProperty("MarketUIInstance", SR2MPBridge.Any);
        _econUpdate = typeof(MarketUI).GetMethod("EconUpdate", Type.EmptyTypes);

        harmony.Patch(prices, postfix: new HarmonyMethod(
            typeof(MarketPriceSync).GetMethod(nameof(AfterRead), BindingFlags.NonPublic | BindingFlags.Static)));

        harmony.Patch(handler, prefix: new HarmonyMethod(
            typeof(MarketPriceSync).GetMethod(nameof(BeforeApply), BindingFlags.NonPublic | BindingFlags.Static)));

        Main.Log.Msg("Market prices are matched by plort instead of by position.");
        return true;
    }

    /// <summary>
    /// The priced plorts, in the one order both machines can agree on.
    ///
    /// Sorting by reference id makes the sequence depend on which plorts exist and on nothing else —
    /// not on the order a hash table happens to store them in, which is what differs between two
    /// players.
    /// </summary>
    private static List<PlortEconomyDirector.CurrValueEntry> Ordered()
    {
        PlortEconomyDirector economy = SRSingleton<SceneContext>.Instance?.PlortEconomyDirector;
        if (economy?._currValueMap == null) return null;

        List<(string Id, PlortEconomyDirector.CurrValueEntry Entry)> priced = new();

        Il2CppSystem.Collections.Generic.Dictionary<IdentifiableType, PlortEconomyDirector.CurrValueEntry>
            .Enumerator entries = economy._currValueMap.GetEnumerator();

        while (entries.MoveNext())
        {
            var current = entries.Current;
            if (current.Key == null || current.Value == null) continue;

            priced.Add((current.Key.ReferenceId ?? current.Key.name, current.Value));
        }

        priced.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));

        List<PlortEconomyDirector.CurrValueEntry> ordered = new(priced.Count);
        foreach (var entry in priced) ordered.Add(entry.Entry);
        return ordered;
    }

    /// <summary>Repacks what the host is about to send, in the shared order.</summary>
    private static void AfterRead(ref ValueTuple<float, float>[] __result)
    {
        List<PlortEconomyDirector.CurrValueEntry> ordered = Ordered();
        if (ordered == null || ordered.Count == 0) return;

        ValueTuple<float, float>[] prices = new ValueTuple<float, float>[ordered.Count];
        for (int i = 0; i < ordered.Count; i++)
            prices[i] = new ValueTuple<float, float>(ordered[i].CurrValue, ordered[i].PrevValue);

        __result = prices;
    }

    /// <summary>Applies an update in the shared order, in place of the positional one.</summary>
    private static bool BeforeApply(object packet)
    {
        try
        {
            ValueTuple<float, float>[] prices = packet?.GetType()
                .GetField("Prices", SR2MPBridge.Any)?.GetValue(packet) as ValueTuple<float, float>[];

            List<PlortEconomyDirector.CurrValueEntry> ordered = Ordered();
            if (prices == null || ordered == null) return true;

            if (prices.Length != ordered.Count)
            {
                Main.Log.Warning($"A market update priced {prices.Length} plorts and this game knows " +
                                 $"{ordered.Count}: the other player is not running the same mods, or not " +
                                 "running this patcher. Prices left alone rather than scrambled.");
                return false;
            }

            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].CurrValue = prices[i].Item1;
                ordered[i].PrevValue = prices[i].Item2;
            }

            Refresh();
            return false;
        }
        catch (Exception e)
        {
            Main.Log.Warning($"Market update left to Ranching Together: {e.Message}");
            return true;
        }
    }

    /// <summary>Redraws the board, the way the handler being replaced did.</summary>
    private static void Refresh()
    {
        try
        {
            object ui = _marketUi?.GetValue(null);
            if (ui != null) _econUpdate?.Invoke(ui, null);
        }
        catch
        {
            // The board simply redraws itself the next time it is opened.
        }
    }
}
