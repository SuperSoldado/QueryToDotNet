using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Text;

namespace QueryToDotNet
{
    /// <summary>
    /// Author: Freddy Ullrich
    /// This class is used for DAO classes to fill the "SomethingInfo" with the data reader. 
    /// This class may cause a lot of bugs in future because of difficult to sychronize 
    /// the database data with .Net data, expecialy trating nullables.
    /// How it works: it maps the query "Select col1,col2,col3 from table1" to the info class. So, there must be
    /// a "Table1" class to do "Table1.col1 = (int)reader["col1"]"  Supposing it is a "int" type.
    /// </summary>
    public class ClassFiller
    {
        /// <summary>
        /// Reader used to get the data.
        /// </summary>
        private DbDataReader dbReader;

        /// <summary>
        /// List the columns used in the "select a,b,c..." 
        /// </summary>
        private List<string> ListColumnsInQuery = null;

        /// <summary>
        /// List of properties that the class has, but the query don't. 
        /// </summary>
        List<string> ListDontProcess = null;

        /// <summary>
        /// List of properties that the class has.
        /// </summary>
        PropertyInfo[] ListClassInfoProperties = null;

        private Type Type = null;

        public ClassFiller(Type ClassInfoType, DbDataReader dbReader)
        {
            this.Type = ClassInfoType;
            this.dbReader = dbReader;
            ListColumnsInQuery = GetColumnsInsideQuery(dbReader);
            ListClassInfoProperties = ClassInfoType.GetProperties();
            ListDontProcess = GetDontProcess(ListColumnsInQuery, ListClassInfoProperties);
        }
        ///// <summary>
        ///// Contains the ist of properties (Ex.: MyClass.MyProperty1) that are not in the reader object. 
        ///// Sometimes some properties are not in database and are for calculations or other purposes.
        ///// </summary>
        //private List<string> DontProcess = new List<string>();
        //private List<string> ColumnsInQuery = new List<string>();
        private List<string> GetDontProcess(List<string> ColumnsInQuery, System.Reflection.PropertyInfo[] properties)
        {
            List<string> DontProcess = new List<string>();
            foreach (PropertyInfo property in properties)
            {
                bool found = false;
                foreach (string s in ColumnsInQuery)
                {
                    if (s.ToUpper() == property.Name.ToUpper())
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    DontProcess.Add(property.Name);
                }
            }
            return DontProcess;
        }

