using Autodesk.AutoCAD.DatabaseServices;
using System.Windows.Controls;

namespace ACDll
{
    public partial class LayerPickerControl : UserControl
    {
        public string LayerName { get; set; }
        public DBObject DBObject { get; set; }
        public bool Checked { get; set; }

        public LayerPickerControl()
        {
            InitializeComponent();
            LayerControlStackPanel.DataContext = this;
        }

        public LayerPickerControl(string Name) : this()
        {
            LayerName = Name;
        }
        public LayerPickerControl(DBObject dBObject) : this()
        {
            if (dBObject is LayerTableRecord layerTableRecord)
            {
                DBObject = dBObject;
                LayerName = layerTableRecord.Name;
            }
        }
    }
}
