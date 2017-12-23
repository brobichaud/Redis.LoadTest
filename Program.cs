using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RedisCacheLoad
{
	class Program
	{
		static void Main(string[] args)
		{
			int threadCount = 5;
			int cacheTimeoutSec = 5;

			if (args.Length >= 1) threadCount = Convert.ToInt16(args[0]);
			if (args.Length >= 2) cacheTimeoutSec = Convert.ToInt16(args[1]);

			Console.WriteLine("Thread Count: {0}, Cache Timeout Seconds: {1}", threadCount, cacheTimeoutSec);

			// cache something, for a limited time
			var user = new SdkUser()
			{
				Name = "user@digimarc.com",
				Enabled = true,
				Id = 23,
				SecurityKey = "sss",
				Expires = new DateTime(2014,10,31),
			};
			RedisCache.Instance.Set("TestResolverKey", user, TimeSpan.FromSeconds(cacheTimeoutSec));

			var tasks = new List<Task>();
			for (int loop = 1; loop <= threadCount; loop++)
			{
				int loop1 = loop;
				tasks.Add(Task.Run(async () => { await BlastRedisCache(loop1); }));
			}

			Task.WaitAll(tasks.ToArray());
			Logging.WriteLogData("LoadCache.csv", threadCount);
		}

		private static async Task BlastRedisCache(int instance)
		{
			var logData = new List<Logging.LogData>();
			int loop = 1;

			while (true)
			{
				var sw = Stopwatch.StartNew();
				var user = await RedisCache.Instance.GetAsync<SdkUser>("TestResolverKey");
				sw.Stop();

				if (user == null) break;

				var data = new Logging.LogData() { Thread = instance, Iteration = loop++, ElapsedMs = sw.ElapsedMilliseconds };
				logData.Add(data);
				Console.WriteLine("Thread: {0:d2}, Iteration: {1:d4}, Cached: {2}, Elapsed: {3}ms", data.Thread, data.Iteration, user.Name, data.ElapsedMs);
			}

			Logging.AddLogData(logData);
			Console.WriteLine("Thread: {0:d2} exiting, data no longer cached", instance);
		}
	}
}
