using System;
using System.Runtime.InteropServices;

namespace Amur.IRtekFileSDK
{
    public sealed class IRtekFileSDK
    {
        /** 热像文件类型 */
        public const int IR_FILE_OTHER = 0;              /**< 非热像文件 */
        public const int IR_FILE_THERMAL_PICTURE = 1;    /**< 热像图片 */
        public const int IR_FILE_THERMAL_VIDEO = 2;      /**< 热像视频 */
        public const int IR_FILE_VISUAL_PICTURE = 3;     /**< 可见光图片 */
        public const int IR_FILE_GRID_THERMAL_PICTURE = 4;  /**< 电网热像图片 */

        /** 所有函数的返回值 */
        public const int IR_FILE_EC_OK = 0;              /**< 函数调用成功 */
        public const int IR_FILE_EC_FAIL = -1;           /**< 函数调用失败 */
        public const int IR_FILE_EC_MEMORY = -2;         /**< 内存分配失败 */
        public const int IR_FILE_EC_NULL_POINTER = -3;   /**< 空指针 */
        public const int IR_FILE_EC_INVALID_KEY = -4;    /**< 索引不存在 */
        public const int IR_FILE_EC_NOT_SUPPORTED = -5;  /**< 功能不支持 */

        /** 测量标识值类型 */
        public const int IR_FILE_MAX = 1;                /**< 最高温 */
        public const int IR_FILE_MIN = 2;                /**< 最低温 */
        public const int IR_FILE_AVG = 3;                /**< 平均温 */

        /** 温宽模式 */
        public const int IR_FILE_FIXED = 1;              /**< 固定模式 */
        public const int IR_FILE_AUTO = 2;               /**< 自动模式 */
        public const int IR_FILE_TOUCH = 3;              /**< 触控模式 */

        /** 报警类型 */
        public const int IR_FILE_NORMAL = 0;             /**< 不启用报警 */
        public const int IR_FILE_BELOW = 1;              /**< 低于报警 */
        public const int IR_FILE_ABOVE = 2;              /**< 高于报警 */
        public const int IR_FILE_BETWEEN = 3;            /**< 之间报警 */
        public const int IR_FILE_MAGIC_THERMAL = 4;      /**< MagicThermal报警 */
        public const int IR_FILE_OUTSIDE = 5;            /**< 之外报警 */
        public const int IR_FILE_HUMIDITY = 6;           /**< 湿度报警 */
        public const int IR_FILE_INSULATION = 7;         /**< 保温报警 */

        /** 语音文件格式 */
        public const int IR_FILE_WAV = 1;                /**< WAV */
        public const int IR_FILE_MP3 = 2;                /**< MP3 */

        /** 测量标识类型 */
        public const int IR_FILE_SPOT = 1;               /**< 点 */
        public const int IR_FILE_LINE = 2;               /**< 线 */
        public const int IR_FILE_RECTANGLE = 3;          /**< 矩形 */
        public const int IR_FILE_ELLIPSE = 4;            /**< 椭圆 */
        public const int IR_FILE_POLYGON = 5;            /**< 多边形 */
        public const int IR_FILE_POLYLINE = 6;           /**< 折线 */

        /** 显示模式 */
        public const int IR_FILE_THERMAL = 1;            /**< 红外图像模式 */
        public const int IR_FILE_PIC_IN_PIC = 2;         /**< 画中画模式 */
        public const int IR_FILE_T_DEF = 3;              /**< 可见光测温模式 */
        public const int IR_FILE_FUSION = 4;             /**< 融合模式 */

        /** 色彩分布类型 */
        public const int IR_FILE_T_TWB = 1;              /**< 高温差均衡成像分布 */
        public const int IR_FILE_TEMP_LINEAR = 2;        /**< 温度线性 */
        public const int IR_FILE_AD_LINEAR = 3;          /**< 信号线性 */

        /** 旋转方式 */
        public const int IR_FILE_ROTATE_90 = 1;          /**< 顺时针旋转90° */
        public const int IR_FILE_ROTATE_180 = 2;         /**< 顺时针旋转180° */
        public const int IR_FILE_ROTATE_270 = 3;         /**< 顺时针旋转270° */

        /** 镜像方式 */
        public const int IR_FILE_MIRROR_HORIZONTAL = 1;  /**< 左右镜像 */
        public const int IR_FILE_MIRROR_VERTICAL = 2;    /**< 上下镜像 */

        public const int IR_FILE_TEMP_MAX = 0;
        public const int IR_FILE_TEMP_MIN = 1;
        public const int IR_FILE_TEMP_AVG = 2;

        public const int IR_FILE_SPOT_HOT = 0;
        public const int IR_FILE_SPOT_COLD = 1;

        public const string GLOBAL_MARKER = "global";

        public const int MODEL_LEN = 32;
        public const int SERIAL_NO_LEN = 48;
        public const int BRAND_LEN = 32;
        public const int COMPANY_LEN = 64;
        public const int NAME_LEN = 32;
        public const int BAND_LEN = 32;
        public const int COLORS_LEN = 256;
        public const int KEY_LEN = 16;

        /** 点 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_point
        {
            public short x;                        /**< 点的X轴坐标 */
            public short y;                        /**< 点的Y轴坐标 */
        }

