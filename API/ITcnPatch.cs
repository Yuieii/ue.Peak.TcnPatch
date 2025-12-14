// Copyright (c) 2025 Yuieii.

using System;

namespace ue.Peak.TcnPatch.API
{
    public interface ITcnPatch
    {
        /// <summary>
        /// Register the localization key with its original, unlocalized text.
        /// </summary>
        /// <remarks>
        /// Note that this does not register the localization entry with translations.
        /// It only allows us to localize the previously non-localizable text.
        /// </remarks>
        /// <param name="key"></param>
        /// <param name="unlocalized"></param>
        void RegisterLocalizationKey(
            string key,
            string unlocalized
        );
    }
}