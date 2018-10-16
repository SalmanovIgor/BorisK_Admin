using log4net;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace BorisK_Admin.Models
{
    public class FileUploadModel
    {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(FileUploadModel));
        public bool Upload(string originalFileName, string fileId, DataTable table, string Emails)
        {

            try
            {
                using (SqlConnection _connection = new SqlConnection(ConfigurationManager.ConnectionStrings["_DBConnection"].ConnectionString))
                {
                    _connection.Open();
                    using (SqlCommand _command = new SqlCommand("dbo.API_FileTrackings_Add"))
                    {
                        _command.CommandType = CommandType.StoredProcedure;
                        _command.CommandTimeout = 300;
                        _command.Connection = _connection;
                        _command.Parameters.Add(new SqlParameter("@fileName", SqlDbType.VarChar, 1000) { Value = originalFileName });
                        _command.Parameters.Add(new SqlParameter("@fileId", SqlDbType.VarChar, 50) { Value = fileId });
                        _command.Parameters.Add(new SqlParameter("@email", SqlDbType.VarChar, 1000) { Value = Emails });
                        _command.ExecuteNonQuery();
                    }

                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(_connection))
                    {
                        bulkCopy.DestinationTableName = "dbo.ISBNs";
                        bulkCopy.BulkCopyTimeout = 1000;
                        bulkCopy.BatchSize = 1000;
                        bulkCopy.ColumnMappings.Add("FileID", "FileID");
                        bulkCopy.ColumnMappings.Add("ISBN", "ISBN");

                        try
                        {
                            bulkCopy.WriteToServer(table);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("EXCEPTION : Failed to load data in ISBN table: " + ex.Message);
                        }

                        if (_connection.State == ConnectionState.Open)
                            _connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("EXCEPTION : Failed to store data in file table: " + ex.Message);
                return false;
            }
            return true;
        }
    }
}