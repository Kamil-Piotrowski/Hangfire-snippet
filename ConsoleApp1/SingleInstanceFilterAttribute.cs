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
using Newtonsoft.Json;

namespace ConsoleApp1
{
    class SingleInstanceFilterAttribute : JobFilterAttribute, IElectStateFilter, IApplyStateFilter//, IServerFilter
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        #region pre
        public void OnStateElection(ElectStateContext context)
        {
            if (context.CandidateState is ProcessingState)
            {
                bool IsAdded = BlockQueue(context.BackgroundJob.Job.Method.Name, context);

            };
        }

        private bool BlockQueue(string key, ElectStateContext context)
        {
            using (context.Connection.AcquireDistributedLock(key, TimeSpan.FromMinutes(1)))
            {
                bool IsJobAdded = false;
                if (IsQueueEmpty(key, context))
                {
                    IsJobAdded = AddJobId(key, context);
                    setMeAsLastJob(key, context);
                    incrementJobCount(key, context);
                }
                else if (!IsJobEnqueued(key, context))
                {
                    IsJobAdded = AddJobId(key, context);
                    context.CandidateState = new AwaitingState(GetLastJobId(key, context));
                    setParentReferenceToMe(key, context);
                    setMyReferenceToParent(key, context);
                    setMeAsLastJob(key, context);
                    incrementJobCount(key, context);
                }
                else
                {

                    var referencesraw = context.Connection.GetAllItemsFromSet(key + ":" + context.BackgroundJob.Id)
                        ?.FirstOrDefault();
                        var references = referencesraw.Split(':');
                    if (references.Length > 0)
                    {
                        bool IsParentEnqueued = context.Connection.GetAllItemsFromSet(key + ":" + references[0])
                                                    ?.FirstOrDefault() != null;
                        if (IsParentEnqueued)
                        {
                            context.CandidateState = new AwaitingState(references[0]);
                        }
                        
                    }
                   

                }
                return IsJobAdded;
            }
        }

        


        #endregion

        #region post

        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {

            if (context.BackgroundJob.Job == null) return;

            if (context.NewState is SucceededState || context.NewState is FailedState || context.NewState is DeletedState)
            {
                bool IsRemoved = UnblockQueue(context, context.BackgroundJob.Job.Method.Name);
            }


        }

        private bool UnblockQueue(ApplyStateContext context, string key)
        {
            using (context.Connection.AcquireDistributedLock(key, TimeSpan.FromMinutes(1)))
            {
                
                if (context.NewState is DeletedState)
                {
                    UpdateParentNextJobReference(key, context);
                    UpdateContinuationPreviusJobReference(key, context);
                }
                var isRemoved = RemoveJobId(key, context);

                decrementJobCount(key, context);
                Cleanup(context, key);
                return isRemoved;
            }
        }

        private void Cleanup(ApplyStateContext context, string key)
        {
            if (IsQueueEmpty(key, context) && GetLastJobId(key, context) == context.BackgroundJob.Id)
            {
                var localTransaction = context.Connection.CreateWriteTransaction();
                localTransaction.RemoveFromSet(key + ":count", "0");
                localTransaction.RemoveFromSet(key + ":last", context.BackgroundJob.Id);
                localTransaction.Commit();
            }
        }

        #endregion


        #region not used
        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {

        }
        #endregion



        #region helpers

        private bool AddJobId(string key, ElectStateContext context)
        {
            var localTransaction = context.Connection.CreateWriteTransaction();
            localTransaction.AddToSet(key+":"+ context.BackgroundJob.Id, ":");
            localTransaction.Commit();
            return false;
        }
        private bool RemoveJobId(string key, ApplyStateContext context)
        {
            try
            {
                var localTransaction = context.Connection.CreateWriteTransaction();
                string rawref = GetThisJobReferences(key, context);
                localTransaction.RemoveFromSet(key + ":" + context.BackgroundJob.Id, rawref);
                localTransaction.Commit();
                
            }
            catch (Exception e)
            {
                Logger.Info("wyjebongo");
            }
            return false;
        }

        private string GetLastJobId(string key, ElectStateContext context)
        {
            return context.Connection.GetAllItemsFromSet(key + ":last")?.FirstOrDefault();
        }
        private string GetLastJobId(string key, ApplyStateContext context)
        {
            return context.Connection.GetAllItemsFromSet(key + ":last")?.FirstOrDefault();
        }
        private string GetThisJobReferences(string key, ElectStateContext context)
        {
            return context.Connection.GetAllItemsFromSet(key + ":" + context.BackgroundJob.Id)?.FirstOrDefault();
        }
        private string GetThisJobReferences(string key, ApplyStateContext context)
        {
            return context.Connection.GetAllItemsFromSet(key + ":" + context.BackgroundJob.Id)?.FirstOrDefault();
        }

        private bool IsJobEnqueued(string key, ElectStateContext context)
        {
            var test = context.Connection.GetAllItemsFromSet(key + ":" + context.BackgroundJob.Id).FirstOrDefault();
            return  !string.IsNullOrEmpty(test) ;
        }

