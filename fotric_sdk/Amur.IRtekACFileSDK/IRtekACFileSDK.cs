using System;
using System.Runtime.InteropServices;

namespace Amur.IRtekACFileSDK
{
    public class IRtekACFileSDK
    {
        private const string DLL_PATH = "IRtekACFileSDK.dll";
        
        public const string GLOBAL_MARKER = "";

        /** 声像文件类型 */
        public const int AC_FILE_OTHER = 0;              /**< 非声像文件 */
        public const int AC_FILE_PICTURE = 1;            /**< 声像图片 */
        public const int AC_FILE_VIDEO = 2;              /**< 声像视频 */

        /** 所有函数的返回值 */
        public const int AC_FILE_EC_OK = 0;              /**< 函数调用成功 */
        public const int AC_FILE_EC_FAIL = -1;           /**< 函数调用失败 */
        public const int AC_FILE_EC_MEMORY = -2;         /**< 内存分配失败 */
        public const int AC_FILE_EC_NULL_POINTER = -3;   /**< 空指针 */
        public const int AC_FILE_EC_INVALID_KEY = -4;    /**< 索引不存在 */
        public const int AC_FILE_EC_NOT_SUPPORTED = -5;  /**< 功能不支持 */

        /** 声宽模式 */
        public const int AC_FILE_AUTO_MONO = 1;          /**< 单声源模式 */
        public const int AC_FILE_AUTO_MULTI = 2;         /**< 多声源模式 */
        public const int AC_FILE_FIXED = 3;              /**< 固定模式 */
        public const int AC_FILE_AUTO_HOLO = 4;          /**< 全息模式 */

        /** 检测模式 */
        public const int AC_FILE_CAPTURE_MODE_LEAK = 1;  /**< 单声源模式 */
        public const int AC_FILE_CAPTURE_MODE_PD = 2;    /**< 多声源模式 */

        /** 语音文件格式 */
        public const int AC_FILE_WAV = 1;                /**< WAV */
        public const int AC_FILE_MP3 = 2;                /**< MP3 */

        /** 测量标识类型 */
        public const int AC_FILE_SPOT = 1;               /**< 点 */
        public const int AC_FILE_ELLIPSE = 2;            /**< 椭圆 */
        public const int AC_FILE_RECTANGLE = 3;          /**< 矩形 */

        /** 旋转方式 */
        public const int AC_FILE_ROTATE_90 = 1;          /**< 顺时针旋转90° */
        public const int AC_FILE_ROTATE_180 = 2;         /**< 顺时针旋转180° */
        public const int AC_FILE_ROTATE_270 = 3;         /**< 顺时针旋转270° */

        /** 镜像方式 */
        public const int AC_FILE_MIRROR_HORIZONTAL = 1;  /**< 左右镜像 */
        public const int AC_FILE_MIRROR_VERTICAL = 2;    /**< 上下镜像 */

        public const int MODEL_LEN = 32;
        public const int SERIAL_NO_LEN = 48;
        public const int BRAND_LEN = 32;
        public const int COMPANY_LEN = 64;
        public const int NAME_LEN = 32;
        public const int COLORS_LEN = 256;
        public const int KEY_LEN = 16;
        public const int VERSION_LEN = 48;

        /** 点 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_point
        {
            public short x;                        /**< 点的X轴坐标 */
            public short y;                        /**< 点的Y轴坐标 */
        }

