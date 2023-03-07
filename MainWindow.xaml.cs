using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Media;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Linq;
using System.Diagnostics;

namespace ACDll
{
    public partial class MainWindow : Window
    {
        Document acDoc = Application.DocumentManager.MdiActiveDocument;
        Database acCurrentDb;
        CancellationTokenSource cancellationTokenSourceOuter;
        private static List<string> _layerErrors = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            acCurrentDb = acDoc.Database;
            foreach (var item in LayersToList())
            {
                MainStackPanel.Children.Add(new LayerPickerControl(item));
            }
        }

        public List<DBObject> LayersToList()
        {
            List<DBObject> listLayers = new List<DBObject>();
            DBObjectCollection dBObjectCollection = new DBObjectCollection();
            LayerTableRecord layer;
            using (Transaction tr = acCurrentDb.TransactionManager.StartOpenCloseTransaction())
            {
                LayerTable lt = tr.GetObject(acCurrentDb.LayerTableId, OpenMode.ForRead) as LayerTable;
                foreach (ObjectId layerId in lt)
                {
                    layer = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;
                    listLayers.Add(layer);
                }
            }
            return listLayers;
        }

        private void CheckAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (LayerPickerControl item in MainStackPanel.Children)
            {
                item.LayerControlCheckbox.IsChecked = ((ToggleButton)sender).IsChecked;
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            _ = StartAsDistributedTasks(sender);
        }

        private async Task StartAsDistributedTasks(object startButton)
        {
            List<ObjectIdCollection> objectIdCollections = new List<ObjectIdCollection>();
            cancellationTokenSourceOuter = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSourceOuter.Token;
            ChangeButtonStatus();
            var collectionLayers = CollectLayerPickersIsChecked();
            ProgressBarWorker.ProgressBarsReset(ProgressBarLayers, ProgressBarEntities);
            ProgressBarWorker.ProgressBarInitialize(ProgressBarLayers, collectionLayers.Count);
            foreach (var item in collectionLayers)
            {
                await ExplodeSaveColor(GetByLayer(item), token);
                ProgressBarWorker.ProgressBasIncrement(ProgressBarLayers, Dispatcher);
            }
            MessageBox.Show("Завершено!");
            ChangeButtonStatus();
            ColorizeLayerNames();
        }

        private void ColorizeLayerNames()
        {
            foreach (LayerPickerControl item in MainStackPanel.Children)
            {
                if (item.Checked)
                {
                    if (_layerErrors.Contains(item.LayerName))
                    {
                        item.LayerControlTextblock.Foreground = new SolidColorBrush(Colors.Red);
                    }
                    else
                    {
                        item.LayerControlTextblock.Foreground = new SolidColorBrush(Colors.LightGreen);
                    }
                }
            }
            _layerErrors.Clear();
        }

