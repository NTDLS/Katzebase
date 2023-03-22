using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Health
{
    public class HealthManager
    {
        public List<HealthCounter> Counters;

        private Core core;
        private DateTime lastCheckpoint = DateTime.MinValue;

        public HealthManager(Core core)
        {
            this.core = core;

            string healthCounterDiskPath = Path.Combine(core.settings.LogDirectory, Constants.HealthStatsFile);
            if (File.Exists(healthCounterDiskPath))
            {
                var result = core.IO.GetJsonNonTracked<List<HealthCounter>>(healthCounterDiskPath);

                if (result == null)
                    throw new Exception("GetJsonNonTracked cannot be null.");

                Counters = result;
            }
            else
            {
                Counters = new List<HealthCounter>();
            }
        }

        public void Close()
        {
            Checkpoint();
        }

        public void Checkpoint()
        {
            lock (Counters)
            {
                lastCheckpoint = DateTime.UtcNow;
                Counters = Counters.Where(o => o.Value > 0).ToList();
                core.IO.PutJsonNonTracked(Path.Combine(core.settings.LogDirectory, Constants.HealthStatsFile), Counters);
            }
        }

        /// <summary>
        /// Increment the specified counter by a defined amount.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public void Increment(HealthCounterType type, double value)
        {
            if (value == 0)
            {
                return;
            }

            lock (Counters)
            {
                var counter = (from o in Counters where o.Type == type select o).FirstOrDefault();
                if (counter != null)
                {
                    counter.Value += value;
                }
                else
                {
                    Counters.Add(new HealthCounter()
                    {
                        Type = type,
                        Value = value
                    });
                }

                if ((DateTime.UtcNow - lastCheckpoint).TotalSeconds > 600)
                {
                    Checkpoint();
                }
            }
        }

        /// <summary>
        /// Increment the specified counter by a defined amount.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public void Increment(HealthCounterType type, string instance, double value)
        {
            if (value == 0 || core.settings.RecordInstanceHealth == false)
            {
                return;
            }

            lock (Counters)
            {
                var counter = (from o in Counters where o.Type == type && o.Instance == instance select o).FirstOrDefault();
                if (counter != null)
                {
                    counter.Value += value;
                }
                else
                {
                    Counters.Add(new HealthCounter()
                    {
                        Type = type,
                        Value = value,
                        Instance = instance
                    });
                }

                if ((DateTime.UtcNow - lastCheckpoint).TotalSeconds > 600)
                {
                    Checkpoint();
                }
            }
        }

        /// <summary>
        /// Increment the specified counter by 1.
        /// </summary>
        /// <param name="type"></param>
        public void Increment(HealthCounterType type)
        {
            Increment(type, 1);
        }

        /// <summary>
        /// Increment the specified counter by 1.
        /// </summary>
        /// <param name="type"></param>
        public void Increment(HealthCounterType type, string instance)
        {
            Increment(type, instance, 1);
        }

        /// <summary>
        /// Set the specified counter to the defined value.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public void Set(HealthCounterType type, Int64 value)
        {
            lock (Counters)
            {
                var counter = (from o in Counters where o.Type == type select o).FirstOrDefault();
                if (counter != null)
                {
                    counter.Value = value;
                }
                else
                {
                    Counters.Add(new HealthCounter()
                    {
                        Type = type,
                        Value = value
                    });
                }

                if ((DateTime.UtcNow - lastCheckpoint).TotalSeconds > 600)
                {
                    Checkpoint();
                }
            }
        }

        /// <summary>
        /// Set the specified counter to the defined value.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public void Set(HealthCounterType type, string instance, double value)
        {
            lock (Counters)
            {
                var counter = (from o in Counters where o.Type == type && o.Instance == instance select o).FirstOrDefault();
                if (counter != null)
                {
                    counter.Value = value;
                }
                else
                {
                    Counters.Add(new HealthCounter()
                    {
                        Type = type,
                        Value = value,
                        Instance = instance
                    });
                }

                if ((DateTime.UtcNow - lastCheckpoint).TotalSeconds > 600)
                {
                    Checkpoint();
                }
            }
        }
    }
}
