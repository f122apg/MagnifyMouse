using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;

namespace MagnifyMouse
{
	public enum DataType
	{
		TEXT = 0,
		NOTNULL = 1,
		PRIMARYKEY = 2
	};

	public enum Column
	{
		URL = 0,
		FILENAME = 1
	}

	public partial class Form1 : Form
	{
		//データベースのテーブル名
		const String TABLENAME = "cache";
		//キャッシュファイルを保存するフォルダ
		static String cacheDir = "cache/";
		//キャッシュファイル名と保存したURL先の関連付けを行うためのデータベース
		static String cacheDB = cacheDir + "cache.sqlite";
		//キャッシュを行うかどうか
		static Boolean cacheMode = true;
		//キャッシュが存在するかどうか
		static Boolean cacheFound = false;
		//ダウンロードされた、または既に存在しているファイル名
		String fileName = null;
		//拡大画像を表示するフォーム
		EnlargementImage enlargementImage = null;
		//画像のサイズ
		Size imageSize = new Size();
		//マウス座標
		//now = 今現在の座標
		//old = １つ前の座標
		int nowMX, nowMY, oldMX, oldMY = 0;

		public Form1()
		{
			InitializeComponent();
			pictureBox1.Capture = true;
			mouseSelectedRect.Parent = pictureBox1;

			//ディレクトリが存在するかチェックし、なかったら作成
			if (!Directory.Exists(cacheDir))
				Directory.CreateDirectory(cacheDir);

			//データベースが存在するかチェックし、なかったら作成
			if (!File.Exists(cacheDB))
			{
				Object[,] column =
					{
						{ ColumnExtended.ToString(Column.URL), DataTypeExtended.ToString(DataType.TEXT), DataTypeExtended.ToString(DataType.PRIMARYKEY) },
						{ ColumnExtended.ToString(Column.FILENAME), DataTypeExtended.ToString(DataType.TEXT), DataTypeExtended.ToString(DataType.NOTNULL) }
					};

				//テーブルを作成する
				if (CreateTable(TABLENAME, column))
					MessageBox.Show("テーブルの作成が正常に終了しました。", "初回処理", MessageBoxButtons.OK, MessageBoxIcon.Information);
				else
				{
					MessageBox.Show("テーブルの作成に異常が発生しました。", "初回処理", MessageBoxButtons.OK, MessageBoxIcon.Error);
					cacheMode = false;
				}
			}
		}

		private async void Form1_Load(object sender, EventArgs e)
		{
#if DEBUG
			String imageURL = "http://netgeek.biz/wp-content/uploads/2018/02/bathcat-12.jpg";
#else
			String imageURL = Environment.GetCommandLineArgs()[1];
#endif
			//キャッシュファイルのパス
			String cachePath;
			//ダウンロードが正常にできたかどうか
			Boolean downloadedImage = false;

			//キャッシュが使えるならばキャッシュの確認を行う
			if (cacheMode)
			{
				//キャッシュがあるかどうかチェックする
				cachePath = Select(TABLENAME, Column.FILENAME, imageURL);
				//nullでなければキャッシュが存在する
				//そしてそのキャッシュを使用する
				if (cachePath != null)
				{
					cacheFound = true;
					fileName = cachePath;
					this.Text = "Show Cache Image";
				}
				else
					//ファイル名を新規に作成
					fileName = CreateFileName();
			}

			//キャッシュが使えるか確認をし、かつキャッシュがなければ画像を取得する
			if (cacheMode && !cacheFound)
			{
				this.Text = "Show Downloaded Image";
				//画像をダウンロード
				downloadedImage = await DownloadImage(imageURL, fileName);
				//画像が正しくダウンロードできたらそれをキャッシュとするので、cacheModeをtrue
				if (downloadedImage)
					cacheMode = true;
				else
					cacheMode = false;
			}

			//前の段階で画像を正常に取得でき、キャッシュとして使える(cacheMode == true)場合かつ
			//キャッシュが存在しない(cacheFound == false)場合、ダウンロードされたファイル名とURLでデータベース上に関連付ける
			if (cacheMode && !cacheFound)
			{
				String[,] data = new String[,] { { imageURL, fileName } };
				//挿入が正しく行えたならば、その画像をキャッシュとして表示することをcacheModeで許可している
				cacheMode = Insert(TABLENAME, data);
			}

			//画像の表示
			//キャッシュが正しくできたか、または画像が正しくダウンロードできたら表示する
			if (cacheMode || downloadedImage)
			{
				Image img = Image.FromFile(cacheDir + fileName);
				//画像サイズを取得
				imageSize.Width = img.Width;
				imageSize.Height = img.Height;
				FormChangeSize(img.Width, img.Height);
				pictureBox1.Image = img;
			}
			//なんらかのエラーが発生したらそれを示す画像を表示
			else
			{
				FormChangeSize(Properties.Resources.error.Width, Properties.Resources.error.Height);
				pictureBox1.Image = Properties.Resources.error;
			}
		}

