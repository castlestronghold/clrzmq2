namespace ZMQ.Counters
{
	using System.Linq;
	using System.Collections.Generic;
	using System.Diagnostics;

	public class PerfCounterRegistry
	{
		public const string CategoryName = "0MQ";

		private static readonly Dictionary<string, PerformanceCounterType> registry = new Dictionary<string, PerformanceCounterType>();
		private static readonly Dictionary<string, PerformanceCounter> counters = new Dictionary<string, PerformanceCounter>();

		public static void Init()
		{
			RegisterAll();
		}

		public static PerformanceCounter Get(string counter)
		{
			return Get(counter, GetInstanceName());
		}

		public static PerformanceCounter Get(string counter, string instance)
		{
			var key = counter + " - " + instance;

			lock (counters)
				if (!counters.ContainsKey(key))
				{
					RegisterAll();

					var performanceCounter = new PerformanceCounter
					{
						CategoryName = CategoryName,
						CounterName = counter,
						InstanceName = instance,
						ReadOnly = false,
						InstanceLifetime = PerformanceCounterInstanceLifetime.Process
					};

					performanceCounter.RawValue = 0;

					counters.Add(key, performanceCounter);
				}

			return counters[key];
		}

		private static void RegisterAll()
		{
			if (registry.Count == 0)
			{
				registry.Add(PerfCounters.NumberOfRequestsReceived, PerformanceCounterType.RateOfCountsPerSecond32);
				registry.Add(PerfCounters.NumberOfResponseReceived, PerformanceCounterType.RateOfCountsPerSecond32);
				registry.Add(PerfCounters.NumberOfResponseSent, PerformanceCounterType.RateOfCountsPerSecond32);
				registry.Add(PerfCounters.NumberOfRequestsSent, PerformanceCounterType.RateOfCountsPerSecond32);
				registry.Add(PerfCounters.AverageReplyTime, PerformanceCounterType.AverageTimer32);
				registry.Add(PerfCounters.BaseReplyTime, PerformanceCounterType.AverageBase);
				registry.Add(PerfCounters.AverageRequestTime, PerformanceCounterType.AverageTimer32);
				registry.Add(PerfCounters.BaseRequestTime, PerformanceCounterType.AverageBase);
				registry.Add(PerfCounters.NumberOfCallForwardedToBackend, PerformanceCounterType.RateOfCountsPerSecond32);
				registry.Add(PerfCounters.NumberOfCallForwardedToFrontend, PerformanceCounterType.RateOfCountsPerSecond32);
			}

			Synchronize();
		}

		private static void Synchronize()
		{
			if (!PerformanceCounterCategory.Exists(CategoryName))
			{
				CreatePerfCounters();
			}
			else
			{
				var category = PerformanceCounterCategory.GetCategories().First(c => c.CategoryName == CategoryName);

				if (!registry.Keys.Any(category.CounterExists))
				{
					PerformanceCounterCategory.Delete(CategoryName);

					CreatePerfCounters();
				}
			}
		}

		private static void CreatePerfCounters()
		{
			var toCreate = new CounterCreationDataCollection();

			foreach (var entry in registry)
			{
				var counter = new CounterCreationData { CounterType = entry.Value, CounterName = entry.Key };

				toCreate.Add(counter);
			}

			PerformanceCounterCategory.Create(CategoryName, "0MQ Performance Counters", PerformanceCounterCategoryType.MultiInstance, toCreate);
		}

		private static string GetInstanceName()
		{
			var port = System.Configuration.ConfigurationManager.AppSettings.Get("listening_port")
					   ?? System.Configuration.ConfigurationManager.AppSettings.Get("clear:app")
					   ?? " - ";

			return Process.GetCurrentProcess().ProcessName + ":" + port;
		}
	}

	public static class PerfCounters
	{
		//Facility
		public const string NumberOfRequestsReceived = "# of Requests Received / sec";
		public const string NumberOfResponseSent = "# of Response Sent / sec";

		public const string NumberOfRequestsSent = "# of Response Received / sec";
		public const string NumberOfResponseReceived = "# of Requests Sent / sec";

		public const string AverageReplyTime = "Average Reply Time";
		public const string AverageRequestTime = "Average Request Time";

		public const string NumberOfCallForwardedToFrontend = "# of Forwarded To Frontend / sec";
		public const string NumberOfCallForwardedToBackend = "# of Forwarded To Backend / sec";

		public const string BaseReplyTime = "Base Average Reply Time";
		public const string BaseRequestTime = "Base Average Request Time";
	}
}
