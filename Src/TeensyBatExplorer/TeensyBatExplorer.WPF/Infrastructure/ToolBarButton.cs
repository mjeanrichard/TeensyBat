// 
// Teensy Bat Explorer - Copyright(C)  Meinard Jean-Richard
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Threading.Tasks;

using MaterialDesignThemes.Wpf;

using Nito.Mvvm;

namespace TeensyBatExplorer.WPF.Infrastructure
{
    public class ToolBarButton
    {
        public ToolBarButton(Func<Task> action, PackIconKind icon, string tooltip)
        {
            Icon = icon;
            Tooltip = tooltip;
            Action = new AsyncCommand(action);
        }

        public PackIconKind Icon { get; set; }
        public string Tooltip { get; set; }
        public AsyncCommand Action { get; private set; }
    }
}