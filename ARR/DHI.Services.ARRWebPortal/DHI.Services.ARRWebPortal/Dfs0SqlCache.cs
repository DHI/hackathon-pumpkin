using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHI.Services.ARRWebPortal
{
    public class Dfs0SqlCache
    {
        public static string GetDfs0(string userId, string name, string type)
        {
            using (var connection = SQL.CreateConnection())
            {
                connection.Open();

                using (var command = SQL.CreateCommand("select data from public.arrweb_results where userid=@userid and name=@name and type=@type and date=@date", connection))
                {
                    SQL.CreateParameter(command, "@userid", DbType.String, userId);
                    SQL.CreateParameter(command, "@name", DbType.String, name);
                    SQL.CreateParameter(command, "@type", DbType.String, type);
                    SQL.CreateParameter(command, "@date", DbType.Date, DateTime.Now.Date);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.GetValue(0) is DBNull ? string.Empty : reader.GetString(0);
                        }
                    }
                    return string.Empty;
                }
            }
        }

        public static void SetDfs0(string userId, string name, string type, string data)
        {
            using (var connection = SQL.CreateConnection())
            {
                connection.Open();

                try
                {
                    using (var command = SQL.CreateCommand("INSERT INTO public.arrweb_results(userid, name, type, data, date) VALUES (@userid, @name, @type, @data, @date)", connection))
                    {
                        SQL.CreateParameter(command, "@userid", DbType.String, userId);
                        SQL.CreateParameter(command, "@name", DbType.String, name);
                        SQL.CreateParameter(command, "@type", DbType.String, type);
                        SQL.CreateParameter(command, "@data", DbType.String, data);
                        SQL.CreateParameter(command, "@date", DbType.Date, DateTime.Now.Date);
                        command.ExecuteNonQuery();
                    }
                }
                catch (NpgsqlException npgsqlException)
                {
                    using (var command = SQL.CreateCommand("update public.arrweb_results set data=@data where userid=@userid and name=@name and type=@type and date=@date", connection))
                    {
                        SQL.CreateParameter(command, "@userid", DbType.String, userId);
                        SQL.CreateParameter(command, "@name", DbType.String, name);
                        SQL.CreateParameter(command, "@type", DbType.String, type);
                        SQL.CreateParameter(command, "@data", DbType.String, data);
                        SQL.CreateParameter(command, "@date", DbType.Date, DateTime.Now.Date);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