        private bool IsQueueEmpty(string key, ElectStateContext context)
        {
            try
            {
                return int.Parse(context.Connection.GetAllItemsFromSet(key + ":count")?.FirstOrDefault()) == 0;
            }
            catch (Exception)
            {
                var localTransaction = context.Connection.CreateWriteTransaction();
                localTransaction.AddToSet(key + ":count", "0");
                localTransaction.Commit();
                return true;
            }
        }
        private bool IsQueueEmpty(string key, ApplyStateContext context)
        {
            try
            {
                return int.Parse(context.Connection.GetAllItemsFromSet(key + ":count")?.FirstOrDefault()) == 0;
            }
            catch (Exception)
            {
                var localTransaction = context.Connection.CreateWriteTransaction();
                localTransaction.AddToSet(key + ":count", "0");
                localTransaction.Commit();
                return true;
            }
        }

        private void incrementJobCount(string key, ElectStateContext context)
        {
            string raw = context.Connection.GetAllItemsFromSet(key + ":count")?.FirstOrDefault();
            int jobCount = 0;
            var localTransaction = context.Connection.CreateWriteTransaction();
            if (raw != null)
            {
                jobCount = int.Parse(raw);
                localTransaction.RemoveFromSet(key + ":count", raw);
            }
            jobCount += 1;
            localTransaction.AddToSet(key + ":count", jobCount.ToString());
            localTransaction.Commit();
        }

        private void setMeAsLastJob(string key, ElectStateContext context)
        {
            string raw = context.Connection.GetAllItemsFromSet(key + ":last")?.FirstOrDefault();
            var localTransaction = context.Connection.CreateWriteTransaction();
            if (raw != null)
            {
                localTransaction.RemoveFromSet(key + ":last", raw);
            }
            localTransaction.AddToSet(key + ":last", context.BackgroundJob.Id);
            localTransaction.Commit();
        }

        private void setMyReferenceToParent(string key, ElectStateContext context)
        {
            string raw = context.Connection.GetAllItemsFromSet(key + ":"+context.BackgroundJob.Id)?.FirstOrDefault();
            if (raw == null) return;
            var localTransaction = context.Connection.CreateWriteTransaction();
            localTransaction.RemoveFromSet(key + ":" + context.BackgroundJob.Id, raw);
            var references = raw.Split(':');
            localTransaction.AddToSet(key + ":" + context.BackgroundJob.Id, GetLastJobId(key, context) + ":" + references[1]);
            localTransaction.Commit();
        }

        private void setParentReferenceToMe(string key, ElectStateContext context)
        {
            string parentId = context.Connection.GetAllItemsFromSet(key + ":last")?.FirstOrDefault();
            if (parentId == null) return;
            var localTransaction = context.Connection.CreateWriteTransaction();


            string parentRaw = context.Connection.GetAllItemsFromSet(key + ":"+ parentId)?.FirstOrDefault();
            var parentReferences = parentRaw.Split(':');

            localTransaction.RemoveFromSet(key + ":" + parentId, parentRaw);
            localTransaction.AddToSet(key + ":" + parentId, parentReferences[0] + ":" + context.BackgroundJob.Id);
            localTransaction.Commit();
        }
        private void UpdateContinuationPreviusJobReference(string key, ApplyStateContext context)
        {
            string rawMe = context.Connection.GetAllItemsFromSet(key + ":" + context.BackgroundJob.Id)?.FirstOrDefault();
            if (rawMe == null) return;
            var myReferences = rawMe.Split(':');

            string rawContinuation = context.Connection.GetAllItemsFromSet(key + ":" + myReferences[1])?.FirstOrDefault();
            if (rawContinuation == null) return;
            var continuationReferences = rawContinuation.Split(':');

            var localTransaction = context.Connection.CreateWriteTransaction();
            localTransaction.RemoveFromSet(key + ":" + myReferences[1], rawContinuation);
            localTransaction.AddToSet(key + ":" + myReferences[1], $"{myReferences[0]}:{continuationReferences[1]}");
            localTransaction.Commit();
        }

        private void UpdateParentNextJobReference(string key, ApplyStateContext context)
        {
            string rawMe = context.Connection.GetAllItemsFromSet(key + ":" + context.BackgroundJob.Id)?.FirstOrDefault();
            if (rawMe == null) return;
            var myReferences = rawMe.Split(':');

            string rawParent = context.Connection.GetAllItemsFromSet(key + ":" + myReferences[0])?.FirstOrDefault();
            if (rawParent == null) return;
            var parentReferences = rawParent.Split(':');

            var localTransaction = context.Connection.CreateWriteTransaction();
            localTransaction.RemoveFromSet(key + ":" + myReferences[0], rawParent);
            localTransaction.AddToSet(key + ":" + myReferences[0], $"{parentReferences[0]}:{myReferences[1]}");
            localTransaction.Commit();
        }

        private void decrementJobCount(string key, ApplyStateContext context)
        {
            string raw = context.Connection.GetAllItemsFromSet(key + ":count")?.FirstOrDefault();
            int jobCount = 0;
            var localTransaction = context.Connection.CreateWriteTransaction();
            if (raw != null)
            {
                jobCount = int.Parse(raw);
                localTransaction.RemoveFromSet(key + ":count", raw);
            }
            jobCount -= 1;
            localTransaction.AddToSet(key + ":count", jobCount.ToString());
            localTransaction.Commit();
        }

        #endregion


    }
}
