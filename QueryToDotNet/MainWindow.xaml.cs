using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QueryToDotNet
{
    public class MyClass
    {
        public string MyName { get; set; }
        public DateTime MyDate { get; set; }
        public Decimal MyDecimal { get; set; }
    }

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            txtQuery.Text = "select 'Hello' as MyName, GetDate() as MyDate, 1.34 as MyDecimal " + Environment.NewLine +
                "union " + Environment.NewLine +
                "select 'Mamma Lauda' as MyName, GetDate() as MyDate, 987.88 as MyDecimal " + Environment.NewLine +
                "union " + Environment.NewLine +
                "select 'Nicky Lauda' as MyName, GetDate() as MyDate, 1589.45 as MyDecimal " + Environment.NewLine;

            txtConnectionString.Text = "Server=WIN16\\SQLEXPRESS01;Database=Northwind;User Id=sa; Password=sa;";
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            List<MyClass> myList = new List<MyClass>();
            MyClass myClass1 = new MyClass();
            myClass1.MyName = "Freddy";
            MyClass myClass2 = new MyClass();
            myClass2.MyName = "Freddy2";
            MyClass myClass3 = new MyClass();
            myClass3.MyName = "Freddy3";

            myList.Add(myClass1);
            myList.Add(myClass2);
            myList.Add(myClass3);
            //myGrid.ItemsSource = myList;

            GenericQuery d = new GenericQuery(txtConnectionString.Text);
            List<MyClass> resultFromDB = (List<MyClass>)d.GetData(txtQuery.Text, typeof(MyClass));
            myGrid.ItemsSource = resultFromDB;
        }
    }
}
