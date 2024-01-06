namespace LazyApiPack.Fluent.RibbonMerge
{
    using global::Fluent;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;

    public static class RibbonEx
    {

        public static void Merge(this Ribbon target, Ribbon source)
        {
            var hic = new HeaderedControlComparer();

            var directory = GetMergedControls(target);
            if (directory == null)
            {
                SetMergedControls(target, directory = new List<MergedControl>());
            }

            foreach (var sourceTab in source.Tabs.Select(t => new TabDefinition(t, GetMergeIndex(t))).OrderBy(t => t.Index))
            {
                var targetTab = target.Tabs.FirstOrDefault(t => hic.Equals(t, sourceTab.Tab));
                if (targetTab == null)
                {
                    // Move Tab
                    directory.Add(new MergedControl(RibbonItemType.RibbonTab, sourceTab.Tab, source, target.Tabs, null));
                    if (sourceTab.Index.HasValue)
                    {
                        target.Tabs.Insert(sourceTab.Index.Value, sourceTab.Tab);
                    }
                    else
                    {
                        target.Tabs.Add(sourceTab.Tab);
                    }
                }
                else
                {
                    // merge tab
                    foreach (var sourceGroup in sourceTab.Tab.Groups.Select(g => new GroupDefinition(g, GetMergeIndex(g))).OrderBy(g => g.Index))
                    {
                        var targetGroup = targetTab.Groups.FirstOrDefault(t => hic.Equals(t, sourceGroup.Group));
                        if (targetGroup == null)
                        {
                            directory.Add(new MergedControl(RibbonItemType.RibbonTab, sourceGroup.Group, source, targetTab.Groups, sourceTab.Tab.Groups));
                            sourceTab.Tab.Groups.Remove(sourceGroup.Group);
                            // todo: add sourceGroup.Group to MergedControl
                            if (sourceGroup.Index.HasValue)
                            {
                                targetTab.Groups.Insert(sourceGroup.Index.Value, sourceGroup.Group);
                            }
                            else
                            {
                                targetTab.Groups.Add(sourceGroup.Group);

                            }
                        }
                        else
                        {
                            foreach (var sourceItem in sourceGroup.Group.Items.OfType<DependencyObject>().Select(i => new ItemDefinition(i, GetMergeIndex(i))).OrderBy(i => i.Index))
                            {
                                directory.Add(new MergedControl(RibbonItemType.RibbonTab, sourceGroup.Group, source, targetGroup.Items, sourceGroup.Group.Items));
                                if (sourceItem.Index.HasValue)
                                {
                                    targetGroup.Items.Insert(sourceItem.Index.Value, sourceItem.Item);
                                }
                                else
                                {
                                    targetGroup.Items.Add(sourceItem.Item);
                                }
                            }
                        }
                    }

                }

            }


            foreach (var sourceToolbarItem in source.ToolBarItems)
            {
                directory.Add(new MergedControl(RibbonItemType.Toolbar, sourceToolbarItem, source, target.ToolBarItems, null));
                target.ToolBarItems.Add(sourceToolbarItem);
            }
        }

        public static void Unmerge(this Ribbon target, Ribbon source)
        {
            var controls = GetMergedControls(target);
            if (controls != null)
            {
                foreach (var mergedItem in controls.Where(r => r.SourceRibbon == source).ToList())
                {
                    switch (mergedItem.Type)
                    {
                        case RibbonItemType.RibbonTab:
                            mergedItem.TargetList.Remove(mergedItem.Control);
                            controls.Remove(mergedItem);
                            if (mergedItem.SourceList != null)
                            {
                                mergedItem.SourceList.Add(mergedItem.Control); // TODO: Index?
                            }
                            break;
                        case RibbonItemType.ContextualGroup:
                            break;
                        case RibbonItemType.Toolbar:
                            mergedItem.TargetList.Remove(mergedItem.Control);
                            controls.Remove(mergedItem);
                            break;
                        case RibbonItemType.Menu:
                            break;
                        case RibbonItemType.QuickAccess:
                            break;
                        default:
                            break;
                    }

                }
            }
            else
            {
                // No ribbon was ever merged
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
            DependencyProperty.RegisterAttached("MergeIndex", typeof(int?), typeof(RibbonEx), new PropertyMetadata(null));

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
            DependencyProperty.RegisterAttached("MergedControls", typeof(List<MergedControl>), typeof(RibbonEx), new PropertyMetadata(null));


        #region Helper classes

        private struct TabDefinition
        {
            public TabDefinition(RibbonTabItem tab, int? index)
            {
                Tab = tab;
                Index = index;
            }
            public int? Index { get; set; }
            public RibbonTabItem Tab { get; set; }

            public override string ToString()
            {
                var tabHeader = Tab?.Header ?? "<null>";
                var index = Index?.ToString() ?? "<null>";
                return $"{tabHeader}: {index}";
            }
        }

        private struct GroupDefinition
        {
            public GroupDefinition(RibbonGroupBox group, int? index)
            {
                Group = group;
                Index = index;
            }
            public int? Index { get; set; }
            public RibbonGroupBox Group { get; set; }

            public override string ToString()
            {
                var header = Group?.Header ?? "<null>";
                var index = Index?.ToString() ?? "<null>";
                return $"{header}: {index}";
            }
        }

        private struct ItemDefinition
        {
            public ItemDefinition(DependencyObject item, int? index)
            {
                Item = item;
                Index = index;
            }
            public int? Index { get; set; }
            public DependencyObject Item { get; set; }

            public override string ToString()
            {
                var header = Item?.ToString() ?? "<null>";

                if (Item is IHeaderedControl headeredCtrl)
                {
                    header = headeredCtrl.Header?.ToString() ?? "<null>";
                }

                var index = Index?.ToString() ?? "<null>";
                return $"{header}: {index}";
            }
        }

        private enum RibbonItemType
        {
            RibbonTab,
            ContextualGroup, // TODO: Not implemented yet
            Toolbar,
            Menu, // TODO: Not implemented yet
            QuickAccess // TODO: Not implemented yet
        }

        private struct MergedControl
        {
            public MergedControl(RibbonItemType type, DependencyObject control, Ribbon sourceRibbon, IList targetList, IList sourceList)
            {
                Control = control;
                SourceRibbon = sourceRibbon;
                TargetList = targetList;
                SourceList = sourceList;
                Type = type;
            }
            public DependencyObject Control { get; set; }
            public Ribbon SourceRibbon { get; set; }
            public IList TargetList { get; set; }

            public IList SourceList { get; set; } // todo? sourceListIndex?

            public RibbonItemType Type { get; set; }

        }

        private class HeaderedControlComparer : IEqualityComparer<IHeaderedControl>
        {
            public bool Equals(IHeaderedControl x, IHeaderedControl y)
            {
                if (x.Header == null && y.Header == null)
                {
                    return true;
                }
                else if (ReferenceEquals(x.Header, null) ^ ReferenceEquals(y.Header, null))
                {
                    return false; // one of them is null
                }
                else
                {
                    return x.Header.Equals(y.Header);
                }
            }

            public int GetHashCode(IHeaderedControl obj)
            {
                return obj.Header?.GetHashCode() ?? 0;
            }
        }
        #endregion


    }
}
