using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.DataHandlers.Adapters
{
    public class BatchComputationalContextFactoryAdapter<TComputationalContext> : IComputationalContextFactory<TComputationalContext>
    {
        private readonly IBatchComputationalContextFactory<TComputationalContext> compontent;

        public BatchComputationalContextFactoryAdapter(IBatchComputationalContextFactory<TComputationalContext> component)
        {
            this.compontent = component;
        }


        public async Task<TComputationalContext> CreateAsync(IRequestContext context)
        {
            var name = context.Request.EnvironmentVariableName;
            var strachedCells = RequestToBatchAdapter.Stratch(context.Request);
            var annotatedCell = strachedCells.Select(c => new NameAnnotatedGeoCell(c,name));
            var result = await compontent.CreateAsync(annotatedCell);            
            return result;
        }
    }
}
