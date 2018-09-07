using System;

using TeensyBatExplorer.Helpers.ViewModels;

namespace TeensyBatExplorer.Helpers.DependencyInjection
{
    public class ParameterCommand<TParam> : BaseCommand
    {
        private readonly Action<TParam> _execute;
        private readonly Func<TParam, bool> _canExecute;

        public ParameterCommand(Action<TParam> execute, Func<TParam, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public override bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute((TParam)parameter);
        }

        public override void Execute(object parameter)
        {
            _execute((TParam)parameter);
        }
    }
}