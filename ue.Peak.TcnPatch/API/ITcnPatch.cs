// Copyright (c) 2025 Yuieii.

using System;

namespace ue.Peak.TcnPatch.API
{
    public interface ITcnPatch
    {
        void RegisterLocalizationKey(
            string key,
            string unlocalized
        );
    }
}