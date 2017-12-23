using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace RedisCacheLoad
{
	public static class RedisCache
	{
		internal static readonly Lazy<ConnectionMultiplexer> _cacheService =
			new Lazy<ConnectionMultiplexer>(() =>
			{
				var options = new ConfigurationOptions();
				options.EndPoints.Add("devmadrasgb.redis.cache.windows.net", 6380);
				options.Password = "DdHaG0cSwNpd1v5w+pCZd14YYGMSVmpQHjtP4/lzHlY=";
				options.Ssl = true;
				//options.AllowAdmin = true;  // needed for FLUSHDB command

				// experimental
				//options.KeepAlive = 30;
				options.ConnectTimeout = 15000;
				options.SyncTimeout = 15000;

				return ConnectionMultiplexer.Connect(options);
			});

		internal static IDatabase Instance
		{
			get { return _cacheService.Value.GetDatabase(); }
		}

		public static T Get<T>(this IDatabase cache, string key)
		{
			return Deserialize<T>(cache.StringGet(key));
		}

		public static async Task<T> GetAsync<T>(this IDatabase cache, string key)
		{
			return Deserialize<T>(await cache.StringGetAsync(key));
		}

		public static object Get(this IDatabase cache, string key)
		{
			return Deserialize<object>(cache.StringGet(key));
		}

		public static void Set(this IDatabase cache, string key, object value, TimeSpan? expires)
		{
			cache.StringSet(key, Serialize(value), expires);
		}

		static byte[] Serialize(object obj)
		{
			if (obj == null) return null;

			var formatter = new BinaryFormatter();
			using (var stream = new MemoryStream())
			{
				formatter.Serialize(stream, obj);
				return stream.ToArray();
			}
		}

		static T Deserialize<T>(byte[] bytes)
		{
			if (bytes == null) return default(T);

			var formatter = new BinaryFormatter();
			using (var stream = new MemoryStream(bytes))
			{
				return (T)formatter.Deserialize(stream);
			}
		}
	}
}
