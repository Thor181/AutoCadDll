using Autodesk.AutoCAD.Runtime;
using System.IO;

namespace ACDll
{
    public class Commands : IExtensionApplication
    {
        public void Initialize()
        {
            
        }

        public void Terminate()
        {
            File.Delete(Config.GetLogFileName());
        }
        [CommandMethod("SPLIT_REPAINT")]
        public void StartPlugin()
        {
            new MainWindow().ShowDialog();
        }
    }
}
