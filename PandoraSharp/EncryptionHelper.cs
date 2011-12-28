using System;
using System.Collections.Generic;
using System.Text;

namespace PandoraSharp
{
    internal static class EncryptionHelper
    {
        private const string HEX_FORMAT_STRING = "{0:x2}";

        public static string DecryptUrlHex(string urlHex)
        {
            //length check

            double mod = Math.Pow(2, 32);

            byte[] bytes = StringToByteArray(urlHex);
            //System.Text.UnicodeEncoding utf8Encoding = new UnicodeEncoding();

            StringBuilder output = new StringBuilder();

            //for (int i = 0; i < 24; i = i + 8)
            for (int i = 0; i < urlHex.Length/2; i = i + 8)
            {
                UInt32 l = (UInt32)(bytes[i] << 24 |
                        bytes[i + 1] << 16 |
                        bytes[i + 2] << 8 |
                        bytes[i + 3]);

                UInt32 r = (UInt32)(bytes[i + 4] << 24 |
                        bytes[i + 5] << 16 |
                        bytes[i + 6] << 8 |
                        bytes[i + 7]);

                for (UInt32 j = UrlKey.N + 1; j > 1; j--)
                {
                    l = l ^ UrlKey.P[j];

                    UInt32 a = (l & 0xFF000000) >> 24;
                    UInt32 b = (l & 0x00FF0000) >> 16;
                    UInt32 c = (l & 0x0000FF00) >> 8;
                    UInt32 d = (l & 0x000000FF);

                    UInt32 f = (UInt32)((UrlKey.S[0, a] + UrlKey.S[1, b]) % mod);
                    f = f ^ UrlKey.S[2, c];
                    f = f + UrlKey.S[3, d];
                    f = (UInt32)(f % mod) & 0xFFFFFFFF;

                    r ^= f;

                    UInt32 tmp = l;
                    l = r;
                    r = tmp;
                }

                UInt32 tmp2 = l;
                l = r;
                r = tmp2;

                r ^= UrlKey.P[1];
                l ^= UrlKey.P[0];

                //output += Environment.NewLine + l.ToString();
                //output += Environment.NewLine + r.ToString();

                //output += Environment.NewLine + System.Text.Encoding.UTF8.GetString(new byte[1] { (byte)((r >> 24) & 0xff) });
                //output += Environment.NewLine + Convert.ToString((byte)((l >> 24) & 0xff), 16);
                //output += Environment.NewLine + ((byte)((l >> 24) & 0xff));
                //output += Environment.NewLine + Convert.ToChar((l >> 24) & 0xff);

                output.Append(Convert.ToChar((l >> 24) & 0xff));
                output.Append(Convert.ToChar((l >> 16) & 0xff));
                output.Append(Convert.ToChar((l >> 8) & 0xff));
                output.Append(Convert.ToChar(l & 0xff));

                output.Append(Convert.ToChar((r >> 24) & 0xff));
                output.Append(Convert.ToChar((r >> 16) & 0xff));
                output.Append(Convert.ToChar((r >> 8) & 0xff));
                output.Append(Convert.ToChar(r & 0xff));
            }

            



            //string dec = utf8Encoding.GetString(bytes);
            //string output = "";

            //for (int i = 0; i < 24; i++)
            //{
            //    byte mybyte = utf8Encoding.GetBytes(dec)[i];
            //    output += Environment.NewLine + ((int)mybyte).ToString();
            //    //output += Environment.NewLine + i.ToString();
            //}

            //byte[] bytes = Encoding.UTF8.GetBytes(encryptedString);
            //string str = Convert.ToString(bytes);

            //for (int i = 0; i < str.Length; i = i + 8)
            //{
            //}


            return output.ToString();
        }

        private static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public static string EncryptString(String inputValue)
        {
            inputValue = inputValue.Replace("\r", String.Empty);

            int blocks = (inputValue.Length / 8) + 1;
            double mod = Math.Pow(2, 32);

            inputValue = inputValue + "\0\0\0\0\0\0\0\0";

            StringBuilder output = new StringBuilder();

            for (int h = 0; h < blocks; h++)
            {
                int i = h << 3; // h * 3
                
                UInt32 l = (UInt32)
                            (
                            Convert.ToByte(inputValue[i + 0]) << 24 |
                            Convert.ToByte(inputValue[i + 1]) << 16 |
                            Convert.ToByte(inputValue[i + 2]) << 8 |
                            Convert.ToByte(inputValue[i + 3])
                            );

                UInt32 r = (UInt32)
                            (
                            Convert.ToByte(inputValue[i + 4]) << 24 |
                            Convert.ToByte(inputValue[i + 5]) << 16 |
                            Convert.ToByte(inputValue[i + 6]) << 8 |
                            Convert.ToByte(inputValue[i + 7])
                            );

                for (int j = 0; j < XmlRpcKey.N; j++)
                {
                    l ^= XmlRpcKey.P[j];

                    UInt32 a = (l & 0xFF000000) >> 24;
                    UInt32 b = (l & 0x00FF0000) >> 16;
                    UInt32 c = (l & 0x0000FF00) >> 8;
                    UInt32 d = (l & 0x000000FF);

                    UInt32 f = (UInt32)((XmlRpcKey.S[0,a] + XmlRpcKey.S[1,b]) % mod);
                    f = f ^ XmlRpcKey.S[2, c];
                    f = f + XmlRpcKey.S[3, d];
                    f = (UInt32)(f % mod) & 0xFFFFFFFF;

                    r ^= f;

                    UInt32 tmp = l;
                    l = r;
                    r = tmp;
                }

                UInt32 tmp2 = l;
                l = r;
                r = tmp2;

                r ^= XmlRpcKey.P[XmlRpcKey.N];
                l ^= XmlRpcKey.P[XmlRpcKey.N + 1];

                //output.Append((l >> 24) & 0xff);
                //output.AppendLine();
                //output.Append((l >> 16) & 0xff);
                //output.AppendLine();
                //output.Append((l >> 8) & 0xff);
                //output.AppendLine();
                //output.Append(l & 0xff);
                //output.AppendLine();

                //output.Append((r >> 24) & 0xff);
                //output.AppendLine();
                //output.Append((r >> 16) & 0xff);
                //output.AppendLine();
                //output.Append((r >> 8) & 0xff);
                //output.AppendLine();
                //output.Append(r & 0xff);
                //output.AppendLine();

                //output.AppendLine(String.Format("{0:x2}", ((l >> 24) & 0xff)));

                output.Append(String.Format(HEX_FORMAT_STRING, (l >> 24) & 0xff));
                output.Append(String.Format(HEX_FORMAT_STRING, (l >> 16) & 0xff));
                output.Append(String.Format(HEX_FORMAT_STRING, (l >> 8) & 0xff));
                output.Append(String.Format(HEX_FORMAT_STRING, l & 0xff));

                output.Append(String.Format(HEX_FORMAT_STRING, (r >> 24) & 0xff));
                output.Append(String.Format(HEX_FORMAT_STRING, (r >> 16) & 0xff));
                output.Append(String.Format(HEX_FORMAT_STRING, (r >> 8) & 0xff));
                output.Append(String.Format(HEX_FORMAT_STRING, r & 0xff));
            }

            return output.ToString();
        }
    }
}
