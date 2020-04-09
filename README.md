# QueryToDotNet
 Convert MySqlQuery to List<MyClass>
 
Do you whant to convert "YourSQLQuery" to "List<YourDotNetClass>"? Here is the simplest way in world. Usage:

GenericQuery genericQuery = new GenericQuery(myConnectionString);
List<MyClass> resultFromDB = (List<MyClass>)genericQuery.GetData(myQuery, typeof(MyClass));

And thats all. 
