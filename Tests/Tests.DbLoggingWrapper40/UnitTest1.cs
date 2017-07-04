using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Tests.DbLoggingWrapper
{
    [TestClass]
    public class UnitTest1
    {
        [TestInitialize]
        public void Initialize()
        {
            Common.Logging.LogManager.Adapter = new Common.Logging.Simple.ConsoleOutLoggerFactoryAdapter();
        }

        [TestMethod]
        public void TestMethod1()
        {
            var conn = new System.Data.SQLite.SQLiteConnection("data source=:memory:");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "select 1";
            Assert.AreEqual((long)1, cmd.ExecuteScalar());
        }

        [TestMethod]
        public void TestMethod2()
        {
            var conn = new global::DbLoggingWrapper.LoggingConnection(new System.Data.SQLite.SQLiteConnection("data source=:memory:"), global::DbLoggingWrapper.LoggingWrapper.DefaultLogWriter);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "select 1";
            Assert.AreEqual((long)1, cmd.ExecuteScalar());
        }

        [TestMethod]
        public void TestMethod3()
        {
            var conn = new global::DbLoggingWrapper.LoggingConnection(new System.Data.SQLite.SQLiteConnection("data source=:memory:"), global::DbLoggingWrapper.LoggingWrapper.DefaultLogWriter);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "select 1";
            var reader = cmd.ExecuteReader();
            reader.Read();
            Assert.AreEqual((long)1, reader[0]);
        }

        [TestMethod]
        public void MyTestMethod()
        {
            DbLoggingWrapperEF6.LoggingWrapperEF6.Initialize();
            using (var db = new TestDbContext())
            {
                var added = false;
                var item = db.Sample.FirstOrDefault();
                if (item == null)
                {
                    item = new Sample { Name = "aaa" };
                    db.Sample.Add(item);
                    added = true;
                }
                else
                {
                    db.Sample.Remove(item);
                }
                db.SaveChanges();
                Assert.AreEqual(added ? 1 : 0, db.Sample.Count());
            }
        }
    }
}
