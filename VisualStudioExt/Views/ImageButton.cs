using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VisualStudioExt.Views
{
    internal class ImageButton : Button
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(ImageSource), typeof(ImageButton), null);

        public ImageSource Icon
        {
            get { return (ImageSource)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }
    }
}
