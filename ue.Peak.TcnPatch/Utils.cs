// Copyright (c) 2025 Yuieii.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ue.Peak.TcnPatch.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ue.Peak.TcnPatch
{
    public static class Utils
    {
        public static Option<T> ToOptionUnity<T>(this T obj) where T: Object
        {
            return obj == null 
                ? Option.None 
                : Option.Some(obj);
        }

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

    public enum ReturnFlow
    {
        /// <summary>
        /// The handler should continue the further operations.
        /// </summary>
        Continue,
        
        /// <summary>
        /// The handler should immediately return, not performing all further unnecessary operations.
        /// </summary>
        Break,
    }
}