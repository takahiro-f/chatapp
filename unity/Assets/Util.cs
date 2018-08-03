using System;
using System.Text;

namespace chatapp
{
    public class Util
    {
        /// <summary>
        /// Base64文字列にエンコード
        /// </summary>
        /// <param name="text">入力文字列</param>
        /// <returns>変換後の文字列</returns>
        public static string EncodeBase64(string text)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        }

        /// <summary>
        /// Base64文字列をデコード
        /// </summary>
        /// <param name="text">入力文字列</param>
        /// <returns>変換後の文字列</returns>
        public static string DecodeBase64(string text)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(text));
        }
    }
}