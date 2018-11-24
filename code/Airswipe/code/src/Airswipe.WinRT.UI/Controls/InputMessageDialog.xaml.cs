using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Airswipe.WinRT.UI.Controls
{

    public sealed partial class InputMessageDialog : UserControl
    {
        #region Fields

        private TaskCompletionSource<bool> InputTaskCompletionSource;

        #endregion
        #region Constructors

        public InputMessageDialog(string title, string initialValue)
        {
            this.InitializeComponent();

            Title = title;
            Value = initialValue;
        }

        #endregion
        #region Methods

        public Task<bool> ShowAsync()
        {
            InitFields();
            PopupContainer.IsOpen = true;
            InputTaskCompletionSource = new TaskCompletionSource<bool>();

            InputTextBox.Focus(Windows.UI.Xaml.FocusState.Pointer); //InputTextBox.Focus(FocusState.Pointer);
            InputTextBox.SelectAll();

            return InputTaskCompletionSource.Task;
        }

        public void InitFields()
        {
            OverlayRectangle.Height = Window.Current.Bounds.Height;
            OverlayRectangle.Width = Window.Current.Bounds.Width;

            DialogBackgroundRectangle.Width = Window.Current.Bounds.Width;
            InputTextBox.Width = Window.Current.Bounds.Width / 2;
            
            TitleTextBlock.Text = Title;
        }

        private void OkButton_Clicked(object sender, RoutedEventArgs e)
        {
            InputTaskCompletionSource.SetResult(true);
            PopupContainer.IsOpen = false;
        }

        private void CancelButton_Clicked(object sender, RoutedEventArgs e)
        {
            InputTaskCompletionSource.SetResult(false);
            PopupContainer.IsOpen = false;
        }

        #endregion
        #region Properties

        public string Title { get; private set; }

        public string Value
        {
            get { return InputTextBox.Text; }
            private set { InputTextBox.Text = value; }
        }

        #endregion
    }
}
