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
        public void OnStateElection(ElectStateContext context)
        {
            if (context.CandidateState is ProcessingState)
            {
                //Logger.InfoFormat($"job {context.BackgroundJob.Id} entered");
                bool IsAdded = BlockQueue(context.BackgroundJob.Job.Method.Name, context);
                //Logger.InfoFormat($"job {context.BackgroundJob.Id} added: {IsAdded}. {context.BackgroundJob.Job.Method.Name} list count: {JsonConvert.DeserializeObject<List<string>>(context.Connection.GetAllItemsFromSet(context.BackgroundJob.Job.Method.Name).FirstOrDefault()).Count}");

                //Logger.InfoFormat($"{context.BackgroundJob.Job.Method.Name} after adding: {context.Connection.GetAllItemsFromSet(context.BackgroundJob.Job.Method.Name).FirstOrDefault()}");
                

            };
        }

        private bool BlockQueue(string key, ElectStateContext context)
        {
            using (context.Connection.AcquireDistributedLock("lock", TimeSpan.FromMinutes(1)))
            {
                var alljobs = GetJobsIds(key, context);
                //ni mo
                if (alljobs.Count == 0)
                {
                    bool IsJobAdded = AddJobId(key, context);
                    //Logger.InfoFormat($"job {context.BackgroundJob.Id} block1, {IsJobAdded}");
                    return IsJobAdded;
                }
                else
                {
                    if (!alljobs.Contains(context.BackgroundJob.Id))
                    {
                        bool IsJobAdded = AddJobId(key, context);
                        context.CandidateState = new AwaitingState(alljobs.Last());
                        //Logger.InfoFormat($"job {context.BackgroundJob.Id} block2, {IsJobAdded}");
                        return IsJobAdded;
                    }
                    //Logger.InfoFormat($"job {context.BackgroundJob.Id} block3, {false}");
                    return false;
                }
            }
        }

        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            
            if (context.BackgroundJob.Job == null) return;

            if (context.NewState is SucceededState || context.NewState is FailedState || context.NewState is DeletedState)
            {
                bool IsRemoved = UnblockQueue(context, context.BackgroundJob.Job.Method.Name);
            }
            
             
            //Logger.InfoFormat($"job {context.BackgroundJob.Id} removed: {true}. {context.BackgroundJob.Job.Method.Name} list count: {JsonConvert.DeserializeObject<List<string>>(context.Connection.GetAllItemsFromSet(context.BackgroundJob.Job.Method.Name).FirstOrDefault()).Count}");
        }

        private bool UnblockQueue(ApplyStateContext context, string key)
        {
            using (context.Connection.AcquireDistributedLock("lock", TimeSpan.FromMinutes(1)))
            {
                
                
                var isRemoved = RemoveJobId(key, context);
                //Logger.InfoFormat($"job {context.BackgroundJob.Id} block4, {isRemoved}");
                //Logger.InfoFormat($"{context.BackgroundJob.Job.Method.Name} Jobs after removing: {JsonConvert.DeserializeObject<List<string>>(context.Connection.GetAllItemsFromSet(context.BackgroundJob.Job.Method.Name).FirstOrDefault()).Count}");
                return isRemoved;
            }
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            
        }
        #region helpers

        private List<string> GetJobsIds(string key, ElectStateContext context)
        {
            var all = context.Connection.GetAllItemsFromSet(key);
            var serialized = all?.FirstOrDefault();
            if(serialized is null)
            {
                return new List<string>();
            }
            var processedIds = JsonConvert.DeserializeObject<List<string>>(serialized);
            if(processedIds is null)
            {
                return new List<string>();
            }
            
            return processedIds;

        }
        private List<string> GetJobsIds(string key, ApplyStateContext context)
        {
            var all = context.Connection.GetAllItemsFromSet(key);
            var serialized = all?.FirstOrDefault();
            if (serialized is null)
            {
                return new List<string>();
            }

            var processedIds = JsonConvert.DeserializeObject<List<string>>(serialized);
            if (processedIds is null)
            {
                return new List<string>();
            }
            
            return processedIds;

        }

        private bool AddJobId(string key, ElectStateContext context)
        {
            var jobs = GetJobsIds(key, context);
            //Logger.InfoFormat($"add: before, {JsonConvert.SerializeObject(jobs)}");
            jobs.Add(context.BackgroundJob.Id);
            Logger.InfoFormat($"add {context.BackgroundJob.Id}: after, {JsonConvert.SerializeObject(jobs)}");
            var localTransaction = context.Connection.CreateWriteTransaction();
            localTransaction.AddToSet(key, JsonConvert.SerializeObject(jobs));
            localTransaction.Commit();



            return false;// JsonConvert.DeserializeObject<List<string>>(context.Connection.GetAllItemsFromSet(key).FirstOrDefault()).Count(x=>x == context.BackgroundJob.Id) == 1;
        }
        private bool RemoveJobId(string key, ApplyStateContext context)
        {
            
            var localTransaction = context.Connection.CreateWriteTransaction();
            var jobs = GetJobsIds(key, context);

            string before = JsonConvert.SerializeObject(jobs);
            //localTransaction.RemoveFromSet(key, JsonConvert.SerializeObject(jobs));
            foreach (var item in context.Connection.GetAllItemsFromSet(key).ToList())
            {
                localTransaction.RemoveFromSet(key, item);
            }
            //jobs.Remove(context.BackgroundJob.Id);
            jobs = jobs.Where(x => x != context.BackgroundJob.Id).ToList();
            string after = JsonConvert.SerializeObject(jobs);
            localTransaction.AddToSet(key, JsonConvert.SerializeObject(jobs));
            localTransaction.Commit();
            Logger.InfoFormat($"job: {context.BackgroundJob.Id} Before: {before}, after: {after}");
            //Logger.InfoFormat($"remove {context.BackgroundJob.Id}: after, {JsonConvert.SerializeObject(GetJobsIds(key, context))}");
            return false;//JsonConvert.DeserializeObject<List<string>>(context.Connection.GetAllItemsFromSet(key).FirstOrDefault()).Count(x => x == context.BackgroundJob.Id) == 0;
        }
        #endregion


    }
}