		private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
		{
			//マウスがタイトルバーよりも下にあり、かつ画像が読み込まれた状態ならば画像の拡大処理を開始する
			if (pictureBox1.Image != null && SystemInformation.CaptionHeight < e.Y)
			{
				//初期化処理
				if (enlargementImage == null)
				{
					enlargementImage = new EnlargementImage(new Bitmap(cacheDir + fileName));

					try
					{
						//コマンドライン引数にクロップする範囲が指定されているかチェック
						if (Environment.GetCommandLineArgs().Length == 4)
						{
							int cropW = int.Parse(Environment.GetCommandLineArgs()[2]);
							int cropH = int.Parse(Environment.GetCommandLineArgs()[3]);
							//指定されたクロップ範囲が画像のサイズを超えてない場合のみ、クロップ範囲を設定
							if (cropW <= imageSize.Width && cropH <= imageSize.Height)
							{
								enlargementImage.SetCropSize(int.Parse(Environment.GetCommandLineArgs()[2]), int.Parse(Environment.GetCommandLineArgs()[3]));
							}
						}
					}
					catch (FormatException formatEx)
					{
						MessageBox.Show("指定されたクロップ範囲の設定値が不正です。\n" + formatEx.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}

					mouseSelectedRect.Size = new Size(enlargementImage.cropWidth, enlargementImage.cropHeight);
				}

				//フォームの位置を決定
				enlargementImage.Location = new Point(this.Location.X + this.Width, this.Location.Y);

				//mouseSelectedRectをどこに描画すればいいか計算する
				nowMX = GetLocationX(e.X - enlargementImage.cropWidth / 2, enlargementImage.cropWidth, imageSize.Width);
				nowMY = GetLocationY(e.Y - enlargementImage.cropHeight / 2, enlargementImage.cropHeight, imageSize.Height);

				//負荷対策としてマウスが動かされた時、座標が変わっていなかったら何もしない
				if (oldMX != nowMX || oldMY != nowMY)
				{
					//１つ前の座標を更新
					oldMX = nowMX;
					oldMY = nowMY;

					//フォームの位置を決定
					enlargementImage.Location = new Point(this.Location.X + this.Width, this.Location.Y);
					//マウスの位置を設定
					enlargementImage.SetMouseXY(e.X, e.Y);

					//コントロールの位置を設定 マウスが画像に対して中心に来るようにする
					mouseSelectedRect.Location = new Point(nowMX, nowMY);
				}

				//表示制御
				if (!enlargementImage.Visible)
					enlargementImage.Visible = true;

				if (!mouseSelectedRect.Visible)
				{
					mouseSelectedRect.Image = Properties.Resources.selected;
					mouseSelectedRect.Visible = true;
				}
			}
		}

		private void pictureBox1_MouseLeave(object sender, EventArgs e)
		{
			if (enlargementImage != null && enlargementImage.Visible)
				enlargementImage.Visible = false;

			if (mouseSelectedRect.Visible)
				mouseSelectedRect.Visible = false;
		}

		/***************************************************/
		/********************** 自作関数 *********************/
		/***************************************************/

		/// <summary>
		/// PictureBoxのX座標を求める
		/// </summary>
		/// <param name="mouseX">今現在のマウスのX座標</param>
		/// <param name="coverWidth">親コントロールを覆う画像の幅</param>
		/// <param name="imageWidth">画像の幅</param>
		/// <returns>クロップするY座標</returns>
		private int GetLocationX(int mouseX, int coverWidth, int imageWidth)
		{
			if (mouseX < 0)
				return 0;
			//もしマウスのX座標+覆う画像の幅が画像の幅を超えたら、
			//X座標を画像 - 覆う画像の幅にする
			else if (mouseX + coverWidth > imageWidth)
				return imageWidth - coverWidth;
			else
				return mouseX;

		}

		/// <summary>
		/// PictureBoxのY座標を求める
		/// </summary>
		/// <param name="mouseY">今現在のマウスのY座標</param>
		/// <param name="coverHeight">親コントロールを覆う画像の高さ</param>
		/// <param name="imageheight">画像の高さ</param>
		/// <returns>クロップするY座標</returns>
		private int GetLocationY(int mouseY, int coverHeight, int imageheight)
		{
			if (mouseY < 0)
				return 0;
			//もしマウスのY座標+覆う画像の高さが画像の高さを超えたら、
			//Y座標を画像 - 覆う画像の高さにする
			if (mouseY + coverHeight > imageheight)
				return imageheight - coverHeight;
			else
				return mouseY;

		}

		/// <summary>
		/// GUID形式でファイル名を新規作成する
		/// </summary>
		/// <returns>GUID形式の文字列</returns>
		private String CreateFileName()
		{
			Guid guid = Guid.NewGuid();
			return guid.ToString();
		}

		/// <summary>
		/// フォームとピクチャボックスのサイズを一括で変える
		/// </summary>
		/// <param name="width">横幅</param>
		/// <param name="height">縦幅</param>
		private void FormChangeSize(int width, int height)
		{
			//罫線およびタイトル バーを除くフォームのサイズを設定する
			this.ClientSize = new Size(width, height);
			pictureBox1.Width = width;
			pictureBox1.Height = height;
		}

		/// <summary>
		/// データベースに指定されたテーブル名でテーブルを作成する
		/// </summary>
		/// <param name="tablename">テーブル名</param>
		/// <param name="column">追加する列。e.g. { { ColumnExtended.ToString(Column.XXXX), DataTypeExtended.ToString(DataType.XXX), DataTypeExtended.ToString(DataType.PRIMARYKEY) or DataType.NOTNULL or null } }</param>
		/// <returns>テーブルの作成が正常にできたらtrue。異常が発生したときはfalse。</returns>
		private Boolean CreateTable(String tablename, Object[,] column)
		{
			//テーブル名が空白ではないかチェック かつ
			//列が1以上あるかチェックし、問題なければそのまま処理を続行
			if (!tablename.Equals("") && column.GetLength(0) < 1)
				return false;

			//テーブルを作成するクエリを作成
			StringBuilder createQuery = new StringBuilder();

			try
			{
				createQuery.Append("CREATE TABLE " + tablename + "(");

				//列を追加する処理
				for (int i = 0; i < column.GetLength(0); i++)
				{
					//最後の列でなければカンマ区切りで列を追加
					if (i != column.GetLength(0) - 1)
						createQuery.Append(
							(String)column[i, 0] + " " +
							(String)column[i, 1] + " " +
							(String)column[i, 2] + ",");
					else
						createQuery.Append(
							(String)column[i, 0] + " " +
							(String)column[i, 1] + " " +
							(String)column[i, 2]);
				}

				createQuery.Append(")");

				var sqlConnectionSb = new SQLiteConnectionStringBuilder { DataSource = cacheDB };
				SQLiteConnection con = null;
				SQLiteCommand cmd = null;

				//SQLiteでデータベースに接続
				try
				{
					con = new SQLiteConnection(sqlConnectionSb.ToString());
					con.Open();

					//テーブルを作成する
					cmd = new SQLiteCommand(con);
					cmd.CommandText = createQuery.ToString();
					cmd.ExecuteScalar();
				}
				catch (Exception e)
				{
				}
				finally
				{
					if (con != null)
						con.Dispose();

					if (cmd != null)
						cmd.Dispose();
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			return true;
		}

		/// <summary>
		/// テーブルの特定列のデータを取得する
		/// </summary>
		/// <param name="tableName">テーブル名</param>
		/// <param name="column">取得する列</param>
		/// <param name="data">プライマリキーに相当するデータが入った変数。e.g. "https://www.AAA/cat.jpg"</param>
		/// <returns>取得されたデータを返す</returns>
		private String Select(String tableName, Column column, String data)
		{
			//テーブル名が空白ではないかチェック かつ
			//文字列が1以上あるかチェックし、問題なければそのまま処理を続行
			if (!tableName.Equals("") && data.Length < 1)
				return null;

			try
			{
				var sqlConnectionSb = new SQLiteConnectionStringBuilder { DataSource = cacheDB };
				SQLiteConnection con = null;
				SQLiteCommand cmd = null;

				try
				{
					con = new SQLiteConnection(sqlConnectionSb.ToString());
					con.Open();

					//データ挿入処理
					cmd = new SQLiteCommand("SELECT " + ColumnExtended.ToString(column) + " FROM " + tableName +
						" WHERE " + ColumnExtended.ToString(Column.URL) + " = ?", con);
					//Parameterを追加するとき、追加するデータはObject型に変換しないと正しく追加されない模様
					cmd.Parameters.Add(new SQLiteParameter(DbType.String, (Object)data));
					SQLiteDataReader reader = cmd.ExecuteReader();
					reader.Read();

					String readStr = (String)reader[0];
					return readStr;
				}
				catch (Exception e)
				{
					Console.WriteLine("---------------------------> ERROR:" + e.Message);
					return null;
				}
				finally
				{
					if (con != null)
						con.Dispose();

					if (cmd != null)
						cmd.Dispose();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("---------------------------> ERROR:" + e.Message);
				return null;
			}
		}

		/// <summary>
		/// テーブルにデータを挿入する
		/// </summary>
		/// <param name="tablename">テーブル名</param>
		/// <param name="data">挿入するデータ。e.g. { { "http://www.example/dog.jpg", "{3F2504E0-4F89-11D3-9A0C-0305E82C3301}"(GUID) } }</param>
		/// <returns></returns>
		private Boolean Insert(String tableName, String[,] data)
		{
			//テーブル名が空白ではないかチェック かつ
			//列が1以上あるかチェックし、問題なければそのまま処理を続行
			if (!tableName.Equals("") && data.GetLength(0) < 1)
				return false;

			try
			{
				var sqlConnectionSb = new SQLiteConnectionStringBuilder { DataSource = cacheDB };
				SQLiteConnection con = null;
				SQLiteCommand cmd = null;

				//SQLiteでデータベースに接続
				try
				{
					con = new SQLiteConnection(sqlConnectionSb.ToString());
					con.Open();

					//データ挿入処理
					cmd = new SQLiteCommand("INSERT INTO " + tableName + " VALUES(?, ?);", con);

					for (int i = 0; i < data.GetLength(0); i++)
					{
						//Parameterを追加するとき、追加するデータはObject型に変換しないと正しく追加されない模様
						cmd.Parameters.Add(new SQLiteParameter(DbType.String, (Object)data[i, 0]));
						cmd.Parameters.Add(new SQLiteParameter(DbType.String, (Object)data[i, 1]));
						cmd.ExecuteNonQuery();
						cmd.Parameters.Clear();
					}
				}
				catch (Exception e)
				{
				}
				finally
				{
					if (con != null)
						con.Dispose();

					if (cmd != null)
						cmd.Dispose();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("---------------------------> ERROR:" + e.Message);
				return false;
			}

			return true;
		}

		//画像を保存するタスク
		private async Task<Boolean> DownloadImage(String url, String fileName)
		{
			var client = new HttpClient();
			HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead);
			//通信が成功したら画像を保存する
			if (response.IsSuccessStatusCode)
			{
				using (var filestream = File.Create(cacheDir + fileName))
				{
					using (var httpstream = await response.Content.ReadAsStreamAsync())
					{
						httpstream.CopyTo(filestream);
						return true;
					}
				}
			}
			else
				return false;
		}
	}

	//拡張メソッド
	public static class DataTypeExtended
	{
		public static string ToString(this DataType value)
		{
			String[] values = { "TEXT", "NOT NULL", "PRIMARY KEY" };
			return values[(int)value];
		}
	}

	public static class ColumnExtended
	{
		public static string ToString(this Column value)
		{
			String[] values = { "URL", "FILENAME" };
			return values[(int)value];
		}
	}
}
