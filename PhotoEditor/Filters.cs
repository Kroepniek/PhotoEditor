using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;

namespace PhotoEditor
{
	class Filters 
	{
		public List<string> filters = new List<string>();

		public Filters()
		{
			Type thisType = this.GetType();
			MethodInfo[] availableFilters = thisType.GetMethods();

			foreach (MethodInfo availableFilter in availableFilters)
			{
				if (availableFilter.ReturnType.ToString() == "System.Drawing.Bitmap" &&
					availableFilter.IsPublic &&
					availableFilter.Name != "Filter")
					filters.Add(availableFilter.Name);
			}
		}

		public Bitmap Filter(string filterName, Bitmap image, float filterParam)
		{
			Type thisType = this.GetType();
			MethodInfo filter = thisType.GetMethod(filterName);
			return (Bitmap)filter.Invoke(this, new object[] { new object[] { image, filterParam } });
		}

		public Bitmap Monochrome(object[] prms)
		{
			Bitmap image = (Bitmap)prms[0];

			unsafe
			{
				BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

				int bytesPerPixel = Bitmap.GetPixelFormatSize(PixelFormat.Format32bppArgb) / 8;
				int heightInPixels = bitmapData.Height;
				int widthInBytes = bitmapData.Width * bytesPerPixel;
				byte* ptrFirstPixel = (byte*)bitmapData.Scan0;

				for (int y = 0; y < heightInPixels; y++)
				{
					byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);
					for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
					{
						int oldBlue = currentLine[x];
						int oldGreen = currentLine[x + 1];
						int oldRed = currentLine[x + 2];

						byte avg = (byte)((oldRed + oldGreen + oldBlue) / 3);

						currentLine[x] = avg;
						currentLine[x + 1] = avg;
						currentLine[x + 2] = avg;
					}
				}

				image.UnlockBits(bitmapData);
			}

			return image;
		}

		public Bitmap Blur(object[] prms)
		{
			Bitmap image = (Bitmap)prms[0];
			float blurSize = (float)prms[1];

			unsafe
			{
				BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

				int bytesPerPixel = Bitmap.GetPixelFormatSize(PixelFormat.Format32bppArgb) / 8;
				int heightInPixels = bitmapData.Height;
				int widthInBytes = bitmapData.Width * bytesPerPixel;
				byte* ptrFirstPixel = (byte*)bitmapData.Scan0;

				for (int y = 0; y < heightInPixels; y++)
				{
					byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);
					for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
					{
						int avgBlue		= currentLine[x];
						int avgGreen	= currentLine[x + 1];
						int avgRed		= currentLine[x + 2];
						int avgAlpha	= currentLine[x + 3];

						int blurPixelCount = 1;

						for (int yy = y; (yy < y + blurSize && yy < heightInPixels); yy++)
						{
							byte* currentLinee = ptrFirstPixel + (yy * bitmapData.Stride);
							for (int xx = x; (xx < x + blurSize && xx < widthInBytes); xx = xx + bytesPerPixel)
							{
								avgBlue		+= currentLinee[x];
								avgGreen	+= currentLinee[x + 1];
								avgRed		+= currentLinee[x + 2];
								avgAlpha	+= currentLinee[x + 3];

								blurPixelCount++;
							}
						}

						avgBlue		= avgBlue	/ blurPixelCount;
						avgGreen	= avgGreen	/ blurPixelCount;
						avgRed		= avgRed	/ blurPixelCount;
						avgAlpha	= avgAlpha	/ blurPixelCount;

						for (int yy = y; (yy < y + blurSize && yy < heightInPixels); yy++)
						{
							byte* currentLinee = ptrFirstPixel + (yy * bitmapData.Stride);
							for (int xx = x; (xx < x + blurSize && xx < widthInBytes); xx = xx + bytesPerPixel)
							{
								currentLine[x]		= (byte)avgBlue;
								currentLine[x + 1]	= (byte)avgGreen;
								currentLine[x + 2]	= (byte)avgRed;
								currentLine[x + 3]	= (byte)avgAlpha;
							}
						}
					}
				}

				image.UnlockBits(bitmapData);
			}

