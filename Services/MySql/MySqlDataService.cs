using MySql.Data.MySqlClient;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wpf_RunVision.Services.MySql;
using Wpf_RunVision.Utils;

namespace Wpf_RunVision.Services.Mysql
{
    /// <summary>
    /// MySQL 数据服务（单例模式）
    /// 负责数据库初始化、数据入队、批量上传
    /// </summary>
    public sealed class MySqlDataService
    {
        #region 单例初始化（线程安全）
        private static readonly Lazy<MySqlDataService> _instance = new Lazy<MySqlDataService>(() => new MySqlDataService(), LazyThreadSafetyMode.ExecutionAndPublication);
        public static MySqlDataService Instance => _instance.Value;

        // 私有构造函数防止外部实例化
        private MySqlDataService() { }
        #endregion

        #region 配置字段（只读化+初始化校验）
        private string _connectionString = string.Empty;
        private bool _isInitialized = false; // 初始化状态标记

        // 配置参数（私有只读，仅通过 Initialize 赋值）
        private string _ip = string.Empty;
        private int _port;
        private string _database = string.Empty;
        private string _user = string.Empty;
        private string _password = string.Empty;
        #endregion

        #region 数据队列（优化命名+容量限制）
        /// <summary>
        /// 检测数据队列（最大容量 10000，防止内存溢出）
        /// </summary>
        private readonly ConcurrentQueue<DataTableName> _dataQueue = new ConcurrentQueue<DataTableName>();
        private const int MaxQueueCapacity = 10000;

        /// <summary>
        /// 编码数据队列
        /// </summary>
        private readonly ConcurrentQueue<CodeTableName> _codeQueue = new ConcurrentQueue<CodeTableName>();
        #endregion

        #region 上传控制（线程安全优化）
        private readonly SemaphoreSlim _uploadSemaphore = new SemaphoreSlim(1, 1); // 信号量保证上传操作互斥
        private bool _isUploading => _uploadSemaphore.CurrentCount == 0; // 简化上传状态判断
        #endregion

        #region 核心方法：初始化数据库（修复异步报错，改为同步）
        /// <summary>
        /// 初始化数据库连接（创建数据库+测试连接）
        /// </summary>
        /// <param name="ip">数据库IP</param>
        /// <param name="port">端口号</param>
        /// <param name="database">数据库名</param>
        /// <param name="user">用户名</param>
        /// <param name="password">密码</param>
        /// <returns>初始化成功返回 true</returns>
        /// <exception cref="ArgumentNullException">必填参数为空时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">端口号无效时抛出</exception>
        public bool Initialize(string ip, int port, string database, string user, string password)
        {
            // 1. 入参校验（提前拦截无效配置）
            if (string.IsNullOrWhiteSpace(ip))
                throw new ArgumentNullException(nameof(ip), "数据库IP不能为空");
            if (port is < 1 or > 65535)
                throw new ArgumentOutOfRangeException(nameof(port), "端口号必须在 1-65535 之间");
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentNullException(nameof(database), "数据库名不能为空");
            if (string.IsNullOrWhiteSpace(user))
                throw new ArgumentNullException(nameof(user), "用户名不能为空");

            // 2. 保存配置
            _ip = ip;
            _port = port;
            _database = database;
            _user = user;
            _password = password;

            // 3. 连接MySQL服务（无数据库）
            string masterConnStr = BuildMasterConnectionString();
            try
            {
                using (var conn = new MySqlConnection(masterConnStr))
                {
                    conn.Open();
                    //MyLogger.Info($"MySQL服务连接成功 -> {ip}:{port}");

                    // 4. 检查并创建数据库（修复异步，改为同步）
                    if (!CheckDatabaseExists(conn, database))
                    {
                        CreateDatabase(conn, database);
                        MyLogger.Info($"数据库 [{database}] 不存在，已自动创建");
                    }
                    else
                    {
                        //MyLogger.Info($"数据库 [{database}] 已存在，跳过创建");
                    }
                }

                // 5. 测试数据库连接（带数据库名）
                _connectionString = BuildDatabaseConnectionString();
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    MyLogger.Info($"MySQL [{ip}:{port}/{database}] 连接成功！");
                }

                _isInitialized = true;
                return true;
            }
            catch (MySqlException ex)
            {
                MyLogger.Error($"数据库初始化失败 -> {ip}:{port}/{database}，MySQL错误：{ex.Message}，错误码：{ex.Number}");
                _isInitialized = false;
                return false;
            }
            catch (Exception ex)
            {
                MyLogger.Error($"数据库初始化失败 -> {ip}:{port}/{database}，原因：{ex.Message}");
                _isInitialized = false;
                return false;
            }
        }
        #endregion

