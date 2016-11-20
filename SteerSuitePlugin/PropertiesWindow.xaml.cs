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

namespace SteerSuitePlugin
{
    /// <summary>
    /// Interaction logic for PropertiesWindow.xaml
    /// </summary>
    public partial class PropertiesWindow : Window
    {
        public PropertiesWindow(string xtitle, params Property[] properties)
        {
            InitializeComponent();
            this.Title += $" - {xtitle}";
            dataGrid.ItemsSource = properties;
        }

        private void ok_button_Click(object sender, RoutedEventArgs e)
        {
            bool hasexc = false;
            foreach (Property r in dataGrid.Items)
            {
                try
                {
                    if (typeof(int) == r.Type)
                    {
                        Int32.Parse(r.Value);
                    }
                    if (typeof(double) == r.Type)
                    {
                        double.Parse(r.Value);
                    }
                    else
                    {
                        
                    }
                }
                catch (Exception k)
                {
                    hasexc = true;
                    MessageBox.Show(k.Message);
                    MessageBox.Show($"Wrong format for {r.Name}, try again.");
                }
            }
            if (!hasexc)
                this.DialogResult = true;
        }

        public Dictionary<string, dynamic> GetValues()
        {
            Dictionary<string, dynamic> res = new Dictionary<string, dynamic>();
            foreach (Property r in dataGrid.Items)
            {
                try
                {
                    if (typeof(int) == r.Type)
                    {
                        res.Add(r.Name, Int32.Parse(r.Value));
                    }
                    else if (typeof(double) == r.Type)
                    {
                        res.Add(r.Name, double.Parse(r.Value));
                    }
                    else
                    {
                        res.Add(r.Name, (r.Value));
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    MessageBox.Show($"Wrong format for {r.Name}, try again.");
                }
            }
            return res;
        }


    }
}
