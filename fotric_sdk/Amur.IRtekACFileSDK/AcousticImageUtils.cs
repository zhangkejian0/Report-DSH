// Copyright(c) IRtek.Co.Ltd.All rights reserved.

using System;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Amur.IRtekACFileSDK
{
    public class AcousticImageUtils
    {
        private const string DLL_PATH = "AcousticUtils.dll";

        /** 所有函数的返回值 */
        public const int EC_OK = 0;                     /**< 函数调用成功 */
        public const int EC_FAIL = -1;                  /**< 函数调用失败 */
        public const int EC_SPL_PARAM_ERR = -2;         /**< 声压级参数错误 */
        public const int EC_DB_SCALE_PARAM_ERR = -3;    /**< dbScale参数错误 */
        public const int EC_FOCUS_MODE_PARAM_ERR = 4;   /**< focusMode参数错误 */
        public const int EC_PALETTE_PARAM_ERR = -5;     /**< 调色板参数错误 */
        public const int EC_OUT_DATA_PARAM_ERR = -6;    /**< 输出缓冲区参数错误 */

        public const int OUT_EXTREME_POINT_COUNT = 3;
        public const int ARGB = 0;
        public const int RGBA = 1;

        public struct grid_spl
        {
            public int width;                      /**< 区域数组宽 */
            public int height;                     /**< 区域数组高 */
            public IntPtr value;                    /**< 声压值float数组 */
        }

        public struct acoustic_point
        {
            public int x;
            public int y;
            public float value;
        }

        public struct acoustic_summary
        {
            public acoustic_point max;
            public acoustic_point min;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.I1)]
            public acoustic_point[] extreme_points;
            public int extreme_points_count;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct acoustic_file_buffer
        {
            public int length;                     /**< 缓存区长度 */
            public IntPtr buffer;                  /**< 字节数组首地址 */
        }

        [DllImport(DLL_PATH, EntryPoint = "acoustic_get_sdk_version", CallingConvention = CallingConvention.Cdecl)]
        public static extern int acoustic_get_sdk_version(ref uint ver, ref acoustic_file_buffer str);

        [DllImport(DLL_PATH, EntryPoint = "acoustic_get_spl_image", CallingConvention = CallingConvention.Cdecl)]
        public static extern int acoustic_get_spl_image(ref grid_spl gridSpl, ref IRtekACFileSDK.ac_file_db_scale dbScale, ref IRtekACFileSDK.ac_file_focus_mode focusMode, ref IRtekACFileSDK.ac_file_palette palette, byte imageType, float scale, IntPtr buffer, ref acoustic_summary summary);
    }
}
