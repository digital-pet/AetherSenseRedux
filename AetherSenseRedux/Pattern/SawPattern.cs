﻿using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AetherSenseRedux.Pattern
{
    internal class SawPattern : IPattern
    {
        public DateTime Expires { get; set; }
        private readonly double startLevel;
        private readonly double endLevel;
        private readonly long duration;
        private readonly long duration1;


        public SawPattern(SawPatternConfig config)
        {
            startLevel = config.Start;
            endLevel = config.End;
            this.duration = config.Duration;
            this.duration1 = config.Duration1;
            Expires = DateTime.UtcNow + TimeSpan.FromMilliseconds(duration);
        }

        public double GetIntensityAtTime(DateTime time)
        {
            if (Expires < time)
            {
                throw new PatternExpiredException();
            }
            double progress = 1.0 - ((Expires.Ticks - time.Ticks) / ((double)duration*10000));
            return (endLevel - startLevel) * progress + startLevel;
        }

        public static PatternConfig GetDefaultConfiguration()
        {
            return new RampPatternConfig();
        }
    }
    [Serializable]
    public class SawPatternConfig : PatternConfig
    {
        public override string Type { get; } = "Saw";
        public double Start { get; set; } = 0;
        public double End { get; set; } = 1;
        public long Duration1 { get; set; } = 500;
    }
}