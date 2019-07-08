using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Newtonsoft.Json;

namespace ConsoleApp1
{
    class FollowupAttribute : JobFilterAttribute, IApplyStateFilter
    {
        private readonly MethodInfo _callback;
        private readonly string _parameter;
        public FollowupAttribute(string type, string method, string parameter)
        {
            _parameter = parameter;
            _callback = Type.GetType(type)?.GetMethod(method);
            
        }
        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            if (!(context.NewState is FailedState)) return;
            try
            {
                _callback.Invoke(null, new object[] { _parameter });
            }
            catch
            {
                // ignored
            }
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            
        }
    }
}
