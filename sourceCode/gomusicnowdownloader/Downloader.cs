using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace GoMusicNowDownloader
{

	public class ProgressEventArgs : EventArgs
	{
		public int FileNumber { get; set; }
		public int TotalNumber { get; set; }

		public ProgressEventArgs(int fileNumber, int totalNumber)
		{
			FileNumber = fileNumber;
			TotalNumber = totalNumber;
		}
	}


	static class Downloader
	{
		private static int totalLinks = 0;
		private static DownloadableItems downloadableItems = new DownloadableItems();
		private static int fileNumber = 0;
		private static WebClientEx client;

		public static string LoginEmail { get; set; }
		public static string LoginPassword { get; set; }
		public static string LinksUrl { get; set; }
		public static string LocalFolder { get; set; }

		public static event EventHandler FetchingTrackList = delegate { };
		public static event EventHandler ProgressChanged = delegate { };
		public static event EventHandler Error = delegate { };
		public static event EventHandler Cancelling = delegate { };
		public static event EventHandler Cancelled = delegate { };
		public static event EventHandler Finished = delegate { };

		private static bool _downloadCancel = false;


		public static void Begin()
		{
			if (!HasWritePermissionOnDir(LocalFolder))
			{
				throw new UnauthorizedAccessException();		
			}
			//We use a class-wide WebClientEx, because any subsequent request using the same WebClient will continue to use the authentication cookie set after logging in
			client = new WebClientEx();

			FetchingTrackList(null, null);

			var postData = new NameValueCollection
			{
				{ "login", LoginEmail },
				{ "password", LoginPassword },
				{ "submit", ""},
				{ "back", LinksUrl}
			};
			client.UploadValuesCompleted += StartMp3Downloads;
			client.UploadValuesAsync(new Uri("http://www.gomusicnow.com/login.html"), postData);
		}



		private static void StartMp3Downloads(object sender, UploadValuesCompletedEventArgs e)
		{
			_downloadCancel = false;

			if (e.Error != null)
			{
				Error(null, new EventArgs());
				return;
			}

			var rawLoginResponse = e.Result;
			
			var downloadedLinksRaw = Encoding.Default.GetString(rawLoginResponse);
			var regex = new Regex(@"https?.*/(.*\.mp3)");

			var match = Regex.Match(downloadedLinksRaw, @"https?.*/(.*\.mp3)");
			while (match.Success)
			{
				downloadableItems.Add(new DownloadableItem
				{
					Id = Guid.NewGuid(),
					RemotePath = match.Groups[0].ToString(),
					LocalPath = Path.Combine(LocalFolder, match.Groups[1].ToString()),
					Complete = false
				});
				match = match.NextMatch();
			}

			totalLinks = downloadableItems.Count();
			client.DownloadFileCompleted += DownloadFileCallback;

			DownloadNextFile();
		}


		public static void CancelDownloads()
		{
			_downloadCancel = true;
			Cancelling(null, null);
		}



		private static void DownloadNextFile()
		{
			if (_downloadCancel)
			{
				Cancelled(null, null);
				Reset();
				return;
			}

			if (fileNumber >= downloadableItems.Count())
			{
				Finished(null, null);
				Reset();
				return;
			}

			ProgressChanged(null, new ProgressEventArgs(fileNumber + 1, totalLinks));

			if (!File.Exists(downloadableItems[fileNumber].LocalPath))
			{
				client.DownloadFileAsync(new Uri(downloadableItems[fileNumber].RemotePath), downloadableItems[fileNumber].LocalPath);
				fileNumber++;
			} 
			else
			{
				fileNumber++;
				DownloadNextFile();
			}
				
		}



		private static void DownloadFileCallback(object sender, AsyncCompletedEventArgs e)
		{
			DownloadNextFile();
		}


		private static void Reset()
		{
			totalLinks = 0;
			downloadableItems = new DownloadableItems();
			fileNumber = 0;
			client.Dispose();
		}


		private static bool HasWritePermissionOnDir(string path)
		{
			try
			{
				using (var fs = File.Create(Path.Combine(path, "Access.txt"), 1, FileOptions.DeleteOnClose)) { }
				return true;
			}
			catch
			{
				return false;
			}
		}

	}

}
