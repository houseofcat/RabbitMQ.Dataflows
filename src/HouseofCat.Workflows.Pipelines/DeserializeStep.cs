using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HouseofCat.Workflows.Pipelines
{
    public class DeserializeStep<TIn, TOut>
    {
        private readonly Func<TIn, Task<TOut>> _deserializeFunction;

        public DeserializeStep(Func<TIn, Task<TOut>> deserializeFunction)
        {
            _deserializeFunction = deserializeFunction;
        }
    }
}
