using System;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace BubbleSlimes;

/// <summary>
/// Pops the bubble slime when the player runs into it, leaving a splash of water behind.
///
/// This is what makes the slime a liability to keep: it is fragile, and a careless rancher bursts it.
/// </summary>
[RegisterTypeInIl2Cpp]
public class BubblePop : MonoBehaviour
{
    /// <summary>Radius of the water splash left where the slime popped.</summary>
    private const float SplashRadius = 1.5f;
    private const float SplashUnits = 2f;

    public BubblePop(IntPtr pointer) : base(pointer) { }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject player = SceneContext.Instance?.Player;
        if (player == null || collision.gameObject != player) return;
        Pop();
    }

    private void Pop()
    {
        LiquidDefinition water = Main.Water;
        if (water != null)
            ILiquidConsumer.ApplyLiquid(transform.position, SplashRadius, water, SplashUnits);

        Destroyer.DestroyActor(gameObject, "BubblePop.Pop");
    }
}
