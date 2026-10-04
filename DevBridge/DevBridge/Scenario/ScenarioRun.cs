using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using DevBridge.Server;

namespace DevBridge.Scenario
{
    /// <summary>One run of a scenario: setup, steps and cleanup in order; a failure or the end of its time skips to cleanup.</summary>
    internal sealed class ScenarioRun
    {
        /// <summary>Cleanup keeps the last part of the time, so a run whose steps overrun still tidies up before it answers.</summary>
        private const double CleanupShare = 0.2;
        private static readonly TimeSpan CleanupMost = TimeSpan.FromSeconds(15);

        private readonly ScenarioPlan plan;
        private readonly TimeSpan budget;
        private readonly RunContext context;
        private readonly ScenarioReport report;
        private string stopped;

        internal ScenarioRun(ScenarioPlan plan, TimeSpan budget, Dispatcher dispatch)
        {
            this.plan = plan;
            this.budget = budget;
            context = new RunContext(dispatch);
            report = new ScenarioReport(plan.Name);
        }

        internal Dictionary<string, object> Execute()
        {
            DateTime end = DateTime.UtcNow + budget;
            report.Started(plan.Count, budget.TotalSeconds);
            context.Deadline = end - Reserve();
            RunPhase(plan.Setup, false);
            RunPhase(plan.Steps, false);
            context.Deadline = end;
            RunPhase(plan.Cleanup, true);
            return report.Finish(stopped, context.Saved);
        }

        private TimeSpan Reserve()
        {
            if (plan.Cleanup.Count == 0) return TimeSpan.Zero;
            var share = TimeSpan.FromTicks((long)(budget.Ticks * CleanupShare));
            return share < CleanupMost ? share : CleanupMost;
        }

        /// <summary>Cleanup runs every step whatever failed before; only shutting down or running out of time stops it.</summary>
        private void RunPhase(List<PlanStep> steps, bool cleanup)
        {
            foreach (PlanStep planned in steps)
            {
                string halt = context.Halt;
                if (halt != null || (!cleanup && stopped != null))
                {
                    stopped = stopped ?? halt;
                    report.Add(new StepResult { Phase = planned.Phase, Number = planned.Number, Name = planned.Label, Skipped = true });
                    continue;
                }
                StepResult result = RunOne(planned);
                report.Add(result);
                if (!result.Ok && !cleanup && Stops(planned)) stopped = stopped ?? result.Where + " failed";
            }
        }

        /// <summary>A failed step stops the run unless it is optional or continue_on_fail (its own, else the scenario's) says go on.</summary>
        private bool Stops(PlanStep planned) => !planned.Optional && !(planned.ContinueOnFail ?? plan.ContinueOnFail);

        private StepResult RunOne(PlanStep planned)
        {
            var result = new StepResult { Phase = planned.Phase, Number = planned.Number, Name = planned.Label, Optional = planned.Optional };
            Stopwatch clock = Stopwatch.StartNew();
            context.StepStarts();
            try { Judge(result, StepParser.Parse(Substitution.Apply(planned.Raw, context.Saved))); }
            catch (Exception error) { result.Reason = Why(error); }
            result.Ms = clock.ElapsedMilliseconds;
            return result;
        }

        /// <summary>A step's value is saved even when a check on it fails, so the report and later steps can show it.</summary>
        private void Judge(StepResult result, Step step)
        {
            result.Name = step.Label;
            StepOutcome outcome = StepRunner.Run(step, context);
            StepValue value = outcome.Failure == null ? StepValue.Of(outcome, step.Json) : new StepValue();
            result.Got = value.Text ?? outcome.Text;
            result.Reason = outcome.Failure ?? value.Problem ?? Missing(step, value) ?? step.Expect.Check(value.Text, value.Count);
            if (value.Text != null && step.Save != null) context.Saved[step.Save] = value.Text;
            result.Ok = result.Reason == null;
        }

        private static string Missing(Step step, StepValue value) =>
            value.Text == null && (step.Expect.OnValue || step.Save != null) ? $"json path {step.Json} picked nothing" : null;

        private static string Why(Exception error)
        {
            while ((error is AggregateException || error is TargetInvocationException) && error.InnerException != null) error = error.InnerException;
            return error is BridgeException ? error.Message : $"{error.GetType().Name}: {error.Message}";
        }
    }
}
