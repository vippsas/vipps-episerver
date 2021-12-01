using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Vipps.Services;
using Xunit;

namespace Vipps.Test.Tests
{
    public class SynchronizerTests
    {
        [Fact]
        public async Task AwaitSynchronizer_ShouldNotThrowException()
        {
            var orderIds = new [] { "PO1", "PO2", "PO3" };
            var randomizer = new Random(123);
            var operations = 10000;
            var synchronizer = new DefaultVippsOrderSynchronizer();
            var maxDop = 100;

            var parallelGroups = Enumerable.Range(0, operations)
                                           .GroupBy(r => r % maxDop);

            var parallelTasks = parallelGroups.Select(groups =>
            {
                return Task.Run(() =>
                {
                    var randomOrderId = orderIds[randomizer.Next(0, orderIds.Length)];
                    var readLock = synchronizer.Get(randomOrderId);

                    readLock.Wait();

                    Assert.NotNull(readLock);

                    var result = PerformWork();
                    var released = synchronizer.TryRelease(randomOrderId);
                });
            });

            await Task.WhenAll(parallelTasks);

            Assert.Equal(0, synchronizer.Requests);
        }

        public Guid PerformWork()
        {
            var id = $"{Guid.NewGuid()}";
            var bytes = Encoding.UTF8.GetBytes(id);

            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(bytes);
                return new Guid(hash);
            }
        }
    }
}
