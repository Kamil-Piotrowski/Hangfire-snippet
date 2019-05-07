using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.States;
using Hangfire.Storage;

namespace ConsoleApp1
{
    class SingleInstanceFilterAttribute : JobFilterAttribute, IElectStateFilter, IApplyStateFilter
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        public void OnStateElection(ElectStateContext context)
        {
            if (context.CandidateState is ProcessingState)
            {
                using (context.Connection.AcquireDistributedLock("lock", TimeSpan.FromMinutes(1)))
                {
                    if (context.Connection.GetAllItemsFromSet(context.BackgroundJob.Job.Method.Name).Count > 0)
                    {
                        context.CandidateState = new EnqueuedState();
                    }
                    else
                    {
                        var localTransaction = context.Connection.CreateWriteTransaction();
                        localTransaction.AddToSet(context.BackgroundJob.Job.Method.Name, context.BackgroundJob.Id);
                        localTransaction.Commit();
                    }
                }
            }
        }
        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)

        {

            if (context.BackgroundJob.Job == null) return;



            if (context.OldStateName != ProcessingState.StateName) return;



            using (context.Connection.AcquireDistributedLock("lock", TimeSpan.FromMinutes(1)))
            {
                var localTransaction = context.Connection.CreateWriteTransaction();
                localTransaction.RemoveFromSet(context.BackgroundJob.Job.Method.Name, context.BackgroundJob.Id);
                localTransaction.Commit();

            }

        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            
        }
    }
}
