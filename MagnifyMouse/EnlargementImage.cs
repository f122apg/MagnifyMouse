using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MagnifyMouse
{
	public partial class EnlargementImage : Form
	{
		//マウスのX、Y座標
		int mouseX, mouseY = 0;
		//クロップする横幅
		public int cropWidth = 300;
		//クロップする高さ
		public int cropHeight = 300;
		//クロップの対象となる画像
		Bitmap image;
		//画像の横幅、高さ
		int imageWidth, imageHeight = 0;

		public EnlargementImage(Bitmap image)
		{
			InitializeComponent();
			
			this.image = image;
			imageWidth = image.Width;
			imageHeight = image.Height;
		}

		private void EnlargementImage_Load(object sender, EventArgs e)
		{
			FormChangeSize(cropWidth * 2, cropHeight * 2);
		}

		/// <summary>
		/// フォームとピクチャボックスのサイズを一括で変える
		/// </summary>
		/// <param name="width">横幅</param>
		/// <param name="height">縦幅</param>
		public void FormChangeSize(int width, int height)
		{
			this.Width = width;
			this.Height = height;
			pictureBox1.Width = width;
			pictureBox1.Height = height;
		}

		/// <summary>
		/// MouseX、Yのセッター
		/// </summary>
		/// <param name="posY">マウスのY座標</param>
		public void SetMouseXY(int posX, int posY)
		{
			mouseX = posX;
			mouseY = posY;
			
			//画像をクロップし、表示
			Bitmap cropImage = ClipImage(image, GetCropX(mouseX - cropWidth / 2, cropWidth, imageWidth), GetCropY(mouseY - cropHeight / 2, cropHeight, imageHeight));
			Bitmap zoomImage = new Bitmap(cropImage, cropWidth * 2, cropHeight * 2);

			pictureBox1.Image = (Bitmap)zoomImage.Clone();
			zoomImage.Dispose();
			cropImage.Dispose();
		}

		/// <summary>
		/// クロップする範囲を設定する
		/// </summary>
		/// <param name="width">横幅 省略時、300</param>
		/// <param name="height">縦幅省略時、300</param>
		public void SetCropSize(int width = 300, int height = 300)
		{
			cropWidth = width;
			cropHeight = height;
		}

		/// <summary>
		/// 画像をクロップする
		/// </summary>
		/// <param name="srcimage">クロップする画像</param>
		/// <param name="cropX">クロップを開始するX座標</param>
		/// <param name="cropY">クロップを開始するY座標</param>
		/// <returns>クロップされたBitmap</returns>
		private Bitmap ClipImage(Bitmap srcImage, int cropX, int cropY)
		{
			//クロップしたBitmapの書き込み先
			Bitmap canvas = new Bitmap(cropWidth, cropHeight);
			//クロップ処理を受け持つ
			Graphics drawImage = Graphics.FromImage(canvas);
			//クロップする領域
			Rectangle cropRect = new Rectangle(cropX, cropY, cropWidth, cropHeight);
			//出力領域
			Rectangle distRect = new Rectangle(0, 0, cropWidth, cropHeight);
			//クロップした画像を書き込む
			drawImage.DrawImage(srcImage, distRect, cropRect, GraphicsUnit.Pixel);

			drawImage.Dispose();
			return canvas;
		}

		/// <summary>
		/// クロップを開始するX座標を求める
		/// </summary>
		/// <param name="mouseX">今現在のマウスのX座標</param>
		/// <param name="cropWidth">クロップしたい横幅</param>
		/// <param name="imageWidth">画像の幅</param>
		/// <returns>クロップするY座標</returns>
		private int GetCropX(int mouseX, int cropWidth, int imageWidth)
		{
			if (mouseX < 0)
				return 0;
			//もしマウスのY座標+クロップしたい高さが画像の高さを超えたら、
			//クロップをするY座標を画像 - クロップする高さにする
			else if (mouseX + cropWidth > imageWidth)
				return imageWidth - cropWidth;
			else
				return mouseX;

		}

		/// <summary>
		/// クロップを開始するY座標を求める
		/// </summary>
		/// <param name="mouseY">今現在のマウスのY座標</param>
		/// <param name="cropHeight">クロップしたい高さ</param>
		/// <param name="imageheight">画像の高さ</param>
		/// <returns>クロップするY座標</returns>
		private int GetCropY(int mouseY, int cropHeight, int imageheight)
		{
			if (mouseY < 0)
				return 0;
			//もしマウスのY座標+クロップしたい高さが画像の高さを超えたら、
			//クロップをするY座標を画像 - クロップする高さにする
			if (mouseY + cropHeight > imageheight)
				return imageheight - cropHeight;
			else
				return mouseY;

		}
	}
}
