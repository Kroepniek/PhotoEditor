using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PhotoEditor
{
	public partial class frmManualDmon : Form
	{
		public frmManualDmon()
		{
			InitializeComponent();

			tctTabsDmon.Location = new Point(155, -25);
		}

		private void colorKeyWords(RichTextBox rtb)
		{
			var examples = new Regex(@"<exp>(.*?)<\/exp>", RegexOptions.Singleline).Matches(rtb.Text);

			int startIndex = 0;

			rtb.ReadOnly = false;

			foreach (Match example in examples)
			{
				startIndex = rtb.Text.IndexOf(example.Groups[1].Value, startIndex);

				try
				{
					rtb.SelectionStart = rtb.Text.IndexOf(example.Groups[1].Value, startIndex) + 1;
					rtb.SelectionLength = example.Groups[1].Value.Length - 2;
					rtb.SelectionColor = Color.FromArgb(178, 178, 178);
					rtb.SelectionFont = new Font(rtb.Font, FontStyle.Italic);

					rtb.SelectionStart = rtb.Text.IndexOf(example.Groups[1].Value, startIndex) - 5;
					rtb.SelectionLength = 5;
					rtb.SelectedText = "";

					rtb.SelectionStart = rtb.Text.IndexOf(example.Groups[1].Value, startIndex - 5) + example.Groups[1].Value.Length;
					rtb.SelectionLength = 6;
					rtb.SelectedText = "";
				}
				catch (Exception e)
				{
					continue;
				}
			}

			rtb.ReadOnly = true;

			var specialWords = new Regex(@"\'.*?\'").Matches(rtb.Text);
			List<string> keyWords = Regex.Split(rtb.Text, @"\W+").ToList();

			startIndex = 0;

			foreach (var specialWord in specialWords)
			{
				startIndex = rtb.Text.IndexOf(specialWord.ToString(), startIndex);

				try
				{
					rtb.SelectionStart = rtb.Text.IndexOf(specialWord.ToString(), startIndex) + 1;
					rtb.SelectionLength = specialWord.ToString().Length - 2;
					rtb.SelectionColor = Color.FromArgb(255, 178, 128);
					rtb.SelectionFont = (rtb.SelectionFont.Italic ? new Font(rtb.Font, FontStyle.Bold | FontStyle.Italic) :
											new Font(rtb.Font, FontStyle.Bold));
					rtb.SelectionLength = 0;
					rtb.SelectionStart = 0;
				}
				catch (Exception)
				{
					continue;
				}
			}

			startIndex = 0;

			foreach (string keyWord in keyWords)
			{
				startIndex = rtb.Text.IndexOf(keyWord, startIndex);

				try
				{
					rtb.SelectionStart = rtb.Text.IndexOf(keyWord, startIndex);
					rtb.SelectionLength = keyWord.Length;
					rtb.SelectionColor = (frmMainDmon.actions.Contains(keyWord) ? Color.FromArgb(40, 120, 190)
													: keyWord.All(char.IsDigit) ? Color.FromArgb(105, 185, 255)
													: rtb.SelectionColor);
					rtb.SelectionFont = (frmMainDmon.actions.Contains(keyWord) ?
													(rtb.SelectionFont = (rtb.SelectionFont.Italic ?
														new Font(rtb.Font, FontStyle.Bold | FontStyle.Italic) :
														new Font(rtb.Font, FontStyle.Bold))
													)
													: keyWord.All(char.IsDigit) ?
														(rtb.SelectionFont = (rtb.SelectionFont.Italic ?
															new Font(rtb.Font, FontStyle.Bold | FontStyle.Italic) :
															new Font(rtb.Font, FontStyle.Bold))
													)
													: rtb.SelectionFont);
					rtb.SelectionLength = 0;
					rtb.SelectionStart = 0;
					rtb.SelectionFont = rtb.Font;
				}
				catch (Exception)
				{
					continue;
				}
			}
		}

		private void frmManualDmon_Load(object sender, EventArgs e)
		{
			List<RichTextBox> rtbs = new List<RichTextBox>()
			{
				rtbRenameDmon,
				rtbResizeDmon,
				rtbMarginDmon,
				rtbExtensionDmon,
				rtbFilterDmon,
				rtbCropDmon,
				rtbOutputDmon,
				rtbPictureDmon
			};

			foreach(RichTextBox rtb in rtbs)
			{
				colorKeyWords(rtb);
			}
		}

		private void btnAction_Click(object sender, EventArgs e)
		{
			tctTabsDmon.SelectedIndex = (sender as Button).TabIndex;
		}

		private void tpgPages_Enter(object sender, EventArgs e)
		{
			pnlDeviderDmon.Focus();
		}
	}
}
