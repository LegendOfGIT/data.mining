using System;
using System.Collections.Generic;

namespace Data.Mining
{
    public class MiningFilter
    {
        public string Target { get; set; }
        public object Maximum { get; set; }
        public object Minimum { get; set; }
        public object Value { get; set; }

        public IEnumerable<MiningFilter> And { get; set; }
        public IEnumerable<MiningFilter> Or { get; set; }

        public MiningFilter Parent { get; set; }
    }
}
