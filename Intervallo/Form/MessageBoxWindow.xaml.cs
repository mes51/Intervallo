using Intervallo.Config;
using Intervallo.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Intervallo.Form
{
    /// <summary>
    /// MessageBoxWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MessageBoxWindow : Window
    {
#if DEBUG
        const bool ForceVisibleException = true;
#else
        const bool ForceVisibleException = false;
#endif

        public class MessageBoxResultEventArgs : EventArgs
        {
            public MessageBoxResultEventArgs(MessageBoxResult result)
            {
                Result = result;
            }

            public MessageBoxResult Result { get; }
        }

        public delegate void MessageBoxWindowResultEventHandler(object sender, MessageBoxResultEventArgs e);

        public MessageBoxWindow()
        {
            InitializeComponent();
        }

        public event MessageBoxWindowResultEventHandler RecieveResult;

        SystemSound Sound { get; set; }

        bool FixedResult { get; set; }

        MessageBoxButton Buttons { get; set; }

        void ShowExceptionExpander(Exception exception)
        {
            if (!ForceVisibleException && !ApplicationSettings.Setting.General.ShowExceptionInMessageBox)
            {
                return;
            }

            MessageTextBlock.Margin = new Thickness(MessageTextBlock.Margin.Left, MessageTextBlock.Margin.Top, MessageTextBlock.Margin.Right, 0.0);
            ExceptionInfomation.Visibility = Visibility.Visible;
            ExceptionInformationText.Text = exception.ToString();
        }

        void OnReceiveResult(MessageBoxResult result)
        {
            RecieveResult?.Invoke(this, new MessageBoxResultEventArgs(result));
        }

        void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FixedResult = true;
            Close();
            OnReceiveResult((MessageBoxResult)e.Parameter);
        }

        void Window_Activated(object sender, EventArgs e)
        {
            Sound?.Play();
            Activated -= Window_Activated;
        }

        void Window_Closed(object sender, EventArgs e)
        {
            if (!FixedResult)
            {
                switch (Buttons)
                {
                    case MessageBoxButton.OKCancel:
                    case MessageBoxButton.YesNoCancel:
                        OnReceiveResult(MessageBoxResult.Cancel);
                        break;
                    case MessageBoxButton.YesNo:
                        OnReceiveResult(MessageBoxResult.No);
                        break;
                    default:
                        OnReceiveResult(MessageBoxResult.OK);
                        break;
                }
            }
        }

        internal static MessageBoxWindow CreateWindow(string message, string caption, MessageBoxButton button, MessageBoxImage icon, SystemSound sound, Exception exception)
        {
            MessageBoxWindow window = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                window = new MessageBoxWindow();
                window.Title = caption;
                window.MessageTextBlock.Text = message;

                window.Buttons = button;
                switch (button)
                {
                    case MessageBoxButton.OK:
                        window.OKButton.Visibility = Visibility.Visible;
                        break;
                    case MessageBoxButton.OKCancel:
                        window.OKButton.Visibility = Visibility.Visible;
                        window.CancelButton.Visibility = Visibility.Visible;
                        break;
                    case MessageBoxButton.YesNo:
                        window.YesButton.Visibility = Visibility.Visible;
                        window.NoButton.Visibility = Visibility.Visible;
                        break;
                    case MessageBoxButton.YesNoCancel:
                        window.YesButton.Visibility = Visibility.Visible;
                        window.NoButton.Visibility = Visibility.Visible;
                        window.CancelButton.Visibility = Visibility.Visible;
                        break;
                }

                var iconHandle = IntPtr.Zero;
                switch (icon)
                {
                    case MessageBoxImage.Hand:
                        iconHandle = SystemIcons.Hand.Handle;
                        break;
                    case MessageBoxImage.Question:
                        iconHandle = SystemIcons.Question.Handle;
                        break;
                    case MessageBoxImage.Exclamation:
                        iconHandle = SystemIcons.Exclamation.Handle;
                        break;
                    case MessageBoxImage.Asterisk:
                        iconHandle = SystemIcons.Asterisk.Handle;
                        break;
                }
                if (iconHandle != IntPtr.Zero)
                {
                    window.IconImage.Source = Imaging.CreateBitmapSourceFromHIcon(iconHandle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    window.IconImage.Visibility = Visibility.Visible;
                }

                if (exception != null)
                {
                    window.ShowExceptionExpander(exception);
                }

                window.Sound = sound;
            });

            return window;
        }
    }

    public class MessageBox
    {
        private MessageBox(MessageBoxWindow window)
        {
            Window = window;
            window.RecieveResult += MessageBox_RecieveResult;
        }

        Dictionary<MessageBoxResult, Action> Actions { get; } = new Dictionary<MessageBoxResult, Action>
        {
            [MessageBoxResult.OK] = () => { },
            [MessageBoxResult.Cancel] = () => { },
            [MessageBoxResult.Yes] = () => { },
            [MessageBoxResult.No] = () => { }
        };

        MessageBoxWindow Window { get; }

        public void AddOKAction(Action action)
        {
            Actions[MessageBoxResult.OK] += action;
        }

        public void AddCancelAction(Action action)
        {
            Actions[MessageBoxResult.Cancel] += action;
        }

        public void AddYesAction(Action action)
        {
            Actions[MessageBoxResult.Yes] += action;
        }

        public void AddNoAction(Action action)
        {
            Actions[MessageBoxResult.No] += action;
        }

        public void Show()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Window.ShowDialog();
            });
        }

        void MessageBox_RecieveResult(object sender, MessageBoxWindow.MessageBoxResultEventArgs e)
        {
            Actions[e.Result]();
        }

        public static void ShowError(string message, string caption = null, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Error, Exception exception = null)
        {
            CreateError(message, caption, button, icon, exception).Show();
        }

        public static MessageBox CreateError(string message, string caption = null, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Error, Exception exception = null)
        {
            return new MessageBox(MessageBoxWindow.CreateWindow(message, caption ?? LangResources.MessageBoxWindow_TitleError, button, icon, SystemSounds.Hand, exception));
        }

        public static void ShowWarning(string message, string caption = null, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Warning, Exception exception = null)
        {
            CreateWarning(message, caption, button, icon, exception).Show();
        }

        public static MessageBox CreateWarning(string message, string caption = null, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Warning, Exception exception = null)
        {
            return new MessageBox(MessageBoxWindow.CreateWindow(message, caption ?? LangResources.MessageBoxWindow_TitleWarning, button, icon, SystemSounds.Exclamation, exception));
        }

        public static void ShowInformation(string message, string caption = null, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information, Exception exception = null)
        {
            CreateInformation(message, caption, button, icon, exception).Show();
        }

        public static MessageBox CreateInformation(string message, string caption = null, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information, Exception exception = null)
        {
            return new MessageBox(MessageBoxWindow.CreateWindow(message, caption ?? LangResources.MessageBoxWindow_TitleInformation, button, icon, SystemSounds.Asterisk, exception));
        }

        public static void ShowQuestion(string message, string caption = null, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Question, Exception exception = null)
        {
            CreateQuestion(message, caption, button, icon, exception).Show();
        }

        public static MessageBox CreateQuestion(string message, string caption = null, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Question, Exception exception = null)
        {
            return new MessageBox(MessageBoxWindow.CreateWindow(message, caption ?? LangResources.MessageBoxWindow_TitleQuestion, button, icon, SystemSounds.Question, exception));
        }
    }
}
