using Airswipe.WinRT.Core.Log;
using Airswipe.WinRT.UI.Controls;
using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Airswipe.WinRT.UI.Controls
{
    public class EnumComboBox<T> : ComboBox
    {
        #region Fields

        private ILogger log = new TypeLogger<EnumComboBox<T>>();

        ObservableCollection<string> enumMemberNames = new ObservableCollection<string>(Enum.GetNames(typeof(T)));

        public static DependencyProperty SelectedMemberValueProperty = DependencyProperty.Register(
            "SelectedMemberValue",
            typeof(T),
            typeof(EnumComboBox<T>),
            new PropertyMetadata(null, PropertyChangedCallback)
            );

        #endregion
        #region Constructor

        public EnumComboBox()
        {
            base.ItemsSource = enumMemberNames;

            //SetBinding(SelectedValueProperty, new Windows.UI.Xaml.Data.BindingBase )

            this.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                //SelectedMemberValue = (T)Enum.Parse(typeof(T), (string)SelectedValue);
                SetValue(SelectedMemberValueProperty, SelectedMemberValue);
            };
        }

        #endregion
        #region Event handlers 

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            ((EnumComboBox<T>)dependencyObject).SelectedMemberValue = (T)args.NewValue;
            //SuperButton userControl = ((SuperButton)dependencyObject);
            //userControl.label.Text = (string)args.NewValue;
        }

        #endregion
        #region Properties

        public new object ItemsSource { get { return base.ItemsSource; } set { throw new Exception("not allowed."); } }

        public T SelectedMemberValue
        {
            //get { return (T)GetValue(SelectedMemberValueProperty); }
            get { return (T)Enum.Parse(typeof(T), (string)SelectedValue); }
            set
            {
                //
                SelectedIndex = enumMemberNames.IndexOf(value.ToString());
                //SetValue(SelectedMemberValueProperty, value);
            }
            //get { return (T)GetValue(SelectedMemberValueProperty); }
            //set {
            //    //log.Info("x");
            //    SetValue(SelectedMemberValueProperty, value);
            //    //SelectedIndex = enumMemberNames.IndexOf(value.ToString());
            //}
        }

        //public static DependencyProperty LabelProperty
        //{
        //    get
        //    {
        //        return SelectedMemberValueProperty;
        //    }

        //    set
        //    {
        //        SelectedMemberValueProperty = value;
        //    }
        //}

        #endregion
    }
}
