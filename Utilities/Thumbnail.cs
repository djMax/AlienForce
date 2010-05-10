using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace AlienForce.Utilities
{
	/// <summary>
	/// Helper for building bicubic-ly sampled thumbnails with optional
	/// cheesy beveled edges.
	/// </summary>
	public sealed class Thumbnail
	{
		private static bool ThumbnailCallback() { return false; }

		/// <summary>
		/// Save a jpeg image from a bitmap, to a file, at a given quality setting
		/// (100 being the max)
		/// </summary>
		/// <param name="dest"></param>
		/// <param name="bitmap"></param>
		/// <param name="quality">0-100 Jpeg quality level</param>
		public static void EncodeBitmapToJpeg(FileInfo dest, Bitmap bitmap, short quality)
		{
			ImageCodecInfo myImageCodecInfo;
			myImageCodecInfo = GetEncoderInfo("image/jpeg");
			Encoder myEncoder = Encoder.Quality;
			EncoderParameters myEncoderParameters = new EncoderParameters(1);
			EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, (long)quality);
			myEncoderParameters.Param[0] = myEncoderParameter;
			bitmap.Save(dest.FullName, myImageCodecInfo, myEncoderParameters);
		}

		/// <summary>
		/// Construct a thumbnail from an original file source.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="bevel">If true, we will attempt to add wonderfully cheesy bevels to the edges</param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="preserveRatio">If true, we will pick the best match
		/// that maintains the same h-w ratio</param>
		/// <returns></returns>
		public static Bitmap BuildThumbnail(FileInfo source, bool bevel, int width, int height, bool preserveRatio)
		{
			// create thumbnail using .net function GetThumbnailImage
			Image image = Image.FromFile(source.FullName); // load original image
			return BuildThumbnail(image, bevel, width, height, preserveRatio);
		}

		/// <summary>
		/// Construct a thumbnail from an original file source.
		/// </summary>
		/// <param name="source">The source image from wich to construct the thumnail</param>
		/// <param name="bevel">If true, we will attempt to add wonderfully cheesy bevels to the edges</param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="preserveRatio">If true, we will pick the best match
		/// that maintains the same h-w ratio</param>
		/// <returns>
		/// The thumbnail image as a Bitmap (it is the responsibility of the caller to convert the image
		/// into the desired format).
		/// </returns>
		public static Bitmap BuildThumbnail(Image source, bool bevel, int width, int height, bool preserveRatio)
		{
			int widthOrig, heightOrig, widthTh, heightTh;
			double fx, fy;
			if (preserveRatio)
			{ // retain aspect ratio
				widthOrig = source.Width;
				heightOrig = source.Height;
				fx = (double)widthOrig / (double)width;
				fy = (double)heightOrig / (double)height; // subsampling factors
				// must fit in thumbnail size
				double f = Math.Max(fx, fy);
				if (f < 1)
					f = 1;
				widthTh = (int)Math.Round(widthOrig / f);
				heightTh = (int)Math.Round(heightOrig / f);
			}
			else
			{
				widthTh = width;
				heightTh = height;
			}
			Bitmap bitmapNew = CreateThumbnail(source, widthTh, heightTh, preserveRatio);
			if (!bevel)
				return bitmapNew;

			// ---- apply bevel
			int widTh, heTh;
			widTh = bitmapNew.Width; heTh = bitmapNew.Height;
			int BevW = 10, LowA = 0, HighA = 180, Dark = 80, Light = 255;
			// hilight color, low and high
			Color clrHi1 = Color.FromArgb(LowA, Light, Light, Light);
			Color clrHi2 = Color.FromArgb(HighA, Light, Light, Light);
			Color clrDark1 = Color.FromArgb(LowA, Dark, Dark, Dark);
			Color clrDark2 = Color.FromArgb(HighA, Dark, Dark, Dark);
			LinearGradientBrush br; Rectangle rectSide;
			Graphics newG = Graphics.FromImage(bitmapNew);
			Size szHorz = new Size(widTh, BevW);
			Size szVert = new Size(BevW, heTh);
			// ---- draw dark (shadow) sides first
			// draw bottom-side of bevel
			szHorz += new Size(0, 2); szVert += new Size(2, 0);
			rectSide = new Rectangle(new Point(0, heTh - BevW), szHorz);
			br = new LinearGradientBrush(
				rectSide, clrDark1, clrDark2, LinearGradientMode.Vertical);
			rectSide.Inflate(0, -1);
			newG.FillRectangle(br, rectSide);
			// draw right-side of bevel
			rectSide = new Rectangle(new Point(widTh - BevW, 0), szVert);
			br = new LinearGradientBrush(
				rectSide, clrDark1, clrDark2, LinearGradientMode.Horizontal);
			rectSide.Inflate(-1, 0);
			newG.FillRectangle(br, rectSide);
			// ---- draw bright (hilight) sides next
			szHorz -= new Size(0, 2); szVert -= new Size(2, 0);
			// draw top-side of bevel
			rectSide = new Rectangle(new Point(0, 0), szHorz);
			br = new LinearGradientBrush(
				rectSide, clrHi2, clrHi1, LinearGradientMode.Vertical);
			newG.FillRectangle(br, rectSide);
			// draw left-side of bevel
			rectSide = new Rectangle(new Point(0, 0), szVert);
			br = new LinearGradientBrush(
				rectSide, clrHi2, clrHi1, LinearGradientMode.Horizontal);
			newG.FillRectangle(br, rectSide);
			// dispose graphics objects and return the new image.
			br.Dispose();
			newG.Dispose();
			return bitmapNew;
		}

		/// <summary>
		/// Returns the first appropriate ImageCodecInfo for the given MIME
		/// type, or null if none exists/
		/// </summary>
		/// <param name="mimeType">The MIME type of the requested ImageCodecInfo</param>
		public static ImageCodecInfo GetEncoderInfo(String mimeType)
		{
			int j;
			ImageCodecInfo[] encoders;
			encoders = ImageCodecInfo.GetImageEncoders();
			for (j = 0; j < encoders.Length; ++j)
			{
				if (encoders[j].MimeType == mimeType)
					return encoders[j];
			}
			return null;
		}

		private static Bitmap CreateThumbnail(Image source, int thumbWi, int thumbHi, bool preserveRatio)
		{
			// return the source image if it's smaller than the designated thumbnail
			if (source.Width < thumbWi && source.Height < thumbHi)
			{
				// TODO: Is there anyway to updated this so that we can return
				// the Image as-is and not confuse the caller?
				return DrawThumbnail(source, source.Width, source.Height);
			}

			Bitmap ret = null;

			try
			{
				int wi, hi;

				// maintain the aspect ratio despite the thumbnail size parameters
				if (preserveRatio)
				{
					if (source.Width > source.Height)
					{
						wi = thumbWi;
						hi = (int)(source.Height * ((decimal)thumbWi / source.Width));
					}
					else
					{
						hi = thumbHi;
						wi = (int)(source.Width * ((decimal)thumbHi / source.Height));
					}
				}
				else
				{
					wi = thumbWi;
					hi = thumbHi;
				}

				ret = DrawThumbnail(source, wi, hi);
			}
			catch
			{
				ret = null;
			}

			return ret;
		}

		private static Bitmap DrawThumbnail(Image source, int width, int height)
		{
			// original code that creates lousy thumbnails
			// System.Drawing.Image ret = source.GetThumbnailImage(wi,hi,null,IntPtr.Zero);

			Bitmap result = new Bitmap(width, height);

			using (Graphics g = Graphics.FromImage(result))
			{
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				//g.FillRectangle(Brushes.White, 0, 0, width, height);
				g.DrawImage(source, 0, 0, width, height);
			}

			return result;
		}
	}
}
