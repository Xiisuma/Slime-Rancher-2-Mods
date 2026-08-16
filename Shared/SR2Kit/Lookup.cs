using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace SR2Kit;

/// <summary>Finding vanilla assets, the replacement for MelonSRML's <c>SRLookup</c>.</summary>
public static class Lookup
{
    /// <summary>Every identifiable type the game knows about.</summary>
    public static IEnumerable<IdentifiableType> IdentifiableTypes
    {
        get
        {
            LookupDirector director = LookupDirector.GetIfReady();
            if (director != null && director._identifiableTypeByRefId != null)
            {
                foreach (Il2CppSystem.Collections.Generic.KeyValuePair<string, IdentifiableType> pair
                         in director._identifiableTypeByRefId)
                {
                    if (pair.Value != null) yield return pair.Value;
                }
                yield break;
            }

            foreach (IdentifiableType type in Resources.FindObjectsOfTypeAll<IdentifiableType>())
                yield return type;
        }
    }

    /// <summary>
    /// First identifiable type whose reference id contains every keyword, case-insensitive.
    /// Reference ids look like <c>PlortPink</c> or <c>SlimeTwinkle</c>.
    /// </summary>
    public static IdentifiableType FindIdentifiable(params string[] keywords)
    {
        foreach (IdentifiableType type in IdentifiableTypes)
        {
            string refId = type.ReferenceId;
            if (string.IsNullOrEmpty(refId)) continue;

            refId = refId.ToLowerInvariant();
            bool matches = true;
            foreach (string keyword in keywords)
            {
                if (refId.Contains(keyword.ToLowerInvariant())) continue;
                matches = false;
                break;
            }
            if (matches) return type;
        }
        return null;
    }

    /// <summary>Slime definition of an identifiable type, or null if it is not a slime.</summary>
    public static SlimeDefinition FindSlimeDefinition(SlimeDefinitions definitions, params string[] keywords)
    {
        foreach (IdentifiableType type in IdentifiableTypes)
        {
            string refId = type.ReferenceId;
            if (string.IsNullOrEmpty(refId)) continue;

            refId = refId.ToLowerInvariant();
            bool matches = true;
            foreach (string keyword in keywords)
            {
                if (refId.Contains(keyword.ToLowerInvariant())) continue;
                matches = false;
                break;
            }
            if (!matches) continue;

            SlimeDefinition definition = definitions.GetSlimeByIdentifiableId(type);
            if (definition != null) return definition;
        }
        return null;
    }

    /// <summary>First asset of type <typeparamref name="T"/> with the given name.</summary>
    public static T FindAsset<T>(string name) where T : Object
    {
        foreach (T asset in Resources.FindObjectsOfTypeAll<T>())
        {
            if (asset.name == name) return asset;
        }
        return null;
    }
}
