using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Adf.Cs
{
    /// <summary>
    /// Cs 信息
    /// </summary>
    [ProtoContract]
    public class CsInfo
    {
        ///// <summary>
        ///// 名称
        ///// </summary>
        //[ProtoMember(1)]
        //public string Name
        //{
        //    get;
        //    set;
        //}

        /// <summary>
        /// 时间
        /// </summary>
        [ProtoMember(2)]
        public long TimeTicks
        {
            get;
            set;
        }

        /// <summary>
        /// 类型
        /// </summary>
        [ProtoMember(3)]
        public CsType CsType
        {
            get;
            set;
        }

        /// <summary>
        /// 版本号
        /// </summary>
        [ProtoMember(4)]
        public string Version
        {
            get;
            set;
        }
    }
}
