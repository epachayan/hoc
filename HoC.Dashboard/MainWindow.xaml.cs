//-----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs">
//     Copyright (c) Nitin Stephen Koshy. All rights reserved.
// </copyright>
// <author>Nitin Stephen Koshy</author>
// <email>nitinkoshy@gmail.com</email>
// <date>03.Sep.2010</date>
// <license>Microsoft Public License (http://www.microsoft.com/opensource/licenses.mspx#Ms-PL)</license>
// <Url>http://hoc.codeplex.com</Url>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HoC.Common;
using System.ComponentModel;
using System.Windows.Threading;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Windows.Controls.Primitives;
using System.Reflection;


namespace HoC.Dashboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<string> _activeNodes = new List<string>();
        public NodeTracker _nodeTracker = NodeTrackerSingleton.Instance;
        BindingList<KeyValuePair<string, string>> dataList = new BindingList<KeyValuePair<string, string>>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dgProperties.AutoGenerateColumns = true;
            dgProperties.ItemsSource = dataList;

            _nodeTracker.OnNodeListUpdated += new NodeTracker.NodeListUpdatedHandler(NodeListUpdated);

            DispatcherTimer activeViewUpdater = new DispatcherTimer();
            activeViewUpdater.Tick += new EventHandler(TimerCallback);
            activeViewUpdater.Interval = new TimeSpan(0, 0, 2);
            activeViewUpdater.Start();
        }

        public void TimerCallback(object state, EventArgs args)
        {
            Dispatcher.Invoke((Action)(() => { UpdateGridDataSource(); }));
        }

        private void NodeListUpdated(Node node, NodeUpdateAction updateAction)
        {
            if (updateAction == NodeUpdateAction.Added)
            {
                Dispatcher.Invoke((Action)(() => { 
                    tvNodes.Items.Add(node.EndPoint.ToString());
                    if (tvNodes.Items.Count == 1)
                    {
                        SetSelectedItem(tvNodes, tvNodes.Items[0]);
                    }

                    UpdateStatusMessage("Discovered new node " + node.EndPoint.ToString());
                }));
            }
            else if (updateAction == NodeUpdateAction.Removed)
            {
                Dispatcher.Invoke((Action)(() => { 
                    tvNodes.Items.Remove(node.EndPoint.ToString());
                    UpdateStatusMessage("Removed dead node " + node.EndPoint.ToString());
                    ClearGrid();
                }));
            }
        }

        public static void SetSelectedItem(TreeView control, object item)
        {
            try
            {
                DependencyObject dObject = control
                    .ItemContainerGenerator
                    .ContainerFromItem(item);

                MethodInfo selectMethod =
                   typeof(TreeViewItem).GetMethod("Select",
                   BindingFlags.NonPublic | BindingFlags.Instance);

                selectMethod.Invoke(dObject, new object[] { true });
            }
            catch { }
        }

        private void tvNodes_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            UpdateGridDataSource();
        }

        private void ClearGrid()
        {
            lock (dataList)
            {
                if ((tvNodes.Items.Count == 0) || (tvNodes.SelectedValue == null))
                  dataList.Clear();
            }
        }

        private void UpdateGridDataSource()
        {
            lock (dataList)
            {
                if ((tvNodes.Items.Count == 0) || (tvNodes.SelectedValue == null))
                {
                    ClearGrid();
                    return;
                }

                Func<Node, bool> nodeFinder = (x => x.EndPoint.ToString().CompareTo(tvNodes.SelectedValue) == 0);
                Node node = _nodeTracker.ActiveNodes.First<Node>(nodeFinder);
                if (node == null)
                    return;

                dataList.Clear();

                //for this node, get the active data from the node
                string endPoint = string.Format("net.tcp://{0}:{1}/HoCCacheService", node.EndPoint.ToString(), node.ServicePort);
                CacheService.CacheServiceClient nodeService = new CacheService.CacheServiceClient(new NetTcpBinding(SecurityMode.None), new EndpointAddress(endPoint));
                try
                {
                    nodeService.Open();
                    CacheHealth cacheHealth = nodeService.GetCacheHealth();
                    dataList.Add(new KeyValuePair<string, string>("Machine Name", cacheHealth.MachineName));
                    dataList.Add(new KeyValuePair<string, string>("Host", tvNodes.SelectedValue.ToString()));
                    dataList.Add(new KeyValuePair<string, string>("Heartbeat Heard at", node.HeartBeatLastHeardAt.ToLongTimeString()));
                    dataList.Add(new KeyValuePair<string, string>("Service Port", node.ServicePort.ToString()));
                    dataList.Add(new KeyValuePair<string, string>("Node State", node.NodeState.ToString()));

                    dataList.Add(new KeyValuePair<string, string>("Object Count", cacheHealth.ObjectCount.ToString()));
                    dataList.Add(new KeyValuePair<string, string>("Object(s) Size", cacheHealth.TotalObjectSize.ToString() + "Bytes"));
                    dataList.Add(new KeyValuePair<string, string>("Process Working Set", cacheHealth.ProcessWorkingSet.ToString() + "K"));
                    dataList.Add(new KeyValuePair<string, string>("Eviction Strategy", cacheHealth.EvictionStrategy));
                    if (cacheHealth.EvictionLastAt == DateTime.MinValue)
                        dataList.Add(new KeyValuePair<string, string>("Last Eviction Time", "never"));
                    else
                        dataList.Add(new KeyValuePair<string, string>("Last Eviction Time", cacheHealth.EvictionLastAt.ToLongTimeString()));

                    UpdateStatusMessage("Updated status for " + tvNodes.SelectedValue.ToString());
                }
                catch(Exception E)
                {
                    UpdateStatusMessage("Error when connecting to " + tvNodes.SelectedValue.ToString());
                }
            }
        }

        private void UpdateStatusMessage(string message)
        {
            ((TextBlock)((StatusBarItem)statusBar.Items[0]).Content).Text = message;
        }

        private void tvNodes_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if ((tvNodes.Items.Count == 0) || (tvNodes.SelectedValue == null))
            {
                return;
            }

            Popup.StaysOpen = false;
            Popup.IsOpen = true;
        }

        private void InitiateShutdown()
        {
            if ((tvNodes.Items.Count == 0) || (tvNodes.SelectedValue == null))
            {
                return;
            }

            Func<Node, bool> nodeFinder = (x => x.EndPoint.ToString().CompareTo(tvNodes.SelectedValue) == 0);
            Node node = _nodeTracker.ActiveNodes.First<Node>(nodeFinder);
            if (node == null)
            {
                MessageBox.Show("Could not locate the node service entry. Shutdown call will not be made.");
                return;
            }

            //for this node, get the active data from the node
            string endPoint = string.Format("net.tcp://{0}:{1}/HoCCacheService", node.EndPoint.ToString(), node.ServicePort);
            CacheService.CacheServiceClient nodeService = new CacheService.CacheServiceClient(new NetTcpBinding(SecurityMode.None), new EndpointAddress(endPoint));
            nodeService.Open();
            nodeService.Stop();

            MessageBox.Show("Successfully called Shutdown on server : " + tvNodes.SelectedValue);
        }

        private void Shutdown_Click(object sender, RoutedEventArgs e)
        {
            InitiateShutdown();
        }
    }


}