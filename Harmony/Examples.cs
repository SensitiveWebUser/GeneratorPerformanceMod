using HarmonyLib;
using UnityEngine;

namespace MyTestMod.Harmony
{
    [HarmonyPatch(typeof(XUiC_MainMenu))]
    public class MyTestModPatch
    {
        public static void Postfix(XUiC_MainMenu __instance)
        {
            if (__instance.GetChildById("btnContinueGame") is XUiC_SimpleButton btnContinueGame)
            {
                btnContinueGame.Label = "Continue ASA Game";
                btnContinueGame.OnPressed += (_sender, _mouseButton) =>
                {
                    Log.Out("btnNewGame pressed");
                };
            }
        }
    }
}