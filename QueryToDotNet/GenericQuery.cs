using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace QueryToDotNet
{
    /// <summary>
    /// Athor: Freddy Ullrich
    /// Convert query to .net class list
    /// </summary>
    public class GenericQuery
    {

        public GenericQuery(string connectionStringSystem)
        {
            this.connectionString = connectionStringSystem;
        }

        private string connectionString = "";

        /// <summary>
        /// Give me one "SQL query" and one "class type" and return a list<class type>
        /// Example:
        /// GenericQuery d = new GenericQuery();
        /// List<MyObject> l1 = new List<MyObject>();
        /// l1 = (List<MyObject>)d.GetData("select 1 as col1, 'abc' as col2", typeof(MyObject));
        /// </summary>
        /// <param name="query">SQL query</param>
        /// <param name="type">Type with fields that mach query</param>
        /// <returns>List of data</returns>
        public IList GetData(string queryString, Type type)
        {
            Type customList = typeof(List<>).MakeGenericType(type);
            IList objectList = (IList)Activator.CreateInstance(customList);

            using (SqlConnection connection =
            new SqlConnection(connectionString))
            {
                // Create the Command and Parameter objects.
                SqlCommand command = new SqlCommand(queryString, connection);

                // Open the connection in a try/catch block. 
                // Create and execute the DataReader, writing the result
                // set to the console window.
                try
                {
                    connection.Open();
                    SqlDataReader dbReader = command.ExecuteReader();

                    ClassFiller classFiller = new ClassFiller(type, dbReader);
                    using (dbReader)
                    {
                        while (dbReader.Read())
                        {
                            var obj = Activator.CreateInstance(type);
                            classFiller.Fill(obj);
                            objectList.Add(obj);
                        }
                    }

                    dbReader.Close();
                }
                catch (Exception ex)
                {
                    throw (ex);
                }
            }

            return objectList;
        }
    }
}
