using Migration.Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace WorkItemImport
{
    public class ExecutionPlanBuilder
    {
        private readonly MigrationContext _context;

        public ExecutionPlanBuilder(MigrationContext context)
        {
            _context = context;
        }
        
        public ExecutionPlan BuildExecutionPlan()
        {
            var path = _context.MigrationWorkspace;

            // get the file attributes for file or directory
            FileAttributes attr = File.GetAttributes(path);
            if (attr.HasFlag(FileAttributes.Directory))
                return new ExecutionPlan(BuildExecutionPlanFromDir(), _context);
            else
                return new ExecutionPlan(BuildExecutionPlanFromFile(path), _context);
        }

        private IEnumerable<RevisionReference> BuildExecutionPlanFromDir()
        {
            var actionPlan = new List<RevisionReference>();
            foreach (var wi in _context.EnumerateAllItems())
            {
                Logger.Log(LogLevel.Debug, $"Processing {wi.OriginId}");
                foreach (var rev in wi.Revisions)
                {
                    var revRef = new RevisionReference() { OriginId = wi.OriginId, RevIndex = rev.Index, Time = rev.Time };
                    actionPlan.Add(revRef);
                }
            }
            actionPlan.Sort();

            EnsureIncreasingTimes(actionPlan);

            return actionPlan;
        }

        private void EnsureIncreasingTimes(List<RevisionReference> actionPlan)
        {
            for (int i = 1; i < actionPlan.Count; i++)
            {
                var prev = actionPlan[i - 1];
                var current = actionPlan[i];

                DateTime? nextTime = null;
                if (i + 1 < actionPlan.Count)
                {
                    var next = actionPlan[i + 1];
                    if (next.Time > prev.Time)
                        nextTime = next.Time;
                }

                if (prev.Time >= current.Time)
                    current.Time = RevisionUtility.NextValidDeltaRev(prev.Time, nextTime);
            }
        }

        private IEnumerable<RevisionReference> BuildExecutionPlanFromFile(string path)
        {
            throw new NotImplementedException();
        }
    }
}
