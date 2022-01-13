using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umwelt.Faculty.Csv.Aggregator2.Models;

namespace Umwelt.Faculty.Csv.Aggregator2
{
    class Faculty
    {
        private readonly string _inputPath;
        private readonly string _outputPath;

        private readonly string[] _targetHeaders;
        private readonly string[] _keyHeaders;

        public Faculty(IConfiguration configuration)
        {
            (_inputPath, _outputPath) = Initializer.InFileOutFile(configuration);
            var incar = configuration.GetSection("INCAR");
            // ここで設定を読み取ります。
            _targetHeaders = incar["TargetHeaders"]
                .Split(',')
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .ToArray();

            _keyHeaders = incar["KeyHeaders"]
                .Split(',')
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .ToArray();
        }

        public async Task ExecuteAsync()
        {
            // ここにアルゴリズムの処理を書きます。
            using var reader = Csv.OpenRead(_inputPath);
            using var writer = Csv.Create(_outputPath);

            // 読み込み
            reader.Read();
            reader.ReadHeader();
            string[] headerRecord = reader.HeaderRecord;

            var records = new Dictionary<string[], Record>(StringArrayEqualityComparer.Default);
            while (reader.Read())
            {
                var keys = reader.GetFields(_keyHeaders).ToArray();
                if (!records.TryGetValue(keys, out var record))
                {
                    record = new Record(keys, _targetHeaders.Length);
                    records.Add(keys, record);
                }

                for (int i = 0; i < _targetHeaders.Length; i++)
                {
                    var num = reader[_targetHeaders[i]].AsDecimal() ?? 0;
                    record.Sums[i] += num;
                    // 二乗の和
                    record.Sum2s[i] += num * num;
                }

                record.Count++;
            }

            // 書き込み
            // ヘッダー
            writer.WriteFields(_keyHeaders);
            foreach (var targetHeader in _targetHeaders)
            {
                writer.WriteField(targetHeader + "_sum");
                writer.WriteField(targetHeader + "_ave");
                writer.WriteField(targetHeader + "_stdev");
            }
            writer.NextRecord();

            // 集計値
            foreach (var record in records.Values)
            {
                writer.WriteField(record.Keys);

                foreach (var val in Enumerable.Zip(record.Sums, record.Sum2s, (sum, sum2) => new { sum, sum2 }))
                {
                    var ave = val.sum / record.Count;
                    writer.WriteField(val.sum);
                    writer.WriteField(ave);

                    // 分散 = 二乗の平均 - 平均の二乗
                    var variance = (double)(val.sum2 / record.Count - ave * ave);
                    writer.WriteField(Math.Sqrt(variance));
                }
                writer.NextRecord();
            }
        }
    }
}
