using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Airswipe.WinRT.UI.Controls
{
    // Extension of MS example code: 
    public class DoubleBox : TextBox
    {
        #region Fields 

        public static DependencyProperty SelectedMemberValueProperty = DependencyProperty.Register(
            "Value",
            typeof(double),
            typeof(DoubleBox),
            new PropertyMetadata(null, ValuePropertyChangedCallback)
            );

        public delegate void ValueChangeHandler(double newValue);
        public event ValueChangeHandler ValueChange;

        #endregion
        #region Constructor

        public DoubleBox()
        {
            AllowNumbersOnly();

            TextChanged += (object sender, TextChangedEventArgs e) =>
            {
                if (ValueChange != null)
                    ValueChange(Value);
            };

            this.KeyDown += DoubleBox_KeyDown;

            //SetValueOnTextChanged();
        }

        private void DoubleBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Up)
            {
                Value++;
                e.Handled = true;
            }
            if (e.Key == Windows.System.VirtualKey.Down)
            {
                Value--;
                e.Handled = true;
            }
        }

        #endregion
        #region Methods 

        //private void SetValueOnTextChanged()
        //{
        //    TextChanged += (object sender, TextChangedEventArgs e) =>
        //    {
        //        Value = int.Parse(Text);
        //    };
        //}

        private void AllowNumbersOnly()
        {
            InputScope = new InputScope();
            InputScope.Names.Add(new InputScopeName(InputScopeNameValue.Number));
        }

        private static void ValuePropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var control = (DoubleBox)dependencyObject;

            control.Value = (double)args.NewValue;
            //if (control.Text != control.Value.ToString())
            //    control.Text = control.Value.ToString();
        }

        #endregion
        #region Properties 

        public double Value
        {
            get { return string.IsNullOrEmpty(Text) ? 0 : double.Parse(Text); }
            set
            {
                string valueStr = value.ToString();
                if (valueStr != Text)
                    Text = valueStr;
            }
        }

        #endregion
    }
}
