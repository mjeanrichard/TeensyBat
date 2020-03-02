// 
// Teensy Bat Explorer - Copyright(C) 2017 Meinrad Jean-Richard
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

using TeensyBatExplorer.Helpers.DependencyInjection;

namespace TeensyBatExplorer.Helpers.ViewModels
{
    //public class NonAsyncCommand : RelayCommand
    //{
    //    private readonly BaseViewModel _viewModel;
    //    private readonly Action _action;
    //    private readonly string _busyMessage;

    //    public NonAsyncCommand(Action action, string busyMessage, Func<bool> canExecute, BaseViewModel viewModel)
    //        : base(null, canExecute)
    //    {
    //        _action = action;
    //        _busyMessage = busyMessage;
    //        _viewModel = viewModel;
    //    }

    //    public NonAsyncCommand(Action action, Func<bool> canExecute, BaseViewModel viewModel)
    //        : this(action, null, canExecute, viewModel)
    //    {
    //    }

    //    public NonAsyncCommand(Action action, string busyMessage, BaseViewModel viewModel)
    //        : this(action, busyMessage, null, viewModel)
    //    {
    //    }

    //    public NonAsyncCommand(Action action, BaseViewModel viewModel)
    //        : this(action, null, null, viewModel)
    //    {
    //    }

    //    public override async void Execute(object parameter = null)
    //    {
    //        if (_viewModel != null)
    //        {
    //            await _viewModel.RunBusy(_action, _busyMessage);
    //        }
    //        else
    //        {
    //            await Task.Run(() => _action()).ConfigureAwait(false);
    //        }
    //    }
    //}

    public class AsyncCommand : RelayCommand
    {
        private readonly BaseViewModel _viewModel;
        private readonly Func<Task> _action;
        private readonly string _busyMessage;

        public AsyncCommand(Func<Task> action, string busyMessage, Func<bool> canExecute, BaseViewModel viewModel)
            : base(null, canExecute)
        {
            _action = action;
            _busyMessage = busyMessage;
            _viewModel = viewModel;
        }

        public AsyncCommand(Func<Task> action, Func<bool> canExecute, BaseViewModel viewModel)
            : this(action, null, canExecute, viewModel)
        {
        }

        public AsyncCommand(Func<Task> action, string busyMessage, BaseViewModel viewModel)
            : this(action, busyMessage, null, viewModel)
        {
        }

        public AsyncCommand(Func<Task> action, BaseViewModel viewModel)
            : this(action, null, null, viewModel)
        {
        }

        public override async void Execute(object parameter = null)
        {
            if (_viewModel != null)
            {
                await _viewModel.RunBusy(_action, _busyMessage);
            }
            else
            {
                await Task.Run(async () => await _action().ConfigureAwait(false)).ConfigureAwait(false);
            }
        }
    }

    public class AsyncParameterCommand<TParam> : RelayCommand
    {
        private readonly BaseViewModel _viewModel;
        private readonly Func<TParam, Task> _action;
        private readonly string _busyMessage;

        public AsyncParameterCommand(Func<TParam, Task> action, string busyMessage, Func<bool> canExecute, BaseViewModel viewModel)
            : base(null, canExecute)
        {
            _action = action;
            _busyMessage = busyMessage;
            _viewModel = viewModel;
        }

        public AsyncParameterCommand(Func<TParam, Task> action, Func<bool> canExecute, BaseViewModel viewModel)
            : this(action, null, canExecute, viewModel)
        {
        }

        public AsyncParameterCommand(Func<TParam, Task> action, string busyMessage, BaseViewModel viewModel)
            : this(action, busyMessage, null, viewModel)
        {
        }

        public AsyncParameterCommand(Func<TParam, Task> action, BaseViewModel viewModel)
            : this(action, null, null, viewModel)
        {
        }

        public override async void Execute(object parameter = null)
        {
            if (_viewModel != null)
            {
                await _viewModel.RunBusy(async () => await _action((TParam)parameter), _busyMessage);
            }
            else
            {
                await Task.Run(async () => await _action((TParam)parameter).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }
    }
}