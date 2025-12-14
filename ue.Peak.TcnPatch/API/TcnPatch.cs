// Copyright (c) 2025 Yuieii.

namespace ue.Peak.TcnPatch.API
{
    public class TcnPatch : ITcnPatch
    {
        public static ITcnPatch Instance => InternalInstance;
    
        internal static TcnPatch InternalInstance { get; } = new();
    
        public void RegisterLocalizationKey(string key, string unlocalized)
        {
            Plugin.KeyToUnlocalizedLookup[key] = unlocalized;
        }
    }
}