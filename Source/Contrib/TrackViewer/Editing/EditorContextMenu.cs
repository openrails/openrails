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
        Dictionary<contextMenuAction, MenuItem> menuItems = new Dictionary<contextMenuAction, MenuItem>();

         /// <summary>
        /// Create the context menu. Needs to be done only once.
        /// </summary>
        public EditorContextMenu(PathEditor pathEditor, Dictionary<contextMenuAction,string> menuHeaders)
        {
            this.pathEditor = pathEditor;
            contextMenu = new ContextMenu();

            foreach (contextMenuAction action in Enum.GetValues(typeof(contextMenuAction)))
            {
                if (!menuHeaders.ContainsKey(action))
                {
                    contextMenu.Items.Add(new Separator());
                }
                else
                {
                    MenuItem menuItem = new MenuItem();
                    menuItem.Header = menuHeaders[action];
                    menuItem.IsCheckable = false;
                    menuItem.CommandParameter = action; // by storing this parameter, we can give it back again upon the callback
                    menuItem.Click += new RoutedEventHandler(contextExecuteAction_Click);
                    contextMenu.Items.Add(menuItem);
                    menuItems[action] = menuItem;
                }
            }

            contextMenu.Closed += new RoutedEventHandler(ContextMenu_Closed);
        }

        /// <summary>
        /// Popup the context menu at the given location. Also disable updates related to mouse movement while menu is open.
        /// </summary>
        /// <param name="mouseX">X-screen location of the mouse</param>
        /// <param name="mouseY">Y-screen location of the mouse</param>
        /// <param name="menuHeaders">Name of the various context menu headers, indexed by contextMenuAction</param>
        /// <param name="menuEnabled">Booleans whether the action (indexed by contextMenuAction) is currently enabled</param>
        public void PopupContextMenu(int mouseX, int mouseY, 
                                     Dictionary<contextMenuAction, string> menuHeaders,
                                     Dictionary<contextMenuAction, bool> menuEnabled)
        {
            pathEditor.EnableMouseUpdate = false;
            
            foreach (contextMenuAction action in Enum.GetValues(typeof(contextMenuAction)))
            {
                if (menuHeaders.ContainsKey(action))
                {
                    menuItems[action].IsEnabled = menuEnabled[action];
                }
            }

            contextMenu.PlacementRectangle = new Rect((double)mouseX, (double)mouseY, 20, 20);
            contextMenu.IsOpen = true;
        }

        /// <summary>
        /// When the context menu closes, enable updates based on mouse movement again.
        /// </summary>
        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            pathEditor.EnableMouseUpdate = true;
        }

        /// <summary>
        /// A certain item in the context menu has been clicked. Execute the corresponding action
        /// </summary>
        private void contextExecuteAction_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            pathEditor.ExecuteAction(menuItem.CommandParameter);
        }

    }
}