        public ObjectIdCollection GetByLayer(string layerName)
        {
            var ids = new ObjectIdCollection();
            using (var tr = acCurrentDb.TransactionManager.StartOpenCloseTransaction())
            {
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(acCurrentDb), OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    var ent = (Entity)tr.GetObject(id, OpenMode.ForRead);
                    if (ent.Layer.Equals(layerName, StringComparison.CurrentCultureIgnoreCase))
                        ids.Add(id);
                }
                tr.Commit();
            }
            return ids;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSourceOuter.Cancel();
            ChangeButtonStatus();
        }

        private void ChangeButtonStatus()
        {
            StartButton.Visibility = StartButton.Visibility == System.Windows.Visibility.Visible ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            StopButton.Visibility = StopButton.Visibility == System.Windows.Visibility.Visible ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }

        private List<string> CollectLayerPickersIsChecked()
        {
            List<string> listLayerNames = new List<string>();
            foreach (LayerPickerControl item in MainStackPanel.Children)
            {
                if (item.LayerControlCheckbox.IsChecked == true)
                {
                    listLayerNames.Add(item.LayerName);
                }
            }
            return listLayerNames;
        }

        private async Task<bool> ExplodeSaveColor(ObjectIdCollection objectIdCollection, CancellationToken token)
        {
            ProgressBarWorker.ProgressBarInitialize(ProgressBarEntities, objectIdCollection.Count);
            bool resultIsGood = false;
            await Task.Run(() =>
            {
                using (DocumentLock lockDocument = acDoc.LockDocument())
                {
                    using (Transaction tr = acCurrentDb.TransactionManager.StartOpenCloseTransaction())
                    {
                        try
                        {
                            DBObjectCollection dBObjectCollection = new DBObjectCollection();
                            foreach (ObjectId itemOfObjectIdCollection in objectIdCollection)
                            {
                                ProgressBarWorker.ProgressBasIncrement(ProgressBarEntities, Dispatcher);
                                if (token.IsCancellationRequested)
                                {
                                    MessageBox.Show("Операция прервана");
                                    tr.Abort();
                                    token.ThrowIfCancellationRequested();
                                    cancellationTokenSourceOuter.Dispose();
                                    return;
                                }
                                var entity = (Entity)tr.GetObject(itemOfObjectIdCollection, OpenMode.ForRead);
                                if (!EntityCanExplode(entity))
                                {
                                    var entityForWrite = (Entity)tr.GetObject(itemOfObjectIdCollection, OpenMode.ForWrite);
                                    ChangeColorEntity(entity);
                                    continue;
                                }
                                //var layerName = entity.Layer;

                                entity.Explode(dBObjectCollection);
                                entity.UpgradeOpen();
                                entity.Erase();
                            }
                            BlockTableRecord blockTableRecord = (BlockTableRecord)tr.GetObject(acCurrentDb.CurrentSpaceId, OpenMode.ForWrite);
                            foreach (Entity entity in dBObjectCollection)
                            {
                                if (token.IsCancellationRequested)
                                {
                                    MessageBox.Show("Операция прервана");
                                    tr.Abort();
                                    token.ThrowIfCancellationRequested();
                                    cancellationTokenSourceOuter.Dispose();
                                    return;
                                }
                                ChangeColorEntity(entity);
                                blockTableRecord.AppendEntity(entity);
                                tr.AddNewlyCreatedDBObject(entity, true);
                            }
                            dBObjectCollection.Clear();

                            if (token.IsCancellationRequested)
                            {
                                MessageBox.Show("Операция прервана");
                                tr.Abort();
                                token.ThrowIfCancellationRequested();
                                cancellationTokenSourceOuter.Dispose();
                                return;
                            }
                            tr.Commit();
                        }
                        catch (OperationCanceledException ex)
                        {
                            LogToFile(ex.ToString());
                        }
                        catch (System.Exception ex)
                        {
                            LogToFile(ex.ToString());
                            MessageBox.Show("Возникла непредвиденная ошибка. См. подробнее.");
                        }
                    }
                }
                resultIsGood = true;
            }, token);
            return resultIsGood;
        }

        private bool EntityCanExplode(Entity entity)
        {
            try
            {
                entity.Explode(new DBObjectCollection());
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private void ChangeColorEntity(Entity entity)
        {
            if (entity.Color.ColorMethod == ColorMethod.ByLayer)
            {
                Autodesk.AutoCAD.Colors.Color color = GetColorByLayer(entity.Layer);
                entity.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(color.ColorValue.R, color.ColorValue.G, color.ColorValue.B);
            }
            else
            {
                entity.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(entity.Color.ColorValue.R, entity.Color.ColorValue.G, entity.Color.ColorValue.B);
            }
        }

        private Autodesk.AutoCAD.Colors.Color GetColorByLayer(string layer)
        {
            using (Transaction tr = acCurrentDb.TransactionManager.StartOpenCloseTransaction())
            {
                LayerTable layerTable = (LayerTable)tr.GetObject(acCurrentDb.LayerTableId, OpenMode.ForRead);
                foreach (var item in layerTable)
                {
                    LayerTableRecord currentLayer = (LayerTableRecord)tr.GetObject(item, OpenMode.ForRead);
                    if (currentLayer.Name == layer)
                    {
                        return currentLayer.Color;
                    }
                }
            }
            throw new ArgumentNullException($"Layer {layer} not found.");
        }

        private void LogToFile(string str)
        {
            var fileName = Config.GetLogFileName();
            File.AppendAllText(fileName, $"[{DateTime.Now}] {str} \n");
        }

        private void LogErrorLayers(string layerName)
        {
            _layerErrors.Add(layerName);
        }

        private void DetailButton_Click(object sender, RoutedEventArgs e)
        {
            new DetailWindow().ShowDialog();
        }
    }
}
