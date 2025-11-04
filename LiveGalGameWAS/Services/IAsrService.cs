using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveGalGameWAS.Services
{
    interface IAsrService
    {
        public Task InitializeAsync(object config);

        public Task Start();

        public Task Stop();

        public event EventHandler<string> OnTextResult;
    }
}
