using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MagnifyMouse
{
	static class Program
	{
		/// <summary>
		/// コマンドライン引数にURL, Width, Heightを入れると
		/// 画像を表示し、マウスオーバー時にWidth, Heightを元に画像をトリミング、拡大を行うプログラム
		/// Width, Heightは省略可能
		/// </summary>
		[STAThread]
		static void Main()
		{
#if !DEBUG
			//コマンドライン引数が渡されたかチェックを行う
			//引数は必ず先頭に実行ファイルのパスが入っているため、Lengthが2以上ならば渡されたと判断する
			//かつ
			//渡されたコマンドライン引数がURL文字列かどうかチェックを行い、不正ならばエラーメッセージを出し終了する
			if (Environment.GetCommandLineArgs().Length == 1 ||
				!System.Text.RegularExpressions.Regex.IsMatch(Environment.GetCommandLineArgs()[1], "^https?://.*"))
			{
				String errorMsg = "";

				if (Environment.GetCommandLineArgs().Length == 1)
					errorMsg = "直接起動せず、コマンドライン引数を渡してください。";
				else
					errorMsg = "URL文字列をコマンドライン引数に'含めて'、渡してください。";

				MessageBox.Show(errorMsg, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Environment.Exit(0);
			}
			else
#endif
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new Form1());
			}
		}
	}
}
