// Copyright (c) 2025 Yuieii.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ue.Peak.TcnPatch
{
    public static class Utils
    {
        public static Task WaitForFramesAsync(int frames)
        {
            var tcs = new TaskCompletionSource<bool>();
            Plugin.Instance.StartCoroutine(DelayFramesCoroutine(tcs, frames, () => true));
            return tcs.Task;
        }

        public static async Task WaitForNextFrameAsync() => await WaitForFramesAsync(1);

        private static IEnumerator DelayFramesCoroutine<T>(TaskCompletionSource<T> source, int frames, Func<T> resultGetter)
        {
            for (var i = 0; i < frames; i++)
            {
                yield return new WaitForEndOfFrame();
            }
        
            source.SetResult(resultGetter());
        }
    }
}