using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;
using GoMusicNowDownloader.Properties;
using Ookii.Dialogs.Wpf;

namespace GoMusicNowDownloader
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private const string TxtUrlOfLinksHelperText = "Paste the URL to a GoMusicNow 'Links' page here";
		public string LocalDownloadPath { get; set; }	


		public MainWindow()
		{
			InitializeComponent();

			txtUrlOfLinks.Text = TxtUrlOfLinksHelperText;
			txtLoginEmail.Text = Settings.Default.GoMusicNowUsername;
			txtLoginPassword.Password = Settings.Default.GoMusicNowPassword;
			txtLocalMusicRootFolder.Text = Settings.Default.LocalMusicFolder;
		}


		
		private void Start_Click(object sender, RoutedEventArgs evt)
		{
			Downloader.LoginEmail = txtLoginEmail.Text;
			Downloader.LoginPassword = txtLoginPassword.Password;
			Downloader.LinksUrl = txtUrlOfLinks.Text;
			Downloader.LocalFolder = txtLocalMusicRootFolder.Text;

			//Subscribe to the Downloader's Progress and Error events
			Downloader.FetchingTrackList += FetchingTrackListHandler;
			Downloader.ProgressChanged += ProgressChangedHandler;
			Downloader.Error += ErrorHandler;
			Downloader.Cancelling += CancellingHandler;
			Downloader.Cancelled += CancelledHandler;
			Downloader.Finished += FinishedHandler;
			
			//save settings
			Settings.Default.GoMusicNowUsername = txtLoginEmail.Text;
			Settings.Default.GoMusicNowPassword = txtLoginPassword.Password;
			Settings.Default.LocalMusicFolder = txtLocalMusicRootFolder.Text;
			Settings.Default.Save();

			//Show 'Cancel' button instead of 'Start'
			btnStartDownload.Visibility = Visibility.Collapsed;
			btnCancelDownload.Visibility = Visibility.Visible;

			try
			{
				Downloader.Begin();
			}
			catch (UnauthorizedAccessException)
			{
				lblProgress.Content = string.Format("Error writing to the path \"{0}\"", txtLocalMusicRootFolder.Text);
			}
			catch (Exception err)
			{
				lblProgress.Content = string.Format("Error. {0}", err.Message);
			}
		}


		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			ShowStartButton();
			Downloader.CancelDownloads();
		}



		#region Event handling to update the user on what is happening

		private void FetchingTrackListHandler(object sender, EventArgs e)
		{
			lblProgress.Content = "Getting list of tracks";
		}


		private void CancellingHandler(object sender, EventArgs e)
		{
			lblProgress.Content = "Cancelling";
		}


		private void CancelledHandler(object sender, EventArgs e)
		{
			ShowStartButton();
			lblProgress.Content = "";
		}


		private void FinishedHandler(object sender, EventArgs e)
		{
			ShowStartButton();
			lblProgress.Content = "Finished";
		}


		private void ProgressChangedHandler(object sender, EventArgs e)
		{
			var args = (ProgressEventArgs) e;
			lblProgress.Content = string.Format("Downloading {0} / {1}", args.FileNumber, args.TotalNumber);
		}


		private void ErrorHandler(object sender, EventArgs e)
		{
			lblProgress.Content = string.Format("Error getting list of track from {0}", txtUrlOfLinks.Text);
			ShowStartButton();
		}

		#endregion



		private void ShowStartButton()
		{
			btnStartDownload.Visibility = Visibility.Visible;
			btnCancelDownload.Visibility = Visibility.Collapsed;
			EnableOrDisableStartDownload();
		}


		private void txtUrlOfLinks_GotFocus(object sender, RoutedEventArgs e)
		{
			if (txtUrlOfLinks.Text == TxtUrlOfLinksHelperText)
			{
				txtUrlOfLinks.Text = "";
			}
			txtUrlOfLinks.Foreground = Brushes.Black;
			lblProgress.Content = "";
		}


		private void txtUrlOfLinks_LostFocus(object sender, RoutedEventArgs e)
		{
			if (txtUrlOfLinks.Text == "")
			{
				txtUrlOfLinks.Text = TxtUrlOfLinksHelperText;
				txtUrlOfLinks.Foreground = Brushes.DimGray;
			}
		}

		private void Browse_Click(object sender, RoutedEventArgs e)
		{
			lblProgress.Content = "";

			var dialog = new VistaFolderBrowserDialog();
			dialog.SelectedPath = txtLocalMusicRootFolder.Text;
			dialog.ShowDialog();
			txtLocalMusicRootFolder.Text = dialog.SelectedPath;
		}


		private void txtUrlOfLinks_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			EnableOrDisableStartDownload();
		}


		private void txtLocalMusicRootFolder_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			EnableOrDisableStartDownload();
		}


		private void EnableOrDisableStartDownload()
		{
			if (btnStartDownload != null)
			{
				Uri uri;
				btnStartDownload.IsEnabled = (Uri.TryCreate(txtUrlOfLinks.Text, UriKind.Absolute, out uri)
											  && txtUrlOfLinks.Text.StartsWith("http")
											  && txtLocalMusicRootFolder.Text.Length > 0
											 );
			}
		}

		private void OpenBrowserToGoMusicNow(object sender, RequestNavigateEventArgs e)
		{
			Process.Start(new ProcessStartInfo(weblink.NavigateUri.ToString()));
			e.Handled = true;
		}

	}



	public class DownloadableItem
	{
		public Guid Id { get; set; }
		public string LocalPath { get; set; }
		public string RemotePath { get; set; }
		public bool Complete { get; set; }
	}



	public class DownloadableItems : List<DownloadableItem> {}


	//From http://stackoverflow.com/questions/3258044/c-sharp-webrequest-http-post-with-cookie-port-from-curl-script
	public class WebClientEx : WebClient
	{
		private CookieContainer _cookieContainer = new CookieContainer();

		protected override WebRequest GetWebRequest(Uri address)
		{
			var request = base.GetWebRequest(address);
			if (request is HttpWebRequest)
			{
				(request as HttpWebRequest).CookieContainer = _cookieContainer;
			}
			return request;
		}
	}
}
