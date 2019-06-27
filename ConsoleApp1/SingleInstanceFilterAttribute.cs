using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;

namespace ConsoleApp1
{
    class SingleInstanceFilterAttribute : JobFilterAttribute, IElectStateFilter, IApplyStateFilter//, IServerFilter
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        public void OnStateElection(ElectStateContext context)
        {
            if (!(context.CandidateState is ProcessingState)) return;
            if (context.CurrentState == AwaitingState.StateName)
            {
                return;
            }


            using (context.Connection.AcquireDistributedLock("lock", TimeSpan.FromMinutes(1)))
            {
                if (context.Connection.GetAllItemsFromSet("processing-"+context.BackgroundJob.Job.Method.Name).Count > 0)
                {
                    var processingJobId = context.Connection.GetAllItemsFromSet("processing-" + context.BackgroundJob.Job.Method.Name).FirstOrDefault();//currently processing
                    var lastAwaitingJobId = context.Connection.GetAllItemsFromSet("lastawaiting-"+ context.BackgroundJob.Job.Method.Name).FirstOrDefault();//last awaiting
                        
                    string precedessorId = lastAwaitingJobId is null ? processingJobId : lastAwaitingJobId;

                    context.CandidateState = new AwaitingState(precedessorId);
                    var localTransaction = context.Connection.CreateWriteTransaction();
                    if (lastAwaitingJobId != null)
                    {
                        localTransaction.RemoveFromSet("lastawaiting-"+ context.BackgroundJob.Job.Method.Name, lastAwaitingJobId);
                    }
                        
                    localTransaction.AddToSet("lastawaiting-"+ context.BackgroundJob.Job.Method.Name, context.BackgroundJob.Id);
                    localTransaction.Commit();
                    Logger.InfoFormat($"onstateelection, job {context.BackgroundJob.Id} awaiting: job {precedessorId}." );
                }
                else
                {
                    Logger.InfoFormat($"onstateelection, job {context.BackgroundJob.Id} entering processing.");
                    var localTransaction = context.Connection.CreateWriteTransaction();
                    localTransaction.AddToSet("processing-" + context.BackgroundJob.Job.Method.Name, context.BackgroundJob.Id);
                    localTransaction.Commit();
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
                localTransaction.RemoveFromSet("processing-" + context.BackgroundJob.Job.Method.Name, context.BackgroundJob.Id);
                var lastAwaitingJobId = context.Connection.GetAllItemsFromSet("lastawaiting-" + context.BackgroundJob.Job.Method.Name).FirstOrDefault();
                if (lastAwaitingJobId == context.BackgroundJob.Id)
                {
                    localTransaction.RemoveFromSet("lastawaiting-" + context.BackgroundJob.Job.Method.Name, context.BackgroundJob.Id);
                    
                }
                localTransaction.Commit();

            }

        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            
        }


    }
}
