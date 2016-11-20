using Autodesk.Revit.DB;
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
    /// Interaction logic for RoomRelationship.xaml
    /// </summary>
    public partial class RoomRelationship : Window
    {
        GraphGenerator graphGenerator;
        public RoomRelationship(GraphGenerator gg)
        {
            InitializeComponent();
            graphGenerator = gg;
            foreach(ExitData ed in gg.Exits)
            {
                ListViewItem lbi = new ListViewItem();
                StackPanel sp = new StackPanel();
                sp.Orientation = Orientation.Horizontal;
                List<object> tlist = new List<object>();
                tlist.Add(ed);
                {
                    TextBlock a = new TextBlock();
                    a.Text = ed.Rooms[0].Number;
                    TextBox valuer = new TextBox();
                    valuer.Text = ed.Weight2.ToString();
                    valuer.Width = 30;
                    TextBlock b = new TextBlock();
                    b.Text = ed.Rooms[1].Number;
                    sp.Children.Add(a);
                    sp.Children.Add(valuer);
                    sp.Children.Add(b);
                    tlist.Add(valuer);
                }
                {
                    TextBlock a = new TextBlock();
                    a.Text = ed.Rooms[1].Number;
                    TextBox valuer = new TextBox();
                    valuer.Text = ed.Weight1.ToString();
                    valuer.Width = 30;
                    TextBlock b = new TextBlock();
                    b.Text = ed.Rooms[0].Number;
                    sp.Children.Add(a);
                    sp.Children.Add(valuer);
                    sp.Children.Add(b);
                    tlist.Add(valuer);
                }
                lbi.Tag = tlist;
                lbi.Content = sp;
                lister.Items.Add(lbi);
            }
        }

        private void ok_button_Click(object sender, RoutedEventArgs e)
        {
            bool hasexc = false;
            foreach (ListBoxItem lbi in lister.Items)
            {
                try
                {
                    List<object> tb = (List<object>)lbi.Tag;
                    float val = float.Parse(((TextBox)tb[1]).Text);
                    float val2 = float.Parse(((TextBox)tb[2]).Text);
                    ExitData data = ((ExitData)tb[0]);
                    data.Weight2 = val;
                    data.Weight1 = val2;
                }
                catch (Exception k)
                {
                    hasexc = true;
                    MessageBox.Show(k.StackTrace);
                    MessageBox.Show($"Wrong format for floating number, try again.");
                }
            }
            hasexc = hasexc && !checkWeights();
            if (!hasexc)
                this.DialogResult = true;

        }

        bool checkWeights()
        {
            return true;
        }

        public string GetGraphData()
        {
            string res = "<RoomsExits>";
            foreach (RoomData rd in graphGenerator.Rooms)
            {
                res += "<Room>";
                res += "<Location>";
                res += $@"
<x>{rd.Position.Y}</x>
<y>{rd.Position.Z}</y>
<z>{rd.Position.X}</z>
</Location>
<Number>{rd.Number}</Number>
<Area>{rd.Area}</Area>";
                foreach(XYZ xy in rd.Vertices)
                {
                    res += "<Vertex>";
                    res += $@"
<x>{xy.Y}</x>
<y>{xy.Z}</y>
<z>{xy.X}</z></Vertex>
";
                }
                res += " </Room>";
            }
            int num = 1;
            foreach(ExitData ed in graphGenerator.Exits)
            {
                res += "<Exit>";
                res += $"<Number>exit{num}</Number>";
                res += "<Normal>";
                res += $"<x>{ed.Normal.Y}</x><y>{ed.Normal.Z}</y><z>{ed.Normal.X}</z>";
                res += "</Normal>";
                res += "<Location>";
                res += $@"
<x>{ed.Position.Y}</x>
<y>{ed.Position.Z}</y>
<z>{ed.Position.X}</z>";
                res += " </Location>"; 
                foreach(RoomData rr in ed.Rooms)
                {
                    res += "<Room>";
                    res += "<Number>";
                    res += rr.Number;
                    res += "</Number>";
                    res += "<Weight>" + (rr == ed.Rooms[0] ? ed.Weight1 : ed.Weight2) + "</Weight>";
                    res += "</Room>";
                }
                res += "</Exit>";
                num++;
            }
            res += "</RoomsExits>";
            return res;
        }
    }
}
/*<Exits>";
                foreach (ExitData ed in rd.Exits)
                {
                    res += $@"
<Exit>
<Location>
<x>{ed.Position.X}</x>
<y>{ed.Position.Y}</y>
<z>{ed.Position.Z}</z>
</Location>
<Weight>{ed.Weight}</Weight>
</Exit>";
                }
                res += "</Exits>";
                res += "<RelatedRooms>";
                foreach (ExitData ed in rd.Exits)
                {
                    if (default(KeyValuePair<RoomData, RoomData>).Equals(ed.Direction))
                    {
                        res += "<RelatedRoom>";
                        res += $"<weight>{ed.Weight}</weight>";
                        if (ed.Rooms.Count == 1)
                        {
                            res += $"<Number>Outside</Number>";
                        }
                        else
                        {
                            RoomData nearby = ed.Rooms[0] == rd ? ed.Rooms[1] : ed.Rooms[0];
                            res += $"<Number>" + nearby.Number +"</Number>";
                            res += "<Location>";
                            res += $@"
<x>{nearby.Position.X}</x>
<y>{nearby.Position.Y}</y>
<z>{nearby.Position.Z}</z>
</Location>
<Area>{rd.Area}</Area>";
                        }
                        res += " </RelatedRoom>";
                    }
                    else
                    {
                        res += "<RelatedRoom>";
                        res += $"<weight>{ed.Weight}</weight>";
                        if (ed.Rooms.Count == 1)
                        {
                            res += $"<Number>Outside</Number>";
                        }
                        else
                        {
                            if (ed.Rooms[0] == rd)
                            {
                                RoomData nearby = ed.Rooms[1];
                                res += $"<Number>" + nearby.Number + "</Number>";
                                res += "<Location>";
                                res += $@"
<x>{nearby.Position.X}</x>
<y>{nearby.Position.Y}</y>
<z>{nearby.Position.Z}</z>
</Location>
<Area>{rd.Area}</Area>";
                            }
                        }
                        res += " </RelatedRoom>";

                    }
                }
                res += "</RelatedRooms>";
*/