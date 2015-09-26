using System;
using System.Windows.Controls;
using ReactiveUI;

namespace Pack_v2.Tools
{
    public static class BindingEx
    {
        public static void BindDataContext<TView>(this TView source) where TView : Control, IViewFor
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            source.WhenAnyValue(v => v.ViewModel).BindTo(source, v => v.DataContext);
        } 
    }
}