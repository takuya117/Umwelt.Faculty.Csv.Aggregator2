using System;

namespace Umwelt.Faculty.Csv.Aggregator2.Models
{
    /// <summary>
    /// CSVの1行を表現します。
    /// </summary>
    public class Record
    {
        public string[] Keys { get; set; }

        public decimal[] Sums { get; set; }

        /// <summary>
        /// 二乗和
        /// </summary>
        public decimal[] Sum2s { get; set; }

        public int Count { get; set; }

        public Record(string[] keys, int length)
        {
            Keys = keys ?? throw new ArgumentNullException(nameof(keys));
            Sums = new decimal[length];
            Sum2s = new decimal[length];
        }
    }
}