        #region 队列操作（优化容量控制+空值过滤）
        /// <summary>
        /// 检测数据入队（超过最大容量时丢弃最旧数据）
        /// </summary>
        public void EnqueueData(DataTableName item)
        {
            if (item == null)
            {
                MyLogger.Warn("尝试入队空的检测数据，已忽略");
                return;
            }

            // 队列满时丢弃最旧数据（防止内存溢出）
            if (_dataQueue.Count >= MaxQueueCapacity)
            {
                if (_dataQueue.TryDequeue(out _))
                {
                    MyLogger.Warn($"检测数据队列已满（最大容量：{MaxQueueCapacity}），已丢弃最旧数据");
                }
            }

            _dataQueue.Enqueue(item);
            MyLogger.Debug($"检测数据入队成功，当前队列长度：{_dataQueue.Count}");
        }

        /// <summary>
        /// 编码数据入队
        /// </summary>
        public void EnqueueCode(CodeTableName item)
        {
            if (item == null)
            {
                MyLogger.Warn("尝试入队空的编码数据，已忽略");
                return;
            }

            if (_codeQueue.Count >= MaxQueueCapacity)
            {
                if (_codeQueue.TryDequeue(out _))
                {
                    MyLogger.Warn($"编码数据队列已满（最大容量：{MaxQueueCapacity}），已丢弃最旧数据");
                }
            }

            _codeQueue.Enqueue(item);
            MyLogger.Debug($"编码数据入队成功，当前队列长度：{_codeQueue.Count}");
        }

        /// <summary>
        /// 清空队列（用于重置场景）
        /// </summary>
        public void ClearQueues()
        {
            while (_dataQueue.TryDequeue(out _)) { }
            while (_codeQueue.TryDequeue(out _)) { }
            MyLogger.Info("数据队列已清空");
        }
        #endregion

