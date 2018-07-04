using System.Windows;

namespace Intaller.Wpf.UIServices
{
    public class WpfMessageBoxService : IMessageBoxService
    {
        #region IMessageBoxService Members

        /// <summary>
        /// Displays an error dialog with a given message.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        public void ShowError(string message)
        {
            ShowMessage(message, "Error", CustomDialogIcons.Stop);
        }

        /// <summary>
        /// Displays an error dialog with a given message and caption.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="caption">The caption of the message box window</param>
        public void ShowError(string message, string caption)
        {
            ShowMessage(message, caption, CustomDialogIcons.Stop);
        }

        /// <summary>
        /// Displays an error dialog with a given message.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        public void ShowInformation(string message)
        {
            ShowMessage(message, "Information", CustomDialogIcons.Information);
        }

        /// <summary>
        /// Displays an error dialog with a given message.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="caption">The caption of the message box window</param>
        public void ShowInformation(string message, string caption)
        {
            ShowMessage(message, caption, CustomDialogIcons.Information);
        }

        /// <summary>
        /// Displays an error dialog with a given message.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        public void ShowWarning(string message)
        {
            ShowMessage(message, "Warning", CustomDialogIcons.Warning);
        }

        /// <summary>
        /// Displays an error dialog with a given message.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="caption">The caption of the message box window</param>
        public void ShowWarning(string message, string caption)
        {
            ShowMessage(message, caption, CustomDialogIcons.Warning);
        }

        /// <summary>
        /// Displays a Yes/No dialog and returns the user input.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="icon">The icon to be displayed.</param>
        /// <returns>User selection.</returns>
        public CustomDialogResults ShowYesNo(string message, CustomDialogIcons icon)
        {
            return ShowQuestionWithButton(message, icon, CustomDialogButtons.YesNo);
        }

        /// <summary>
        /// Displays a Yes/No dialog and returns the user input.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="caption">The caption of the message box window</param>
        /// <param name="icon">The icon to be displayed.</param>
        /// <returns>User selection.</returns>
        public CustomDialogResults ShowYesNo(string message, string caption, CustomDialogIcons icon)
        {
            return ShowQuestionWithButton(message, caption, icon, CustomDialogButtons.YesNo);
        }

        /// <summary>
        /// Displays a Yes/No dialog with a default button selected, and returns the user input.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="caption">The caption of the message box window</param>
        /// <param name="icon">The icon to be displayed.</param>
        /// <param name="defaultResult">Default result for the message box</param>
        /// <returns>User selection.</returns>
        public CustomDialogResults ShowYesNo(string message, string caption, CustomDialogIcons icon, CustomDialogResults defaultResult)
        {
            return ShowQuestionWithButton(message, caption, icon, CustomDialogButtons.YesNo, defaultResult);
        }

        /// <summary>
        /// Displays a Yes/No/Cancel dialog and returns the user input.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="icon">The icon to be displayed.</param>
        /// <returns>User selection.</returns>
        public CustomDialogResults ShowYesNoCancel(string message, CustomDialogIcons icon)
        {
            return ShowQuestionWithButton(message, icon, CustomDialogButtons.YesNoCancel);
        }


        /// <summary>
        /// Displays a Yes/No/Cancel dialog and returns the user input.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="caption">The caption of the message box window</param>
        /// <param name="icon">The icon to be displayed.</param>
        /// <returns>User selection.</returns>
        public CustomDialogResults ShowYesNoCancel(string message, string caption, CustomDialogIcons icon)
        {
            return ShowQuestionWithButton(message, caption, icon, CustomDialogButtons.YesNoCancel);
        }

        /// <summary>
        /// Displays a Yes/No/Cancel dialog with a default button selected, and returns the user input.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="caption">The caption of the message box window</param>
        /// <param name="icon">The icon to be displayed.</param>
        /// <param name="defaultResult">Default result for the message box</param>
        /// <returns>User selection.</returns>
        public CustomDialogResults ShowYesNoCancel(string message, string caption, CustomDialogIcons icon, CustomDialogResults defaultResult)
        {
            return ShowQuestionWithButton(message, caption, icon, CustomDialogButtons.YesNoCancel, defaultResult);
        }

