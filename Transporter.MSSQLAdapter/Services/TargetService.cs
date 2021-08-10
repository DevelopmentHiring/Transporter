using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Transporter.Core;
using Transporter.MSSQLAdapter.Data;

namespace Transporter.MSSQLAdapter.Services
{
    public class TargetService : ITargetService
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public TargetService(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task SetTargetDataAsync(ISqlTargetSettings setting, string data)
        {
            var insertData = data.ToObject<List<Dictionary<string, string>>>();
            if (insertData is null || !insertData.Any()) return;

            var parameters = await GetParameters(insertData);

            using var connection = _dbConnectionFactory.GetConnection(setting.Options.ConnectionString);
            var query = await GetTargetInsertQueryAsync(setting, insertData.FirstOrDefault());
            await connection.ExecuteAsync(query, parameters);
        }

        public async Task SetTargetTemporaryDataAsync(ISqlTargetSettings setting, string data, string dataSourceName)
        {
            var insertData = data.ToObject<List<Dictionary<string, string>>>();
            if (insertData is null || !insertData.Any()) return;

            var upperCasedInsertData = insertData
                .Select(ConvertDictionaryKeysToUpperCase())
                .ToList();

            foreach (var dictionary in upperCasedInsertData)
            {
                dictionary.Add("Lmd", DateTime.Now.ToString(CultureInfo.InvariantCulture));
                dictionary.Add("DataSourceName", dataSourceName);
            }

            var parameters = await GetParameters(upperCasedInsertData);

            using var connection = _dbConnectionFactory.GetConnection(setting.Options.ConnectionString);
            var query = await GetTargetInsertIdDataQueryAsync(setting, upperCasedInsertData.FirstOrDefault());
            await connection.ExecuteAsync(query, parameters);
        }

        private static Func<Dictionary<string, string>, Dictionary<string, string>> ConvertDictionaryKeysToUpperCase()
        {
            return dictionary => dictionary.ToDictionary(x => x.Key.ToUpper(), x => x.Value);
        }

        private async Task<List<DynamicParameters>> GetParameters(List<Dictionary<string, string>> insertData)
        {
            var parameters = new List<DynamicParameters>();
            insertData.ForEach(sData =>
            {
                var values = sData.Select(x => new KeyValuePair<string, object>($"@{x.Key}", x.Value));
                parameters.Add(new DynamicParameters(values));
            });
            return await Task.Run(() => parameters);
        }

        private async Task<string> GetTargetInsertQueryAsync(ISqlTargetSettings setting,
            Dictionary<string, string> insertData)
        {
            var excludedColumns = setting.Options.ExcludedColumns?.Split(",") ?? Array.Empty<string>();
            var queryData = insertData.Where(x => !excludedColumns.Contains(x.Key)).ToList();
            var query = new StringBuilder();
            query.Append($"INSERT INTO {setting.Options.Schema}.{setting.Options.Table}");
            query.AppendLine(" (" + string.Join(',', queryData.Select(x => x.Key)));
            query.AppendLine(" ) VALUES (");
            query.AppendLine(string.Join(',', queryData.Select(x => $"@{x.Key}")));
            query.AppendLine(" )");

            return await Task.FromResult(query.ToString());
        }

        private async Task<string> GetTargetInsertIdDataQueryAsync(ISqlTargetSettings setting,
            Dictionary<string, string> insertData)
        {
            var queryData = insertData.ToList();
            var query = new StringBuilder();
            query.Append($"INSERT INTO {setting.Options.Schema}.{setting.Options.Table}");
            query.AppendLine(" (" + string.Join(',', queryData.Select(x => x.Key)));
            query.AppendLine(" ) VALUES (");
            query.AppendLine(string.Join(',', queryData.Select(x => $"@{x.Key}")));
            query.AppendLine(" )");

            return await Task.FromResult(query.ToString());
        }
    }
}