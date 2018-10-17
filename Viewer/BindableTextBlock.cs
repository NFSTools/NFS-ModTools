using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Viewer
{
    public class BindableTextBlock : TextBlock
    {
        public static DependencyProperty InlineProperty;

        static BindableTextBlock()
        {
            //OverrideMetadata call tells the system that this element wants to provide a style that is different than in base class
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BindableTextBlock), new FrameworkPropertyMetadata(
                typeof(BindableTextBlock)));
            InlineProperty = DependencyProperty.Register("RichText", typeof(List<Inline>), typeof(BindableTextBlock),
            new PropertyMetadata(null, new PropertyChangedCallback(OnInlineChanged)));
        }
        public List<Inline> RichText
        {
            get { return (List<Inline>)GetValue(InlineProperty); }
            set { SetValue(InlineProperty, value); }
        }
        public static void OnInlineChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == e.OldValue)
                return;
            BindableTextBlock r = sender as BindableTextBlock;
            List<Inline> i = e.NewValue as List<Inline>;
            if (r == null || i == null)
                return;
            r.Inlines.Clear();
            foreach (Inline inline in i)
            {
                r.Inlines.Add(inline);
            }
        }
    }
}
