// 
// Teensy Bat Explorer - Copyright(C) 2018 Meinrad Jean-Richard
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

using System.Threading.Tasks;

namespace TeensyBatExplorer.Activation
{
    // For more information on application activation see https://github.com/Microsoft/WindowsTemplateStudio/blob/master/docs/activation.md
    public abstract class ActivationHandler
    {
        public abstract bool CanHandle(object args);

        public abstract Task HandleAsync(object args);

        public abstract int Priority { get; }
    }

    public abstract class ActivationHandler<T> : ActivationHandler
        where T : class
    {
        protected abstract Task HandleInternalAsync(T args);

        public override async Task HandleAsync(object args)
        {
            await HandleInternalAsync(args as T);
        }

        public override bool CanHandle(object args)
        {
            return args is T && CanHandleInternal(args as T);
        }

        protected virtual bool CanHandleInternal(T args)
        {
            return true;
        }
    }
}