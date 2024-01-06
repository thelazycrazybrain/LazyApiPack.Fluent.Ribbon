using Fluent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LazyApiPack.Fluent.Ribbon
{
    public static class StatusBarEx
    {
        public static void Merge(this StatusBar target, StatusBar source, object? newDataContext = null)
        {
            var directory = GetMergedControls(target);
            if (directory == null)
            {
                SetMergedControls(target, directory = new List<MergedControl>());
            }


            foreach (var sourceItem in source.Items.OfType<DependencyObject>().Select(i => new StatusBarItemDefinition(i, GetMergeIndex(i))).OrderBy(t => t.Index))
            {
                directory.Add(new MergedControl(sourceItem.Item, source, target.Items, source.Items));
                if (newDataContext != null && sourceItem.Item is FrameworkElement fe)
                {
                    fe.DataContext = newDataContext;
                }

                source.Items.Remove(sourceItem.Item);
                if (sourceItem.Index.HasValue)
                {
                    target.Items.Insert(sourceItem.Index.Value, sourceItem.Item);
                }
                else
                {
                    target.Items.Add(sourceItem.Item);
                }
            }
        }

        public static void Unmerge(this StatusBar target, StatusBar? source = null)
        {
            var controls = GetMergedControls(target);
            if (controls != null)
            {
                foreach (var mergedItem in controls.Where(r => source == null || r.SourceStatusBar == source).ToList())
                {
                    mergedItem.TargetList.Remove(mergedItem.Control);
                    controls.Remove(mergedItem);
                    if (mergedItem.SourceList != null)
                    {
                        mergedItem.SourceList.Add(mergedItem.Control);
                    }
                    break;


                }
            }

        }



        public static int? GetMergeIndex(DependencyObject obj)
        {
            return (int?)obj.GetValue(MergeIndexProperty);
        }

        public static void SetMergeIndex(DependencyObject obj, int? value)
        {
            obj.SetValue(MergeIndexProperty, value);
        }


        public static readonly DependencyProperty MergeIndexProperty =
            DependencyProperty.RegisterAttached("MergeIndex", typeof(int?), typeof(StatusBarEx), new PropertyMetadata(null));

        public static StatusBar GetStoredStatusBar(DependencyObject obj)
        {
            return (StatusBar)obj.GetValue(StoredStatusBarProperty);
        }

        public static void SetStoredStatusBar(DependencyObject obj, StatusBar value)
        {
            obj.SetValue(StoredStatusBarProperty, value);
        }

        public static readonly DependencyProperty StoredStatusBarProperty =
            DependencyProperty.RegisterAttached("StoredStatusBar", typeof(StatusBar), typeof(StatusBarEx), new PropertyMetadata(null));




        static List<MergedControl> GetMergedControls(DependencyObject obj)
        {
            return (List<MergedControl>)obj.GetValue(MergedControlsProperty);
        }

        static void SetMergedControls(DependencyObject obj, List<MergedControl> value)
        {
            obj.SetValue(MergedControlsProperty, value);
        }

        // Using a DependencyProperty as the backing store for MergedControls.  This enables animation, styling, binding, etc...
        static readonly DependencyProperty MergedControlsProperty =
            DependencyProperty.RegisterAttached("MergedControls", typeof(List<MergedControl>), typeof(StatusBarEx), new PropertyMetadata(null));


        #region Helper classes

        struct StatusBarItemDefinition
        {
            public StatusBarItemDefinition(DependencyObject item, int? index)
            {
                Item = item;
                Index = index;
            }
            public int? Index { get; set; }
            public DependencyObject Item { get; set; }

            public override string ToString()
            {
                var title = Item.ToString();
                if (Item is StatusBarItem sbi)
                {
                    title = sbi.Title;
                }

                var index = Index?.ToString() ?? "<null>";
                return $"{title}: {index}";
            }
        }

        struct MergedControl
        {
            public MergedControl(DependencyObject control, StatusBar sourceStatusBar, IList targetList, IList sourceList)
            {
                Control = control;
                SourceStatusBar = sourceStatusBar;
                TargetList = targetList;
                SourceList = sourceList;
            }
            public DependencyObject Control { get; set; }
            public StatusBar SourceStatusBar { get; set; }
            public IList TargetList { get; set; }

            public IList SourceList { get; set; } // todo? sourceListIndex?

        }

        #endregion

    }
}