        #region 批量上传（修复异步+语法兼容）
        /// <summary>
        /// 同步批量上传（阻塞当前线程，适配低版本.NET）
        /// </summary>
        /// <param name="dataTableName">检测数据表名</param>
        /// <param name="codeTableName">编码数据表名</param>
        /// <param name="retryCount">上传失败重试次数（默认2次）</param>
        public void UploadAll(string dataTableName = "DataTable", string codeTableName = "CodeTable", int retryCount = 2)
        {
            // 1. 前置校验
            if (!_isInitialized)
            {
                MyLogger.Error("数据库未初始化，请先调用 Initialize() 方法");
                return;
            }

            if (string.IsNullOrWhiteSpace(dataTableName) || string.IsNullOrWhiteSpace(codeTableName))
            {
                MyLogger.Error("表名不能为空");
                return;
            }

            // 2. 信号量控制：保证同一时间只有一个上传任务执行
            if (!_uploadSemaphore.Wait(0))
            {
                MyLogger.Info("已有上传任务正在执行，跳过本次请求");
                return;
            }

            int dataCount = _dataQueue.Count;
            int codeCount = _codeQueue.Count;

            // 3. 无数据时直接返回
            if (dataCount == 0 && codeCount == 0)
            {
                MyLogger.Info("无待上传数据，跳过上传");
                _uploadSemaphore.Release();
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();
            bool uploadSuccess = false;

            try
            {
                // 4. 失败重试机制
                for (int i = 0; i <= retryCount; i++)
                {
                    try
                    {
                        ExecuteUpload(dataTableName, codeTableName);
                        uploadSuccess = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (i == retryCount)
                        {
                            MyLogger.Error($"上传失败（已重试 {retryCount} 次）：{ex.Message}");
                            throw; // 重试次数用尽，抛出异常
                        }

                        int delayMs = (int)Math.Pow(2, i) * 100; // 指数退避重试（100ms, 200ms, 400ms...）
                        MyLogger.Warn($"第 {i + 1}/{retryCount} 次上传失败：{ex.Message}，{delayMs}ms 后重试");
                        Thread.Sleep(delayMs); // 低版本替代 Task.Delay
                    }
                }

                if (uploadSuccess)
                {
                    sw.Stop();
                    MyLogger.Info($"批量上传成功 -> 检测数据：{dataCount} 条，编码数据：{codeCount} 条，耗时：{sw.ElapsedMilliseconds} ms");
                }
            }
            finally
            {
                // 5. 释放信号量（确保无论成功失败都释放）
                if (_uploadSemaphore.CurrentCount == 0)
                {
                    _uploadSemaphore.Release();
                }
            }
        }

        /// <summary>
        /// 后台异步上传（无阻塞，fire-and-forget）
        /// </summary>
        public void UploadAllFireAndForget(string dataTableName = "DataTable", string codeTableName = "CodeTable")
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    UploadAll(dataTableName, codeTableName, 2);
                }
                catch (Exception ex)
                {
                    MyLogger.Error($"后台上传任务异常：{ex.InnerException?.Message ?? ex.Message}");
                }
            }, TaskCreationOptions.LongRunning);
        }
        #endregion

        #region 内部辅助方法（修复语法兼容问题）
        /// <summary>
        /// 执行实际的上传操作（事务+批量插入）
        /// </summary>
        private void ExecuteUpload(string dataTableName, string codeTableName)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        // 1. 创建表（不存在则创建）
                        CreateTables(conn, dataTableName, codeTableName);

                        // 2. 批量插入检测数据
                        if (_dataQueue.Count > 0)
                        {
                            BatchInsertDataTable(conn, tran, dataTableName);
                        }

                        // 3. 批量插入编码数据
                        if (_codeQueue.Count > 0)
                        {
                            BatchInsertCodeTable(conn, tran, codeTableName);
                        }

                        // 4. 提交事务
                        tran.Commit();
                    }
                    catch (Exception)
                    {
                        // 回滚事务
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 批量插入检测数据（修复 ?? 运算符报错）
        /// </summary>
        private void BatchInsertDataTable(MySqlConnection conn, MySqlTransaction tran, string tableName)
        {
            var sb = new StringBuilder();
            sb.Append($"INSERT INTO `{EscapeTableName(tableName)}` (DetaTime, PhotoName, `PCS号`, PaperCode, LaserCode, Lot, UserID, Item, Model, PointSet, Result) VALUES ");

            var parameters = new List<MySqlParameter>();
            int index = 0;

            foreach (var item in _dataQueue)
            {
                sb.Append($"(@DetaTime{index}, @PhotoName{index}, @PCS号{index}, @PaperCode{index}, @LaserCode{index}, @Lot{index}, @UserID{index}, @Item{index}, @Model{index}, @PointSet{index}, @Result{index}),");

                // 修复：用三元表达式替代 ?? 运算符（string 不能直接与 DBNull 拼接）
                parameters.Add(new MySqlParameter($"@DetaTime{index}", MySqlDbType.DateTime) { Value = item.DetaTime });
                parameters.Add(new MySqlParameter($"@PhotoName{index}", MySqlDbType.VarChar) { Value = string.IsNullOrEmpty(item.PhotoName) ? DBNull.Value : (object)item.PhotoName });
                parameters.Add(new MySqlParameter($"@PCS号{index}", MySqlDbType.VarChar) { Value = string.IsNullOrEmpty(item.PCS号) ? DBNull.Value : (object)item.PCS号 });
                parameters.Add(new MySqlParameter($"@PaperCode{index}", MySqlDbType.VarChar) { Value = string.IsNullOrEmpty(item.PaperCode) ? DBNull.Value : (object)item.PaperCode });
                parameters.Add(new MySqlParameter($"@LaserCode{index}", MySqlDbType.VarChar) { Value = string.IsNullOrEmpty(item.LaserCode) ? DBNull.Value : (object)item.LaserCode });
                parameters.Add(new MySqlParameter($"@Lot{index}", MySqlDbType.VarChar) { Value = string.IsNullOrEmpty(item.Lot) ? DBNull.Value : (object)item.Lot });
                parameters.Add(new MySqlParameter($"@UserID{index}", MySqlDbType.VarChar) { Value = string.IsNullOrEmpty(item.UserID) ? DBNull.Value : (object)item.UserID });
                parameters.Add(new MySqlParameter($"@Item{index}", MySqlDbType.VarChar) { Value = string.IsNullOrEmpty(item.Item) ? DBNull.Value : (object)item.Item });
                parameters.Add(new MySqlParameter($"@Model{index}", MySqlDbType.VarChar) { Value = string.IsNullOrEmpty(item.Model) ? DBNull.Value : (object)item.Model });
                parameters.Add(new MySqlParameter($"@PointSet{index}", MySqlDbType.Text) { Value = string.IsNullOrEmpty(item.PointSet) ? DBNull.Value : (object)item.PointSet });
                parameters.Add(new MySqlParameter($"@Result{index}", MySqlDbType.VarChar) { Value = string.IsNullOrEmpty(item.Result) ? DBNull.Value : (object)item.Result });

                index++;
            }

            // 移除最后一个逗号
            if (sb.Length > 0 && sb[sb.Length - 1] == ',')
            {
                sb.Length--;
            }

            using (var cmd = new MySqlCommand(sb.ToString(), conn, tran))
            {
                cmd.Parameters.AddRange(parameters.ToArray());
                cmd.ExecuteNonQuery();
            }

            // 清空队列
            while (_dataQueue.TryDequeue(out _)) { }
        }

        /// <summary>
        /// 批量插入编码数据（修复 ?? 运算符报错）
        /// </summary>
        private void BatchInsertCodeTable(MySqlConnection conn, MySqlTransaction tran, string tableName)
        {
            var sb = new StringBuilder();
            sb.Append($"INSERT INTO `{EscapeTableName(tableName)}` (Code, DetaTime) VALUES ");

            var parameters = new List<MySqlParameter>();
            int index = 0;

            foreach (var item in _codeQueue)
            {
                sb.Append($"(@Code{index}, @DetaTime{index}),");
                // 修复：用三元表达式替代 ?? 运算符
                parameters.Add(new MySqlParameter($"@Code{index}", MySqlDbType.VarChar) { Value = string.IsNullOrEmpty(item.Code) ? DBNull.Value : (object)item.Code });
                parameters.Add(new MySqlParameter($"@DetaTime{index}", MySqlDbType.DateTime) { Value = item.DetaTime });
                index++;
            }

            if (sb.Length > 0 && sb[sb.Length - 1] == ',')
            {
                sb.Length--;
            }

            using (var cmd = new MySqlCommand(sb.ToString(), conn, tran))
            {
                cmd.Parameters.AddRange(parameters.ToArray());
                cmd.ExecuteNonQuery();
            }

            while (_codeQueue.TryDequeue(out _)) { }
        }

        /// <summary>
        /// 检查数据库是否存在（同步版本）
        /// </summary>
        private bool CheckDatabaseExists(MySqlConnection conn, string databaseName)
        {
            string sql = "SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = @DatabaseName";
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.Add(new MySqlParameter("@DatabaseName", databaseName));
                var result = cmd.ExecuteScalar();
                return result != null;
            }
        }

        /// <summary>
        /// 创建数据库（同步版本）
        /// </summary>
        private void CreateDatabase(MySqlConnection conn, string databaseName)
        {
            string sql = $"CREATE DATABASE `{EscapeTableName(databaseName)}` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;";
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 创建数据表（不存在则创建）
        /// </summary>
        private void CreateTables(MySqlConnection conn, string dataTableName, string codeTableName)
        {
            // 检测数据表SQL（优化字段长度+索引）
            string createDataTableSql = $@"
            CREATE TABLE IF NOT EXISTS `{EscapeTableName(dataTableName)}`(
                `index` INT AUTO_INCREMENT PRIMARY KEY COMMENT '自增主键',
                `DetaTime` DATETIME NOT NULL COMMENT '数据时间',
                `PhotoName` VARCHAR(255) DEFAULT '' COMMENT '照片名称',
                `PCS号` VARCHAR(50) DEFAULT '' COMMENT 'PCS编号',
                `PaperCode` VARCHAR(50) DEFAULT '' COMMENT '纸张编码',
                `LaserCode` VARCHAR(50) DEFAULT '' COMMENT '激光编码',
                `Lot` VARCHAR(50) DEFAULT '' COMMENT '批次号',
                `UserID` VARCHAR(50) DEFAULT '' COMMENT '用户ID',
                `Item` VARCHAR(50) DEFAULT '' COMMENT '项目名称',
                `Model` VARCHAR(50) DEFAULT '' COMMENT '型号',
                `PointSet` TEXT COMMENT '检测点配置',
                `Result` VARCHAR(50) DEFAULT '' COMMENT '检测结果',
                INDEX idx_detime (`DetaTime`) COMMENT '时间索引，优化查询'
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='检测数据表';";

            // 编码数据表SQL
            string createCodeTableSql = $@"
            CREATE TABLE IF NOT EXISTS `{EscapeTableName(codeTableName)}`(
                `index` INT AUTO_INCREMENT PRIMARY KEY COMMENT '自增主键',
                `Code` VARCHAR(255) DEFAULT '' COMMENT '编码值',
                `DetaTime` DATETIME NOT NULL COMMENT '数据时间',
                INDEX idx_detime (`DetaTime`) COMMENT '时间索引，优化查询'
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='编码数据表';";

            // 执行建表SQL
            using (var cmd = new MySqlCommand(createDataTableSql, conn))
            {
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new MySqlCommand(createCodeTableSql, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 构建主连接字符串（无数据库名，修复旧版MySQL Connector兼容）
        /// </summary>
        private string BuildMasterConnectionString()
        {
            // 旧版 MySQL Connector 不支持 MySqlConnectionStringBuilder 的部分属性，直接拼接字符串
            return $"Server={_ip};Port={_port};User ID={_user};Password={_password};CharSet=utf8mb4;Allow User Variables=True;Connect Timeout=10;";
        }

        /// <summary>
        /// 构建数据库连接字符串（带数据库名）
        /// </summary>
        private string BuildDatabaseConnectionString()
        {
            return $"Server={_ip};Port={_port};Database={_database};User ID={_user};Password={_password};CharSet=utf8mb4;Allow User Variables=True;Connect Timeout=10;Default Command Timeout=30;";
        }

        /// <summary>
        /// 表名转义（防止表名包含特殊字符）
        /// </summary>
        private string EscapeTableName(string tableName)
        {
            return tableName.Replace("`", "``"); // 转义反引号
        }
        #endregion

        #region 资源释放（单例模式可选，按需使用）
        /// <summary>
        /// 释放资源（如信号量）
        /// </summary>
        public void Dispose()
        {
            _uploadSemaphore.Dispose();
        }
        #endregion
    }
}