			return image;
		}

		public Bitmap Negative(object[] prms)
		{
			Bitmap image = (Bitmap)prms[0];

			unsafe
			{
				BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

				int bytesPerPixel = Bitmap.GetPixelFormatSize(PixelFormat.Format32bppArgb) / 8;
				int heightInPixels = bitmapData.Height;
				int widthInBytes = bitmapData.Width * bytesPerPixel;
				byte* ptrFirstPixel = (byte*)bitmapData.Scan0;

				for (int y = 0; y < heightInPixels; y++)
				{
					byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);
					for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
					{
						int oldBlue = currentLine[x];
						int oldGreen = currentLine[x + 1];
						int oldRed = currentLine[x + 2];

						currentLine[x] = (byte)(255 - oldBlue);
						currentLine[x + 1] = (byte)(255 - oldGreen);
						currentLine[x + 2] = (byte)(255 - oldRed);
					}
				}

				image.UnlockBits(bitmapData);
			}

			return image;
		}

		public Bitmap Alpha(object[] prms)
		{
			Bitmap image = (Bitmap)prms[0];
			float alphaEdge = (float)prms[1];

			unsafe
			{
				BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

				int bytesPerPixel = Bitmap.GetPixelFormatSize(PixelFormat.Format32bppArgb) / 8;
				int heightInPixels = bitmapData.Height;
				int widthInBytes = bitmapData.Width * bytesPerPixel;
				byte* ptrFirstPixel = (byte*)bitmapData.Scan0;

				for (int y = 0; y < heightInPixels; y++)
				{
					byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);

					for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
					{

						int oldBlue = currentLine[x];
						int oldGreen = currentLine[x + 1];
						int oldRed = currentLine[x + 2];

						byte avg = (byte)((oldRed + oldGreen + oldBlue) / 3);

						if (avg >= 255 * alphaEdge)
						{
							currentLine[x + 3] = 0;
						}
					}
				}

				image.UnlockBits(bitmapData);
			}

			return image;
		}

		public Bitmap Opacity(object[] prms)
		{
			Bitmap image = (Bitmap)prms[0];
			float filterParam = (float)prms[1];

			unsafe
			{
				BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

				int bytesPerPixel = Bitmap.GetPixelFormatSize(PixelFormat.Format32bppArgb) / 8;
				int heightInPixels = bitmapData.Height;
				int widthInBytes = bitmapData.Width * bytesPerPixel;
				byte* ptrFirstPixel = (byte*)bitmapData.Scan0;

				for (int y = 0; y < heightInPixels; y++)
				{
					byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);
					for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
					{
						currentLine[x + 3] = (byte)(255 * filterParam);
					}
				}

				image.UnlockBits(bitmapData);
			}

			return image;
		}

		public Bitmap Binary(object[] prms)
		{
			Bitmap image = (Bitmap)prms[0];
			float edge = (float)prms[1];

			unsafe
			{
				BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

				int bytesPerPixel = Bitmap.GetPixelFormatSize(PixelFormat.Format32bppArgb) / 8;
				int heightInPixels = bitmapData.Height;
				int widthInBytes = bitmapData.Width * bytesPerPixel;
				byte* ptrFirstPixel = (byte*)bitmapData.Scan0;

				for (int y = 0; y < heightInPixels; y++)
				{
					byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);
					for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
					{
						byte oldBlue = currentLine[x];
						byte oldGreen = currentLine[x + 1];
						byte oldRed = currentLine[x + 2];

						int avg = (oldBlue + oldGreen + oldRed) / 3;

						currentLine[x] = (byte)(avg >= edge ? 255 : 0);
						currentLine[x + 1] = (byte)(avg >= edge ? 255 : 0);
						currentLine[x + 2] = (byte)(avg >= edge ? 255 : 0);
					}
				}

				image.UnlockBits(bitmapData);
			}

			return image;
		}

		private Bitmap Example(object[] prms)
		{
			Bitmap image = (Bitmap)prms[0];

			unsafe
			{
				BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

				int bytesPerPixel = Bitmap.GetPixelFormatSize(PixelFormat.Format32bppArgb) / 8;
				int heightInPixels = bitmapData.Height;
				int widthInBytes = bitmapData.Width * bytesPerPixel;
				byte* ptrFirstPixel = (byte*)bitmapData.Scan0;

				for (int y = 0; y < heightInPixels; y++)
				{
					byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);
					for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
					{
						byte oldBlue = currentLine[x ];
						byte oldGreen = currentLine[x + 1];
						byte oldRed = currentLine[x + 2];
						byte oldAlpha = currentLine[x + 3];

						// Do something usefull...
					}
				}

				image.UnlockBits(bitmapData);
			}

			return image;
		}
	}
}
