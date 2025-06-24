using Dapper;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.SapSync;
using System.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RouteCardProcess.Repositories
{
    public class SapSyncService : ISapSyncService
    {
        private readonly HttpClient _httpClient;
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly string _baseUrl;
        private readonly string _materialBaseUrl;

        public SapSyncService(HttpClient httpClient, IConfiguration configuration, SqlConnectionFactory connectionFactory)
        {
            _httpClient = httpClient;
            _connectionFactory = connectionFactory;

            var username = configuration["SapSettings:Username"];
            var password = configuration["SapSettings:Password"];
            _baseUrl = configuration["SapSettings:BaseUrl"];
            _materialBaseUrl = configuration["RoutingData:MaterialBaseUrl"];

            var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }
        private TimeSpan ParseToTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return TimeSpan.Zero;
            value = value.Trim().Split('.')[0];
            return int.TryParse(value, out var minutes) ? TimeSpan.FromMinutes(minutes) : TimeSpan.Zero;
        }

        private int ParseToInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            value = value.Trim().Split('.')[0];
            return int.TryParse(value, out var result) ? result : 0;
        }

        public async Task SyncRoutingDataAsync(string orderNumber)
        {
            var url = $"{_baseUrl}ZROUTING_DATASet/?$filter=ORDER_NUMBER eq '{orderNumber}'&$format=json";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var sapResponse = JsonSerializer.Deserialize<SapRoutingResponse>(json);

            if (sapResponse?.d?.results == null || !sapResponse.d.results.Any())
                return;

            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var item in sapResponse.d.results)
                {
                    int totalQty = ParseToInt(item.TARGET_QUANTITY);
                    int confirmedQty = ParseToInt(item.CONFIRMED_QUANTIT);
                    TimeSpan stdSetupTime = ParseToTime(item.SETUP_TIME);
                    TimeSpan stdMachiningTime = ParseToTime(item.PROCESSING_TIME);

                    var existingData = await connection.QueryFirstOrDefaultAsync<int?>(
                       "usp_GetCompletedQty",
                        new { WorkOrder = item.ORDER_NUMBER, OperationNo = item.OPERATION_NUMBER },
                        transaction: transaction);

                    int completedQty = confirmedQty;
                    if (existingData.HasValue && existingData.Value >= confirmedQty)
                        completedQty = existingData.Value;

                    var parameters = new
                    {
                        WorkOrder = item.ORDER_NUMBER,
                        WorkCenter = item.WORK_CENTER,
                        OperationNo = item.OPERATION_NUMBER,
                        TotalQty = totalQty,
                        S_ConfirmedQuantity = confirmedQty,
                        L_CompletedQty = completedQty,
                        OperationDescription = item.DESCRIPTION,
                        WorkCenterText = item.WORK_CENTER_TEXT,
                        StdSetupTime = stdSetupTime,
                        StdMachiningTime = stdMachiningTime,
                        S_RoutingDataStatus = item.STATUS ?? "",
                        SetupUnit = item.SETUP_UNIT ?? "MIN",
                        ProcessingUnit = item.PROCESSING_UNIT ?? "MIN",
                        Material = item.MATERIAL,
                        MaterialText = item.MATERIAL_TEXT,
                        OrderType = item.ORDER_TYPE,
                        ProductionPlant = item.PRODUCTION_PLANT,
                        ProductionUnit = item.UNIT,
                        MrpController = item.MRP_CONTROLLER,
                        ProductionScheduler = item.PRODUCTION_SCHEDULER
                    };

                    await connection.ExecuteAsync(
                "usp_SyncSapRoutingData",
                parameters,
                transaction: transaction,
                commandType: CommandType.StoredProcedure);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        public async Task<IEnumerable<RoutingDataResponse>> GetSelectedRoutingDataAsync(string orderNumber)
        {
            using var connection = _connectionFactory.CreateConnection();

            var data = (await connection.QueryAsync<RoutingDataResponse>(
     "usp_GetRoutingDataByOrder",
     new { OrderNumber = orderNumber },
     commandType: CommandType.StoredProcedure)).ToList();

            foreach (var item in data)
            {
                item.MaterialTextLink = $"{_materialBaseUrl}{item.Material}";
            }

            return data;

        }

    }
}
