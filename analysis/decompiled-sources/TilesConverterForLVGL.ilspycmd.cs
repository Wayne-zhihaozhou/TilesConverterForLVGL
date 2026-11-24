using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Markup;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: AssemblyTitle("lvglConverter")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("lvglConverter")]
[assembly: AssemblyCopyright("Copyright ©  2021")]
[assembly: AssemblyTrademark("")]
[assembly: ComVisible(false)]
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: TargetFramework(".NETFramework,Version=v4.7.2", FrameworkDisplayName = ".NET Framework 4.7.2")]
[assembly: AssemblyVersion("1.0.0.0")]
namespace lvglConverter
{
	public class App : System.Windows.Application
	{
		[DebuggerNonUserCode]
		[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
		public void InitializeComponent()
		{
			StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
		}

		[STAThread]
		[DebuggerNonUserCode]
		[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
		public static void Main()
		{
			App app = new App();
			app.InitializeComponent();
			app.Run();
		}
	}
	public class MainWindow : Window, IComponentConnector
	{
		private List<string> UnhandledFiles = new List<string>();

		internal System.Windows.Controls.TextBox txtFolderPath;

		internal System.Windows.Controls.Button btnSelectFolder;

		internal System.Windows.Controls.ComboBox cbType;

		internal System.Windows.Controls.Button btnHandle;

		internal System.Windows.Controls.Button btnStart;

		internal System.Windows.Controls.CheckBox cbIgnoreExistedFile;

		internal TextBlock txtTip;

		internal TextBlock txtProgress;

		internal System.Windows.Controls.ProgressBar gdProgress;

		private bool _contentLoaded;

		private string FolderPath
		{
			get
			{
				return txtFolderPath.Text;
			}
			set
			{
				txtFolderPath.Text = value;
			}
		}

		public MainWindow()
		{
			InitializeComponent();
			Loaded += MainWindow_Loaded;
			Closing += MainWindow_Closing;
		}

		private void MainWindow_Closing(object sender, CancelEventArgs e)
		{
			Environment.Exit(0);
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			cbType.Items.Add("RGB332 Not Available");
			cbType.Items.Add("Binary RGB565");
			cbType.Items.Add("Binary RGB565 Swap");
			cbType.Items.Add("RGB888 Not Available");
			cbType.SelectedIndex = 1;
		}

		private void btnSelectFolder_Click(object sender, RoutedEventArgs e)
		{
			FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
			if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				FolderPath = folderBrowserDialog.SelectedPath;
			}
		}

		private void GetTileFiles(string folderPath, List<string> path)
		{
			string[] files = Directory.GetFiles(folderPath);
			foreach (string text in files)
			{
				FileInfo fileInfo = new FileInfo(text);
				int result = -1;
				int.TryParse(fileInfo.Name.Replace(fileInfo.Extension, ""), out result);
				if (fileInfo.Extension.ToLower().IndexOf("png") > 0)
				{
					path.Add(text);
				}
			}
			files = Directory.GetDirectories(folderPath);
			foreach (string text2 in files)
			{
				DirectoryInfo directoryInfo = new DirectoryInfo(text2);
				int result2 = -1;
				int.TryParse(directoryInfo.Name, out result2);
				if (result2 >= 0)
				{
					GetTileFiles(text2, path);
				}
			}
		}

		private void ThreadProcess()
		{
			Dispatcher.Invoke(() =>
			{
				txtTip.Text = $"Scanning ...";
			});
			List<string> tileFilePath = new List<string>();
			string folderPath = "";
			Dispatcher.Invoke(() =>
			{
				folderPath = FolderPath;
			});
			GetTileFiles(folderPath, tileFilePath);
			Dispatcher.Invoke(() =>
			{
				if (tileFilePath.Count == 0)
				{
					txtTip.Text = $"Empty folder.";
				}
				else
				{
					txtTip.Text = $"File count : {tileFilePath.Count}";
				}
				gdProgress.Maximum = tileFilePath.Count;
			});
			if (tileFilePath.Count == 0)
			{
				return;
			}
			Thread.Sleep(500);
			int num = 16;
			int num2 = tileFilePath.Count / num;
			int num3 = tileFilePath.Count - num2 * num;
			List<FileHandler> fileHandlers = new List<FileHandler>();
			LVGLImageType targetType = LVGLImageType.RGB565;
			bool ignoreFileEnable = false;
			Dispatcher.Invoke(() =>
			{
				switch (cbType.SelectedIndex)
				{
				case 0:
					targetType = LVGLImageType.RGB332;
					break;
				case 1:
					targetType = LVGLImageType.RGB565;
					break;
				case 2:
					targetType = LVGLImageType.RGB565SWAP;
					break;
				case 3:
					targetType = LVGLImageType.RGB888;
					break;
				}
				ignoreFileEnable = cbIgnoreExistedFile.IsChecked.Value;
			});
			for (int num4 = 0; num4 < num; num4++)
			{
				FileHandler fileHandler = new FileHandler(tileFilePath, num4 * num2, num2 + ((num4 == num - 1) ? num3 : 0), targetType, ignoreFileEnable);
				fileHandlers.Add(fileHandler);
				new Thread(fileHandler.Start).Start();
			}
			int totalHandledCount = 0;
			while (true)
			{
				bool allCompleted = true;
				Dispatcher.Invoke(() =>
				{
					totalHandledCount = 0;
					foreach (FileHandler item in fileHandlers)
					{
						totalHandledCount += item.HandledCount;
						allCompleted &= item.IsCompleted;
					}
					int num5 = 100 * totalHandledCount / tileFilePath.Count;
					txtProgress.Text = $"{num5.ToString()}%";
					gdProgress.Value = totalHandledCount;
					txtTip.Text = $"Converting ... ({totalHandledCount}/{tileFilePath.Count})";
				});
				if (allCompleted)
				{
					break;
				}
				Thread.Sleep(10);
			}
			Dispatcher.Invoke(() =>
			{
				foreach (FileHandler item2 in fileHandlers)
				{
					UnhandledFiles.AddRange(item2.UnhandledFiles);
				}
				if (UnhandledFiles.Count > 0)
				{
					btnStart.Visibility = Visibility.Hidden;
					txtTip.Text = $"Conversion Completed. {totalHandledCount} OK, {tileFilePath.Count - totalHandledCount} Error(s)";
				}
				else
				{
					txtTip.Text = $"Conversion Completed. All Done !";
				}
			});
		}

		private void btnStart_Click(object sender, RoutedEventArgs e)
		{
			if (FolderPath.Length > 0)
			{
				if (cbType.SelectedIndex == 0 || cbType.SelectedIndex == 3)
				{
					System.Windows.MessageBox.Show("Target type is not available now.", "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
					return;
				}
				cbType.IsEnabled = false;
				btnSelectFolder.IsEnabled = false;
				btnStart.IsEnabled = false;
				cbIgnoreExistedFile.IsEnabled = false;
				new Thread(ThreadProcess).Start();
			}
		}

		private void btnHandle_Click(object sender, RoutedEventArgs e)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("These files will be deleted :");
			foreach (string unhandledFile in UnhandledFiles)
			{
				stringBuilder.Append("\r\n" + unhandledFile);
			}
			if (System.Windows.MessageBox.Show(stringBuilder.ToString(), "Tip", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
			{
				return;
			}
			btnHandle.IsEnabled = false;
			foreach (string unhandledFile2 in UnhandledFiles)
			{
				File.Delete(unhandledFile2);
			}
			btnHandle.Content = "All done !";
		}

		[DebuggerNonUserCode]
		[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
		public void InitializeComponent()
		{
			if (!_contentLoaded)
			{
				_contentLoaded = true;
				Uri resourceLocator = new Uri("/lvglConverter;component/mainwindow.xaml", UriKind.Relative);
				System.Windows.Application.LoadComponent(this, resourceLocator);
			}
		}

		[DebuggerNonUserCode]
		[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		void IComponentConnector.Connect(int connectionId, object target)
		{
			switch (connectionId)
			{
			case 1:
				txtFolderPath = (System.Windows.Controls.TextBox)target;
				break;
			case 2:
				btnSelectFolder = (System.Windows.Controls.Button)target;
				btnSelectFolder.Click += btnSelectFolder_Click;
				break;
			case 3:
				cbType = (System.Windows.Controls.ComboBox)target;
				break;
			case 4:
				btnHandle = (System.Windows.Controls.Button)target;
				btnHandle.Click += btnHandle_Click;
				break;
			case 5:
				btnStart = (System.Windows.Controls.Button)target;
				btnStart.Click += btnStart_Click;
				break;
			case 6:
				cbIgnoreExistedFile = (System.Windows.Controls.CheckBox)target;
				break;
			case 7:
				txtTip = (TextBlock)target;
				break;
			case 8:
				txtProgress = (TextBlock)target;
				break;
			case 9:
				gdProgress = (System.Windows.Controls.ProgressBar)target;
				break;
			default:
				_contentLoaded = true;
				break;
			}
		}
	}
	public enum LVGLImageType
	{
		RGB332,
		RGB565,
		RGB565SWAP,
		RGB888
	}
	public class FileHandler
	{
		public List<string> Path;

		public List<string> UnhandledFiles = new List<string>();

		public int StartIndex;

		public int Length;

		public int HandledCount;

		public bool IgnoreExistedFile;

		public bool IsCompleted;

		public LVGLImageType Type = LVGLImageType.RGB565;

		public FileHandler(List<string> path, int startIndex, int length, LVGLImageType type, bool ignoreExistedFile)
		{
			Path = path;
			StartIndex = startIndex;
			Length = length;
			Type = type;
			IgnoreExistedFile = ignoreExistedFile;
		}

		private List<Color> GetPixels(string path, out int width, out int height)
		{
			List<Color> list = new List<Color>();
			Bitmap bitmap = new Bitmap(path);
			width = bitmap.Width;
			height = bitmap.Height;
			for (int i = 0; i < bitmap.Height; i++)
			{
				for (int j = 0; j < bitmap.Width; j++)
				{
					list.Add(bitmap.GetPixel(j, i));
				}
			}
			return list;
		}

		private byte[] RGB565GetBinary(List<Color> pixels, int width, int height)
		{
			List<byte> list = new List<byte>();
			uint num = 0u;
			num = (uint)height;
			num <<= 11;
			num |= (uint)width;
			num <<= 2;
			num <<= 3;
			num <<= 5;
			num |= 4;
			list.Add((byte)(num & 0xFF));
			list.Add((byte)((num >> 8) & 0xFF));
			list.Add((byte)((num >> 16) & 0xFF));
			list.Add((byte)((num >> 24) & 0xFF));
			for (int i = 0; i < pixels.Count; i++)
			{
				int num2 = 0;
				num2 |= pixels[i].R >> 3;
				num2 <<= 6;
				num2 |= pixels[i].G >> 2;
				num2 <<= 5;
				num2 |= pixels[i].B >> 3;
				list.Add((byte)(num2 & 0xFF));
				list.Add((byte)((num2 >> 8) & 0xFF));
			}
			return list.ToArray();
		}

		private byte[] RGB565SwapGetBinary(List<Color> pixels, int width, int height)
		{
			List<byte> list = new List<byte>();
			uint num = 0u;
			num = (uint)height;
			num <<= 11;
			num |= (uint)width;
			num <<= 2;
			num <<= 3;
			num <<= 5;
			num |= 4;
			list.Add((byte)(num & 0xFF));
			list.Add((byte)((num >> 8) & 0xFF));
			list.Add((byte)((num >> 16) & 0xFF));
			list.Add((byte)((num >> 24) & 0xFF));
			for (int i = 0; i < pixels.Count; i++)
			{
				int num2 = 0;
				num2 |= pixels[i].R >> 3;
				num2 <<= 6;
				num2 |= pixels[i].G >> 2;
				num2 <<= 5;
				num2 |= pixels[i].B >> 3;
				list.Add((byte)((num2 >> 8) & 0xFF));
				list.Add((byte)(num2 & 0xFF));
			}
			return list.ToArray();
		}

		private byte[] GetBinary(List<Color> pixels, int width, int height)
		{
			return Type switch
			{
				LVGLImageType.RGB332 => null, 
				LVGLImageType.RGB565 => RGB565GetBinary(pixels, width, height), 
				LVGLImageType.RGB565SWAP => RGB565SwapGetBinary(pixels, width, height), 
				LVGLImageType.RGB888 => null, 
				_ => null, 
			};
		}

		public void Start()
		{
			for (int i = 0; i < Length; i++)
			{
				string text = Path[i + StartIndex];
				int width = 0;
				int height = 0;
				try
				{
					FileInfo fileInfo = new FileInfo(text);
					string text2 = fileInfo.DirectoryName + "\\" + fileInfo.Name.Replace(fileInfo.Extension, "") + ".bin";
					FileInfo fileInfo2 = new FileInfo(text2);
					if (!IgnoreExistedFile || !File.Exists(text2) || fileInfo2.Length != 131076)
					{
						List<Color> pixels = GetPixels(text, out width, out height);
						byte[] binary = GetBinary(pixels, width, height);
						FileStream fileStream = new FileStream(text2, FileMode.Create);
						fileStream.Write(binary, 0, binary.Length);
						fileStream.Close();
					}
					HandledCount++;
				}
				catch (Exception)
				{
					UnhandledFiles.Add(text);
				}
			}
			IsCompleted = true;
		}
	}
}
namespace lvglConverter.Properties
{
	[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
	[DebuggerNonUserCode]
	[CompilerGenerated]
	internal class Resources
	{
		private static ResourceManager resourceMan;

		private static CultureInfo resourceCulture;

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static ResourceManager ResourceManager
		{
			get
			{
				if (resourceMan == null)
				{
					resourceMan = new ResourceManager("lvglConverter.Properties.Resources", typeof(Resources).Assembly);
				}
				return resourceMan;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static CultureInfo Culture
		{
			get
			{
				return resourceCulture;
			}
			set
			{
				resourceCulture = value;
			}
		}

		internal Resources()
		{
		}
	}
	[CompilerGenerated]
	[GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "11.0.0.0")]
	internal sealed class Settings : ApplicationSettingsBase
	{
		private static Settings defaultInstance = (Settings)SettingsBase.Synchronized(new Settings());

		public static Settings Default => defaultInstance;
	}
}
