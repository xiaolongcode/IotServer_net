using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace IotServer
{
    public abstract class SqlHelper
    {
        /// <summary>
        /// 数据库连接字符串，配置文件在appsettings.json文件中
        /// </summary>
        private static  string _connectionString;

        public static string GetConnStr()
        {
            if (string.IsNullOrEmpty(_connectionString))
                _connectionString = ConfigurationManager.ConnectionStrings["DBConnectionString"].ToString();
            return _connectionString;
        }

        #region 1.0 执行查询语句，返回一个表 
        /// <summary>
        /// 1.0 执行查询语句，返回一个表
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ps">参数数组</param>
        /// <returns>返回一张表</returns>
        public static DataTable ExcuteTable(string sql, params SqlParameter[] ps)
        {
            SqlDataAdapter da = new SqlDataAdapter(sql, GetConnStr());
            if (ps != null)
                da.SelectCommand.Parameters.AddRange(ps);
            DataTable dt = new DataTable();
            da.Fill(dt);
            return dt;
        }
        #endregion

        #region 2.0 执行增删改的方法 
        /// <summary>
        /// 2.0 执行增删改的方法
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ps">参数数组</param>
        /// <returns>返回一条记录</returns>
        public static int ExcuteNoQuery(string sql, params SqlParameter[] ps)
        {
            using (SqlConnection conn = new SqlConnection(GetConnStr()))
            {
                conn.Open();
                SqlCommand command = new SqlCommand(sql, conn);
                if (ps != null)
                    command.Parameters.AddRange(ps);
                return command.ExecuteNonQuery();
            }
        }
        #endregion

        #region 3.0 执行存储过程的方法 
        /// <summary>
        /// 3.0 执行存储过程的方法
        /// </summary>
        /// <param name="procName">存储过程名</param>
        /// <param name="ps">参数数组</param>
        /// <returns></returns>
        public static int ExcuteProc(string procName, params SqlParameter[] ps)
        {
            using (SqlConnection conn = new SqlConnection(GetConnStr()))
            {
                conn.Open();
                SqlCommand command = new SqlCommand(procName, conn);
                command.CommandType = CommandType.StoredProcedure;
                if (ps != null)
                    command.Parameters.AddRange(ps);
                return command.ExecuteNonQuery();
            }
        }
        #endregion

        #region 4.0 查询结果集，返回的是首行首列 
        /// <summary>
        /// 4.0 查询结果集，返回的是首行首列
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ps">参数数组</param>
        /// <returns></returns>
        public static object ExecScalar(string sql, params SqlParameter[] ps)  //调用的时候才判断是什么类型
        {
            using (SqlConnection conn = new SqlConnection(GetConnStr()))
            {
                conn.Open();
                SqlCommand command = new SqlCommand(sql, conn);
                if (ps != null)
                    command.Parameters.AddRange(ps);
                return command.ExecuteScalar();
            }
        }
        #endregion
    }
}
