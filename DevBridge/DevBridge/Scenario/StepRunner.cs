using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using DevBridge.Server;

namespace DevBridge.Scenario
{
    /// <summary>Carries out one step's action on the scenario thread; endpoint calls go through the router to the main thread.</summary>
    internal static class StepRunner
    {
        private static readonly TimeSpan Slice = TimeSpan.FromMilliseconds(250);

        internal static StepOutcome Run(Step step, RunContext run)
        {
            switch (step.Kind)
            {
                case StepKind.Do: return Call(step.Target, step.Args, run, false);
                case StepKind.Eval: return Call("/eval", step.Args, run, true);
                case StepKind.Wait: return Sleep(step.Seconds, run);
                case StepKind.WaitEvent: return EventCheck.Wait(step, run);
                default: return LogCheck.Run(step, run);
            }
        }

        /// <summary>The call is left behind when the run's time runs out; the router still ends it at its own timeout.</summary>
        private static StepOutcome Call(string path, Dictionary<string, string> args, RunContext run, bool eval)
        {
            Task<Reply> call = Task.Run(() => run.Dispatch(path, args));
            if (!call.Wait(Clamp(run.Left))) return StepOutcome.Fail(run.Halt ?? "out of time", "(no reply yet)");
            Reply reply = call.Result;
            if (reply.Status != 200) return StepOutcome.Fail($"{reply.Status} {Fmt.Clip(reply.Body, 300)}", reply.Body);
            if (eval) return StepOutcome.Of(StepValue.EvalValue(reply.Body));
            return StepOutcome.Of(reply.Body, reply.ContentType == "application/json");
        }

        private static StepOutcome Sleep(float seconds, RunContext run)
        {
            DateTime end = DateTime.UtcNow.AddSeconds(seconds);
            for (TimeSpan left = end - DateTime.UtcNow; left > TimeSpan.Zero; left = end - DateTime.UtcNow)
            {
                if (run.Halt != null) return StepOutcome.Fail(run.Halt);
                Thread.Sleep(left < Slice ? left : Slice);
            }
            return StepOutcome.Of($"waited {seconds.ToString(CultureInfo.InvariantCulture)} s");
        }

        internal static TimeSpan Clamp(TimeSpan span) => span < TimeSpan.Zero ? TimeSpan.Zero : span;
    }
}
