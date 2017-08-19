using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Intervallo.UI
{
    /// <summary>
    /// NumericUpDown.xaml の相互作用ロジック
    /// </summary>
    public partial class NumericUpDown : UserControl
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(NumericUpDown),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, PropertyChanged)
        );

        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
            nameof(MaxValue),
            typeof(double),
            typeof(NumericUpDown),
            new FrameworkPropertyMetadata((double)int.MaxValue, FrameworkPropertyMetadataOptions.AffectsRender, PropertyChanged)
        );

        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
            nameof(MinValue),
            typeof(double),
            typeof(NumericUpDown),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, PropertyChanged)
        );

        public static readonly DependencyProperty DecimalDigitProperty = DependencyProperty.Register(
            nameof(DecimalDigit),
            typeof(int),
            typeof(NumericUpDown),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender, PropertyChanged)
        );

        public NumericUpDown()
        {
            InitializeComponent();
        }

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, Math.Max(Math.Min(Math.Round(value, DecimalDigit), MaxValue), MinValue)); }
        }

        public double MaxValue
        {
            get { return (double)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }

        public double MinValue
        {
            get { return (double)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }

        public int DecimalDigit
        {
            get { return (int)GetValue(DecimalDigitProperty); }
            set { SetValue(DecimalDigitProperty, value); }
        }

        void UpButton_Click(object sender, RoutedEventArgs e)
        {
            Value += Math.Pow(10, -DecimalDigit);
        }

        void DownButton_Click(object sender, RoutedEventArgs e)
        {
            Value -= Math.Pow(10, -DecimalDigit);
        }

        static void PropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var numericUpDown = dependencyObject as NumericUpDown;
            numericUpDown.Value = numericUpDown.Value;
            numericUpDown.UpButton.IsEnabled = numericUpDown.Value != numericUpDown.MaxValue;
            numericUpDown.DownButton.IsEnabled = numericUpDown.Value != numericUpDown.MinValue;
        }
    }
}