        /** 归一化浮点 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_point_f
        {
            public float x;                        /**< 矩形左上角点的X轴相对坐标 */
            public float y;                        /**< 矩形左上角点的Y轴相对坐标 */
        }

        /** 矩形 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_rectangle
        {
            public short x;                        /**< 矩形左上角点的X轴坐标 */
            public short y;                        /**< 矩形左上角点的Y轴坐标 */
            public short width;                    /**< 矩形的长度 */
            public short height;                   /**< 矩形的宽度 */
        }

        /** 归一化浮点矩形 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_rectangle_f
        {
            public float x;                        /**< 矩形左上角点的X轴相对坐标 */
            public float y;                        /**< 矩形左上角点的Y轴相对坐标 */
            public float width;                    /**< 矩形的相对长度 */
            public float height;                   /**< 矩形的相对宽度 */
        }

        /** 缓存区 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_buffer
        {
            public int length;                     /**< 缓存区长度 */
            public IntPtr buffer;                  /**< 字节数组首地址 */
        }

        /** 设备信息 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_camera_info
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MODEL_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] model;                   /**< 设备型号 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = SERIAL_NO_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] serialNo;                /**< 设备序列号 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = BRAND_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] brand;                   /**< 品牌 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = COMPANY_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] company;                 /**< 厂商 */
            public int logoLength;                /**< 图片大小 */
            public IntPtr logo;                    /**< logo图片，jpg文件，字节数组首地址 */
        }

        /** 传感器信息 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_sensor_info
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = SERIAL_NO_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] serial_no;                    /**< 传感器序列号 */
            public int channel_count;                   /**< 传感器通道数 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = VERSION_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] version;                      /**< 传感器固件版本号 */
            public int ref_channel;                     /**< 传感器监视麦克风编号 */
            public int sample_rate;                     /**< 传感器采样率 */
            public float amplitude;                     /**< 传感器声音信号的最大幅度值 */
        }

        /** 频率范围 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_band
        {
            public float max;                      /**< 频率范围上限，取值范围0.0~1000000.0，默认值为
                                                    *   100000.0 */
            public float min;                      /**< 频率范围下限，取值范围0.0~1000000.0，默认值为
                                                    *   5000.0 */
        }

        /** 调色板 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_palette
        {
            public byte refNo;                     /**< 调色板编号，等于0xFF表示使用自定义调色板 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = COLORS_LEN, ArraySubType = UnmanagedType.I1)]
            public uint[] colors;                  /**< 调色板，每个UINT32前24位分别为RGB，最后8位不使用 */
            public byte alpha;                      /**< 调色板透明度，取值范围0~100，表示透明度百分比, 默认值为60 */
        }

        /** 图像分辨率 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_size
        {
            public short width;                    /**< 宽度 */
            public short height;                   /**< 高度 */
        }

        /** 声像参数 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_acoustic_params
        {
            public int count;
            public IntPtr bands;                  /**< 传感器成像频带的上限值，取值范围0.0~1000000.0 
                                                        默认值50000.0
                                                        传感器成像频带的下限值，取值范围0.0~1000000.0
                                                        默认值30000*/
            public int gain;                        /**< 传感器成像算法增益，枚举类型。枚举值从-1~5 */
            public float smooth;                    /**< 帧间平均系数，取值范围0~1.0
                                                        默认值为0 */
        }

        /** 声宽信息 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_db_scale
        {
            public int mode;                       /**< 声宽模式，默认为自动模式 */
            public float max;                      /**< 声宽上限值，取值范围0~5750 */
            public float min;                      /**< 声宽下限值，取值范围0~5750 */
            public byte fixedSpan;
            public float span;
        }

        /** 聚焦模式 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_focus_mode
        {
            public byte enabled;                       /**< 聚焦模式启用标识：
                                                            1.0x01表示启用
                                                            2.其它表示不启用
                                                            默认值为0x00 */
            public float scope;                      /**< 聚焦范围，取值范围0.1~1.0
                                                            默认值为0.5 */
        }

        /** 地理位置 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_geo_location
        {
            public byte isGPSValid;                /**< GPS信息有效标识。0x01表示启用，其它表
                                                    *   示不启用，默认值为0x00 */
            public double longitude;               /**< 经度 */
            public double latitude;                /**< 纬度 */
            public double altitude;                /**< 海拔 */
            public byte isCompassValid;            /**< 指南针有效标识。0x01表示启用，其它表示
                                                    *   不启用，默认值为0x00 */
            public short direction;                 /**< 方向，为-180~180的角度值，+-180表示正
                                                    *   南方向，0度表示正北，-90表示正西，+90
                                                    *   表示正东。-180~180范围外的值表示无意义 */
        }

        /** 音频文件 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_audio
        {
            public int type;                       /**< 语音文件格式，默认为MP3 */
            public int length;                     /**< 音频大小 */
            public IntPtr buffer;                  /**< 字节数组首地址 */
        }

        /** 用户数据键列表 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_keys
        {
            public int count;                      /**< 用户数据键数量 */
            public IntPtr keys;                    /**< 用户数据键数组首地址 */
        }

        /** 键值对 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_kvPair
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = KEY_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] key;                     /**< 键 */
            public int vLength;                    /**< 值长度 */
            public IntPtr vBuffer;                 /**< 值 */
        }

        /** 数码变倍信息 */
        public struct ac_file_zoom_info
        {
            public byte enabled;                    /**< 是否启用数码变倍，0x01表示启用，其它表
                                                    *   示不启用，默认值为0x00 */
            public ac_file_point_f location;        /**< 左上角相对坐标，最小相对坐标为(0.0,0.0)，
                                                    *   最大相对坐标为(1.0,1.0)，默认值为
                                                    *   (0.0,0.0) */
            public float ratio;                     /**< 数码变倍数，变焦倍数需大于1.0，默认值
                                                    *   为1 */
        }

        /** 测量标识名列表 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_marker_names
        {
            public int count;                      /**< 测量标识名数量 */
            public IntPtr names;                   /**< 测量标识名数组首地址 */
        }

        /** 测量标识 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_marker
        {
            public int type;                       /**< 测量标识类型，默认值为点 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = NAME_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] name;                    /**< 名称 */
            public int point_count;                /**< 点数量 */
            public IntPtr points;                  /**< 点数组首地址 */
            public float db_value;                 /**< 最大分贝数 */
            public ac_file_point point;            /**< 最大分贝点位置 */
        }

        /** 数据帧 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file
        {
            public int count;                      /**< 字节数组大小 */
            public IntPtr buffer;                  /**< 字节数组首地址 byte[] */
        }

        /** 数据帧 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_raw
        {
            public int count;                      /**< 字节数组大小 */
            public IntPtr buffer;                  /**< 字节数组首地址 float[] */
        }

        /** 声像传感器最后一帧的频谱幅值 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_spectrum
        {
            public ac_file_raw fft;                /**< fft */
            public ac_file_band range;             /**< 范围 */
            public float delta;                    /**< 变化率 */
        }

        /** 声像传感器最后一帧的频谱幅值 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_sound
        {
            public ac_file_raw source;             /**< 源数据 */
            public ac_file_raw filter;             /**< 滤镜 */
            public ac_file_raw down_sample;        /**< 降采样 */
        }

        /** 声像传感器prpd数据 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_prpd
        {
            public uint points_count;               /**< 点数量 */
            public IntPtr points;                   /**< 数据 */
        }

        /** 声像原始帧 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ac_file_frame
        {
            public ulong timeStamp;                /**< 相对于拍摄时间shotTime的毫秒数 */
            public ac_file_raw acoustic_image_spl; /**< 声像传感器声压级网格数据 */
            public ac_file visual_image;           /**< 可见光图像 */
            public ac_file_sound sound;            /**< 声像传感器声音数据 */
            public ac_file_spectrum spectrum;      /**< 声像传感器最后一帧的频谱幅值 */
            public ac_file_prpd prpd;              /**< 声像传感器prpd数据 */
        }

        /** 图片 */
        public struct ac_file_image
        {
            public int offset;                    /**< 行字节对齐偏移 */
            public IntPtr data;                    /**< 数据区首地址 */
        }

        /** 预置调色板 */
        public struct ac_file_preset_palette
        {
            public byte refNo;                     /**< 调色板编号 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = NAME_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] name;                    /**< 调色板名 */
        }

        /** 预置调色板列表 */
        public struct ac_file_preset_palettes
        {
            public int count;                      /**< 预置调色板数量 */
            public IntPtr presetPalettes;          /**< 预置调色板数组首地址 */
        }

        /** 标记点 */
        public struct ac_file_db_point
        {
            public ac_file_point point;             /**< 点坐标 */
            public float value;                     /**< 声压值 */
        }

        /** 标记矩形 */
        public struct ac_file_db_rect
        {
            public ac_file_rectangle rect;          /**< 矩形坐标 */
            public uint count;                      /**< 区域数组大小 */
            public IntPtr value;                    /**< 声压值float数组 */
        }

        /** 获取SDK版本信息 V
        * @param ver    [out] SDK版本号，四字节形式，如0x02040100为2.4.1.0。若为空，则
        *                     不读取。
        * @param str    [out] SDK版本号，字符串形式。若为空，则不读取。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_sdk_version", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_sdk_version(ref uint ver, ref ac_file_buffer str);

        /** 检查文件是否为声像格式 V
        * @param file_name [in] 文件路径。
        * @param value     [out] 声像文件类型。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_peek_file_type", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_peek_file_type(string file_name, ref int value);

        /** 打开声像文件 V
        * 若文件不存在则失败。
        * @param file_name [in] 文件路径。
        * @param h         [out] AC_FILE句柄。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_open", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_open(string file_name, ref IntPtr h);

        /** 创建声像文件 V
        * 若文件已存在将被覆盖。
        * @param file_name [in] 文件路径。
        * @param file_type [in] 文件类型。可创建类型为：
        *                       AC_FILE_PICTURE，
        *                       AC_FILE_VIDEO，
        * @param h         [out] AC_FILE句柄。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_create", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_create(string file_name, int file_type, ref IntPtr h);

        /** 保存声像文件 V
        * @param  h [in] AC_FILE句柄。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_save", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_save(IntPtr h);

        /** 声像文件另存为 V
        * @param  h         [in] AC_FILE句柄。
        * @param file_name  [in] 文件路径。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_save_as", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_save_as(IntPtr h, string file_name);

        /** 关闭声像文件 V */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_close", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_close(IntPtr h);

        /** 获取声像文件版本号 V
        * @param h      [in]  AC_FILE句柄
        * @param ver    [out] 文件版本号
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_file_version", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_file_version(IntPtr h, ref ushort value);

        /** 读声像文件名 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 声像文件名。buffer需要用户分配内存，若buffer为空，不读取
        *                     备注信息，length返回所需内存大小。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_file_name", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_file_name(IntPtr h, ref ac_file_buffer value);

        /** 读声像文件类型 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 声像文件类型。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_file_type", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_file_type(IntPtr h, ref int value);

        /** 读设备信息 V
         * @param h      [in] AC_FILE句柄。
         * @param value  [out] 设备信息。logo需要用户分配内存，若logo为空，不 读取logo图
         *                     片，logoLength返回所需内存大小。
         */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_camera_info", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_camera_info(IntPtr h, ref ac_file_camera_info value);

        /** 写设备信息 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [in] 设备信息。若logo为空，不写入logo图片。若value为空，清空设
        *                    备信息。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_camera_info", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_camera_info(IntPtr h, ref ac_file_camera_info value);

        /** 读传感器信息 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 传感器信息。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_sensor_info", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_sensor_info(IntPtr h, ref ac_file_sensor_info value);

        /** 写传感器信息 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [in] 传感器信息。若value为空，清空传感器信息。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_sensor_info", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_sensor_info(IntPtr h, ref ac_file_sensor_info value);

        /** 读频率范围 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 频率范围。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_frequency_range", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_frequency_range(IntPtr h, ref ac_file_band value);

        /** 写频率范围 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [in] 频率范围。若value为空，清空频率范围。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_frequency_range", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_frequency_range(IntPtr h, ref ac_file_band value);

        /** 读调色板 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 调色板信息。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_palette", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_palette(IntPtr h, ref ac_file_palette value);

        /** 写调色板 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [in] 调色板信息。若value为空，清空调色板信息。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_palette", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_palette(IntPtr h, ref ac_file_palette value);

        /** 读图像分辨率 V
        * @param h      [in] AC_FILE句柄。
        * @param size   [out] 传感器成像分辨率。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_acoustic_size", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_acoustic_size(IntPtr h, ref ac_file_size value);

        /** 写图像分辨率 V
        * @param h      [in] AC_FILE句柄。
        * @param size   [in] 传感器成像分辨率。若value为空，清空传感器成像分辨率。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_acoustic_size", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_acoustic_size(IntPtr h, ref ac_file_size value);

        /** 读声像参数 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 声像参数。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_acoustic_params", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_acoustic_params(IntPtr h, ref ac_file_acoustic_params value);

        /** 写声像参数 V
        * @param h      [in] AC_FILE句柄。
        * @paramvalue   [in] 声像参数。若value为空，清空声像参数。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_acoustic_params", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_acoustic_params(IntPtr h, ref ac_file_acoustic_params value);

        /** 读声宽信息 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out]声宽信息。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_db_scale", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_db_scale(IntPtr h, ref ac_file_db_scale value);

        /** 写声宽信息 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [in|out] 声宽信息。若value为空，清空温宽信息。若为自动模式，写
        *                        声宽信息成功后min和max将会被更新。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_db_scale", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_db_scale(IntPtr h, ref ac_file_db_scale value);

        /** 读检测模式 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out]检测模式。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_capture_mode", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_capture_mode(IntPtr h, ref byte value);

        /** 写检测模式 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [in] 检测模式。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_capture_mode", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_capture_mode(IntPtr h, byte value);

        /** 读聚焦模式 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out]聚焦模式。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_focus_mode", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_focus_mode(IntPtr h, ref ac_file_focus_mode value);

        /** 写聚焦模式 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [in] 聚焦模式。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_focus_mode", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_focus_mode(IntPtr h, ref ac_file_focus_mode value);

        /** 读目标距离 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out]目标距离。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_distance", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_distance(IntPtr h, ref float value);

        /** 写目标距离 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [in] 目标距离。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_distance", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_distance(IntPtr h, float value);

        /** 读标准声压级启用标识 V
       * @param h      [in] AC_FILE句柄。
       * @param value  [out]标准声压级启用标识。
       */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_normalize_distance", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_normalize_distance(IntPtr h, ref byte value);

        /** 写标准声压级启用标识 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [in] 标准声压级启用标识。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_normalize_distance", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_normalize_distance(IntPtr h, byte value);

        /** 读PRPD散点图对应交流电频率 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out]标准声压级启用标识。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_ac_frequency", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_ac_frequency(IntPtr h, ref float value);

        /** 写PRPD散点图对应交流电频率
        * @param h      [in] AC_FILE句柄。
        * @param value  [in] 标准声压级启用标识。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_ac_frequency", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_ac_frequency(IntPtr h, float value);

        /** 读地理位置信息 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 地理位置信息。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_geo_location", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_geo_location(IntPtr h, ref ac_file_geo_location value);

        /** 写地理位置信息 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [in] 地理位置信息。若value为空，清空地理位置信息。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_geo_location", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_geo_location(IntPtr h, ref ac_file_geo_location value);

        /** 读文本备注信息 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 文本备注信息。buffer需要用户分配内存，若buffer为空，不读
        *                     取备注信息，length返回所需内存大小。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_note", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_note(IntPtr h, ref ac_file_buffer value);

        /** 写文本备注信息 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [in] 文本备注信息。若buffer为空，写入失败。若value为空，清空备
        *                    注信息。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_note", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_note(IntPtr h, ref ac_file_buffer value);

        /** 读音频文件
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 音频文件。buffer需要用户分配内存，若buffer为空，不读取音
        *                     频文件，length返回所需内存大小。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_audio", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_audio(IntPtr h, ref ac_file_audio value);

        /** 写音频文件
        * @param h      [in] AC_FILE句柄。
        * @param value  [in] 音频文件。若buffer为空，写入失败。若value为空，清空音频文
        *                    件。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_audio", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_audio(IntPtr h, ref ac_file_audio value);

        /** 获取用户数据键列表 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 用户数据键列表。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_keys", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_keys(IntPtr h, ref ac_file_keys value);


        /** 读用户数据列表 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [in|out] 用户数据项，key为输入项，其它为输出项。vBuffer需要用户
        *                        分配内存vLength字节，若为空，不读取值，vLength返回值长
        *                        度。
        * @retval AC_FILE_EC_INVALID_KEY 键不存在。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_user_data", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_user_data(IntPtr h, ref ac_file_kvPair value);

        /** 写用户数据列表
        * 用户数据以键值对方式存取，其中已定义：
        * 其中已定义：
        * 1.收藏标签：
        * Key：“FAVORITE”
        * Value：收藏标签，长度为1个字节，“Y”表示收藏，其它表示正常
        * 2.注释标签：
        * Key：“TAG”
        * Value：注释标签，若存在多个标签采用字符‘\n’分隔
        * 3.分组文件名列表：
        * Key：“GROUPFILES”
        * Value：关联分组文件名列表，若存在多个文件采用字符‘\n’分隔
        * 4.定时拍照文件名列表：
        * Key：“TIMERFILE”
        * Value：采用定时拍摄方式时，保存GUID和编号，GUID采用编码格式“EDDCC222-97B3-47
        * 24-A4C9-742A7A28EA25”，属于同一定时拍摄的文件保持GUID不变，GUID和编号之间采
        * 用‘\n’分隔符，编号采用字符串编码格式。
        * 5.泄漏量评估：
        * Key：“LEAK”
        * Value：
        * {
        * Version:100,
        * LeakUnit:1
        * Currency:“¥”,
        * EnergyCost:2.7
        * GasCost:0.0
        * LeakCorrection:1.0
        * SpecificPower:7
        * LeakLevel:5.5
        * LeakRate:100
        * }
        * 1）泄漏量单位：该单位可以调整，用于显示泄漏量估计值。可用选项包括:
        * 0x01：公制，L/min(升/分钟)
        * 0x02：英制，CFM(立方英尺/分钟)。
        * 2）货币单位：显示泄漏成本估计的货币，可以从可用选项中进行选择。
        * 3）能源成本：要估计压缩空气泄漏的成本，须了解能源成本。默认为上海工业用电的电费(每千瓦时)。
        * 4）气体成本：特殊气体的价格（每升或立方英尺的价格，根据泄漏量单位变化）
        * 5）泄漏校正：由于泄漏量取决于许多不同的因素，如果泄漏量估计值与观察的结果之间存在差异，可以通过设置泄漏校正系数来进行调整。为大于0的数值，根据设置数值
        * 6）比功率：比功率可以衡量产生一定量的压缩空气需要多少能量。单位是
        * 公制：kW/m³/min 
        * 英制：kW/100CFM
        * 根据泄漏量单位选择变化。默认值为7kW/m³/min，7kWh能压缩60m³的空气。
        * 7）泄漏等级：根据泄漏量模型评估出来的泄漏等级。
        * 8）泄漏量：根据泄漏量模型评估出来的泄漏量。单位是
        * 公制：L/min 
        * 英制：CFM
        * @param h      [in] AC_FILE句柄。
        * @param value  [in] 用户数据项。若vBuffer为空，删除该项用户数据。若value为空，
        *                    清空用户数据列表。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_user_data", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_user_data(IntPtr h, ref ac_file_kvPair value);

        /** 读拍摄时间 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 拍摄时间，采用UTC时间，从1970-01-01 00:00:00.000起的毫秒
        *                     数。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_shot_time", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_shot_time(IntPtr h, ref ulong value);

        /** 读修改时间 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 修改时间，采用UTC时间，从1970-01-01 00:00:00.000起的毫秒
        *                     数。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_modified_time", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_modified_time(IntPtr h, ref ulong value);

        /** 读数码变焦倍数 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 数码变焦倍数。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_zoom_info", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_zoom_info(IntPtr h, ref ac_file_zoom_info value);

        /** 读可见光灰度模式启用标识 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 可见光灰度模式启用标识。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_grey_scale", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_grey_scale(IntPtr h, ref byte value);

        /** 写可见光灰度模式启用标识 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 可见光灰度模式启用标识。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_grey_scale", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_grey_scale(IntPtr h, byte value);

        /** 获取测量标识名列表 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 测量标识名列表。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_marker_names", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_marker_names(IntPtr h, ref ac_file_marker_names value);

        /** 读测量标识 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [in|out] 测量标识，name为输入项，其它为输出项。points需要用户分
        *                        配内存（ac_file_point[pointCount]），若为空，不读取点
        *                        列表，pointCount返回点列表长度。
        * @retval AC_FILE_EC_INVALID_KEY 该测量标识不存在。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_marker", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_marker(IntPtr h, ref ac_file_marker value);

        /** 写测量标识 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [in|out] 测量标识，tempValues、tempPos、area为输出项，其它为输
        *                        入项。若测量标识存在则修改，不存在则添加。若points为空，
        *                        写入失败。写测量标识成功后输出项将会被更新。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_marker", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_marker(IntPtr h, ref ac_file_marker value);

        /** 删除测量标识 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [in] 测量标识名。若value为空，清空测量标识列表。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_remove_marker", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_remove_marker(IntPtr h, byte[] value);

        /** 重命名测量标识
        * @param h      [in] AC_FILE句柄。
        * @param from   [in] 旧测量标识名。
        * @param to     [in] 新测量标识名。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_rename_marker", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_rename_marker(IntPtr h, byte[] from, byte[] to);

        /** 读封面图 V
        * 封面图即声像文件中的普通jpg部分，使用看图工具查看图片将显示封面图。
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 封面图，jpg文件。buffer需要用户分配内存，若buffer为空，不
        *                     读取封面图，length返回图片大小。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_cover_image", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_cover_image(IntPtr h, ref ac_file_buffer value);

        /** 写封面图 V
        * 封面图即声像文件中的普通jpg部分，使用看图工具查看图片将显示封面图。
        * @param h      [in] AC_FILE句柄。
        * @param value  [in] 封面图，jpg文件，单边最小尺寸240像素。若buffer为空，写入失
        *                    败。若value为空，清空封面图。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_cover_image", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_cover_image(IntPtr h, ref ac_file_buffer value);

        /** 读总帧数
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 总帧数。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_frame_count", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_frame_count(IntPtr h, ref uint value);

        /** 读帧频
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 帧频。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_frame_rate", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_frame_rate(IntPtr h, ref float value);

        /** 写帧频
        * @param h      [in] AC_FILE句柄。
        * @param value  [in] 帧频。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_frame_rate", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_frame_rate(IntPtr h, float value);

        /** 获取声像原始帧 V
        * @param h      [in] AC_FILE句柄。
        * @param index  [in] 帧索引。
        * @param value  [in] 声像原始帧。buffer需要用户分配内存，若buffer为空，不读
        *                    取声像原始帧，length返回buffer数组所需大小。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_frame", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_frame(IntPtr h, uint index, ref ac_file_frame value);

        /** 追加声像原始帧 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [in] 声像原始帧。若buffer为空，写入失败。若value为空，清空帧
        *                    数据区。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_append_frame", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_append_frame(IntPtr h, ref ac_file_frame value);

        /** 读点声压值 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 点
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_db_spot", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_db_spot(IntPtr h, ref ac_file_db_point value);
   
        /** 读椭圆区域声压值
        * @param h      [in] AC_FILE句柄。
        * @param value  [in|out] 椭圆区域
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_db_ellipse", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_db_ellipse(IntPtr h, ref ac_file_db_rect value);

        /** 读调色板色带 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 调色板色带，256行1列BGR。
      */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_palette_image", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_palette_image(IntPtr h, ref ac_file_image value);

        /** 读预置调色板 V
        * @param value  [out] 预置调色板。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_preset_palettes", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_preset_palettes(ref ac_file_preset_palettes value);

        /** 读预置调色板色带 V
        * @param refNo  [in] 调色板编号。
        * @param value  [out] 预置调色板色带，256行1列BGR。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_preset_palette_image", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_preset_palette_image(byte refNo, ref ac_file_image value);

        /** 获取当前全息数据帧索引 V
        * @param h      [in] AC_FILE句柄。
        * @param value  [out] 帧索引，取值范围0到总帧数-1。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_frame_index", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_frame_index(IntPtr h, ref uint value);

        /** 设置当前全息数据帧索引
        * @param h      [in] AC_FILE句柄。
        * @param value  [in] 帧索引，取值范围0到总帧数-1。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_set_frame_index", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_set_frame_index(IntPtr h, uint value);

        /** 声像文件当前帧另存为
        * @param h          [in] AC_FILE句柄。
        * @param file_name  [in] 文件路径。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_save_frame_as", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_save_frame_as(IntPtr h, string file_name);

        /** 读拍摄时间
        * @param h      [in] IR_FILE句柄。
        * @param index	[in] 帧索引。
        * @param value  [out] 拍摄时间，采用UTC时间，从1970-01-01 00:00:00.000起的毫秒
        *                     数。
        */
        [DllImport(DLL_PATH, EntryPoint = "ac_file_get_index_shot_time", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_get_index_shot_time(IntPtr h, uint index, ref ulong value);

        [DllImport(DLL_PATH, EntryPoint = "ac_file_save_frame_as", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ac_file_save_frame_as(IntPtr h, string file_name, ref ac_file_buffer value);
    }
}
