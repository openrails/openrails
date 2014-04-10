// COPYRIGHT 2014 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ORTS.TrackViewer.Editing
{
    class EditorContextMenu
    { 
        /// <summary>
        /// All code for the context menu
        /// Most of the definition of the context menu is actually in PathEditor.
        /// Context menu is a windows control, without XAML definition. In other words, it is completely defined in code.
        /// so here we translate the information in PathEditor to a real context menu
        /// And in reverse, whenever an items is selected the callback is called.
        /// </summary>
        ContextMenu contextMenu;
        PathEditor pathEditor;
        Dictionary<ContextMenuAction, MenuItem> menuItems = new Dictionary<ContextMenuAction, MenuItem>();
        MenuItem noActionPossibleMenuItem;

        /// <summary>
        /// Create the context menu. Needs to be done only once.
        /// </summary>
        public EditorContextMenu(PathEditor pathEditor, List<EditorAction> editorActions)
        {
            this.pathEditor = pathEditor;
            contextMenu = new ContextMenu();

            noActionPossibleMenuItem = new MenuItem();
            noActionPossibleMenuItem.Header = "No action possible\nPerhaps paths are not drawn.";
            contextMenu.Items.Add(noActionPossibleMenuItem);

            foreach (EditorAction action in editorActions) {
                contextMenu.Items.Add(action.ActionMenuItem);
            }
        }

        ///// <summary>
        ///// Create the context menu. Needs to be done only once.
        ///// </summary>
        //public EditorContextMenu(PathEditor pathEditor, Dictionary<ContextMenuAction,string> menuHeaders)
        //{
        //    this.pathEditor = pathEditor;
        //    contextMenu = new ContextMenu();

        //    noActionPossibleMenuItem = new MenuItem();
        //    noActionPossibleMenuItem.Header = "No action possible\nPerhaps paths are not drawn.";
        //    contextMenu.Items.Add(noActionPossibleMenuItem);

        //    foreach (ContextMenuAction action in Enum.GetValues(typeof(ContextMenuAction)))
        //    {
        //        if (!menuHeaders.ContainsKey(action))
        //        {
        //            contextMenu.Items.Add(new Separator());
        //        }
        //        else
        //        {
        //            MenuItem menuItem = new MenuItem();
        //            menuItem.Header = menuHeaders[action];
        //            menuItem.IsCheckable = false;
        //            menuItem.CommandParameter = action; // by storing this parameter, we can give it back again upon the callback
        //            menuItem.Click += new RoutedEventHandler(contextExecuteAction_Click);
        //            contextMenu.Items.Add(menuItem);
        //            menuItems[action] = menuItem;
        //        }
        //    }

        //    contextMenu.Closed += new RoutedEventHandler(ContextMenu_Closed);
        //}

        //public void PopupContextMenu(int mouseX, int mouseY, List<EditorAction> editorActions)
        //{
        //    bool someActionIsPossible = false;
        //    foreach (EditorAction action in editorActions)
        //    {
        //        someActionIsPossible = someActionIsPossible || action.MenuState();
        //    }
        //    noActionPossibleMenuItem.Visibility = someActionIsPossible ? Visibility.Collapsed : Visibility.Visible;

        //    contextMenu.PlacementRectangle = new Rect((double)mouseX, (double)mouseY, 20, 20);
        //    contextMenu.IsOpen = true;
        //}

        /// <summary>
        /// Popup the context menu at the given location. Also disable updates related to mouse movement while menu is open.
        /// </summary>
        /// <param name="mouseX">X-screen location of the mouse</param>
        /// <param name="mouseY">Y-screen location of the mouse</param>
        /// <param name="menuHeaders">Name of the various context menu headers, indexed by contextMenuAction</param>
        /// <param name="menuEnabled">Booleans whether the action (indexed by contextMenuAction) is currently enabled</param>
        //public void PopupContextMenu(int mouseX, int mouseY, 
        //                             Dictionary<ContextMenuAction, string> menuHeaders,
        //                             Dictionary<ContextMenuAction, bool> menuEnabled)
        //{
        //    pathEditor.EnableMouseUpdate = false;
        //    bool someActionIsPossible = false;
            
        //    foreach (ContextMenuAction action in Enum.GetValues(typeof(ContextMenuAction)))
        //    {
        //        if (menuHeaders.ContainsKey(action))
        //        {
        //            menuItems[action].IsEnabled = menuEnabled[action];
        //            menuItems[action].Visibility = menuEnabled[action] ? Visibility.Visible : Visibility.Collapsed;
        //            someActionIsPossible = someActionIsPossible || menuEnabled[action];
        //        }
        //    }

        //    noActionPossibleMenuItem.Visibility = someActionIsPossible ? Visibility.Collapsed : Visibility.Visible;

        //    contextMenu.PlacementRectangle = new Rect((double)mouseX, (double)mouseY, 20, 20);
        //    contextMenu.IsOpen = true;
        //}

        public void CloseContextMenu()
        {
            if (contextMenu == null) {return;}
            contextMenu.IsOpen = false;
        }

        /// <summary>
        /// When the context menu closes, enable updates based on mouse movement again.
        /// </summary>
        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            pathEditor.EnableMouseUpdate = true;
        }

        ///// <summary>
        ///// A certain item in the context menu has been clicked. Execute the corresponding action
        ///// </summary>
        //private void contextExecuteAction_Click(object sender, RoutedEventArgs e)
        //{
        //    MenuItem menuItem = sender as MenuItem;
        //    pathEditor.ExecuteAction((ContextMenuAction)menuItem.CommandParameter);
        //}

    }
}
