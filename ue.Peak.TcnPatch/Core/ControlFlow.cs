// Copyright (c) 2025 Yuieii.

#nullable enable
using System;

namespace ue.Peak.TcnPatch.Core
{
    public static class ControlFlow
    {
        public static PartialWithBreak<T> Break<T>(T value) => new(value);
        public static PartialWithBreak<Unit> Break() => new(Unit.Instance);
        public static PartialWithContinue<T> Continue<T>(T value) => new(value);
        public static PartialWithContinue<Unit> Continue() => new(Unit.Instance);

        public class PartialWithBreak<T>(T value)
        {
            internal T Value { get; } = value;

            public ControlFlow<T, TContinue> FulfillContinueType<TContinue>()
                => ControlFlow<T, TContinue>.Break(Value);
        }

        public class PartialWithContinue<T>(T value)
        {
            internal T Value { get; } = value;

            public ControlFlow<TBreak, T> FulfillBreakType<TBreak>()
                => ControlFlow<TBreak, T>.Continue(Value);
        }
    }

    /// <summary>
    /// Used to tell an operation whether it should exit early or go on as usual.
    /// </summary>
    /// <remarks>
    /// This is used when exposing things (like graph traversals or visitors) where you want the user to be able to
    /// choose whether to exit early.
    /// </remarks>
    /// <typeparam name="TBreak">The value type of the <c>Break</c> variant.</typeparam>
    /// <typeparam name="TContinue">The value type of the <c>Continue</c> variant.</typeparam>
    public abstract class ControlFlow<TBreak, TContinue>
    {
        /// <summary>
        /// Returns <c>true</c> if this is a <c>Break</c> variant.
        /// </summary>
        public abstract bool IsBreak { get; }

        /// <summary>
        /// Returns <c>true</c> if this is a <c>Continue</c> variant.
        /// </summary>
        public abstract bool IsContinue { get; }

        /// <summary>
        /// Move on to the next phase of the operation as normal.
        /// </summary>
        public static ControlFlow<TBreak, TContinue> Continue(TContinue value)
            => new ContinueBranch(value);

        /// <summary>
        /// Exit the operation without running subsequent phases.
        /// </summary>
        public static ControlFlow<TBreak, TContinue> Break(TBreak value)
            => new BreakBranch(value);

        public static implicit operator ControlFlow<TBreak, TContinue>(ControlFlow.PartialWithContinue<TContinue> part)
            => new ContinueBranch(part.Value);

        public static implicit operator ControlFlow<TBreak, TContinue>(ControlFlow.PartialWithBreak<TBreak> part)
            => new BreakBranch(part.Value);

        /// <summary>
        /// Converts the <see cref="ControlFlow{TBreak,TContinue}">ControlFlow</see> into an <see cref="Option{T}">Option</see>
        /// which is <c>Some</c> if the <c>ControlFlow</c> was <c>Continue</c> and <c>None</c> otherwise.
        /// </summary>
        public abstract Option<TContinue> GetContinueValue();

        /// <summary>
        /// Converts the <see cref="ControlFlow{TBreak,TContinue}">ControlFlow</see> into an <see cref="Result{T,TError}">Result</see>
        /// which is <c>Success</c> if the <c>ControlFlow</c> was <c>Continue</c> and <c>Error</c> otherwise.
        /// </summary>
        public abstract Result<TContinue, TBreak> ToContinueSuccess();

        public abstract ControlFlow<TBreak, TResult> SelectContinue<TResult>(Func<TContinue, TResult> transform);

        /// <summary>
        /// Converts the <see cref="ControlFlow{TBreak,TContinue}">ControlFlow</see> into an <see cref="Option{T}">Option</see>
        /// which is <c>Some</c> if the <c>ControlFlow</c> was <c>Break</c> and <c>None</c> otherwise.
        /// </summary>
        public abstract Option<TBreak> GetBreakValue();

        /// <summary>
        /// Converts the <see cref="ControlFlow{TBreak,TContinue}">ControlFlow</see> into an <see cref="Result{T,TError}">Result</see>
        /// which is <c>Success</c> if the <c>ControlFlow</c> was <c>Break</c> and <c>Error</c> otherwise.
        /// </summary>
        public abstract Result<TBreak, TContinue> ToBreakSuccess();

        public abstract ControlFlow<TResult, TContinue> SelectBreak<TResult>(Func<TBreak, TResult> transform);

        private class BreakBranch : ControlFlow<TBreak, TContinue>
        {
            private readonly TBreak _value;

            public override bool IsBreak => true;
            public override bool IsContinue => false;

            public BreakBranch(TBreak value)
            {
                _value = value;
            }

            public override Option<TContinue> GetContinueValue()
                => Option.None;

            public override Result<TContinue, TBreak> ToContinueSuccess()
                => Result.Error(_value);

            public override ControlFlow<TBreak, TResult> SelectContinue<TResult>(Func<TContinue, TResult> transform)
                => ControlFlow.Break(_value);

            public override Option<TBreak> GetBreakValue()
                => Option.Some(_value);

            public override Result<TBreak, TContinue> ToBreakSuccess()
                => Result.Success(_value);

            public override ControlFlow<TResult, TContinue> SelectBreak<TResult>(Func<TBreak, TResult> transform)
                => ControlFlow.Break(transform(_value));
        }

        private class ContinueBranch : ControlFlow<TBreak, TContinue>
        {
            private readonly TContinue _value;

            public override bool IsBreak => false;
            public override bool IsContinue => true;

            public ContinueBranch(TContinue value)
            {
                _value = value;
            }

            public override Option<TContinue> GetContinueValue()
                => Option.Some(_value);

            public override Result<TContinue, TBreak> ToContinueSuccess()
                => Result.Success(_value);

            public override ControlFlow<TBreak, TResult> SelectContinue<TResult>(Func<TContinue, TResult> transform)
                => ControlFlow.Continue(transform(_value));

            public override Option<TBreak> GetBreakValue()
                => Option.None;

            public override Result<TBreak, TContinue> ToBreakSuccess()
                => Result.Error(_value);

            public override ControlFlow<TResult, TContinue> SelectBreak<TResult>(Func<TBreak, TResult> transform)
                => ControlFlow.Continue(_value);
        }
    }
}