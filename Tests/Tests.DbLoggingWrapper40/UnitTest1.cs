using System;
using Common.Logging;
using DbLoggingWrapper;
using Xunit;

namespace Tests.DbLoggingWrapper
{
    public class UnitTest1
    {
        private TestLogger logger;

        public UnitTest1()
        {
            logger = new TestLogger();
            LoggingWrapper.Logger = logger;
        }

        [Fact]
        public void Connection()
        {
            using (var conn = new LoggingConnection(new System.Data.SQLite.SQLiteConnection("Data Source=:memory:")))
            {
                conn.Open();
                Assert.Equal(LogLevel.Trace, logger.Last.Level);
                Assert.Matches(@"コネクションを接続しました。 実行時間:\d+:\d+:\d+\.\d+", logger.Last.Message);
                Assert.Null(logger.Last.Exception);

                var openException = Assert.Throws<InvalidOperationException>(() => conn.Open());
                Assert.Equal(LogLevel.Error, logger.Last.Level);
                Assert.Equal(string.Format(@"接続に失敗
--- SQLiteConnection ---
ConnectionString  = {0}
ConnectionTimeout = {1}
Database = main
State = Open
", conn.ConnectionString, conn.ConnectionTimeout), logger.Last.Message);
                Assert.Equal(openException, logger.Last.Exception);

                conn.Close();
                Assert.Equal(LogLevel.Trace, logger.Last.Level);
                Assert.Equal("コネクションを閉じました。", logger.Last.Message);
                Assert.Null(logger.Last.Exception);
            }
            Assert.Equal(LogLevel.Trace, logger.Last.Level);
            Assert.Equal("コネクションが破棄(Dispose)されました。", logger.Last.Message);
            Assert.Null(logger.Last.Exception);
        }

        [Fact]
        public void Transaction()
        {
            var conn = new System.Data.SQLite.SQLiteConnection("Data Source=:memory:");
            using (var wconn = new LoggingConnection(conn))
            {
                wconn.Open();

                Assert.Equal(LogLevel.Trace, logger.Last.Level);
                Assert.StartsWith("コネクションを接続しました。 実行時間:", logger.Last.Message);

                using (var trans = wconn.BeginTransaction())
                {
                    Assert.Equal(LogLevel.Trace, logger.Last.Level);
                    Assert.Equal("トランザクションを開始しました。", logger.Last.Message);

                    trans.Commit();

                    Assert.Equal(LogLevel.Trace, logger.Last.Level);
                    Assert.Matches(@"トランザクションをコミットしました。 実行時間:\d\d:\d\d:\d\d\.\d+", logger.Last.Message);
                }
                Assert.Equal(LogLevel.Trace, logger.Last.Level);
                Assert.Equal("トランザクションが破棄(Dispose)されました。", logger.Last.Message);
                Assert.Null(logger.Last.Exception);

                using (var trans = wconn.BeginTransaction())
                {
                    Assert.Equal(LogLevel.Trace, logger.Last.Level);
                    Assert.Equal("トランザクションを開始しました。", logger.Last.Message);

                    trans.Rollback();

                    Assert.Equal(LogLevel.Trace, logger.Last.Level);
                    Assert.Matches(@"トランザクションをロールバックしました。 実行時間:\d\d:\d\d:\d\d\.\d+", logger.Last.Message);
                }
                Assert.Equal(LogLevel.Trace, logger.Last.Level);
                Assert.Equal("トランザクションが破棄(Dispose)されました。", logger.Last.Message);
                Assert.Null(logger.Last.Exception);

                using (var trans = wconn.BeginTransaction())
                {
                    Assert.Equal(LogLevel.Trace, logger.Last.Level);
                    Assert.Equal("トランザクションを開始しました。", logger.Last.Message);

                }
                Assert.Equal(LogLevel.Error, logger.Last.Level);
                Assert.Equal("コミットもロールバックも実行されずに、トランザクションが破棄(Dispose)されました。", logger.Last.Message);
                Assert.Null(logger.Last.Exception);
            }
            Assert.Equal(LogLevel.Trace, logger.Last.Level);
            Assert.Equal("コネクションが破棄(Dispose)されました。", logger.Last.Message);

            Assert.Equal(10, logger.Items.Count);
        }

        [Fact]
        public void Command()
        {
            using (var conn = new System.Data.SQLite.SQLiteConnection("Data Source=:memory:"))
            {
                conn.Open();

                using (var command = new LoggingCommand(new System.Data.SQLite.SQLiteCommand()))
                {
                    command.CommandText = "select 1";
                    Assert.Throws<InvalidOperationException>(() => command.ExecuteReader());
                    Assert.Equal(LogLevel.Error, logger.Last.Level);
                    Assert.Equal(@"クエリ実行に失敗
--- SQLiteCommand ---
CommandTimeout   = 30
CommandType      = Text
UpdatedRowSource = None
--- CommandText ---
select 1
--- Parameters ---
", logger.Last.Message);

                    command.Connection = conn;
                    using (var reader = command.ExecuteReader())
                    {
                        Assert.Equal(LogLevel.Trace, logger.Last.Level);
                        Assert.Matches(@"ExecuteReader. 実行時間:\d\d:\d\d:\d\d\.\d+
--- CommandText ---
select 1
--- Parameters ---
", logger.Last.Message);

                        Assert.True(reader.Read());
                        Assert.Equal((long)1, reader[0]);
                    }

                    Assert.Equal((long)1, command.ExecuteScalar());
                    Assert.Matches(@"ExecuteScalar. result=1 実行時間:\d\d:\d\d:\d\d\.\d+
--- CommandText ---
select 1
--- Parameters ---
", logger.Last.Message);

                    command.CommandText = "select @p";
                    command.Parameters.Add(new System.Data.SQLite.SQLiteParameter("p", "abc"));
                    Assert.Equal("abc", command.ExecuteScalar());
                    Assert.Matches(@"ExecuteScalar. result=abc 実行時間:\d\d:\d\d:\d\d\.\d+
--- CommandText ---
select @p
--- Parameters ---
p\(String\)=abc
", logger.Last.Message);

                    Assert.IsType<System.Data.SQLite.SQLiteParameter>(command.CreateParameter());
                }
                Assert.Equal("コマンドが破棄(Dispose)されました。", logger.Last.Message);
            }
        }

        [Fact]
        public void BindByName()
        {
            var con = new Oracle.ManagedDataAccess.Client.OracleConnection();
            var cmd = con.CreateCommand();
            var wcon = new LoggingConnection(con);
            var wcmd = wcon.CreateCommand();
            Assert.False(cmd.BindByName);
            Assert.True(wcmd.TryCast<Oracle.ManagedDataAccess.Client.OracleCommand>().BindByName);
        }
    }
}