        /// <summary>
        /// Displays a OK/Cancel dialog and returns the user input.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="icon">The icon to be displayed.</param>
        /// <returns>User selection.</returns>
        public CustomDialogResults ShowOkCancel(string message, CustomDialogIcons icon)
        {
            return ShowQuestionWithButton(message, icon, CustomDialogButtons.OKCancel);
        }

        /// <summary>
        /// Displays a OK/Cancel dialog and returns the user input.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="caption">The caption of the message box window</param>
        /// <param name="icon">The icon to be displayed.</param>
        /// <returns>User selection.</returns>
        public CustomDialogResults ShowOkCancel(string message, string caption, CustomDialogIcons icon)
        {
            return ShowQuestionWithButton(message, caption, icon, CustomDialogButtons.OKCancel);
        }

        /// <summary>
        /// Displays a OK/Cancel dialog with a default result selected, and returns the user input.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="caption">The caption of the message box window</param>
        /// <param name="icon">The icon to be displayed.</param>
        /// <param name="defaultResult">Default result for the message box</param>
        /// <returns>User selection.</returns>
        public CustomDialogResults ShowOkCancel(string message, string caption, CustomDialogIcons icon, CustomDialogResults defaultResult)
        {
            return ShowQuestionWithButton(message, caption, icon, CustomDialogButtons.OKCancel, defaultResult);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Shows a standard System.Windows.MessageBox using the parameters requested
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="caption">The heading to be displayed</param>
        /// <param name="icon">The icon to be displayed.</param>
        private void ShowMessage(string message, string caption, CustomDialogIcons icon)
        {
            MessageBox.Show(message, caption, MessageBoxButton.OK, GetImage(icon));
        }


        /// <summary>
        /// Shows a standard System.Windows.MessageBox using the parameters requested
        /// but will return a translated result to enable adhere to the IMessageBoxService
        /// implementation required. 
        /// 
        /// This abstraction allows for different frameworks to use the same ViewModels but supply
        /// alternative implementations of core service interfaces
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="icon">The icon to be displayed.</param>
        /// <param name="button"></param>
        /// <returns>CustomDialogResults results to use</returns>
        private CustomDialogResults ShowQuestionWithButton(string message,
            CustomDialogIcons icon, CustomDialogButtons button)
        {
            MessageBoxResult result = MessageBox.Show(message, "Please confirm...",
                GetButton(button), GetImage(icon));
            return GetResult(result);
        }


        /// <summary>
        /// Shows a standard System.Windows.MessageBox using the parameters requested
        /// but will return a translated result to enable adhere to the IMessageBoxService
        /// implementation required. 
        /// 
        /// This abstraction allows for different frameworks to use the same ViewModels but supply
        /// alternative implementations of core service interfaces
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="caption">The caption of the message box window</param>
        /// <param name="icon">The icon to be displayed.</param>
        /// <param name="button"></param>
        /// <returns>CustomDialogResults results to use</returns>
        private CustomDialogResults ShowQuestionWithButton(string message, string caption,
            CustomDialogIcons icon, CustomDialogButtons button)
        {
            MessageBoxResult result = MessageBox.Show(message, caption,
                GetButton(button), GetImage(icon));
            return GetResult(result);
        }

        /// <summary>
        /// Shows a standard System.Windows.MessageBox using the parameters requested
        /// but will return a translated result to enable adhere to the IMessageBoxService
        /// implementation required. 
        /// 
        /// This abstraction allows for different frameworks to use the same ViewModels but supply
        /// alternative implementations of core service interfaces
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="caption">The caption of the message box window</param>
        /// <param name="icon">The icon to be displayed.</param>
        /// <param name="button"></param>
        /// <param name="defaultResult">Default result for the message box</param>
        /// <returns>CustomDialogResults results to use</returns>
        private CustomDialogResults ShowQuestionWithButton(string message, string caption,
            CustomDialogIcons icon, CustomDialogButtons button, CustomDialogResults defaultResult)
        {
            MessageBoxResult result = MessageBox.Show(message, caption,
                GetButton(button), GetImage(icon), GetResult(defaultResult));
            return GetResult(result);
        }


        /// <summary>
        /// Translates a CustomDialogIcons into a standard WPF System.Windows.MessageBox MessageBoxImage.
        /// This abstraction allows for different frameworks to use the same ViewModels but supply
        /// alternative implementations of core service interfaces
        /// </summary>
        /// <param name="icon">The icon to be displayed.</param>
        /// <returns>A standard WPF System.Windows.MessageBox MessageBoxImage</returns>
        private MessageBoxImage GetImage(CustomDialogIcons icon)
        {
            MessageBoxImage image = MessageBoxImage.None;

            switch (icon)
            {
                case CustomDialogIcons.Information:
                    image = MessageBoxImage.Information;
                    break;
                case CustomDialogIcons.Question:
                    image = MessageBoxImage.Question;
                    break;
                case CustomDialogIcons.Exclamation:
                    image = MessageBoxImage.Exclamation;
                    break;
                case CustomDialogIcons.Stop:
                    image = MessageBoxImage.Stop;
                    break;
                case CustomDialogIcons.Warning:
                    image = MessageBoxImage.Warning;
                    break;
            }
            return image;
        }


        /// <summary>
        /// Translates a CustomDialogButtons into a standard WPF System.Windows.MessageBox MessageBoxButton.
        /// This abstraction allows for different frameworks to use the same ViewModels but supply
        /// alternative implementations of core service interfaces
        /// </summary>
        /// <param name="btn">The button type to be displayed.</param>
        /// <returns>A standard WPF System.Windows.MessageBox MessageBoxButton</returns>
        private MessageBoxButton GetButton(CustomDialogButtons btn)
        {
            MessageBoxButton button = MessageBoxButton.OK;

            switch (btn)
            {
                case CustomDialogButtons.OK:
                    button = MessageBoxButton.OK;
                    break;
                case CustomDialogButtons.OKCancel:
                    button = MessageBoxButton.OKCancel;
                    break;
                case CustomDialogButtons.YesNo:
                    button = MessageBoxButton.YesNo;
                    break;
                case CustomDialogButtons.YesNoCancel:
                    button = MessageBoxButton.YesNoCancel;
                    break;
            }
            return button;
        }


        /// <summary>
        /// Translates a standard WPF System.Windows.MessageBox MessageBoxResult into a
        /// CustomDialogIcons.
        /// This abstraction allows for different frameworks to use the same ViewModels but supply
        /// alternative implementations of core service interfaces
        /// </summary>
        /// <param name="result">The standard WPF System.Windows.MessageBox MessageBoxResult</param>
        /// <returns>CustomDialogResults results to use</returns>
        private CustomDialogResults GetResult(MessageBoxResult result)
        {
            CustomDialogResults customDialogResults = CustomDialogResults.None;

            switch (result)
            {
                case MessageBoxResult.Cancel:
                    customDialogResults = CustomDialogResults.Cancel;
                    break;
                case MessageBoxResult.No:
                    customDialogResults = CustomDialogResults.No;
                    break;
                case MessageBoxResult.None:
                    customDialogResults = CustomDialogResults.None;
                    break;
                case MessageBoxResult.OK:
                    customDialogResults = CustomDialogResults.OK;
                    break;
                case MessageBoxResult.Yes:
                    customDialogResults = CustomDialogResults.Yes;
                    break;
            }
            return customDialogResults;
        }

        /// <summary>
        /// Translates a CustomDialogResults into a standard WPF System.Windows.MessageBox MessageBoxResult
        /// This abstraction allows for different frameworks to use the same ViewModels but supply
        /// alternative implementations of core service interfaces
        /// </summary>
        /// <param name="result">The CustomDialogResults</param>
        /// <returns>The standard WPF System.Windows.MessageBox MessageBoxResult results to use</returns>
        private MessageBoxResult GetResult(CustomDialogResults result)
        {
            MessageBoxResult customDialogResults = MessageBoxResult.None;

            switch (result)
            {
                case CustomDialogResults.Cancel:
                    customDialogResults = MessageBoxResult.Cancel;
                    break;
                case CustomDialogResults.No:
                    customDialogResults = MessageBoxResult.No;
                    break;
                case CustomDialogResults.None:
                    customDialogResults = MessageBoxResult.None;
                    break;
                case CustomDialogResults.OK:
                    customDialogResults = MessageBoxResult.OK;
                    break;
                case CustomDialogResults.Yes:
                    customDialogResults = MessageBoxResult.Yes;
                    break;
            }
            return customDialogResults;
        }
        #endregion
    }
}