        public bool ColumnExists(DbDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Fill a basic "Info" class with the data in DataReader.
        ///
        /// WARNING: Due to reflection process, the "ClassFiller" class may result overwhelming CPU process 
        /// Should consider dump the data using static access. How replace classFiller: comment the line  
        /// "classFiller.Fill(MyClassInfo);" and manually add the parameters. Example:
        /// myClassInfoOvr = new myClassInfoOvr();
        /// myClassInfoOvr.MyNumber =  (int)dbReader["MyNumber"];
        /// </summary>             
        /// <param name="classInfo">Class to be filled</param>        
        /// <param name="reader">Data Reader with all the data</param>
        public void Fill(object classInfo/*, DbDataReader reader*/)
        {
            string castWarning = "";
            try
            {
                foreach (PropertyInfo property in ListClassInfoProperties)
                {
                    if (ListDontProcess.Contains(property.Name))
                    {
                        continue;
                    }


                    /*bool columnInDBExists = ColumnExists(dbReader, property.Name);
                    if (!columnInDBExists)
                    {
                        continue;
                    }*/

                    castWarning = TypeConversionWarning(classInfo, property, dbReader);

                    if (!PropertyExistsInReader(property.Name))
                        continue;

                    //New code: for nullable stuff... damn, this sucks!
                    Type t = property.PropertyType;
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        t = t.GetGenericArguments()[0];
                    }


                    if (t.BaseType == typeof(System.Enum))
                    {
                        //Enters here when parse products.MainState
                        FillEnum(classInfo, t, property, dbReader);
                        continue;
                    }
                    if (t == typeof(String))
                    {
                        FillString(classInfo, property, dbReader);
                        continue;
                    }

                    if (t == typeof(Int16))
                    {
                        FillShort(classInfo, property, dbReader);
                        continue;
                    }

                    if (t == typeof(Int32))
                    {
                        FillInt(classInfo, property, dbReader);
                        continue;
                    }

                    if (t == typeof(Int64))
                    {
                        FillInt64(classInfo, property, dbReader);
                        continue;
                    }

                    if (t == typeof(Decimal))
                    {
                        FillDecimal(classInfo, property, dbReader);
                        continue;
                    }

                    if (t == typeof(Double))
                    {
                        FillDouble(classInfo, property, dbReader);
                        continue;
                    }

                    if (t == typeof(DateTime))
                    {
                        FillDateTime(classInfo, property, dbReader);
                        continue;
                    }

                    if (t == typeof(bool))
                    {
                        FillBool(classInfo, property, dbReader);
                        continue;
                    }

                    if (t == typeof(Guid))
                    {
                        FillGuid(classInfo, property, dbReader);
                        continue;
                    }

                    if (t == typeof(byte[]))
                    {
                        FillByteArray(classInfo, property, dbReader);
                        continue;
                    }

                    throw new Exception("Type whithout implementation: " + property.Name + " at ClassFiller.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error during parse Database->Class: " + ex.Message + " - " + castWarning);
            }
        }

        private void FillEnum(object classInfo, Type t, PropertyInfo property, DbDataReader dbReader)
        {
            if (!(dbReader[property.Name] is System.DBNull))
            {
                var valueInDataBase = dbReader[property.Name];

                var enumType = Type.GetType(t.FullName);
                if (enumType.IsEnum)
                {
                    var enumReflectedFromDBData = Enum.ToObject(enumType, valueInDataBase);
                    property.SetValue(classInfo, enumReflectedFromDBData);
                }
            }
            else
            {
                property.SetValue(classInfo, null);
            }
        }

        public void FillField(object classInfo)
        {
            try
            {
                FieldInfo[] fields = this.Type.GetFields();



                foreach (FieldInfo field in fields)
                {
                    if (!PropertyExistsInReader(field.Name))
                        continue;

                    //New code: for nullable stuff... damn, this sucks!
                    Type t = field.FieldType;
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        t = t.GetGenericArguments()[0];
                    }

                    if (t == typeof(String))
                    {
                        FillString(classInfo, field, dbReader);
                        continue;
                    }

                    if (t == typeof(Int32))
                    {
                        FillInt(classInfo, field, dbReader);
                        continue;
                    }

                    if (t == typeof(Int64))
                    {
                        FillInt64(classInfo, field, dbReader);
                        continue;
                    }

                    if (t == typeof(Decimal))
                    {
                        FillDecimal(classInfo, field, dbReader);
                        continue;
                    }

                    if (t == typeof(Double))
                    {
                        FillDouble(classInfo, field, dbReader);
                        continue;
                    }

                    if (t == typeof(DateTime))
                    {
                        FillDateTime(classInfo, field, dbReader);
                        continue;
                    }

                    if (t == typeof(bool))
                    {
                        FillBool(classInfo, field, dbReader);
                        continue;
                    }

                    if (t == typeof(byte[]))
                    {
                        FillByteArray(classInfo, field, dbReader);
                        continue;
                    }

                    if (t == typeof(Guid))
                    {
                        FillGuid(classInfo, field, dbReader);
                        continue;
                    }

                    throw new Exception("Type whithout implementation: " + field.Name + " at ClassFiller.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error during parse Database->Class: " + ex.Message);
            }
        }

        /// <summary>
        /// Find is the myClassInfo.MyProperty existis inside a reader (select MyProperty,XPTO from myClass)
        /// </summary>
        /// <param name="dbReader">Reader used</param>
        /// <param name="nameInDataReader">Name of the column insede reader.</param>
        /// <returns></returns>
        public bool PropertyExistsInReader(string nameInDataReader)
        {
            foreach (string s in ListDontProcess)
            {
                if (s == nameInDataReader)
                    return false;
            }
            return true;
        }


        private List<string> GetColumnsInsideQuery(DbDataReader dbReader)
        {
            System.Data.DataTable schemaTable;
            //Retrieve column schema into a DataTable.
            schemaTable = dbReader.GetSchemaTable();
            List<string> dbReaderColumns = new List<string>();
            foreach (System.Data.DataRow myField in schemaTable.Rows)
            {
                //For each property of the field...
                foreach (System.Data.DataColumn myProperty in schemaTable.Columns)
                {
                    //Display the field name and value.
                    if (myProperty.ColumnName == "ColumnName")
                    {
                        dbReaderColumns.Add(myField[myProperty].ToString());
                        break;
                    }
                }
            }
            return dbReaderColumns;
        }

        private string TypeConversionWarning(object classInfo, PropertyInfo property, DbDataReader reader)
        {
            string warning = "";
            Type readerType = GetReaderType(property, reader);
            Type myTranferClassType = property.GetType();

            string strTypeDBReader = readerType.ToString();
            string strTypeTransferClass = property.PropertyType.ToString();

            if (strTypeDBReader != strTypeTransferClass)
            {
                warning = string.Format("Warning: DbData reader generated type '{0}' for column '{1}' . You are trying to cast to type '{2}' for class '{3}'. Hint: set '{3}.{1}' to '{0}'.",
                   strTypeDBReader, property.Name, strTypeTransferClass, classInfo.GetType());
            }

            return warning;
        }

        /// <summary>
        /// Search the reader for column "property.name" and return the .net type created from reader.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        private Type GetReaderType(PropertyInfo property, DbDataReader reader)
        {
            return reader[property.Name].GetType();
        }

        private void FillInt(object classInfo, PropertyInfo property, DbDataReader reader)
        {
            if (!(reader[property.Name] is System.DBNull))
            {
                property.SetValue(classInfo, (int)reader[property.Name]);
            }
            else
            {
                property.SetValue(classInfo, null);
            }
        }

        private void FillShort(object classInfo, PropertyInfo property, DbDataReader reader)
        {
            if (!(reader[property.Name] is System.DBNull))
            {
                property.SetValue(classInfo, (short)reader[property.Name]);
            }
            else
            {
                property.SetValue(classInfo, null);
            }
        }

        private void FillGuid(object classInfo, PropertyInfo property, DbDataReader reader)
        {
            if (!(reader[property.Name] is System.DBNull))
            {
                property.SetValue(classInfo, (Guid)reader[property.Name]);
            }
            else
            {
                property.SetValue(classInfo, Guid.Empty);
            }
        }

        private void FillBool(object classInfo, PropertyInfo property, DbDataReader reader)
        {
            if (!(reader[property.Name] is System.DBNull))
            {
                property.SetValue(classInfo, (bool)reader[property.Name]);
            }
            else
            {
                property.SetValue(classInfo, null);
            }
        }

        private void FillByteArray(object classInfo, PropertyInfo property, DbDataReader reader)
        {
            if (!(reader[property.Name] is System.DBNull))
            {
                property.SetValue(classInfo, (byte[])reader[property.Name]);
            }
            else
            {
                property.SetValue(classInfo, null);
            }
        }

        private void FillInt64(object classInfo, PropertyInfo property, DbDataReader reader)
        {
            if (!(reader[property.Name] is System.DBNull))
            {
                property.SetValue(classInfo, (Int64)reader[property.Name]);
            }
            else
            {
                property.SetValue(classInfo, null);
            }
        }

        private void FillDecimal(object classInfo, PropertyInfo property, DbDataReader reader)
        {
            if (!(reader[property.Name] is System.DBNull))
            {
                //When comes a Double value from Reader (Ex.: when theres a column like "float" in SQlServer) must convert. 
                //Could not make work dynamic cast.... :-(
                decimal z = Convert.ToDecimal(reader[property.Name]);
                property.SetValue(classInfo, z);
            }
            else
            {
                property.SetValue(classInfo, null);
            }
        }

        private void FillDouble(object classInfo, PropertyInfo property, DbDataReader reader)
        {
            if (!(reader[property.Name] is System.DBNull))
            {
                //When comes a Double value from Reader (Ex.: when theres a column like "float" in SQlServer) must convert. 
                //Could not make work dynamic cast.... :-(
                double z = Convert.ToDouble(reader[property.Name]);
                property.SetValue(classInfo, z);
            }
            else
            {
                property.SetValue(classInfo, null);
            }
        }

        private void FillDateTime(object classInfo, PropertyInfo property, DbDataReader reader)
        {
            if (!(reader[property.Name] is System.DBNull))
            {
                property.SetValue(classInfo, (DateTime)reader[property.Name]);
            }
            else
            {
                property.SetValue(classInfo, null);
            }
        }

        private void FillString(object classInfo, PropertyInfo property, DbDataReader reader)
        {
            if (!(reader[property.Name] is System.DBNull))
            {
                property.SetValue(classInfo, (string)reader[property.Name]);
            }
            else
            {
                property.SetValue(classInfo, null);
            }
        }

        //-----
        private void FillInt(object classInfo, FieldInfo property, DbDataReader reader)
        {
            if (!(reader[property.Name] is System.DBNull))
            {
                property.SetValue(classInfo, (int)reader[property.Name]);
            }
            else
            {
                property.SetValue(classInfo, null);
            }
        }

        private void FillBool(object classInfo, FieldInfo property, DbDataReader reader)
        {
            if (!(reader[property.Name] is System.DBNull))
            {
                property.SetValue(classInfo, (bool)reader[property.Name]);
            }
            else
            {
                property.SetValue(classInfo, null);
            }
        }

        private void FillByteArray(object classInfo, FieldInfo property, DbDataReader reader)
        {
            if (!(reader[property.Name] is System.DBNull))
            {
                property.SetValue(classInfo, (byte[])reader[property.Name]);
            }
            else
            {
                property.SetValue(classInfo, null);
            }
        }

        private void FillInt64(object classInfo, FieldInfo property, DbDataReader reader)
        {
            if (!(reader[property.Name] is System.DBNull))
            {
                property.SetValue(classInfo, (Int64)reader[property.Name]);
            }
            else
            {
                property.SetValue(classInfo, null);
            }
        }

        private void FillDecimal(object classInfo, FieldInfo property, DbDataReader reader)
        {
            if (!(reader[property.Name] is System.DBNull))
            {
                //When comes a Double value from Reader (Ex.: when theres a column like "float" in SQlServer) must convert. 
                //Could not make work dynamic cast.... :-(
                decimal z = Convert.ToDecimal(reader[property.Name]);
                property.SetValue(classInfo, z);
            }
            else
            {
                property.SetValue(classInfo, null);
            }
        }

        private void FillGuid(object classInfo, FieldInfo property, DbDataReader reader)
        {
            if (!(reader[property.Name] is System.DBNull))
            {
                //When comes a Double value from Reader (Ex.: when theres a column like "float" in SQlServer) must convert. 
                //Could not make work dynamic cast.... :-(
                Guid z = (Guid)reader[property.Name];
                property.SetValue(classInfo, z);
            }
            else
            {
                property.SetValue(classInfo, null);
            }
        }

        private void FillDouble(object classInfo, FieldInfo property, DbDataReader reader)
        {
            if (!(reader[property.Name] is System.DBNull))
            {
                //When comes a Double value from Reader (Ex.: when theres a column like "float" in SQlServer) must convert. 
                //Could not make work dynamic cast.... :-(
                double z = Convert.ToDouble(reader[property.Name]);
                property.SetValue(classInfo, z);
            }
            else
            {
                property.SetValue(classInfo, null);
            }
        }

        private void FillDateTime(object classInfo, FieldInfo property, DbDataReader reader)
        {
            if (!(reader[property.Name] is System.DBNull))
            {
                property.SetValue(classInfo, (DateTime)reader[property.Name]);
            }
            else
            {
                property.SetValue(classInfo, null);
            }
        }

        private void FillString(object classInfo, FieldInfo property, DbDataReader reader)
        {
            if (!(reader[property.Name] is System.DBNull))
            {
                property.SetValue(classInfo, (string)reader[property.Name]);
            }
            else
            {
                property.SetValue(classInfo, null);
            }
        }
    }
}
