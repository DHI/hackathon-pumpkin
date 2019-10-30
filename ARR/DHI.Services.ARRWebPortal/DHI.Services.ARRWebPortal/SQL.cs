using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHI.Services.ARRWebPortal
{
    public class SQL
    {
        public static IDataParameter CreateParameter(IDbCommand command, string name, DbType dbtype, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.DbType = dbtype;
            if (value is Guid)
            {
                parameter.Value = (Guid)value;
            }
            else
            {
                parameter.Value = value ?? DBNull.Value;
            }
            command.Parameters.Add(parameter);
            return parameter;
        }

        public static IDbCommand CreateCommand(string sql, IDbConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            return command;
        }

        public static IDbConnection CreateConnection()
        {
            string connectionString = Definition.ConnectionString;
            //string connectionString = "Server=127.0.0.1;Port=5432;Database=arrweb;User Id=postgres;Password=Solutions!;";
            return new NpgsqlConnection(connectionString);
        }
    }
}