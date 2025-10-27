using MySql.Data.MySqlClient;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Wpf_RunVision.Services.MySql;
using Wpf_RunVision.Utils;

namespace Wpf_RunVision.Services.Mysql
{
    public sealed class MySqlDataService
    {
        private static readonly Lazy<MySqlDataService> _instance = new Lazy<MySqlDataService>(() => new MySqlDataService());
        public static MySqlDataService Instance => _instance.Value;

        private string _connectionString;
        private string _ip;
        private int _port;
        private string _database;
        private string _user;
        private string _password;

        private readonly ConcurrentQueue<DataTableName> _dataQueue = new ConcurrentQueue<DataTableName>();
        private readonly ConcurrentQueue<CodeTableName> _codeQueue = new ConcurrentQueue<CodeTableName>();
        private bool _isUploading = false;

        private MySqlDataService() { }

        /// <summary>
        /// 初始化数据库（返回 true 表示成功）
        /// </summary>
        public bool Initialize(string ip, int port, string database, string user, string password)
        {
            _ip = ip;
            _port = port;
            _database = database;
            _user = user;
            _password = password;

            string masterConnStr = $"Server={ip};Port={port};User ID={user};Password={password};CharSet=utf8mb4;Allow User Variables=True;";
            try
            {
                using (var conn = new MySqlConnection(masterConnStr))
                {
                    conn.Open();
                    MyLogger.Info($"连接 MySQL 服务成功 -> {ip}:{port}");

                    string checkDbSql = $"SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME='{database}'";
                    using (var cmd = new MySqlCommand(checkDbSql, conn))
                    {
                        var result = cmd.ExecuteScalar();
                        if (result == null)
                        {
                            cmd.CommandText = $"CREATE DATABASE `{database}` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;";
                            cmd.ExecuteNonQuery();
                            MyLogger.Info($"数据库不存在，已创建 -> {database}");
                        }
                        else
                        {
                            //MyLogger.Info($"数据库已存在 -> {database}");
                        }
                    }
                }

                _connectionString = $"Server={ip};Port={port};Database={database};User ID={user};Password={password};CharSet=utf8mb4;Allow User Variables=True;";
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    MyLogger.Info($"数据库连接成功 -> {ip}:{port}/{database}");
                }

                return true;
            }
            catch (Exception ex)
            {
                MyLogger.Error($"数据库初始化失败 -> {ip}:{port}/{database}，原因：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 数据入队
        /// </summary>
        public void EnqueueData(DataTableName item)
        {
            if (item != null) _dataQueue.Enqueue(item);
        }
        public void EnqueueCode(CodeTableName item)
        {
            if (item != null) _codeQueue.Enqueue(item);
        }

        #region 批量上传

        public void UploadAll(string dataTableName = "DataTable", string codeTableName = "CodeTable")
        {
            UploadAllAsync(dataTableName, codeTableName).GetAwaiter().GetResult();
        }

        public async Task UploadAllAsync(string dataTableName = "DataTable", string codeTableName = "CodeTable")
        {
            if (_isUploading) return; // 防止重复上传
            _isUploading = true;

            try
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    MyLogger.Error("数据库未初始化，请先调用 Initialize()");
                    return;
                }

                int dataCount = _dataQueue.Count;
                int codeCount = _codeQueue.Count;
                if (dataCount == 0 && codeCount == 0)
                {
                    MyLogger.Info("无待上传数据，跳过上传。");
                    return;
                }

                Stopwatch sw = Stopwatch.StartNew();

                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            // 创建表（如果不存在）
                            CreateTables(conn, dataTableName, codeTableName);

                            // 批量 DataTable
                            if (dataCount > 0)
                            {
                                var sb = new StringBuilder();
                                sb.Append($"INSERT INTO `{dataTableName}` (DetaTime, PhotoName, `PCS号`, PaperCode, LaserCode, Lot, UserID, Item, Model, PointSet, Result) VALUES ");

                                int index = 0;
                                var parameters = new MySqlParameter[dataCount * 11];
                                foreach (var d1 in _dataQueue)
                                {
                                    sb.Append($"(@DetaTime{index}, @PhotoName{index}, @PCS号{index}, @PaperCode{index}, @LaserCode{index}, @Lot{index}, @UserID{index}, @Item{index}, @Model{index}, @PointSet{index}, @Result{index}),");
                                    parameters[index * 11 + 0] = new MySqlParameter($"@DetaTime{index}", d1.DetaTime);
                                    parameters[index * 11 + 1] = new MySqlParameter($"@PhotoName{index}", d1.PhotoName ?? "");
                                    parameters[index * 11 + 2] = new MySqlParameter($"@PCS号{index}", d1.PCS号 ?? "");
                                    parameters[index * 11 + 3] = new MySqlParameter($"@PaperCode{index}", d1.PaperCode ?? "");
                                    parameters[index * 11 + 4] = new MySqlParameter($"@LaserCode{index}", d1.LaserCode ?? "");
                                    parameters[index * 11 + 5] = new MySqlParameter($"@Lot{index}", d1.Lot ?? "");
                                    parameters[index * 11 + 6] = new MySqlParameter($"@UserID{index}", d1.UserID ?? "");
                                    parameters[index * 11 + 7] = new MySqlParameter($"@Item{index}", d1.Item ?? "");
                                    parameters[index * 11 + 8] = new MySqlParameter($"@Model{index}", d1.Model ?? "");
                                    parameters[index * 11 + 9] = new MySqlParameter($"@PointSet{index}", d1.PointSet ?? "");
                                    parameters[index * 11 + 10] = new MySqlParameter($"@Result{index}", d1.Result ?? "");
                                    index++;
                                }
                                sb.Length--; // 去掉最后的逗号
                                using (var cmd = new MySqlCommand(sb.ToString(), conn, tran))
                                {
                                    cmd.Parameters.AddRange(parameters);
                                    cmd.ExecuteNonQuery();
                                }
                                while (_dataQueue.TryDequeue(out _)) { }
                            }

                            // 批量 CodeTable
                            if (codeCount > 0)
                            {
                                var sb = new StringBuilder();
                                sb.Append($"INSERT INTO `{codeTableName}` (Code, DetaTime) VALUES ");
                                var parameters = new MySqlParameter[codeCount * 2];
                                int index = 0;
                                foreach (var d2 in _codeQueue)
                                {
                                    sb.Append($"(@Code{index}, @DetaTime{index}),");
                                    parameters[index * 2 + 0] = new MySqlParameter($"@Code{index}", d2.Code ?? "");
                                    parameters[index * 2 + 1] = new MySqlParameter($"@DetaTime{index}", d2.DetaTime);
                                    index++;
                                }
                                sb.Length--;
                                using (var cmd = new MySqlCommand(sb.ToString(), conn, tran))
                                {
                                    cmd.Parameters.AddRange(parameters);
                                    cmd.ExecuteNonQuery();
                                }
                                while (_codeQueue.TryDequeue(out _)) { }
                            }

                            tran.Commit();
                            sw.Stop();
                            MyLogger.Info($"数据库批量上传成功 {dataCount} 条检测数据, {codeCount} 条 Code 数据，耗时 {sw.ElapsedMilliseconds} ms。");
                        }
                        catch (Exception ex)
                        {
                            try { tran.Rollback(); } catch { }
                            sw.Stop();
                            MyLogger.Error($"数据库上传失败: {ex.Message}，耗时 {sw.ElapsedMilliseconds} ms。");
                        }
                    }
                }
            }
            finally
            {
                _isUploading = false;
            }
        }

        #endregion

        public void UploadAllFireAndForget(string dataTableName = "DataTable", string codeTableName = "CodeTable")
            => Task.Run(async () => await UploadAllAsync(dataTableName, codeTableName));

        /// <summary>
        /// 创建表（如果不存在）
        /// </summary>
        private void CreateTables(MySqlConnection conn, string dataTableName, string codeTableName)
        {
            string createDataTable = $@"
            CREATE TABLE IF NOT EXISTS `{dataTableName}`(
                indenx INT AUTO_INCREMENT PRIMARY KEY,
                DetaTime DATETIME,
                PhotoName VARCHAR(255),
                `PCS号` VARCHAR(50),
                PaperCode VARCHAR(50),
                LaserCode VARCHAR(50),
                Lot VARCHAR(50),
                UserID VARCHAR(50),
                Item VARCHAR(50),
                Model VARCHAR(50),
                PointSet TEXT,
                Result VARCHAR(50)
            );";

            string createCodeTable = $@"
            CREATE TABLE IF NOT EXISTS `{codeTableName}`(
                indenx INT AUTO_INCREMENT PRIMARY KEY,
                Code VARCHAR(255),
                DetaTime DATETIME
            );";

            using (var cmd = new MySqlCommand(createDataTable, conn)) { cmd.ExecuteNonQuery(); }
            using (var cmd = new MySqlCommand(createCodeTable, conn)) { cmd.ExecuteNonQuery(); }
        }
    }
}