        /** 归一化浮点 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_point_f
        {
            public float x;                        /**< 矩形左上角点的X轴相对坐标 */
            public float y;                        /**< 矩形左上角点的Y轴相对坐标 */
        }

        /** 矩形 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_rectangle
        {
            public short x;                        /**< 矩形左上角点的X轴坐标 */
            public short y;                        /**< 矩形左上角点的Y轴坐标 */
            public short width;                    /**< 矩形的长度 */
            public short height;                   /**< 矩形的宽度 */
        }

        /** 归一化浮点矩形 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_rectangle_f
        {
            public float x;                        /**< 矩形左上角点的X轴相对坐标 */
            public float y;                        /**< 矩形左上角点的Y轴相对坐标 */
            public float width;                    /**< 矩形的相对长度 */
            public float height;                   /**< 矩形的相对宽度 */
        }

        /** 缓存区 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_buffer
        {
            public uint length;                    /**< 缓存区长度 */
            public IntPtr buffer;                  /**< 字节数组首地址 */
        }

        /** 设备信息 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_camera_info
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MODEL_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] model;                   /**< 设备型号 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = SERIAL_NO_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] serialNo;                /**< 设备序列号 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = BRAND_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] brand;                   /**< 品牌 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = COMPANY_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] company;                 /**< 厂商 */
            public uint logoLength;                /**< 图片大小 */
            public IntPtr logo;                    /**< logo图片，jpg文件，字节数组首地址 */
        }

        /** 镜头信息 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_lens_info
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = NAME_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] name;                    /**< 镜头名 */
            public float focalLength;              /**< 镜头焦距，默认为0.0f，表示无效参数 */
            public float hfov;                     /**< 镜头度数，默认为0.0f，表示无效参数 */
        }

        /** 镜头信息V2 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_lens_info_v2
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = NAME_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] name;                    /**< 镜头名 */
            public float focalLength;              /**< 镜头焦距，默认为0.0f，表示无效参数 */
            public float hfov;                     /**< 镜头度数，默认为0.0f，表示无效参数 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = BAND_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] band;                    /**< 镜头响应波段，默认为空，表示无响应波段信息 */
        }

        /** 量程信息 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_temp_range
        {
            public float max;                      /**< 量程上限，取值范围0~5750，默认值为423.15 */
            public float min;                      /**< 量程下限，取值范围0~5750，默认值为253.15 */
        }

        /** 温度查找表 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_lut
        {
            public byte radMethod;                 /**< 温度补偿算法版本。0x00表示温度补偿算法
                                                    *   1.0。0x01表示温度补偿算法2.0，默认值为
                                                    *   0x01。 */
            public uint count;                     /**< 出厂LUT长度 */
            public IntPtr adArray;                 /**< 出厂LUT表的AD值表，USHORT数组 */
            public IntPtr tempArray;               /**< 出厂LUT表的温度值表，float数组 */
        }

        /** 调色板 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_palette
        {
            public byte refNo;                     /**< 调色板编号，等于0xFF表示使用自定义调色板 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = COLORS_LEN, ArraySubType = UnmanagedType.I1)]
            public uint[] colors;                  /**< 调色板，每个UINT32前24位分别为RGB，最后8位不使用 */
            public byte inverted;                  /**< 调色板是否反转显示，0x01表示启用，其它表示不启用，默认值为0x00 */
        }

        /** 调色板 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_palette_v2
        {
            public byte refNo;                     /**< 调色板编号，0xFF表示使用自定义调色板 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = COLORS_LEN, ArraySubType = UnmanagedType.I1)]
            public uint[] srcColors;               /**< 调色板，每个UINT32前24位分别为RGB，最后8位不使用 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = COLORS_LEN, ArraySubType = UnmanagedType.I1)]
            public uint[] destColors;              /**< 调色板叠加信息后的数据，每个UINT32前24位分别为RGB，最后8位不使用 */
            public byte inverted;                  /**< 调色板是否反转显示，0x01表示启用，其它表示不启用，默认值为0x00 */
        }

        /** 图像分辨率 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_size
        {
            public short width;                    /**< 宽度 */
            public short height;                   /**< 高度 */
        }

        /** 测温参数 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_thermal_params
        {
            public float reflTemp;                  /**< 反射温度，取值范围0~5750，默认值为293.15 */
            public float atmTemp;                  /**< 大气温度，取值范围0~5750，默认值为293.15 */
            public float distance;                 /**< 目标距离，取值范围0~50000，默认值为1.0 */
            public float relHumidity;              /**< 相对湿度，取值范围0.01~1.0，默认值为0.5 */
            public float emissivity;               /**< 发射率，取值范围0.01~1.0，默认值为1.0 */
            public float extOptTemp;               /**< 外部光学温度，取值范围0~5750，默认值为293.15 */
            public float extOptTrans;              /**< 外部光学透过率，取值范围0.01~1.0，默认值为1.0 */
        }

        /** 温宽信息 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_gain
        {
            public int mode;                       /**< 温宽模式，默认为自动模式 */
            public float max;                      /**< 温宽上限值 */
            public float min;                      /**< 温宽下限值 */
            public byte fixedSpan;                 /**< 是否使用固定温宽跨度，0x01表示启用，其它表示不启用，默认值为0x00 */
            public float span;                     /**< 温宽跨度，取值范围0.1~5750，默认值为2.0 */
        }

        /** 报警信息 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_alarm
        {
            public int type;                       /**< 报警类型，默认为不启用 */
            public byte custom;                    /**< 是否启用自定义报警，不启用报警和MagicThermal报警类型，此参数无效。
                                                    *   0x01表示启用，其它表示不启用，默认值为
                                                    *   0x00，*/
            public uint color;                     /**< 报警颜色，ARGB，默认值为
                                                    *   （127,255,0,0） */
            public float max;                      /**< 高于报警(Above)类型，高于该阈值；
                                                    *   之间报警(Between)，低于该阈值；
                                                    *   之外报警(Outsize)，高于该阈值；
                                                    *   其他报警类型无效。
                                                    *   取值范围0~5750，默认值为313.15 */
            public float min;                      /**< 低于报警(Below)类型，低于该阈值；
                                                    *   之间报警(Between)，高于该阈值；
                                                    *   之外报警(Outsize)，低于该阈值；
                                                    *   其他模报警类型无效。
                                                    *   取值范围0~5750，默认值为293.15 */
            public float rhLimit;                  /**< 相对湿度限制，除湿度报警(Humidity)类型，
                                                    *   其他类型此参数无效。
                                                    *   取值范围0.01~1.0，默认值为1.0 */
            public float indoorTemp;               /**< 室内温度，除保温报警(Insulation)类型，
                                                    *   其他类型此参数无效。
                                                    *   取值范围0~5750，默认值为293.15 */
            public float outdoorTemp;              /**< 室外温度，除保温报警(Insulation)类型，
                                                    *   其他类型此参数无效。
                                                    *   取值范围0~5750，默认值为283.15 */
            public float heatIndex;                /**< 热量指数，除保温报警(Insulation)类型，
                                                    *   其他类型此参数无效。
                                                    *   取值范围0.0~1.0，默认值为0.8 */
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_alarm_v2
        {
            public int type; /**< 报警类型，默认为不启用 */
            public byte custom; /**< 是否启用自定义报警，不启用报警和复合调色板报警类型，此参数无效。
                                     *   0x01表示启用，其它表示不启用，默认值为
                                     *   0x00，*/
            public uint color; /**< 报警颜色，ARGB，默认值为
                                     *   （127,255,0,0） */
            public float max; /**< 高于报警(Above)类型，高于该阈值；
                                     *   之间报警(Between)，低于该阈值；
                                     *   复合调色板报警(MixPalette)，高于该阈值；
                                     *   之外报警(Outsize)，高于该阈值；
                                     *   其他报警类型无效。
                                     *   取值范围0~5750，默认值为313.15 */
            public float min; /**< 低于报警(Below)类型，低于该阈值；
                                     *   之间报警(Between)，高于该阈值；
                                     *   复合调色板报警(MixPalette)，低于该阈值；
                                     *   之外报警(Outside)，低于该阈值；
                                     *   其他模报警类型无效。
                                     *   取值范围0~5750，默认值为293.15 */
            public float rhLimit; /**< 相对湿度限制，除湿度报警(Humidity)类型，
                                     *   其他类型此参数无效。
                                     *   取值范围0.01~1.0，默认值为1.0 */
            public float indoorTemp; /**< 室内温度，除保温报警(Insulation)类型，
                                     *   其他类型此参数无效。
                                     *   取值范围0~5750，默认值为293.15 */
            public float outdoorTemp; /**< 室外温度，除保温报警(Insulation)类型，
                                     *   其他类型此参数无效。
                                     *   取值范围0~5750，默认值为283.15 */
            public float heatIndex; /**< 热量指数，除保温报警(Insulation)类型，
                                     *   其他类型此参数无效。
                                     *   取值范围0.0~1.0，默认值为0.8 */
            public float threshold; /**< 湿度报警、保温报警类型的温度阈值，
                                     *   其他类型此参数无效。
                                     *   取值范围0~5750，默认值为293.15 */
        };

        /** 地理位置 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_geo_location
        {
            public byte isGPSValid;                /**< GPS信息有效标识。0x01表示启用，其它表
                                                    *   示不启用，默认值为0x00 */
            public double longitude;               /**< 经度 */
            public double latitude;                /**< 纬度 */
            public double altitude;                /**< 海拔 */
            public byte isCompassValid;            /**< 指南针有效标识。0x01表示启用，其它表示
                                                    *   不启用，默认值为0x00 */
            public short direction;                /**< 方向，为-180~180的角度值，+-180表示正
                                                    *   南方向，0度表示正北，-90表示正西，+90
                                                    *   表示正东。-180~180范围外的值表示无意义 */
        }

        /** 音频文件 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_audio
        {
            public int type;                       /**< 语音文件格式，默认为MP3 */
            public uint length;                    /**< 音频大小 */
            public IntPtr buffer;                  /**< 字节数组首地址 */
        }

        /** 用户数据键列表 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_keys
        {
            public uint count;                     /**< 用户数据键数量 */
            public IntPtr keys;                    /**< 用户数据键数组首地址 */
        }

        /** 键值对 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_kvPair
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = KEY_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] key;                     /**< 键 */
            public uint vLength;                   /**< 值长度 */
            public IntPtr vBuffer;                 /**< 值 */
        }

        /** 测量标识名列表 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_marker_names
        {
            public int count;                      /**< 测量标识名数量 */
            public IntPtr names;                   /**< 测量标识名数组首地址 */
        }

        /** 测量标识 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_marker
        {
            public int type;                       /**< 测量标识类型，默认值为点 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = NAME_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] name;                    /**< 名称 */
            public byte localParams;               /**< 是否启用局部参数，0x01表示启用，其它表示不启用，默认值为0x00 */
            public float reflTemp;                 /**< 反射温度，取值范围0~5750，默认值为293.15 */
            public float distance;                 /**< 目标距离，取值范围0~50000，默认值为1.0 */
            public float emissivity;               /**< 发射率，取值范围0.01~1.0，默认值为1.0 */
            public int pointCount;                 /**< 点数量 */
            public IntPtr points;                  /**< 点数组首地址 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.I1)]
            public float[] tempValues;             /**< 最高温、最低温、平均温 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.I1)]
            public ir_file_point[] tempSpots;      /**< 最高温点、最低温点 */
            public float measuredValue;            /**< 点、矩形、椭圆、多边形的面积(平方米)或
                                                    *   折线的长度(米) */
        }

        /** 温差标记名列表 */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ir_file_delta_names
        {
            public int count;                      /**< 温差标记名数量 */
            public IntPtr names;                   /**< 温差标记名数组首地址 */
        }

        /** 温差标记 */
        public struct ir_file_delta
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = NAME_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] name;                    /**< 温差标记名 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = NAME_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] srcName;                 /**< 源名称 */
            public int srcValueType;               /**< 源标识值类型，默认为最高温 */
            public float srcTemp;                  /**< 源参考温度值，取值范围0~5750，默认值为293.15 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = NAME_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] destName;                /**< 目标名称 */
            public int destValueType;              /**< 目标标识值类型，默认为最高温 */
            public float destTemp;                 /**< 目标参考温度值，取值范围0~5750，默认值为293.15  */
            public float value;                    /**< 温差值 */
        }

        /** 参考温度 */
        public struct ir_file_reference
        {
            public byte enabled;                  /**< 是否启用基准，0x01表示启用，
                                                   *   其它表示不启用，默认值为0x00 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = NAME_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] name;                    /**< 参考源名称，空表示使用temp，
                                                   *   其他表示测量标识名称 */
            public int valueType;                  /**< 测量标识值类型，默认为最高温 */
            public float temp;                     /**< 参考温度值，取值范围0~5750默
                                                   *   认值为293.15 */
        }

        /** 显示信息 */
        public struct ir_file_view_info
        {
            public int type;                       /**< 显示模式，默认为红外图像模式 */
            public float max;                      /**< 融合阈值上限，除融合模式(Fusion)，其他模式此参数无效，默认值为313.15 */
            public float min;                      /**< 融合阈值下限，除融合模式(Fusion)，其他模式此参数无效，默认值为293.15 */
            public byte alpha;                     /**< 透明度，取值范围0~100，表示透明度百分
                                                    *   比，除可见光测温模式(T_DEF)，其他模式此参数
                                                    *   无效,默认值为0 */
            public ir_file_rectangle_f area;       /**< 热像可视区域，使用区域相对大小，除画中
                                                    *   画模式(Picture In Picture)，其他模式此
                                                    *   参数无效，最大区域相对大小为
                                                    *   (0.0,0.0,1.0,1.0)，默认值为
                                                    *   (0.25,0.25,0.5,0.5)，最小宽高限制为0.1 */
        }

        /** 可见光图像匹配信息 */
        public struct ir_file_mapping_info
        {
            public ir_file_size visualSize;        /**< 可见光图像的长宽，默认值为(800,600) */
            public ir_file_rectangle_f mappingArea;/**< 热像在可见光图像中的映射区域相对大小，
                                                    *   最大区域相对大小为(0.0,0.0,1.0,1.0)，
                                                    *   默认值为(0.25,0.25,0.5,0.5)，
                                                    *   最小宽高限制为0.1 */
        }

        /** 数码变倍信息 */
        public struct ir_file_zoom_info
        {
            public byte enabled;                   /**< 是否启用数码变倍，0x01表示启用，其它表
                                                    *   示不启用，默认值为0x00 */
            public ir_file_point_f location;       /**< 左上角相对坐标，最小相对坐标为(0.0,0.0)，
                                                    *   最大相对坐标为(1.0,1.0)，默认值为
                                                    *   (0.0,0.0) */
            public float ratio;                    /**< 数码变倍数，变焦倍数需大于1.0，默认值
                                                    *   为1 */
        }

        /** 全辐射数据帧 */
        public struct ir_file_frame
        {
            public ulong timeStamp;                /**< 相对于拍摄时间shotTime的毫秒数 */
            public uint length;                    /**< AD值数组大小 */
            public IntPtr buffer;                  /**< AD值数组首地址 */
        }

        /** 图片 */
        public struct ir_file_image
        {
            public uint offset;                    /**< 行字节对齐偏移 */
            public IntPtr data;                    /**< 数据区首地址 */
        }

        /** 预置调色板 */
        public struct ir_file_preset_palette
        {
            public byte refNo;                     /**< 调色板编号 */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = NAME_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] name;                    /**< 调色板名 */
        }

        /** 预置调色板列表 */
        public struct ir_file_preset_palettes
        {
            public int count;                      /**< 预置调色板数量 */
            public IntPtr presetPalettes;          /**< 预置调色板数组首地址 */
        }

        /** 温度点 */
        public struct ir_file_temp_point
        {
            public ir_file_point point;            /**< 点坐标 */
            public byte localParams;               /**< 是否启用局部参数 */
            public float reflTemp;                 /**< 反射温度 */
            public float distance;                 /**< 目标距离 */
            public float emissivity;               /**< 发射率 */
            public float temp;                     /**< 温度值 */
        }

        /** 温度区域-矩形 */
        public struct ir_file_temp_rect
        {
            public ir_file_rectangle rect;         /**< 矩形范围 */
            public byte localParams;               /**< 是否启用局部参数 */
            public float reflTemp;                 /**< 反射温度 */
            public float distance;                 /**< 目标距离 */
            public float emissivity;               /**< 发射率 */
            public int tempCount;                  /**< 区域温度数组大小 */
            public IntPtr temps;                   /**< 区域温度数组首地址，需要用户分配内存 */
        }

        /** 温度区域-折线,多边形 */
        public struct ir_file_temp_points
        {
            public int pointCount;                 /**点数量*/
            public IntPtr points;                  /**< 点列表 */
            public byte localParams;               /**< 是否启用局部参数 */
            public float reflTemp;                 /**< 反射温度 */
            public float distance;                 /**< 目标距离 */
            public float emissivity;               /**< 发射率 */
            public int tempCount;                  /**< 线温度数组大小 */
            public IntPtr temps;                   /**< 线温度数组首地址，需要用户分配内存 */
        }

        public struct ir_file_point_value
        {
            public ir_file_point point;          /**< 温度坐标 */
            public float temp;                   /**< 温度值 */
        }

        /** 获取SDK版本信息
        * @param ver    [out] SDK版本号，四字节形式，如0x02040100为2.4.1.0。若为空，则
        *                     不读取。
        * @param str    [out] SDK版本号，字符串形式。若为空，则不读取。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_sdk_version", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_sdk_version(ref uint ver, ref ir_file_buffer str);

        /** 检查文件是否为热像格式
        * @param file_name [in] 文件路径。
        * @param value     [out] 热像文件类型。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_peek_file_type", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_peek_file_type(string file_name, ref int value);

        /** 打开热像文件
        * 若文件不存在则失败。
        * @param file_name [in] 文件路径。
        * @param h         [out] IR_FILE句柄。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_open", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_open(string file_name, ref IntPtr h);

        /** 创建热像文件
        * 若文件已存在将被覆盖。
        * @param file_name [in] 文件路径。
        * @param file_type [in] 文件类型。可创建类型为：
        *                       IR_FILE_THERMAL_PICTURE，
        *                       IR_FILE_THERMAL_VIDEO，
        *                       IR_FILE_VISUAL_PICTURE。
        * @param h         [out] IR_FILE句柄。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_create", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_create(string file_name, int file_type, ref IntPtr h);

        /** 保存热像文件
        * @param  h [in] IR_FILE句柄。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_save", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_save(IntPtr h);

        /** 热像文件另存为
        * @param  h         [in] IR_FILE句柄。
        * @param file_name  [in] 文件路径。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_save_as", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_save_as(IntPtr h, string file_name);

        /** 保存2.01版本FJPG图片
        * @param  h [in] IR_FILE句柄。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_save_fjpg", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_save_fjpg(IntPtr h);

        /** 另存为2.01版本FJPG图片
        * @param  h         [in] IR_FILE句柄。
        * @param file_name  [in] 文件路径。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_save_as_fjpg", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_save_as_fjpg(IntPtr h, string file_name);

        /** 关闭热像文件 */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_close", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_close(IntPtr h);

        /** 获取热像文件版本号
        * @param h      [in]  IR_FILE句柄
        * @param ver    [out] 文件版本号
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_file_version", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_file_version(IntPtr h, ref ushort value);

        /** 读热像文件名
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 热像文件名。buffer需要用户分配内存，若buffer为空，不读取
        *                     备注信息，length返回所需内存大小。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_file_name", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_file_name(IntPtr h, ref ir_file_buffer value);

        /** 读热像文件类型
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 热像文件类型。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_file_type", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_file_type(IntPtr h, ref int value);

        /** 读设备信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 设备信息。logo需要用户分配内存，若logo为空，不 读取logo图
        *                     片，logoLength返回所需内存大小。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_camera_info", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_camera_info(IntPtr h, ref ir_file_camera_info value);

        /** 写设备信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 设备信息。若logo为空，不写入logo图片。若value为空，清空设
        *                    备信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_camera_info", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_camera_info(IntPtr h, ref ir_file_camera_info value);

        /** 读镜头信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 镜头信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_lens_info", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_lens_info(IntPtr h, ref ir_file_lens_info value);

        /** 写镜头信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 镜头信息。若value为空，清空镜头信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_lens_info", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_lens_info(IntPtr h, ref ir_file_lens_info value);

        /** 读镜头信息v2
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 镜头信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_lens_info_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_lens_info_v2(IntPtr h, ref ir_file_lens_info_v2 value);

        /** 写镜头信息v2
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 镜头信息。若value为空，清空镜头信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_lens_info_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_lens_info_v2(IntPtr h, ref ir_file_lens_info_v2 value);

        /** 读量程信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 量程信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_temp_range", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_temp_range(IntPtr h, ref ir_file_temp_range value);

        /** 写量程信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 量程信息。若value为空，清空量程信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_temp_range", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_temp_range(IntPtr h, ref ir_file_temp_range value);

        /** 读温度查找表
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 温度查找表。adArray需要用户分配内存（USHORT[count]），
        *                     且tempArray需要用户分配内存（float[count]）。若adArray或
        *                     tempArray为空，不读取该项，count返回温度查找表长度。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_lut", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_lut(IntPtr h, ref ir_file_lut value);

        /** 读温度查找表
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 温度查找表。adArray需要用户分配内存（USHORT[count]），
        *                     且tempArray需要用户分配内存（float[count]）。若adArray或
        *                     tempArray为空，不读取该项，count返回温度查找表长度。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_calculated_lut", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_calculated_lut(IntPtr h, ref ir_file_lut value);

        /** 写温度查找表
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 温度查找表。若adArray或tempArray为空，写入失败。若value为
        *                    空，清空温度查找表。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_lut", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_lut(IntPtr h, ref ir_file_lut value);

        /** 读调色板
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 调色板信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_palette", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_palette(IntPtr h, ref ir_file_palette value);

        /** 读调色板V2
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 调色板信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_palette_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_palette_v2(IntPtr h, ref ir_file_palette_v2 value);

        /** 写调色板
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 调色板信息。若value为空，清空调色板信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_palette", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_palette(IntPtr h, ref ir_file_palette value);

        /** 写调色板V2
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 调色板信息。若value为空，清空调色板信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_palette_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_palette_v2(IntPtr h, ref ir_file_palette_v2 value);

        /** 读图像分辨率
        * @param h      [in] IR_FILE句柄。
        * @param size   [out] 图像分辨率。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_thermal_size", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_thermal_size(IntPtr h, ref ir_file_size value);

        /** 写图像分辨率
        * @param h      [in] IR_FILE句柄。
        * @param size   [in] 图像分辨率。若value为空，清空图像分辨率。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_thermal_size", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_thermal_size(IntPtr h, ref ir_file_size value);

        /** 读测温参数
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 测温参数。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_thermal_params", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_thermal_params(IntPtr h, ref ir_file_thermal_params value);

        /** 写测温参数
        * @param h      [in] IR_FILE句柄。
        * @paramvalue   [in] 测温参数。若value为空，清空测温参数。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_thermal_params", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_thermal_params(IntPtr h, ref ir_file_thermal_params value);

        /** 读参考温度信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 参考温度信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_reference", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_reference(IntPtr h, ref ir_file_reference value);

        /** 读参考源温度
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 参考源温度。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_reference_temp", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_reference_temp(IntPtr h, ref float value);

        /** 写参考温度信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 参考温度信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_reference", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_reference(IntPtr h, ref ir_file_reference value);

        /** 读温宽信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [out]温宽信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_gain", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_gain(IntPtr h, ref ir_file_gain value);

        /** 写温宽信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [in|out] 温宽信息。若value为空，清空温宽信息。若为自动模式，写
        *                        温宽信息成功后min和max将会被更新。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_gain", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_gain(IntPtr h, ref ir_file_gain value);

        /** 读报警信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 报警信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_alarm", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_alarm(IntPtr h, ref ir_file_alarm value);

        /** 读报警信息 V2
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 报警信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_alarm_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_alarm_v2(IntPtr h, ref ir_file_alarm_v2 value);

        /** 写报警信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 报警信息。若value为空，清空报警信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_alarm", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_alarm(IntPtr h, ref ir_file_alarm value);

        /** 读地理位置信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 地理位置信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_geo_location", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_geo_location(IntPtr h, ref ir_file_geo_location value);

        /** 写地理位置信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 地理位置信息。若value为空，清空地理位置信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_geo_location", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_geo_location(IntPtr h, ref ir_file_geo_location value);

        /** 读文本备注信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 文本备注信息。buffer需要用户分配内存，若buffer为空，不读
        *                     取备注信息，length返回所需内存大小。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_note", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_note(IntPtr h, ref ir_file_buffer value);

        /** 写文本备注信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 文本备注信息。若buffer为空，写入失败。若value为空，清空备
        *                    注信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_note", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_note(IntPtr h, ref ir_file_buffer value);

        /** 读音频文件
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 音频文件。buffer需要用户分配内存，若buffer为空，不读取音
        *                     频文件，length返回所需内存大小。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_audio", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_audio(IntPtr h, ref ir_file_audio value);

        /** 写音频文件
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 音频文件。若buffer为空，写入失败。若value为空，清空音频文
        *                    件。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_audio", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_audio(IntPtr h, ref ir_file_audio value);

        /** 获取用户数据键列表
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 用户数据键列表。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_keys", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_keys(IntPtr h, ref ir_file_keys value);


        /** 读用户数据列表
        * @param h      [in] IR_FILE句柄。
        * @param value  [in|out] 用户数据项，key为输入项，其它为输出项。vBuffer需要用户
        *                        分配内存vLength字节，若为空，不读取值，vLength返回值长
        *                        度。
        * @retval IR_FILE_EC_INVALID_KEY 键不存在。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_user_data", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_user_data(IntPtr h, ref ir_file_kvPair value);

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
        * 5.耐材厚度监测参数：
        * Key：“THICKNESSPARAMS”
        * Value：采用如下JSON STRING格式存储
        * {
        *   "enabled": //是否启用耐材厚度监测,1表示启用,其他表示未启用
        *   "initInnerTemp": //初始流体温度,取值范围0~5750,默认值为1873.15
        *   "initOuterTemp": //初始外壳温度,取值范围0~5750,默认值为553.15
        *   "initAtmTemp": //初始环境温度,取值范围0~5750,默认值为303.15
        *   "correctionFactor": //修正系统,取值范围0.00-1.00,默认值0.25
        *   "alarmLimit": //报警阈值,取值范围0~100,默认值50
        * }
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 用户数据项。若vBuffer为空，删除该项用户数据。若value为空，
        *                    清空用户数据列表。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_user_data", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_user_data(IntPtr h, ref ir_file_kvPair value);

        /** 读拍摄时间
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 拍摄时间，采用UTC时间，从1970-01-01 00:00:00.000起的毫秒
        *                     数。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_shot_time", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_shot_time(IntPtr h, ref ulong value);

        /** 写拍摄时间
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 拍摄时间，采用UTC时间，从1970-01-01 00:00:00.000起的毫秒
        *                    数。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_shot_time", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_shot_time(IntPtr h, ulong value);

        /** 读修改时间
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 修改时间，采用UTC时间，从1970-01-01 00:00:00.000起的毫秒
        *                     数。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_modified_time", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_modified_time(IntPtr h, ref ulong value);

        /** 读数码变焦倍数
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 数码变焦倍数。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_zoom_info", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_zoom_info(IntPtr h, ref ir_file_zoom_info value);

        /** 写数码变焦倍数
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 数码变焦倍数。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_zoom_info", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_zoom_info(IntPtr h, ref ir_file_zoom_info value);

        /** 读显示信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 显示信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_view_info", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_view_info(IntPtr h, ref ir_file_view_info value);

        /** 写显示信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 显示信息。若value为空，清空显示信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_view_info", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_view_info(IntPtr h, ref ir_file_view_info value);

        /** 读热像区域
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 热像区域，即热像图在可见光图片中匹配的区域，默认值为
        *                     (0,0,384,288)。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_mapping_info", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_mapping_info(IntPtr h, ref ir_file_mapping_info value);

        /** 写热像区域
        * @param h      [in] IR_FILE句柄。
        * @param value  [in]  热像区域，即热像图在可见光图片中匹配的区域。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_mapping_info", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_mapping_info(IntPtr h, ref ir_file_mapping_info value);

        /** 获取测量标识名列表
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 测量标识名列表。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_marker_names", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_marker_names(IntPtr h, ref ir_file_marker_names value);

        /** 读测量标识
        * @param h      [in] IR_FILE句柄。
        * @param value  [in|out] 测量标识，name为输入项，其它为输出项。points需要用户分
        *                        配内存（ir_file_point[pointCount]），若为空，不读取点
        *                        列表，pointCount返回点列表长度。
        * @retval IR_FILE_EC_INVALID_KEY 该测量标识不存在。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_marker", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_marker(IntPtr h, ref ir_file_marker value);

        /** 写测量标识
        * @param h      [in] IR_FILE句柄。
        * @param value  [in|out] 测量标识，tempValues、tempPos、area为输出项，其它为输
        *                        入项。若测量标识存在则修改，不存在则添加。若points为空，
        *                        写入失败。写测量标识成功后输出项将会被更新。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_marker", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_marker(IntPtr h, ref ir_file_marker value);

        /** 删除测量标识，会同时清掉对应的reference
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 测量标识名。若value为空，清空测量标识列表。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_remove_marker", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_remove_marker(IntPtr h, byte[] value);

        /** 重命名测量标识
        * @param h      [in] IR_FILE句柄。
        * @param from   [in] 旧测量标识名。
        * @param to     [in] 新测量标识名。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_rename_marker", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_rename_marker(IntPtr h, byte[] from, byte[] to);

        /** 读可见光图片
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 可见光图片，jpg文件。buffer需要用户分配内存，若buffer为空，
        *                     不读取可见光图片，length返回图片大小。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_visual_image", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_visual_image(IntPtr h, ref ir_file_buffer value);

        /** 写可见光图片
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 可见光图片，jpg文件。若buffer为空，写入失败。若value为空，
        *                    清空可见光图片。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_visual_image", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_visual_image(IntPtr h, ref ir_file_buffer value);

        /** 读封面图
        * 封面图即热像文件中的普通jpg部分，使用看图工具查看图片将显示封面图。
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 封面图，jpg文件。buffer需要用户分配内存，若buffer为空，不
        *                     读取封面图，length返回图片大小。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_cover_image", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_cover_image(IntPtr h, ref ir_file_buffer value);

        /** 写封面图
        * 封面图即热像文件中的普通jpg部分，使用看图工具查看图片将显示封面图。
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 封面图，jpg文件，单边最小尺寸240像素。若buffer为空，写入失
        *                    败。若value为空，清空封面图。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_cover_image", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_cover_image(IntPtr h, ref ir_file_buffer value);

        /** 读图像显示参数信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 图像显示参数信息。buffer需要用户分配内存，若buffer为空，
        *                     不读取图像显示参数信息，length返回图像显示参数信息大小。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_vt_params", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_vt_params(IntPtr h, ref ir_file_buffer value);

        /** 写图像显示参数信息
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 图像显示参数信息。若buffer为空，写入失败。若value为空，清
        *                    空图像显示参数信息。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_vt_params", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_vt_params(IntPtr h, ref ir_file_buffer value);

        /** 获取温差标记名列表
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 温差标记名列表。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_delta_names", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_delta_names(IntPtr h, ref ir_file_delta_names value);

        /** 读温差标记
        * @param h      [in] IR_FILE句柄。
        * @param value  [in|out] 温差标记，name为输入项，其它为输出项。
        * @retval IR_FILE_EC_INVALID_KEY 该温差标记不存在。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_delta", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_delta(IntPtr h, ref ir_file_delta value);

        /** 写温差标记
        * @param h      [in] IR_FILE句柄。
        * @param value  [in|out] 温差标记，温差值为输出项，其它为输入项。若温差标记存在
        *                        则修改，不存在则添加。写温差标记成功后结构体内温差值将
        *                        会被更新。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_delta", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_delta(IntPtr h, ref ir_file_delta value);

        /** 删除温差标记
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 测量标识名。若value为空，清空温差标记列表。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_remove_delta", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_remove_delta(IntPtr h, byte[] value);

        /** 读色彩分布类型
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 色彩分布类型，默认为线性分布。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_color_dist", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_color_dist(IntPtr h, ref int value);

        /** 写色彩分布类型
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 色彩分布类型。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_color_dist", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_color_dist(IntPtr h, int value);

        /** 旋转图像
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 旋转方式。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_rotation", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_rotation(IntPtr h, int value);

        /** 镜像翻转图像
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 镜像方式。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_mirror", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_mirror(IntPtr h, int value);

        /** 读总帧数
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 总帧数。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_frame_count", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_frame_count(IntPtr h, ref uint value);

        /** 读帧频
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 帧频。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_frame_rate", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_frame_rate(IntPtr h, ref float value);

        /** 写帧频
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 帧频。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_frame_rate", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_frame_rate(IntPtr h, float value);

        /** 读帧内压缩方式
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 帧内压缩方式。0x00表示未使用压缩算法，默认值为0x00。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_compress_type", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_compress_type(IntPtr h, ref byte value);

        /** 写帧内压缩方式
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 帧内压缩方式。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_compress_type", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_compress_type(IntPtr h, byte value);

        /** 获取全辐射数据帧
        * @param h      [in] IR_FILE句柄。
        * @param index  [in] 帧索引。
        * @param value  [in] 全辐射数据帧。buffer需要用户分配内存，若buffer为空，不读
        *                    取全辐射数据帧，length返回buffer数组所需大小。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_frame", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_frame(IntPtr h, uint index, ref ir_file_frame value);

        /** 追加全辐射数据帧
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 全辐射数据帧。若buffer为空，写入失败。若value为空，清空帧
        *                    数据区。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_append_frame", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_append_frame(IntPtr h, ref ir_file_frame value);

        /** 读温度点
        * @param h      [in] IR_FILE句柄。
        * @param value  [in|out] 温度点，temp为输出项，其它为输入项。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_temp_spot", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_temp_spot(IntPtr h, ref ir_file_temp_point value);

        /** 读矩形区域温度
        * @param h      [in] IR_FILE句柄。
        * @param value  [in|out] 温度区域，tempCount、temps为输出项，其它为输入项
        *                        temps为NULL时获取tempCount。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_temp_rect", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_temp_rect(IntPtr h, ref ir_file_temp_rect value);

        /** 读椭圆区域温度
        * @param h      [in] IR_FILE句柄。
        * @param value  [in|out] 温度区域，tempCount、temps为输出项，其它为输入项。
        *                        temps为NULL时获取tempCount。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_temp_ellipse", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_temp_ellipse(IntPtr h, ref ir_file_temp_rect value);

        /** 读折线温度
        * @param h      [in] IR_FILE句柄。
        * @param value  [in|out] 温度线，tempCount、temps为输出项，其它为输入项
        *                        temps为NULL时获取tempCount。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_temp_polyline", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_temp_polyline(IntPtr h, ref ir_file_temp_points value);

        /** 读多边形温度
        * @param h      [in] IR_FILE句柄。
        * @param value  [in|out] 温度区域，tempCount、temps为输出项，其它为输入项
                                 temps为NULL时获取tempCount。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_temp_polygon", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_temp_polygon(IntPtr h, ref ir_file_temp_points value);

        /** 读调色板色带
          * @param h      [in] IR_FILE句柄。
          * @param value  [out] 调色板色带，256行1列BGR。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_palette_image", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_palette_image(IntPtr h, ref ir_file_image value);

        /** 读预置调色板
        * @param value  [out] 预置调色板。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_preset_palettes", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_preset_palettes(ref ir_file_preset_palettes value);

        /** 读预置调色板色带
        * @param refNo  [in] 调色板编号。
        * @param value  [out] 预置调色板色带，256行1列BGR。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_preset_palette_image", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_preset_palette_image(byte refNo, ref ir_file_image value);

        /** 读热像图
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 处理后的热像图，图像高度*图像宽度大小的BGR。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_thermal_image", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_thermal_image(IntPtr h, ref ir_file_image value);

        /** 获取当前全辐射数据帧索引
        * @param h      [in] IR_FILE句柄。
        * @param value  [out] 帧索引，取值范围0到总帧数-1。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_frame_index", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_frame_index(IntPtr h, ref uint value);

        /** 设置当前全辐射数据帧索引
        * @param h      [in] IR_FILE句柄。
        * @param value  [in] 帧索引，取值范围0到总帧数-1。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_set_frame_index", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_set_frame_index(IntPtr h, uint value);

        /** 热像文件当前帧另存为
        * @param h          [in] IR_FILE句柄。
        * @param file_name  [in] 文件路径。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_save_frame_as", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_save_frame_as(IntPtr h, string file_name, ref ir_file_buffer value);

        /** 创建视频裁剪句柄
        * @param h              [in] IR_FILE句柄。
        * @param file_name      [in] 文件路径。
        * @param h_new          [out] 视频裁剪句柄。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_save_as_stream", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_save_as_stream(IntPtr h, string file_name, ref IntPtr h_new);

        /** 读测量标识
        * @param h      [in] IR_FILE句柄。
        * @param index	[in] 帧索引。
        * @param value  [in|out] 测量标识，name为输入项，其它为输出项。points需要用户分
        *                        配内存（ir_file_point[pointCount]），若为空，不读取点
        *                        列表，pointCount返回点列表长度。
        * @retval IR_FILE_EC_INVALID_KEY 该测量标识不存在。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_index_marker", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_index_marker(IntPtr h, uint index, ref ir_file_marker value);

        /** 读测量标识
        * @param h      [in] IR_FILE句柄。
        * @param index	[in] 帧索引。
        * @param value  [in|out] 温差标记，name为输入项，其它为输出项。
        * @retval IR_FILE_EC_INVALID_KEY 该测量标识不存在。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_index_delta", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_index_delta(IntPtr h, uint index, ref ir_file_delta value);

        /** 读拍摄时间
        * @param h      [in] IR_FILE句柄。
        * @param index	[in] 帧索引。
        * @param value  [out] 拍摄时间，采用UTC时间，从1970-01-01 00:00:00.000起的毫秒
        *                     数。
        */
        [DllImport("IRtekFileSDK.dll", EntryPoint = "ir_file_get_index_shot_time", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ir_file_get_index_shot_time(IntPtr h, uint index, ref ulong value);
    }
}
