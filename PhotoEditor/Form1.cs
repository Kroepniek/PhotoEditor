using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PhotoEditor
{
	public partial class frmMainDmon : Form
	{
		#region Variables
		CommonOpenFileDialog cofdBrowseDmon;

		frmManualDmon frmManualDmon;

		Filters filters = new Filters();

		Size maxResize = new Size(3840, 2160);

		Bitmap convImage;

		List<string> fileFilter;
		List<string> photoFilter;

		List<Action> loadedActions = new List<Action>();
		List<DefaultImage> loadedImages = new List<DefaultImage>();

		public static List<string> actions;

		Dictionary<string, string[]> editOptionsControlsNames;
		Dictionary<string, ImageFormat> imgFormat;

		bool converting = false;

		string outputDir = "";
		string outputExt = ".png";

		string renamed = "";
		#endregion


		public frmMainDmon()
		{
			InitializeComponent();

			frmManualDmon = new frmManualDmon();

			cofdBrowseDmon = new CommonOpenFileDialog
			{
				IsFolderPicker = true,
				InitialDirectory = fbdBrowserDmon.RootFolder.ToString(),
				Title = "Select Folder"
			};

			#region Data Init
			fileFilter = new List<string>()
			{
				".bmp",
				".gif",
				".ico",
				".jpg",
				".jpeg",
				".png"
			};

			photoFilter = filters.filters.ToList();

			editOptionsControlsNames = new Dictionary<string, string[]>()
			{
				["chbRenameDmon"] = new string[] { "txbRenameDmon" },
				["chbResizeDmon"] = new string[] { "nudResizeWidthDmon", "nudResizeHeightDmon", "lblResizeWidthDmon", "lblResizeHeightDmon" },
				["chbFillDmon"] = new string[] { "pnlFillColorDmon", "chbFillTransparentDmon" },
				["chbMarginDmon"] = new string[] { "nudMarginUpDmon", "nudMarginRightDmon", "nudMarginDownDmon", "nudMarginLeftDmon" },
				["chbExtensionDmon"] = new string[] { "cbbExtensionDmon" },
				["chbFilterDmon"] = new string[] { "cbbFilterDmon" }
			};

			imgFormat = new Dictionary<string, ImageFormat>()
			{
				[".bmp"] = ImageFormat.Bmp,
				[".gif"] = ImageFormat.Gif,
				[".ico"] = ImageFormat.Icon,
				[".jpg"] = ImageFormat.Jpeg,
				[".jpeg"] = ImageFormat.Jpeg,
				[".png"] = ImageFormat.Png
			};

			actions = new List<string>()
			{
				"Rename",
				"Resize",
				"Margin",
				"Extension",
				"Filter",
				"Crop",
				"Output",
				"Picture"
			};

			lblDirDmon.Text = "Browse folder...";
			lblOutputDirDmon.Text = "Browse folder...";
			#endregion
		}


		#region TreeView Events
		private void tvBrowserDmon_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if (converting)
				return;

			TreeNode newSelected = e.Node;

			lvBrowserDmon.Items.Clear();

			DirectoryInfo nodeDirInfo = (DirectoryInfo)newSelected.Tag;
			ListViewItem.ListViewSubItem[] subItems;
			ListViewItem item = null;

			foreach (DirectoryInfo dir in nodeDirInfo.GetDirectories())
			{
				item = new ListViewItem(dir.Name, 0);
				item.Tag = dir.FullName;

				subItems =
					new ListViewItem.ListViewSubItem[] {
						new ListViewItem.ListViewSubItem(item, "Folder"),
						new ListViewItem.ListViewSubItem(item, dir.LastAccessTime.ToShortDateString())
					};

				item.SubItems.AddRange(subItems);
				lvBrowserDmon.Items.Add(item);
			}

			foreach (FileInfo file in nodeDirInfo.GetFiles())
			{
				if (!fileFilter.Contains(file.Extension)) continue;

				item = new ListViewItem(file.Name, 1);
				item.Tag = file.FullName;

				subItems =
					new ListViewItem.ListViewSubItem[] {
						new ListViewItem.ListViewSubItem(item, file.Extension.Substring(1).ToUpper()),
						new ListViewItem.ListViewSubItem(item, file.LastAccessTime.ToShortDateString())
					};

				item.SubItems.AddRange(subItems);
				lvBrowserDmon.Items.Add(item);
			}

			lvDmon_SetColors(lvBrowserDmon, Color.FromArgb(42, 42, 42));
		}
		#endregion

		#region Button Events
		private void btnOutputBrowseDmon_Click(object sender, EventArgs e)
		{
			if (converting)
				return;

			if (cofdBrowseDmon.ShowDialog() == CommonFileDialogResult.Ok)
			{
				outputDir = cofdBrowseDmon.FileName;
				lblOutputDirDmon.Text = outputDir;

				if (lblOutputDirDmon.Text == lblDirDmon.Text)
				{
					MessageBox.Show(this, "You can not use input folder as output folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

					outputDir = Path.GetFullPath(Path.Combine(cofdBrowseDmon.FileName, "pE_Output"));
					lblOutputDirDmon.Text = outputDir;
				}

				tltToolTipDmon.SetToolTip(lblOutputDirDmon, outputDir);
			}
		}

		private void btnConvertDmon_Click(object sender, EventArgs e)
		{
			StartConverting();
		}

		private void btnManualDmon_Click(object sender, EventArgs e)
		{
			frmManualDmon.ShowDialog();
		}

		private void btnBrowseDmon_Click(object sender, EventArgs e)
		{
			if (converting)
				return;

			if (cofdBrowseDmon.ShowDialog() == CommonFileDialogResult.Ok)
			{
				lblDirDmon.Text = cofdBrowseDmon.FileName;
				PopulateTreeView(cofdBrowseDmon.FileName);

				outputDir = Path.GetFullPath(Path.Combine(cofdBrowseDmon.FileName, "pE_Output"));
				lblOutputDirDmon.Text = outputDir;

				tltToolTipDmon.SetToolTip(lblDirDmon, cofdBrowseDmon.FileName);
				tltToolTipDmon.SetToolTip(lblOutputDirDmon, outputDir);
			}
		}
		#endregion

		#region ListView Events
		private void lvDmon_SetColors(ListView sender, Color clr)
		{
			foreach (ListViewItem item in sender.Items)
			{
				if ((item.Index % 2) == 0)
				{
					item.BackColor = clr;
				}
				else
				{
					item.BackColor = Color.FromArgb(clr.R + 10, clr.G + 10, clr.B + 10);
				}
			}
		}

		private void lvDmon_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
		{
			e.Cancel = true;
			e.NewWidth = ((ListView)sender).Columns[e.ColumnIndex].Width;
		}

		private void lvBrowserDmon_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (converting)
				return;

			string selectedPath = ((ListView)sender).SelectedItems[0].Tag.ToString();

			int startIndex = selectedPath.LastIndexOf('.');

			if (startIndex == -1)
			{
				if (!Directory.Exists(selectedPath))
					return;

				lvBrowserDmon.Items.Clear();

				DirectoryInfo nodeDirInfo = new DirectoryInfo(selectedPath);
				ListViewItem.ListViewSubItem[] subItems;
				ListViewItem item = null;

				foreach (DirectoryInfo dir in nodeDirInfo.GetDirectories())
				{
					item = new ListViewItem(dir.Name, 0);
					item.Tag = dir.FullName;

					subItems =
						new ListViewItem.ListViewSubItem[] {
						new ListViewItem.ListViewSubItem(item, "Folder"),
						new ListViewItem.ListViewSubItem(item, dir.LastAccessTime.ToShortDateString())
						};

					item.SubItems.AddRange(subItems);
					lvBrowserDmon.Items.Add(item);
				}

				foreach (FileInfo file in nodeDirInfo.GetFiles())
				{
					if (!fileFilter.Contains(file.Extension))
						continue;

					item = new ListViewItem(file.Name, 1);
					item.Tag = file.FullName;

					subItems =
						new ListViewItem.ListViewSubItem[] {
						new ListViewItem.ListViewSubItem(item, file.Extension.Substring(1).ToUpper()),
						new ListViewItem.ListViewSubItem(item, file.LastAccessTime.ToShortDateString())
						};

					item.SubItems.AddRange(subItems);
					lvBrowserDmon.Items.Add(item);
				}

				lvDmon_SetColors(lvSelectedImagesDmon, Color.FromArgb(42, 42, 42));

				lblDirDmon.Text = selectedPath;

				outputDir = Path.GetFullPath(Path.Combine(selectedPath, "pE_Output"));
				lblOutputDirDmon.Text = outputDir;

				tltToolTipDmon.SetToolTip(lblDirDmon, selectedPath);
				tltToolTipDmon.SetToolTip(lblOutputDirDmon, outputDir);
			}
			else
			{
				if (CheckIfAlreadyAdded(selectedPath))
					return;

				lvSelectedImagesDmon.Items.Add(CreateItemFromPath(selectedPath));
				lvSelectedImagesDmon.Items[lvSelectedImagesDmon.Items.Count - 1].Tag += ";" + loadedImages.Count;
				loadedImages.Add(new DefaultImage(selectedPath, loadedImages.Count));

				lvDmon_SetColors(lvBrowserDmon, Color.FromArgb(42, 42, 42));

				Bitmap image = new Bitmap(selectedPath);

				if (pcbPreviewDmon.Image != null)
				{
					pcbPreviewDmon.Image.Dispose();
				}

				pcbPreviewDmon.Image = (Bitmap)image.Clone();
				pcbPreviewDmon.Tag = selectedPath;
				image.Dispose();

				UpdatePreview();
			}
		}

		private void lvBrowserDmon_MouseClick(object sender, MouseEventArgs e)
		{
			try
			{
				lvSelectedImagesDmon.SelectedIndices.Clear();

				string selectedPath = ((ListView)sender).SelectedItems[0].Tag.ToString();

				int startIndex = selectedPath.LastIndexOf('.');

				if (startIndex != -1)
				{
					Bitmap image = new Bitmap(selectedPath);

					if (pcbPreviewDmon.Image != null)
					{
						pcbPreviewDmon.Image.Dispose();
					}

					pcbPreviewDmon.Image = (Bitmap)image.Clone();
					lblDimensionsDmon.Text = String.Format("{0} × {1} (px)", image.Width.ToString(), image.Height.ToString());

					image.Dispose();
				}
			}
			catch (Exception)
			{

			}
		}

		private void lvSelectedImagesDmon_MouseClick(object sender, MouseEventArgs e)
		{
			lvBrowserDmon.SelectedIndices.Clear();

			if (lvSelectedImagesDmon.SelectedItems.Count > 0)
			{
				string selectedPath = lvSelectedImagesDmon.SelectedItems[0].Tag.ToString().Substring(0, lvSelectedImagesDmon.SelectedItems[0].Tag.ToString().LastIndexOf(';'));
				Bitmap image = new Bitmap(selectedPath);

				pcbPreviewDmon.Image.Dispose();
				pcbPreviewDmon.Image = (Bitmap)image.Clone();
				pcbPreviewDmon.Tag = selectedPath;
				image.Dispose();

				UpdatePreview();
			}
		}

		private void lvBrowser_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Enter ||
				e.KeyChar == (char)Keys.Space)
			{
				if (converting || ((ListView)sender).SelectedItems.Count == 0)
					return;

				string selectedPath = ((ListView)sender).SelectedItems[0].Tag.ToString();

				int startIndex = selectedPath.LastIndexOf('.');

				if (startIndex == -1)
				{
					if (!Directory.Exists(selectedPath))
						return;

					lvBrowserDmon.Items.Clear();

					DirectoryInfo nodeDirInfo = new DirectoryInfo(selectedPath);
					ListViewItem.ListViewSubItem[] subItems;
					ListViewItem item = null;

					foreach (DirectoryInfo dir in nodeDirInfo.GetDirectories())
					{
						item = new ListViewItem(dir.Name, 0);
						item.Tag = dir.FullName;

						subItems =
							new ListViewItem.ListViewSubItem[] {
						new ListViewItem.ListViewSubItem(item, "Folder"),
						new ListViewItem.ListViewSubItem(item, dir.LastAccessTime.ToShortDateString())
							};

						item.SubItems.AddRange(subItems);
						lvBrowserDmon.Items.Add(item);
					}

					foreach (FileInfo file in nodeDirInfo.GetFiles())
					{
						if (!fileFilter.Contains(file.Extension))
							continue;

						item = new ListViewItem(file.Name, 1);
						item.Tag = file.FullName;

						subItems =
							new ListViewItem.ListViewSubItem[] {
						new ListViewItem.ListViewSubItem(item, file.Extension.Substring(1).ToUpper()),
						new ListViewItem.ListViewSubItem(item, file.LastAccessTime.ToShortDateString())
							};

						item.SubItems.AddRange(subItems);
						lvBrowserDmon.Items.Add(item);
					}

					lvDmon_SetColors(lvSelectedImagesDmon, Color.FromArgb(42, 42, 42));

					lblDirDmon.Text = selectedPath;

					outputDir = Path.GetFullPath(Path.Combine(selectedPath, "pE_Output"));
					lblOutputDirDmon.Text = outputDir;

					tltToolTipDmon.SetToolTip(lblDirDmon, selectedPath);
					tltToolTipDmon.SetToolTip(lblOutputDirDmon, outputDir);
				}
				else
				{
					if (CheckIfAlreadyAdded(selectedPath))
						return;

					lvSelectedImagesDmon.Items.Add(CreateItemFromPath(selectedPath));
					lvSelectedImagesDmon.Items[lvSelectedImagesDmon.Items.Count - 1].Tag += ";" + loadedImages.Count;
					loadedImages.Add(new DefaultImage(selectedPath, loadedImages.Count));

					lvDmon_SetColors(lvSelectedImagesDmon, Color.FromArgb(42, 42, 42));

					Bitmap image = new Bitmap(selectedPath);

					if (pcbPreviewDmon.Image != null)
					{
						pcbPreviewDmon.Image.Dispose();
					}

					pcbPreviewDmon.Image = (Bitmap)image.Clone();
					pcbPreviewDmon.Tag = selectedPath;
					image.Dispose();

					UpdatePreview();
				}
			}
		}

		private void lvDmon_SelectionChanged(object sender, EventArgs e)
		{
			try
			{
				string selectedPath = ((ListView)sender).SelectedItems[0].Tag.ToString();
				selectedPath = (selectedPath.Contains(';') ? selectedPath.Substring(0, selectedPath.LastIndexOf(';')) : selectedPath);

				int startIndex = selectedPath.LastIndexOf('.');

				if (startIndex != -1)
				{
					Bitmap image = new Bitmap(selectedPath);

					if (pcbPreviewDmon.Image != null)
					{
						pcbPreviewDmon.Image.Dispose();
					}

					pcbPreviewDmon.Image = (Bitmap)image.Clone();
					lblDimensionsDmon.Text = String.Format("{0} × {1} (px)", image.Width.ToString(), image.Height.ToString());

					image.Dispose();
				}
			}
			catch (Exception)
			{

			}
		}
		#endregion

		#region RichTextBox Events
		private void rtbActionsDmon_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Tab)
			{
				e.Handled = true;

				try
				{
					string lastWord = "";

					List<string> keyWords = Regex.Split(rtbActionsDmon.Text.Substring(0, rtbActionsDmon.SelectionStart), @"\W+").ToList();

					lastWord = keyWords[keyWords.Count - 1];

					var searchActions = actions.Select(a => a + "()").Where(a => a.ToLower().StartsWith(lastWord.ToLower())).ToList();
					var searchFilters = photoFilter.Select(a => a).Where(a => a.ToLower().StartsWith(lastWord.ToLower())).ToList();
					var searchExtensions = imgFormat.Select(a => a.Key.ToUpper().Substring(1)).Where(a => a.StartsWith(lastWord.ToUpper())).ToList();

					string keyWord =
						(searchActions.Count > 0 ? searchActions[0] :
						(searchFilters.Count > 0 ? searchFilters[0] :
						(searchExtensions.Count > 0 ? searchExtensions[0] :"")));

					if (keyWord.Length > 0)
					{
						rtbActionsDmon.SelectionStart -= lastWord.Length;
						rtbActionsDmon.SelectionLength = lastWord.Length;
						this.rtbActionsDmon.SelectedText = keyWord;

						if (keyWord.Contains("()"))
						{
							rtbActionsDmon.SelectionStart--;
						}
					}
				}
				catch (Exception)
				{

				}
			}
		}

		private void rtbActionsDmon_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode != Keys.ControlKey &&
				e.KeyCode != Keys.ShiftKey &&
				e.KeyCode != Keys.Up &&
				e.KeyCode != Keys.Right &&
				e.KeyCode != Keys.Down &&
				e.KeyCode != Keys.Left &&
				e.KeyCode != Keys.A &&
				e.KeyCode != Keys.Z)
			{
				try
				{
					int currentPos = rtbActionsDmon.SelectionStart;
					List<string> keyWords = Regex.Split(rtbActionsDmon.Lines[rtbActionsDmon.GetLineFromCharIndex(rtbActionsDmon.GetFirstCharIndexOfCurrentLine())], @"\W+").ToList();

					foreach (string keyWord in keyWords)
					{
						try
						{
							rtbActionsDmon.SelectionStart = rtbActionsDmon.Text.LastIndexOf(keyWord);
							rtbActionsDmon.SelectionLength = keyWord.Length;
							rtbActionsDmon.SelectionColor = (actions.Contains(keyWord) ? Color.FromArgb(40, 120, 190)
															: keyWord.All(char.IsDigit) ? Color.FromArgb(105, 185, 255)
															: Color.FromArgb(255, 255, 255));
							rtbActionsDmon.SelectionLength = 0;
							rtbActionsDmon.SelectionStart = currentPos;
							rtbActionsDmon.SelectionColor = Color.FromArgb(255, 255, 255);
						}
						catch (Exception)
						{
							continue;
						}
					}

					List<Action> newActions = new List<Action>();

					foreach (string line in rtbActionsDmon.Lines)
					{
						if (line.IndexOf("(") != -1 && line.IndexOf("(") != -1 &&
							line.IndexOf(")") > line.IndexOf("("))
						{
							string action = line.Substring(0, line.IndexOf("("));

							if (action.Length > 0 && action.All(Char.IsLetter))
							{
								string[] parameters = line.Substring(line.IndexOf("(") + 1, line.IndexOf(")") - line.IndexOf("(") - 1).Split(',').Select(parameter => parameter.Trim()).ToArray();

								if (actions.Contains(action))
								{
									newActions.Add(new Action(action, parameters));
								}
							}
						}
					}

					if (!loadedActions.SequenceEqual(newActions))
					{
						loadedActions.Clear();
						loadedActions = newActions.ToList();

						UpdatePreview();
					}
				}
				catch (Exception)
				{

				}
			}
		}
		
		private void rtbActionsDmon_TextChanged(object sender, EventArgs e)
		{
			if (rtbActionsDmon.Text.Length == 0)
			{
				try
				{
					loadedActions.Clear();
					UpdatePreview();
				}
				catch (Exception)
				{

				}
			}
		}
		#endregion

		#region Selection Buttons
		private void btnAddSelectedDmon_Click(object sender, EventArgs e)
		{
			if (converting)
				return;

			for (int i = 0; i < lvBrowserDmon.SelectedItems.Count; i++)
			{
				string selectedPath = lvBrowserDmon.SelectedItems[i].Tag.ToString();

				int startIndex = selectedPath.LastIndexOf('.');

				if (startIndex != -1)
				{
					if (CheckIfAlreadyAdded(selectedPath))
						continue;

					lvSelectedImagesDmon.Items.Add(CreateItemFromPath(selectedPath));
					lvSelectedImagesDmon.Items[lvSelectedImagesDmon.Items.Count - 1].Tag += ";" + loadedImages.Count;
					loadedImages.Add(new DefaultImage(selectedPath, loadedImages.Count));
				}
			}

			lvDmon_SetColors(lvSelectedImagesDmon, Color.FromArgb(42, 42, 42));

			if (loadedImages.Count > 0 && lvBrowserDmon.SelectedItems.Count > 0)
			{
				pcbPreviewDmon.Image = (Image)loadedImages[loadedImages.Count - 1].Image.Clone();
				pcbPreviewDmon.Tag = loadedImages[loadedImages.Count - 1].Path;
				UpdatePreview();
			}
		}

		private void btnAddAllDmon_Click(object sender, EventArgs e)
		{
			if (converting)
				return;

			for (int i = 0; i < lvBrowserDmon.Items.Count; i++)
			{
				string selectedPath = lvBrowserDmon.Items[i].Tag.ToString();

				int startIndex = selectedPath.LastIndexOf('.');

				if (startIndex != -1)
				{
					if (CheckIfAlreadyAdded(selectedPath))
						continue;

					lvSelectedImagesDmon.Items.Add(CreateItemFromPath(selectedPath));
					lvSelectedImagesDmon.Items[lvSelectedImagesDmon.Items.Count - 1].Tag += ";" + loadedImages.Count;
					loadedImages.Add(new DefaultImage(selectedPath, loadedImages.Count));
				}
			}

			lvDmon_SetColors(lvSelectedImagesDmon, Color.FromArgb(42, 42, 42));

			if (loadedImages.Count > 0)
			{
				pcbPreviewDmon.Image = (Image)loadedImages[loadedImages.Count - 1].Image.Clone();
				pcbPreviewDmon.Tag = loadedImages[loadedImages.Count - 1].Path;
				UpdatePreview();
			}
		}

		private void btnRemoveSelectedDmon_Click(object sender, EventArgs e)
		{
			if (converting)
				return;

			List<ListViewItem> itemsToRemove = new List<ListViewItem>();

			for (int i = 0; i < lvSelectedImagesDmon.SelectedItems.Count; i++)
			{
				itemsToRemove.Add(lvSelectedImagesDmon.SelectedItems[i]);
			}

			foreach (ListViewItem itemToRemove in itemsToRemove)
			{
				DefaultImage item = loadedImages.Where(x => loadedImages.Any(s => x.ID.Equals(int.Parse(itemToRemove.Tag.ToString().Substring(itemToRemove.Tag.ToString().LastIndexOf(';') + 1))))).ToList()[0];
				item.Image.Dispose();
				loadedImages.Remove(item);
				lvSelectedImagesDmon.Items.Remove(itemToRemove);
			}

			lvDmon_SetColors(lvSelectedImagesDmon, Color.FromArgb(42, 42, 42));

			if (loadedImages.Count > 0)
			{
				pcbPreviewDmon.Image = (Image)loadedImages[loadedImages.Count - 1].Image.Clone();
				pcbPreviewDmon.Tag = loadedImages[loadedImages.Count - 1].Path;
				lblDimensionsDmon.Text = String.Format("{0} × {1} (px)", pcbPreviewDmon.Image.Width.ToString(), pcbPreviewDmon.Image.Height.ToString());
			}
			else
			{
				pcbPreviewDmon.Image = null;
				pcbPreviewDmon.Tag = "";
				lblDimensionsDmon.Text = "";
			}

			UpdatePreview();
		}

		private void btnClearDmon_Click(object sender, EventArgs e)
		{
			if (converting)
				return;

			lvSelectedImagesDmon.Items.Clear();

			foreach (DefaultImage item in loadedImages)
			{
				item.Image.Dispose();
			}

			loadedImages.Clear();

			pcbPreviewDmon.Image = null;
			pcbPreviewDmon.Tag = "";
			lblDimensionsDmon.Text = "";

			UpdatePreview();

			lvDmon_SetColors(lvSelectedImagesDmon, Color.FromArgb(42, 42, 42));
		}
		#endregion

		#region Edit Options Changed Events
		private void editOption_Changed(object sender, EventArgs e)
		{
			switch ((sender as Control).Tag.ToString())
			{
				case "Name":
					break;

				case "Size":
					break;

				case "Fill":
					break;

				case "Fill_Transparent":
					break;

				case "Margin":
					break;

				case "Extension":
					break;

				case "Filter":
					break;

				default:
					break;
			}

			UpdatePreview();
		}
		#endregion

		#region Functions
		private void PopulateTreeView(string path)
		{
			TreeNode rootNode;

			tvBrowserDmon.Nodes.Clear();

			DirectoryInfo info = new DirectoryInfo(path);

			if (info.Exists)
			{
				rootNode = new TreeNode(info.Name);
				rootNode.Tag = info;
				GetDirectories(info.GetDirectories(), rootNode);
				tvBrowserDmon.Nodes.Add(rootNode);
			}
		}

		private void GetDirectories(DirectoryInfo[] subDirs, TreeNode nodeToAddTo)
		{
			TreeNode aNode;
			DirectoryInfo[] subSubDirs;

			try
			{
				foreach (DirectoryInfo subDir in subDirs)
				{
					aNode = new TreeNode(subDir.Name, 0, 0);
					aNode.Tag = subDir;
					aNode.ImageKey = "folder";
					subSubDirs = subDir.GetDirectories();

					if (subSubDirs.Length != 0)
					{
						GetDirectories(subSubDirs, aNode);
					}

					nodeToAddTo.Nodes.Add(aNode);
				}
			}
			catch (Exception)
			{
				MessageBox.Show("Directory couldn't be loaded.");
			}
		}

		private bool CheckIfAlreadyAdded(string path)
		{
			bool exists = false;

			for (int c = 0; c < lvSelectedImagesDmon.Items.Count; c++)
			{
				if (lvSelectedImagesDmon.Items[c].Tag.ToString().Substring(0, lvSelectedImagesDmon.Items[c].Tag.ToString().LastIndexOf(';')) == path)
					exists = true;
			}

			return exists;
		}

		private ListViewItem CreateItemFromPath(string path)
		{
			ListViewItem.ListViewSubItem[] subItems;
			ListViewItem item = null;
			FileInfo file = new FileInfo(path);

			item = new ListViewItem(file.Name, 1);
			item.Tag = file.FullName;

			subItems =
				new ListViewItem.ListViewSubItem[] {
							new ListViewItem.ListViewSubItem(item, file.Extension.Substring(1).ToUpper()),
							new ListViewItem.ListViewSubItem(item, file.LastAccessTime.ToShortDateString())
				};

			item.SubItems.AddRange(subItems);
			return item;
		}

		private Control FindByTag(string name)
		{
			try
			{
				return Controls.Find(name, true)[0];
			}
			catch (Exception)
			{
				return null;
			}
		}

		private void UpdatePreview()
		{
			var currentDefaultImages = loadedImages.Where(x => loadedImages.Any(s => x.Path.Equals(pcbPreviewDmon.Tag.ToString()))).ToList();

			if (currentDefaultImages.Count == 0)
				return;

			pcbPreviewDmon.Image.Dispose();
			pcbPreviewDmon.Image = (Image)currentDefaultImages[0].Image.Clone();
			pcbPreviewDmon.Tag = currentDefaultImages[0].Path;

			foreach (Action singleAction in loadedActions)
			{
				switch (singleAction.action)
				{
					case "Rename":
						try
						{
							Rename(singleAction.parameters[0]);
						}
						catch (Exception)
						{

						}
						break;

					case "Resize":
						try
						{
							if (singleAction.parameters.Length == 5)
							{
								if (singleAction.parameters[0] == "*")
									singleAction.parameters[0] = pcbPreviewDmon.Image.Width.ToString();

								if (singleAction.parameters[1] == "*")
									singleAction.parameters[1] = pcbPreviewDmon.Image.Height.ToString();

								Color filling = Color.FromArgb(int.Parse(singleAction.parameters[2]), int.Parse(singleAction.parameters[3]), int.Parse(singleAction.parameters[4]));
								pcbPreviewDmon.Image = ResizeAndFill(new Size(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1])), filling);
							}
							if (singleAction.parameters.Length == 3)
							{
								if (singleAction.parameters[0] == "*")
									singleAction.parameters[0] = pcbPreviewDmon.Image.Width.ToString();

								if (singleAction.parameters[1] == "*")
									singleAction.parameters[1] = pcbPreviewDmon.Image.Height.ToString();

								Color filling = Color.FromArgb(int.Parse(singleAction.parameters[2]), int.Parse(singleAction.parameters[2]), int.Parse(singleAction.parameters[2]));
								pcbPreviewDmon.Image = ResizeAndFill(new Size(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1])), filling);
							}
							else
							{
								pcbPreviewDmon.Image = ResizeAndFill(new Size(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1])), Color.Empty);
							}
						}
						catch (Exception)
						{

						}
						break;

					case "Margin":
						try
						{
							switch (singleAction.parameters.Length)
							{
								case 1:
									pcbPreviewDmon.Image = Marginn(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[0]),
									int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[0]), Color.Empty);
									break;
								case 2:
									if (singleAction.parameters[1].StartsWith("c"))
									{
										pcbPreviewDmon.Image = Marginn(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[0]),
										int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[0]),
										Color.FromArgb(int.Parse(singleAction.parameters[1].Substring(1)), int.Parse(singleAction.parameters[1].Substring(1)), int.Parse(singleAction.parameters[1].Substring(1))));
									}
									else
									{
										pcbPreviewDmon.Image = Marginn(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1]),
										int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1]), Color.Empty);
									}
									
									break;
								case 3:
									pcbPreviewDmon.Image = Marginn(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1]),
									int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1]),
									Color.FromArgb(int.Parse(singleAction.parameters[2]), int.Parse(singleAction.parameters[2]), int.Parse(singleAction.parameters[2])));
									break;
								case 4:
									pcbPreviewDmon.Image = Marginn(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1]),
									int.Parse(singleAction.parameters[2]), int.Parse(singleAction.parameters[3]), Color.Empty);
									break;
								case 5:
									if (singleAction.parameters[2].StartsWith("c") &&
										singleAction.parameters[3].StartsWith("c") &&
										singleAction.parameters[4].StartsWith("c"))
									{
										pcbPreviewDmon.Image = Marginn(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1]),
										int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1]),
										Color.FromArgb(int.Parse(singleAction.parameters[2].Substring(1)), int.Parse(singleAction.parameters[3].Substring(1)), int.Parse(singleAction.parameters[4].Substring(1))));	
									}
									else
									{
										pcbPreviewDmon.Image = Marginn(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1]),
										int.Parse(singleAction.parameters[2]), int.Parse(singleAction.parameters[3]),
										Color.FromArgb(int.Parse(singleAction.parameters[4]), int.Parse(singleAction.parameters[4]), int.Parse(singleAction.parameters[4])));
									}
									break;
								case 7:
									pcbPreviewDmon.Image = Marginn(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1]),
									int.Parse(singleAction.parameters[2]), int.Parse(singleAction.parameters[3]),
									Color.FromArgb(int.Parse(singleAction.parameters[4]), int.Parse(singleAction.parameters[5]), int.Parse(singleAction.parameters[6])));
									break;
								default:
									break;
							}
						}
						catch (Exception)
						{

						}
						break;

					case "Extension":
						try
						{
							Extension(singleAction.parameters[0]);
						}
						catch (Exception)
						{

						}
						break;

					case "Filter":
						try
						{
							if (singleAction.parameters.Length == 2)
							{
								pcbPreviewDmon.Image = Filter(singleAction.parameters[0], float.Parse(singleAction.parameters[1]));
							}
							else
							{
								pcbPreviewDmon.Image = Filter(singleAction.parameters[0], 1f);
							}
						}
						catch (Exception)
						{

						}
						break;

					case "Crop":
						try
						{
							pcbPreviewDmon.Image = Crop();
						}
						catch (Exception)
						{

						}
						break;

					case "Output":
						try
						{
							Output(singleAction.parameters[0]);
						}
						catch (Exception)
						{

						}
						break;

					case "Picture":
						try
						{
							if (singleAction.parameters.Length == 5)
							{
								pcbPreviewDmon.Image = Picture(new Point(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1])),
								new Size(int.Parse(singleAction.parameters[2]), int.Parse(singleAction.parameters[3])),
								singleAction.parameters[4]);
							}
							else
							{
								pcbPreviewDmon.Image = Picture(new Point(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1])),
								new Size(0, 0), singleAction.parameters[2]);
							}
							
						}
						catch (Exception)
						{

						}
						break;

					default:
						break;
				}
			}

			if (loadedActions.Select(a => a).Where(a => a.action == "Output").ToList().Count == 0)
			{
				outputDir = Path.GetFullPath(Path.Combine(lblDirDmon.Text, "pE_Output"));
				lblOutputDirDmon.Text = outputDir;

				tltToolTipDmon.SetToolTip(lblOutputDirDmon, outputDir);
			}

			if (pcbPreviewDmon.Image != null)
			{
				lblDimensionsDmon.Text = String.Format("{0} × {1} (px)", pcbPreviewDmon.Image.Width.ToString(), pcbPreviewDmon.Image.Height.ToString());
			}
			else
			{
				lblDimensionsDmon.Text = "";
			}
		}
		#endregion

		#region Actions
		private void Rename(string newName)
		{
			renamed = newName;
		}

		private Bitmap ResizeAndFill(Size newSize, Color fill)
		{
			newSize.Width = Math.Min(maxResize.Width, newSize.Width);
			newSize.Height = Math.Min(maxResize.Height, newSize.Height);

			Bitmap image = converting ? convImage : (Bitmap)pcbPreviewDmon.Image;

			if (image != null)
			{
				unsafe
				{
					int width = newSize.Width;
					int height = newSize.Height;

					var bmp = new Bitmap(width, height);

					if (!fill.IsEmpty)
					{
						if (image.PixelFormat == PixelFormat.Format32bppArgb)
						{
							using (var g = Graphics.FromImage(bmp))
							{
								g.InterpolationMode = InterpolationMode.High;
								g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height));
								g.Save();
							}
						}

						decimal currentW = image.Width;
						decimal currentH = image.Height;

						decimal scaleW = (decimal)width / currentW;
						decimal scaleH = (decimal)height / currentH;

						decimal scale = Math.Min(scaleW, scaleH);

						int newW = (int)(currentW * scale);
						int newH = (int)(currentH * scale);

						decimal xOff = (width > newW ? (width - newW) / 2 : 0);
						decimal yOff = (height > newH ? (height - newH) / 2 : 0);

						Brush brush = new SolidBrush(fill);

						using (var g = Graphics.FromImage(bmp))
						{
							g.CompositingMode = CompositingMode.SourceOver;
							g.CompositingQuality = CompositingQuality.HighQuality;
							g.InterpolationMode = InterpolationMode.HighQualityBicubic;
							g.SmoothingMode = SmoothingMode.HighQuality;
							g.PixelOffsetMode = PixelOffsetMode.HighQuality;

							g.Clear(fill);
							g.DrawImage(image, new Rectangle((int)xOff, (int)yOff, newW, newH));
							g.Save();
						}
					}
					else
					{
						using (var g = Graphics.FromImage(bmp))
						{
							g.InterpolationMode = InterpolationMode.High;
							g.DrawImage(image, new Rectangle(0, 0, width, height));
							g.Save();
						}
					}

					image.Dispose();
					image = (Bitmap)bmp.Clone();
					bmp.Dispose();
				}
			}

			return image;
		}

		private Bitmap Marginn(int mTop, int mRight, int mBottom, int mLeft, Color marginColor)
		{
			Bitmap image = converting ? convImage : (Bitmap)pcbPreviewDmon.Image;

			mTop = Math.Min((maxResize.Height - image.Height) / 2, mTop);
			mRight = Math.Min((maxResize.Width - image.Width) / 2, mRight);
			mBottom = Math.Min((maxResize.Height - image.Height) / 2, mBottom);
			mLeft = Math.Min((maxResize.Width - image.Width) / 2, mLeft);

			if (image != null)
			{
				unsafe
				{
					Point pos = new Point(mLeft, mTop);
					Size size = new Size(image.Width, image.Height);

					Size newSize = new Size(mLeft + size.Width + mRight, mTop + size.Height + mBottom);

					var bmp = new Bitmap(newSize.Width, newSize.Height);

					Brush brush = new SolidBrush(marginColor.IsEmpty ? Color.White : marginColor);

					using (var g = Graphics.FromImage(bmp))
					{
						g.CompositingMode = CompositingMode.SourceCopy;
						g.CompositingQuality = CompositingQuality.HighQuality;
						g.InterpolationMode = InterpolationMode.HighQualityBicubic;
						g.SmoothingMode = SmoothingMode.HighQuality;
						g.PixelOffsetMode = PixelOffsetMode.HighQuality;

						g.FillRectangle(brush, new Rectangle(new Point(0, 0), newSize));
						g.DrawImage(image, new Rectangle(pos, size));
						g.Save();
					}

					image.Dispose();
					image = (Bitmap)bmp.Clone();
					bmp.Dispose();
				}
			}

			return image;
		}

		private void Extension(string extension)
		{
			extension = "." + new String(extension.Where(Char.IsLetter).ToArray()).ToLower();

			outputExt = ".jpg";

			if (extension.Length > 0)
			{
				if (imgFormat.Select(a => a.Key).ToList().Contains(extension))
				{
					outputExt = extension;
				}
			}
		}

		private Bitmap Filter(string filterName, float filterParam)
		{
			Bitmap image = converting ? convImage : (Bitmap)pcbPreviewDmon.Image;

			if (image != null)
			{
				image = filters.Filter(filterName, image, filterParam);
			}

			return image;
		}

		private Bitmap Crop()
		{
			Bitmap image = converting ? convImage : (Bitmap)pcbPreviewDmon.Image;

			if (image != null)
			{
				unsafe
				{
					PixelFormat pxlFormat = image.PixelFormat;

					if (pxlFormat == PixelFormat.Format32bppArgb)
					{
						image = ResizeAndFill(new Size(image.Width, image.Height), Color.FromArgb(255, 255, 255));
					}

					BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, pxlFormat);

					int bytesPerPixel = Bitmap.GetPixelFormatSize(pxlFormat) / 8;
					int heightInPixels = bitmapData.Height;
					int widthInBytes = bitmapData.Width * bytesPerPixel;
					byte* ptrFirstPixel = (byte*)bitmapData.Scan0;

					Bitmap cropped;
					Point from = new Point();
					Size size = new Size();

					int[][] avgAll = new int[heightInPixels][];

					for (int y = 0; y < heightInPixels; y++)
					{
						byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);

						avgAll[y] = new int[widthInBytes / bytesPerPixel];

						for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
						{
							int oldBlue = currentLine[x];
							int oldGreen = currentLine[x + 1];
							int oldRed = currentLine[x + 2];

							byte avg = (byte)((oldRed + oldGreen + oldBlue) / 3);

							avgAll[y][x / bytesPerPixel] = (avg >= 240 || avg == 0 ? 1 : 0);
						}
					}

					// Horizontal Check
					bool[] horizontalOutline = new bool[avgAll.Length];
					for (int tY = 0; tY < avgAll.Length; tY++)
					{
						decimal horizontalSum = 0;

						for (int tX = 0; tX < avgAll[0].Length; tX++)
						{
							horizontalSum += avgAll[tY][tX];
						}

						horizontalOutline[tY] = (horizontalSum / avgAll[0].Length == 1);
					}

					// Vertival Check
					bool[] verticalOutline = new bool[avgAll[0].Length];
					for (int tX = 0; tX < avgAll[0].Length; tX++)
					{
						decimal verticalSum = 0;

						for (int tY = 0; tY < avgAll.Length; tY++)
						{
							verticalSum += avgAll[tY][tX];
						}

						verticalOutline[tX] = (verticalSum / avgAll.Length == 1);
					}

					from = new Point(
						String.Join("", verticalOutline.Select(a => a ? "1" : "0").ToArray()).IndexOf("0"),
						String.Join("", horizontalOutline.Select(a => a ? "1" : "0").ToArray()).IndexOf("0")
						);

					size = new Size(
						String.Join("", verticalOutline.Select(a => a ? "1" : "0").Where(a => a == "0").ToArray()).Length,
						String.Join("", horizontalOutline.Select(a => a ? "1" : "0").Where(a => a == "0").ToArray()).Length
						);

					from.X = Math.Max(0, from.X - 50);
					from.Y = Math.Max(0, from.Y - 50);

					size.Width = Math.Min(image.Width, size.Width + 50 * 2);
					size.Height = Math.Min(image.Height, size.Height + 50 * 2);

					image.UnlockBits(bitmapData);

					cropped = new Bitmap(size.Width, size.Height);

					using (var g = Graphics.FromImage(cropped))
					{
						g.CompositingMode = CompositingMode.SourceCopy;
						g.CompositingQuality = CompositingQuality.HighQuality;
						g.InterpolationMode = InterpolationMode.HighQualityBicubic;
						g.SmoothingMode = SmoothingMode.HighQuality;
						g.PixelOffsetMode = PixelOffsetMode.HighQuality;

						g.DrawImage(image, 0, 0, new Rectangle(from, size), GraphicsUnit.Pixel);
						g.Save();
					}

					image.Dispose();
					image = (Bitmap)cropped.Clone();

					cropped.Dispose();
				}
			}

			return image;
		}
		
		private void Output(string outputFolderName)
		{
			if (outputFolderName.IndexOfAny(Path.GetInvalidPathChars()) == -1)
			{
				outputDir = Path.GetFullPath(Path.Combine(cofdBrowseDmon.FileName, outputFolderName));
				lblOutputDirDmon.Text = outputDir;

				if (lblOutputDirDmon.Text == lblDirDmon.Text)
				{
					MessageBox.Show(this, "You can not use input folder as output folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

					outputDir = Path.GetFullPath(Path.Combine(lblOutputDirDmon.Text, "pE_Output"));
					lblOutputDirDmon.Text = outputDir;
				}

				tltToolTipDmon.SetToolTip(lblOutputDirDmon, outputDir);
			}
		}
		
		private Bitmap Picture(Point pos, Size size, string imagePath)
		{
			Bitmap image = converting ? convImage : (Bitmap)pcbPreviewDmon.Image;

			if (File.Exists(imagePath))
			{
				Bitmap picture = (Bitmap)Image.FromFile(imagePath);

				size.Width = size.Width == 0 ? picture.Width : size.Width;
				size.Height = size.Height == 0 ? picture.Height : size.Height;

				if (image != null)
				{
					unsafe
					{
						using (var g = Graphics.FromImage(image))
						{
							g.CompositingMode = CompositingMode.SourceOver;
							g.CompositingQuality = CompositingQuality.HighQuality;
							g.InterpolationMode = InterpolationMode.HighQualityBicubic;
							g.SmoothingMode = SmoothingMode.HighQuality;
							g.PixelOffsetMode = PixelOffsetMode.HighQuality;

							g.DrawImage(picture, new Rectangle(pos, size));
							g.Save();
						}
					}
				}
			}

			return image;
		}
		#endregion

		#region Convert & Save
		private async void StartConverting()
		{
			if (loadedImages.Count > 0)
			{
				this.Text = "PhotoEditor - Converting...";
				converting = true;

				List<Task> saveItems = new List<Task>();

				for (int i = 0; i < loadedImages.Count; i++)
				{
					DefaultImage selectedImage = loadedImages[i];

					saveItems.Add(SaveNewImage(i, selectedImage.Path, Converting(selectedImage.Path, (Bitmap)selectedImage.Image.Clone())));
				}

				await Task.WhenAll(saveItems);

				converting = false;
				this.Text = "PhotoEditor";

				if (Directory.Exists(outputDir))
				{
					Process.Start(outputDir);
				}

				ntfNotifyDmon.Visible = true;
				ntfNotifyDmon.ShowBalloonTip(1000, "Finished", loadedImages.Count.ToString() + " images converted successfully!", ToolTipIcon.None);

				await Task.Delay(5000);

				lvDmon_SetColors(lvSelectedImagesDmon, Color.FromArgb(42, 42, 42));

				ntfNotifyDmon.Visible = false;
			}
		}

		private Bitmap Converting(string selectedImage, Bitmap imageToConvert)
		{
			convImage = imageToConvert;

			foreach (Action singleAction in loadedActions)
			{
				switch (singleAction.action)
				{
					case "Rename":
						try
						{
							Rename(singleAction.parameters[0]);
						}
						catch (Exception)
						{

						}
						break;

					case "Resize":
						try
						{
							if (singleAction.parameters.Length == 5)
							{
								if (singleAction.parameters[0] == "*")
									singleAction.parameters[0] = convImage.Width.ToString();

								if (singleAction.parameters[1] == "*")
									singleAction.parameters[1] = convImage.Height.ToString();

								Color filling = Color.FromArgb(int.Parse(singleAction.parameters[2]), int.Parse(singleAction.parameters[3]), int.Parse(singleAction.parameters[4]));
								convImage = ResizeAndFill(new Size(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1])), filling);
							}
							if (singleAction.parameters.Length == 3)
							{
								if (singleAction.parameters[0] == "*")
									singleAction.parameters[0] = convImage.Width.ToString();

								if (singleAction.parameters[1] == "*")
									singleAction.parameters[1] = convImage.Height.ToString();

								Color filling = Color.FromArgb(int.Parse(singleAction.parameters[2]), int.Parse(singleAction.parameters[2]), int.Parse(singleAction.parameters[2]));
								convImage = ResizeAndFill(new Size(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1])), filling);
							}
							else
							{
								convImage = ResizeAndFill(new Size(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1])), Color.Empty);
							}
						}
						catch (Exception)
						{

						}
						break;

					case "Margin":
						try
						{
							switch (singleAction.parameters.Length)
							{
								case 1:
									convImage = Marginn(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[0]),
									int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[0]), Color.Empty);
									break;
								case 2:
									if (singleAction.parameters[1].StartsWith("c"))
									{
										convImage = Marginn(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[0]),
										int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[0]),
										Color.FromArgb(int.Parse(singleAction.parameters[1].Substring(1)), int.Parse(singleAction.parameters[1].Substring(1)), int.Parse(singleAction.parameters[1].Substring(1))));
									}
									else
									{
										convImage = Marginn(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1]),
										int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1]), Color.Empty);
									}

									break;
								case 3:
									convImage = Marginn(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1]),
									int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1]),
									Color.FromArgb(int.Parse(singleAction.parameters[2]), int.Parse(singleAction.parameters[2]), int.Parse(singleAction.parameters[2])));
									break;
								case 4:
									convImage = Marginn(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1]),
									int.Parse(singleAction.parameters[2]), int.Parse(singleAction.parameters[3]), Color.Empty);
									break;
								case 5:
									if (singleAction.parameters[2].StartsWith("c") &&
										singleAction.parameters[3].StartsWith("c") &&
										singleAction.parameters[4].StartsWith("c"))
									{
										convImage = Marginn(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1]),
										int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1]),
										Color.FromArgb(int.Parse(singleAction.parameters[2].Substring(1)), int.Parse(singleAction.parameters[3].Substring(1)), int.Parse(singleAction.parameters[4].Substring(1))));
									}
									else
									{
										convImage = Marginn(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1]),
										int.Parse(singleAction.parameters[2]), int.Parse(singleAction.parameters[3]),
										Color.FromArgb(int.Parse(singleAction.parameters[4]), int.Parse(singleAction.parameters[4]), int.Parse(singleAction.parameters[4])));
									}
									break;
								case 7:
									convImage = Marginn(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1]),
									int.Parse(singleAction.parameters[2]), int.Parse(singleAction.parameters[3]),
									Color.FromArgb(int.Parse(singleAction.parameters[4]), int.Parse(singleAction.parameters[5]), int.Parse(singleAction.parameters[6])));
									break;
								default:
									break;
							}
						}
						catch (Exception)
						{

						}
						break;

					case "Extension":
						try
						{
							Extension(singleAction.parameters[0]);
						}
						catch (Exception)
						{

						}
						break;

					case "Filter":
						try
						{
							if (singleAction.parameters.Length == 2)
							{
								convImage = Filter(singleAction.parameters[0], float.Parse(singleAction.parameters[1]));
							}
							else
							{
								convImage = Filter(singleAction.parameters[0], 1f);
							}
						}
						catch (Exception)
						{

						}
						break;

					case "Crop":
						try
						{
							convImage = Crop();
						}
						catch (Exception)
						{

						}
						break;

					case "Output":
						try
						{
							Output(singleAction.parameters[0]);
						}
						catch (Exception)
						{

						}
						break;

					case "Picture":
						try
						{
							if (singleAction.parameters.Length == 5)
							{
								convImage = Picture(new Point(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1])),
								new Size(int.Parse(singleAction.parameters[2]), int.Parse(singleAction.parameters[3])),
								singleAction.parameters[4]);
							}
							else
							{
								convImage = Picture(new Point(int.Parse(singleAction.parameters[0]), int.Parse(singleAction.parameters[1])),
								new Size(0, 0), singleAction.parameters[2]);
							}
						}
						catch (Exception)
						{

						}
						break;

					default:
						break;
				}
			}

			if (loadedActions.Select(a => a).Where(a => a.action == "Output").ToList().Count == 0)
			{
				outputDir = Path.GetFullPath(Path.Combine(lblDirDmon.Text, "pE_Output"));
				lblOutputDirDmon.Text = outputDir;

				tltToolTipDmon.SetToolTip(lblOutputDirDmon, outputDir);
			}

			return convImage;
		}

		private Task SaveNewImage(int id, string selectedImage, Bitmap newImg)
		{
			string imageName = selectedImage.Substring(selectedImage.LastIndexOf('\\') + 1).Split('.')[0];

			if (renamed != "")
			{
				string newImageName = renamed
					.Replace("*", imageName)
					.Replace("{#}", id.ToString())
					.Replace("{x}", newImg.Width.ToString() + "x" + newImg.Height.ToString())
					.Replace("{d}", DateTime.Now.ToString("yyyy-MM-dd"))
					.Replace("{t}", DateTime.Now.ToString("HH-mm-ss"));

				if (newImageName.IndexOfAny(Path.GetInvalidFileNameChars()) == -1)
				{
					imageName = newImageName;
				}
			}

			imageName += (outputExt.Length > 0 ? outputExt : ".jpg");

			if (outputDir.Length == 0)
			{
				if (File.Exists(selectedImage))
				{
					File.Delete(selectedImage);
				}

				newImg.Save(selectedImage, imgFormat[outputExt]);
			}
			else
			{
				if (File.Exists(Path.Combine(outputDir, imageName)))
				{
					File.Delete(Path.Combine(outputDir, imageName));
				}

				if (!Directory.Exists(outputDir))
				{
					Directory.CreateDirectory(outputDir);
				}

				newImg.Save(Path.Combine(outputDir, imageName), imgFormat[outputExt]);
			}

			Color cClr = lvSelectedImagesDmon.Items[id].BackColor;
			lvSelectedImagesDmon.Items[id].BackColor = Color.FromArgb(cClr.R, cClr.G + 20, cClr.B);

			return Task.CompletedTask;
		}
		#endregion
	}
}