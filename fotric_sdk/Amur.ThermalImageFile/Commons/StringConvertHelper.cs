using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amur.ThermalImageFile
{
    public static  class StringConvertHelper
    {
        #region Methods
        public static string BytesToString(byte[] input, Encoding encode)
        {
            if (input == null)
            {
                return string.Empty;
            }

            int count = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == 0)
                {
                    break;
                }
                count++;
            }
            return encode.GetString(input, 0, count);
        }

        public static string BytesToString(byte[] input, int startIndex, int length, Encoding encode)
        {
            if (input == null)
            {
                return string.Empty;
            }

            int count = 0;
            for (int i = startIndex; i < input.Length && i < startIndex + length; i++)
            {
                if (input[i] == 0)
                {
                    break;
                }
                count++;
            }
            return encode.GetString(input, startIndex, count);
        }

        public static byte[] StringToBytes(string input, Encoding encode)
        {
            return encode.GetBytes(input);
        }

        public static byte[] StringToBytes(string input, int length, Encoding encode)
        {
            byte[] result = new byte[length];

            if (input != null)
            {
                byte[] tmp = encode.GetBytes(input);
                for (int i = 0; i < result.Length && i < tmp.Length; i++)
                {
                    result[i] = tmp[i];
                }
            }

            return result;
        }
        #endregion
    }
}
