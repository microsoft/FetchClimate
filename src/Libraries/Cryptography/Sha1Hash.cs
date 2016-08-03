using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;

namespace Microsoft.Research.Science.FetchClimate2
{
    public static class SHA1Hash
    {
        private static TaskFactory taskFactory = new TaskFactory(new LimitedConcurrencyLevelTaskScheduler(1));

        private static SHA1CryptoServiceProvider _cryptoTransformSHA1 = new SHA1CryptoServiceProvider();        

        public static Task<long> HashAsync(byte[] bytes)
        {
            return taskFactory.StartNew(() => BitConverter.ToInt64(_cryptoTransformSHA1.ComputeHash(bytes), 0));
        }
    }
}
